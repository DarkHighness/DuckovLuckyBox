using System.Collections.Generic;
using Duckov.Economy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Duckov.Economy;
using SodaCraft.Localizations;
using Cysharp.Threading.Tasks;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.Patches.StockShopActions;

namespace DuckovLuckyBox.Core
{
  public class StockShopViewUI : IComponent
  {
    private static StockShopViewUI? _instance;
    public static StockShopViewUI Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new StockShopViewUI();
        }
        return _instance;
      }
    }

    private bool isInitialized = false;
    private bool isOpen = false;
    private StockShop? _currentStockShop = null;
    private StockShopView? _currentStockShopView = null;
    private StockShopActionManager? _actionManager = null;
    private Dictionary<string, TextMeshProUGUI> _actionTexts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, Button> _actionButtons = new Dictionary<string, Button>();
    private RectTransform? _actionsContainer;
    private bool _priceChangeSubscribed = false;
    private const float ActionsContainerFallbackWidth = 320f;
    private const float ActionsContainerHeight = 240f;
    private const float ActionsLayoutSpacing = 24f;
    private const int ActionsLayoutPaddingHorizontal = 0;
    private const int ActionsLayoutPaddingTop = 16;
    private const int ActionsLayoutPaddingBottom = 16;
    private const float ActionLabelPreferredHeight = 40f;
    private const float ActionLabelMinWidth = 140f;
    private const float ActionLabelExtraWidth = 24f;
    private const float ActionLabelMinFontSize = 18f;
    private const float ActionLabelFontScale = 0.9f;
    private static readonly Color ActionButtonNormalColor = new Color(1f, 1f, 1f, 0.8f);
    private static readonly Color ActionButtonHighlightedColor = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color ActionButtonPressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color ActionButtonDisabledColor = new Color(1f, 1f, 1f, 0.35f);

    public void Setup(StockShopView view, TextMeshProUGUI merchantNameText, StockShop target)
    {
      if (!isInitialized)
      {
        Log.Debug("Initializing StockShopViewUI");
        isInitialized = true;
        _currentStockShop = target;
        _currentStockShopView = view;
        InitializeActionManager();
        EnsureUIElements(merchantNameText);
        SubscribeToPriceChanges();
        UpdateButtonTexts();
      }

      _currentStockShop = target;
      _currentStockShopView = view;
      isOpen = true; // Setting up the view means it's open
    }

    public void Toggle()
    {
      if (!isInitialized) return;
      if (isOpen)
      {
        Close();
      }
      else
      {
        Open();
      }
    }

    public void Open()
    {
      if (!isInitialized) return;

      Log.Debug("Opening StockShopViewUI");
      isOpen = true;
      // Show UI elements
      UpdateUIElementsVisibility();
      UpdateButtonTexts();
    }

    public void Close()
    {
      if (!isInitialized) return;

      Log.Debug("Closing StockShopViewUI");
      isOpen = false;
      // Hide UI elements
      UpdateUIElementsVisibility();
    }

    public void Destroy()
    {
      if (!isInitialized) return;
      Log.Debug("Destroying StockShopViewUI");
      // Clean up UI elements
      CleanupUIElements();
      _instance = null;
      isInitialized = false;
    }

    private void InitializeActionManager()
    {
      if (_actionManager == null)
      {
        _actionManager = new StockShopActionManager();
        Log.Debug("Stock shop action manager initialized");
      }
    }

    private void CleanupUIElements()
    {
      // Destroy action buttons and texts
      foreach (var text in _actionTexts.Values)
      {
        if (text != null)
        {
          UnityEngine.Object.Destroy(text.gameObject);
        }
      }
      _actionTexts.Clear();

      // Clear buttons dictionary (buttons are components on text game objects, so destroyed with them)
      _actionButtons.Clear();

      // Destroy actions container
      if (_actionsContainer != null)
      {
        UnityEngine.Object.Destroy(_actionsContainer.gameObject);
        _actionsContainer = null;
      }
    }

    private void EnsureUIElements(TextMeshProUGUI merchantNameText)
    {
      // Clean up existing UI elements before re-creating
      // CleanupUIElements();

      EnsureActionContainer(merchantNameText);
      CreateActionButtons(merchantNameText);

      // Hide if disabled
      if (!SettingManager.Instance.EnableStockShopActions.GetAsBool())
      {
        _actionsContainer?.gameObject.SetActive(false);
      }
    }

    private void EnsureActionContainer(TextMeshProUGUI merchantNameText)
    {
      if (_actionsContainer == null)
      {
        var parent = merchantNameText.transform.parent as RectTransform;
        if (parent == null) return;

        var grandParent = parent.parent as RectTransform;
        var greatGrandParent = grandParent?.parent as RectTransform;
        var targetParent = greatGrandParent ?? grandParent ?? parent;

        _actionsContainer = new GameObject("ExtraActionsContainer", typeof(RectTransform)).GetComponent<RectTransform>();
        _actionsContainer.SetParent(targetParent, false);
        _actionsContainer.anchorMin = new Vector2(0.5f, 0f);
        _actionsContainer.anchorMax = new Vector2(0.5f, 0f);
        _actionsContainer.pivot = new Vector2(0.5f, 0f);
        _actionsContainer.anchoredPosition = new Vector2(0f, 20f);

        float width = merchantNameText.rectTransform.rect.width;
        if (width <= 0f) width = ActionsContainerFallbackWidth;
        _actionsContainer.sizeDelta = new Vector2(width, ActionsContainerHeight);
        _actionsContainer.SetAsLastSibling();

        var layout = _actionsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = ActionsLayoutSpacing;
        layout.padding = CreateActionsPadding();
      }
    }

    private void CreateActionButtons(TextMeshProUGUI merchantNameText)
    {
      if (_actionManager == null) return;

      foreach (var action in _actionManager.GetAllActions())
      {
        var actionText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
        ConfigureActionLabel(actionText, action.GetLocalizationKey().ToPlainText());

        var button = actionText.gameObject.AddComponent<Button>();
        ConfigureActionButton(button, actionText);

        string actionName = action.GetType().Name;
        _actionTexts[actionName] = actionText;
        _actionButtons[actionName] = button;

        // Bind click event
        button.onClick.AddListener(() => ExecuteActionAsync(actionName).Forget());
      }

      Log.Debug($"Created {_actionTexts.Count} action buttons");
    }

    private async UniTaskVoid ExecuteActionAsync(string actionName)
    {
      if (_actionManager == null) return;
      if (_currentStockShopView != null)
      {
        await _actionManager.ExecuteAsync(actionName, _currentStockShopView);
      }
      else
      {
        Log.Warning("Cannot execute action: current StockShopView is null");
      }

      UpdateButtonTexts();
    }


    private void UpdateButtonTexts()
    {
      if (_actionTexts.Count == 0) return;

      long refreshPrice = SettingManager.Instance.RefreshStockPrice.Value as long? ?? DefaultSettings.RefreshStockPrice;
      long storePickPrice = SettingManager.Instance.StorePickPrice.Value as long? ?? DefaultSettings.StorePickPrice;
      long streetPickPrice = SettingManager.Instance.StreetPickPrice.Value as long? ?? DefaultSettings.StreetPickPrice;

      var freeText = Localizations.I18n.FreeKey.ToPlainText();

      if (_actionTexts.TryGetValue(nameof(RefreshStockAction), out var refreshText))
      {
        var baseText = Localizations.I18n.RefreshStockKey.ToPlainText();
        refreshText.text = refreshPrice > 0 ? $"{baseText} (${refreshPrice})" : $"{baseText} ({freeText})";
      }

      if (_actionTexts.TryGetValue(nameof(StorePickAction), out var storePickText))
      {
        var baseText = Localizations.I18n.StorePickKey.ToPlainText();
        storePickText.text = storePickPrice > 0 ? $"{baseText} (${storePickPrice})" : $"{baseText} ({freeText})";
      }

      if (_actionTexts.TryGetValue(nameof(StreetPickAction), out var streetPickText))
      {
        var baseText = Localizations.I18n.StreetPickKey.ToPlainText();
        streetPickText.text = streetPickPrice > 0 ? $"{baseText} (${streetPickPrice})" : $"{baseText} ({freeText})";
      }

      if (_actionTexts.TryGetValue(nameof(RecycleAction), out var recycleText))
      {
        // Display "Close Recycle" when the recycle view is open, otherwise display "Open Recycle".
        // This decision is based solely on the IsOpen flag and does not depend on HasItems.
        var text = RecycleSessionUI.Instance.IsOpen
            ? Localizations.I18n.CloseKey.ToPlainText() + " " + Localizations.I18n.RecycleKey.ToPlainText()
            : Localizations.I18n.OpenKey.ToPlainText() + " " + Localizations.I18n.RecycleKey.ToPlainText();
        recycleText.text = text;
      }
    }

    private void SubscribeToPriceChanges()
    {
      if (_priceChangeSubscribed) return;

      var settings = SettingManager.Instance;
      settings.RefreshStockPrice.OnValueChanged += _ => UpdateButtonTexts();
      settings.StorePickPrice.OnValueChanged += _ => UpdateButtonTexts();
      settings.StreetPickPrice.OnValueChanged += _ => UpdateButtonTexts();
      settings.EnableStockShopActions.OnValueChanged += OnEnableStockShopActionsChanged;

      _priceChangeSubscribed = true;
      Log.Debug("Subscribed to price change events");
    }

    private void OnEnableStockShopActionsChanged(object value)
    {
      bool enabled = value is bool b && b;
      _actionsContainer?.gameObject.SetActive(enabled);
    }

    private void ConfigureActionLabel(TextMeshProUGUI label, string text)
    {
      label.text = text;
      label.margin = Vector4.zero;
      label.alignment = TextAlignmentOptions.Center;
      label.enableAutoSizing = false;
      label.fontSize = Mathf.Max(ActionLabelMinFontSize, label.fontSize * ActionLabelFontScale);
      label.raycastTarget = true;

      var rectTransform = label.rectTransform;
      rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
      rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
      rectTransform.pivot = new Vector2(0.5f, 0.5f);
      rectTransform.sizeDelta = new Vector2(0f, ActionLabelPreferredHeight);

      var layoutElement = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
      layoutElement.preferredHeight = ActionLabelPreferredHeight;
      layoutElement.preferredWidth = Mathf.Max(ActionLabelMinWidth, label.preferredWidth + ActionLabelExtraWidth);
      layoutElement.flexibleWidth = 0f;
    }

    private void ConfigureActionButton(Button button, TextMeshProUGUI label)
    {
      button.transition = Selectable.Transition.ColorTint;
      button.targetGraphic = label;

      var colors = button.colors;
      colors.normalColor = ActionButtonNormalColor;
      colors.highlightedColor = ActionButtonHighlightedColor;
      colors.pressedColor = ActionButtonPressedColor;
      colors.selectedColor = ActionButtonHighlightedColor;
      colors.disabledColor = ActionButtonDisabledColor;
      button.colors = colors;
    }

    private RectOffset CreateActionsPadding()
    {
      return new RectOffset(
        ActionsLayoutPaddingHorizontal,
        ActionsLayoutPaddingHorizontal,
        ActionsLayoutPaddingTop,
        ActionsLayoutPaddingBottom);
    }

    private void UpdateUIElementsVisibility()
    {
      if (_actionsContainer == null) return;

      if (!isOpen)
      {
        _actionsContainer.gameObject.SetActive(false);
        return;
      }

      // Show container if enabled in settings
      bool enabled = SettingManager.Instance.EnableStockShopActions.GetAsBool();
      _actionsContainer.gameObject.SetActive(enabled);
    }
  }
}