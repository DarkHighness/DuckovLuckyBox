using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using Duckov;
using Duckov.Economy.UI;
using Duckov.UI;
using Duckov.UI.Animations;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckovLuckyBox.Patches.StockShopActions
{
  /// <summary>
  /// Implements the stock shop "trash bin" action using native inventory widgets.
  /// </summary>
  public sealed class CycleBinAction : IStockShopAction
  {
    private const int ContractSize = 5;
    private const float ContractVerticalOffset = -120f;
    public static bool IsOpen { get; private set; } = false;

    private static readonly Dictionary<StockShopView, ContractSession> Sessions = new Dictionary<StockShopView, ContractSession>();

    public string GetLocalizationKey() => Localizations.I18n.TrashBinKey;

    public async UniTask ExecuteAsync(StockShopView stockShopView)
    {
      if (stockShopView == null)
      {
        Log.Warning("CycleBinAction executed without a StockShopView instance.");
        return;
      }

      var session = GetOrCreateSession(stockShopView);
      session.Toggle();
      await UniTask.CompletedTask;
    }

    private static void SetOpenState(bool isOpen)
    {
      if (!SettingManager.Instance.EnableStockShopActions.GetAsBool()) {
        IsOpen = false;
        return;
      }

      IsOpen = isOpen;
    }

    internal static void OnViewOpened(StockShopView view)
    {
      if (view == null)
      {
        return;
      }

      if (Sessions.TryGetValue(view, out var session))
      {
        session.OnViewOpened();
      }

      SetOpenState(true);
    }

    internal static void OnViewClosed(StockShopView view)
    {
      if (view == null)
      {
        return;
      }

      if (Sessions.TryGetValue(view, out var session))
      {
        session.OnViewClosed();
      }

      SetOpenState(false);
    }

    private static ContractSession GetOrCreateSession(StockShopView view)
    {
      if (!Sessions.TryGetValue(view, out var session))
      {
        session = new ContractSession(view);
        Sessions[view] = session;
      }

      return session;
    }

    private sealed class ContractSession
    {
      private static readonly string[] RewardCategories =
      {
        "Weapon",
        "MeleeWeapon",
        "Helmat",
        "Medic",
        "FaceMask",
        "Armor",
        "Luxury",
        "Injector",
        "Electric",
        "Totem",
        "Tool"
      };

      private readonly StockShopView _view;
      private readonly TextMeshProUGUI? _textTemplate;
      private readonly Dictionary<Item, OriginItemContext> _itemOrigins = new Dictionary<Item, OriginItemContext>();
      private readonly List<InventoryDisplay> _monitoredDisplays = new List<InventoryDisplay>();
      private readonly Dictionary<InventoryDisplay, Delegate?> _suppressedDoubleClickHandlers = new Dictionary<InventoryDisplay, Delegate?>();

      private static readonly FieldInfo? DoubleClickEventField = AccessTools.Field(typeof(InventoryDisplay), "onDisplayDoubleClicked");

      private FadeGroup? _detailsFadeGroup;
      private RectTransform? _contractRoot;
      private InventoryDisplay? _contractDisplay;
      private Inventory? _contractInventory;
      private const float ButtonGroupHeight = 72f;
      private const float ButtonGroupSpacing = 16f;
      private const float ActionLabelPreferredHeight = 40f;
      private const float ActionLabelMinWidth = 140f;
      private const float ActionLabelExtraWidth = 24f;
      private const float ActionLabelMinFontSize = 18f;
      private const float ActionLabelFontScale = 0.9f;
      private static readonly Color ActionButtonNormalColor = new Color(1f, 1f, 1f, 0.8f);
      private static readonly Color ActionButtonHighlightedColor = new Color(1f, 1f, 1f, 0.95f);
      private static readonly Color ActionButtonPressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
      private static readonly Color ActionButtonDisabledColor = new Color(1f, 1f, 1f, 0.35f);
      private RectTransform? _actionButtonContainer;
      private Button? _confirmButton;
      private TextMeshProUGUI? _confirmButtonLabel;
      private Button? _clearButton;
      private TextMeshProUGUI? _clearButtonLabel;

      private bool _initialized;
      private bool _visible;
      private bool _busy;
      private bool _transferring;
      private ItemValueLevel? _targetQuality;
      private ItemValueLevel? _rewardQuality;

      private sealed class OriginItemContext
      {
        public OriginItemContext(Inventory inventory, Item? stackSource)
        {
          Inventory = inventory;
          StackSource = stackSource;
        }

        public Inventory Inventory { get; }
        public Item? StackSource { get; }
      }

      public ContractSession(StockShopView view)
      {
        _view = view;
        _textTemplate = GetTextTemplate(view);
      }

      private static TextMeshProUGUI? GetTextTemplate(StockShopView view)
      {
        if (view == null)
        {
          return null;
        }

        var merchantNameTextField = AccessTools.Field(typeof(StockShopView), "merchantNameText");
        return merchantNameTextField?.GetValue(view) as TextMeshProUGUI;
      }

      public void Toggle()
      {
        if (_visible)
        {
          Hide(false);
        }
        else
        {
          Show();
        }
      }

      public void OnViewOpened()
      {
        Hide(true);
      }

      public void OnViewClosed() => Hide(true);

      private void Show()
      {
        EnsureInitialized();
        if (_contractRoot == null || _contractDisplay == null || _contractInventory == null)
        {
          Log.Warning("Cycle bin UI failed to initialize correctly.");
          return;
        }

        _visible = true;
        _contractRoot?.SetAsLastSibling();
        _contractRoot?.gameObject.SetActive(true);
        _contractDisplay.gameObject.SetActive(true);
        _contractDisplay.Setup(_contractInventory, funcCanOperate: _ => true, movable: false);
        _contractDisplay.onDisplayDoubleClicked -= OnContractDoubleClicked;
        _contractDisplay.onDisplayDoubleClicked += OnContractDoubleClicked;

        SubscribeMonitoredDisplays();

        try
        {
          _detailsFadeGroup?.Hide();
        }
        catch (Exception ex)
        {
          Log.Debug($"Failed to hide details fade group: {ex.Message}");
        }

        UpdateActionButtonsState();
        SetOpenState(true);
      }

      private void Hide(bool force)
      {
        if (!_initialized)
        {
          return;
        }

        if (!_visible && !force)
        {
          return;
        }

        _visible = false;
        if (_contractDisplay != null)
        {
          _contractDisplay.onDisplayDoubleClicked -= OnContractDoubleClicked;
        }

        UnsubscribeMonitoredDisplays();

        ReturnAllItems();

        _contractRoot?.gameObject.SetActive(false);
        SetOpenState(false);
      }

      private void EnsureInitialized()
      {
        if (_initialized)
        {
          return;
        }

        CaptureViewReferences();

        if (_detailsFadeGroup == null)
        {
          Log.Warning("Cycle bin could not locate details fade group in StockShopView.");
          return;
        }

        var templateDisplay = GetInventoryDisplay("playerInventoryDisplay");
        if (templateDisplay == null)
        {
          Log.Warning("Cycle bin could not locate a template InventoryDisplay.");
          return;
        }

        BuildContractRoot();
        if (_contractRoot == null)
        {
          return;
        }

        CreateContractInventory();
        if (_contractInventory == null)
        {
          return;
        }

        CreateContractDisplay(templateDisplay);

        CreateActionButtons();

        _initialized = _contractRoot != null && _contractDisplay != null && _contractInventory != null;
      }

      private void CaptureViewReferences()
      {
        var detailsField = AccessTools.Field(typeof(StockShopView), "detailsFadeGroup");
        _detailsFadeGroup = detailsField?.GetValue(_view) as FadeGroup;

        _monitoredDisplays.Clear();
        AddDisplay("playerInventoryDisplay");
        AddDisplay("petInventoryDisplay");
        AddDisplay("playerStorageDisplay");
      }

      private void AddDisplay(string fieldName)
      {
        var display = GetInventoryDisplay(fieldName);
        if (display != null && !_monitoredDisplays.Contains(display))
        {
          _monitoredDisplays.Add(display);
        }
      }

      private InventoryDisplay? GetInventoryDisplay(string fieldName)
      {
        var field = AccessTools.Field(typeof(StockShopView), fieldName);
        return field?.GetValue(_view) as InventoryDisplay;
      }

      private void BuildContractRoot()
      {
        if (_detailsFadeGroup == null)
        {
          return;
        }

        var detailsRect = _detailsFadeGroup.transform as RectTransform;
        if (detailsRect == null)
        {
          return;
        }

        // Use similar hierarchy logic as PatchStockShopView
        var merchantNameText = GetTextTemplate(_view);
        RectTransform? targetParent = null;

        if (merchantNameText != null)
        {
          // Follow PatchStockShopView's hierarchy logic
          var parent = merchantNameText.transform.parent as RectTransform;
          var grandParent = parent?.parent as RectTransform;
          var greatGrandParent = grandParent?.parent as RectTransform;
          targetParent = greatGrandParent ?? grandParent ?? parent ?? detailsRect.parent as RectTransform;
        }
        else
        {
          // Fallback to original logic
          targetParent = detailsRect.parent as RectTransform;
        }

        if (targetParent == null)
        {
          var fallbackCanvas = _view.GetComponentInParent<Canvas>();
          targetParent = fallbackCanvas != null ? fallbackCanvas.transform as RectTransform : _view.transform as RectTransform;
        }

        _contractRoot = new GameObject("CycleBinContractRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        if (_contractRoot == null)
        {
          return;
        }

        int viewLayer = _view.gameObject.layer;
        _contractRoot.gameObject.layer = viewLayer;

        Vector2 targetSize = detailsRect.rect.size;
        if (targetSize == Vector2.zero)
        {
          targetSize = detailsRect.sizeDelta;
        }

        _contractRoot.SetParent(targetParent, false);
        _contractRoot.anchorMin = new Vector2(0.5f, 1f);
        _contractRoot.anchorMax = new Vector2(0.5f, 1f);
        _contractRoot.pivot = new Vector2(0.5f, 1f);
        _contractRoot.sizeDelta = new Vector2(targetSize.x, 0f);

        var anchoredPosition = detailsRect.anchoredPosition;
        anchoredPosition.y += ContractVerticalOffset;
        _contractRoot.anchoredPosition = anchoredPosition;

        _contractRoot.SetAsLastSibling();

        // Add VerticalLayoutGroup to _contractRoot
        var verticalLayout = _contractRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = false;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.spacing = ButtonGroupSpacing;
        verticalLayout.padding = new RectOffset(0, 0, 0, 0);

        // Add ContentSizeFitter
        var fitter = _contractRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var parentCanvas = targetParent?.GetComponentInParent<Canvas>();
        ConfigureContractRootCanvas(parentCanvas);

        _contractRoot.gameObject.SetActive(false);
      }

      private void CreateContractInventory()
      {
        if (_contractRoot == null)
        {
          return;
        }

        var inventoryObject = new GameObject("CycleBinContractInventory");
        inventoryObject.transform.SetParent(_view.transform, false);

        _contractInventory = inventoryObject.AddComponent<Inventory>();
        if (_contractInventory == null)
        {
          return;
        }
        _contractInventory.DisplayNameKey = Localizations.I18n.TrashBinKey;
        _contractInventory.SetCapacity(ContractSize);
      }

      private void CreateContractDisplay(InventoryDisplay template)
      {
        if (_contractRoot == null || _contractInventory == null)
        {
          return;
        }

        var clone = UnityEngine.Object.Instantiate(template.gameObject, _contractRoot);
        clone.name = "CycleBinContractDisplay";

        _contractDisplay = clone.GetComponent<InventoryDisplay>();
        if (_contractDisplay == null)
        {
          UnityEngine.Object.Destroy(clone);
          Log.Warning("Cycle bin could not clone InventoryDisplay properly.");
          return;
        }

        PrepareClonedInventoryDisplay(_contractDisplay);

        var rect = clone.transform as RectTransform;
        if (rect != null)
        {
          rect.anchorMin = new Vector2(0.5f, 0.5f);
          rect.anchorMax = new Vector2(0.5f, 0.5f);
          rect.pivot = new Vector2(0.5f, 0.5f);
          rect.sizeDelta = Vector2.zero; // Let layout system determine size
          rect.anchoredPosition = Vector2.zero;
        }

        var displayLayout = clone.GetComponent<LayoutElement>() ?? clone.AddComponent<LayoutElement>();
        displayLayout.preferredWidth = -1f; // Use preferred size
        displayLayout.preferredHeight = -1f; // Use preferred size
        displayLayout.flexibleWidth = 1f; // Allow flexible sizing
        displayLayout.flexibleHeight = 1f; // Allow flexible sizing

        DisableInventoryDisplayExtras(_contractDisplay);
        _contractDisplay.Setup(_contractInventory, funcCanOperate: _ => true, movable: false);
        _contractDisplay.onDisplayDoubleClicked += OnContractDoubleClicked;
        _contractDisplay.gameObject.SetActive(true);

        if (_contractRoot != null)
        {
          LayoutRebuilder.ForceRebuildLayoutImmediate(_contractRoot);
        }
      }

      private static void PrepareClonedInventoryDisplay(InventoryDisplay display)
      {
        if (display == null)
        {
          return;
        }

        var displayType = typeof(InventoryDisplay);

        var entryPrefabField = AccessTools.Field(displayType, "entryPrefab");
        var entriesParentField = AccessTools.Field(displayType, "entriesParent");
        var entryPoolField = AccessTools.Field(displayType, "_entryPool");
        var entriesField = AccessTools.Field(displayType, "entries");
        var cachedIndexesField = AccessTools.Field(displayType, "cachedIndexesToDisplay");

        var entryPrefab = entryPrefabField?.GetValue(display) as InventoryEntry;
        Transform? entriesParent = entriesParentField?.GetValue(display) as Transform;
        if (entriesParent != null)
        {
          for (int i = entriesParent.childCount - 1; i >= 0; i--)
          {
            var child = entriesParent.GetChild(i);
            if (entryPrefab != null && child == entryPrefab.transform)
            {
              continue;
            }

            UnityEngine.Object.Destroy(child.gameObject);
          }

          if (entryPrefab != null)
          {
            entryPrefab.gameObject.SetActive(false);
            entryPrefab.transform.SetParent(entriesParent, false);
            entryPrefab.transform.SetAsFirstSibling();
          }
        }

        entryPoolField?.SetValue(display, null);
        entriesField?.SetValue(display, new List<InventoryEntry>());
        cachedIndexesField?.SetValue(display, new List<int>());

        AccessTools.Field(displayType, "cachedSelectedPage")?.SetValue(display, 0);
        AccessTools.Field(displayType, "cachedMaxPage")?.SetValue(display, 1);
        AccessTools.Field(displayType, "cachedCapacity")?.SetValue(display, -1);
        AccessTools.Field(displayType, "filter")?.SetValue(display, null);

        AccessTools.Field(displayType, "usePages")?.SetValue(display, false);
        AccessTools.Field(displayType, "itemsEachPage")?.SetValue(display, ContractSize);
        AccessTools.Field(displayType, "editable")?.SetValue(display, false);
        AccessTools.Field(displayType, "showOperationButtons")?.SetValue(display, false);
        AccessTools.Field(displayType, "showSortButton")?.SetValue(display, false);

        var grid = AccessTools.Field(displayType, "contentLayout")?.GetValue(display) as GridLayoutGroup;
        if (grid != null)
        {
          grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
          grid.constraintCount = ContractSize;
        }

        var placeholder = AccessTools.Field(displayType, "placeHolder")?.GetValue(display) as GameObject;
        placeholder?.SetActive(false);
      }

      private static void DisableInventoryDisplayExtras(InventoryDisplay display)
      {
        try
        {
          var sortButtonField = AccessTools.Field(typeof(InventoryDisplay), "sortButton");
          var sortButton = sortButtonField?.GetValue(display) as Button;
          sortButton?.gameObject.SetActive(false);

          var showButtonsField = AccessTools.Field(typeof(InventoryDisplay), "showOperationButtons");
          showButtonsField?.SetValue(display, false);

          var showSortField = AccessTools.Field(typeof(InventoryDisplay), "showSortButton");
          showSortField?.SetValue(display, false);
        }
        catch (Exception ex)
        {
          Log.Debug($"Failed to adjust InventoryDisplay extras: {ex.Message}");
        }
      }

      private void ConfigureContractRootCanvas(Canvas? parentCanvas)
      {
        if (_contractRoot == null)
        {
          return;
        }

        // Remove any existing Canvas and GraphicRaycaster to use parent's
        var existingCanvas = _contractRoot.GetComponent<Canvas>();
        if (existingCanvas != null)
        {
          UnityEngine.Object.Destroy(existingCanvas);
        }

        var existingRaycaster = _contractRoot.GetComponent<GraphicRaycaster>();
        if (existingRaycaster != null)
        {
          UnityEngine.Object.Destroy(existingRaycaster);
        }

        var group = _contractRoot.GetComponent<CanvasGroup>() ?? _contractRoot.gameObject.AddComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;
        group.ignoreParentGroups = false; // Use parent's groups
        group.alpha = 1f;
      }

      private void CreateActionButtons()
      {
        if (_contractRoot == null)
        {
          return;
        }

        _actionButtonContainer = new GameObject("CycleBinActionButtons", typeof(RectTransform)).GetComponent<RectTransform>();
        if (_actionButtonContainer == null)
        {
          return;
        }

        int uiLayer = _contractDisplay != null ? _contractDisplay.gameObject.layer : _contractRoot.gameObject.layer;
        _actionButtonContainer.gameObject.layer = uiLayer;

        var displayRect = _contractDisplay != null ? _contractDisplay.transform as RectTransform : null;

        _actionButtonContainer.SetParent(_contractRoot, false);
        int siblingIndex = displayRect != null ? displayRect.transform.GetSiblingIndex() + 1 : _contractRoot.childCount;
        _actionButtonContainer.SetSiblingIndex(siblingIndex);

        var actionsRect = _actionButtonContainer;
        actionsRect.anchorMin = new Vector2(0.5f, 0.5f);
        actionsRect.anchorMax = new Vector2(0.5f, 0.5f);
        actionsRect.pivot = new Vector2(0.5f, 0.5f);
        actionsRect.sizeDelta = Vector2.zero; // Let layout system determine size
        actionsRect.anchoredPosition = Vector2.zero;

        var actionsLayoutElement = _actionButtonContainer.GetComponent<LayoutElement>() ?? _actionButtonContainer.gameObject.AddComponent<LayoutElement>();
        actionsLayoutElement.preferredWidth = -1f; // Use preferred size
        actionsLayoutElement.preferredHeight = ButtonGroupHeight;
        actionsLayoutElement.flexibleWidth = 1f; // Allow flexible sizing
        actionsLayoutElement.flexibleHeight = 0f;

        var layout = _actionButtonContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 20f;
        layout.padding = new RectOffset(24, 24, 8, 8);

        _confirmButtonLabel = CreateActionButtonLabel(_actionButtonContainer, "CycleBinConfirmButton", Localizations.I18n.TrashBinConfirmKey.ToPlainText(), out _confirmButton);
        if (_confirmButton != null)
        {
          _confirmButton.onClick.RemoveAllListeners();
          _confirmButton.onClick.AddListener(() =>
          {
            CompleteContractAsync().Forget();
          });
        }

        _clearButtonLabel = CreateActionButtonLabel(_actionButtonContainer, "CycleBinClearButton", Localizations.I18n.TrashBinClearKey.ToPlainText(), out _clearButton);
        if (_clearButton != null)
        {
          _clearButton.onClick.RemoveAllListeners();
          _clearButton.onClick.AddListener(() =>
          {
            ReturnAllItems();
          });
        }

        UpdateActionButtonsState();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_contractRoot);
      }

      private TextMeshProUGUI CreateActionButtonLabel(RectTransform parent, string name, string labelText, out Button button)
      {
        int uiLayer = parent.gameObject.layer;

        TextMeshProUGUI label;
        if (_textTemplate != null)
        {
          // Use template instantiation like PatchStockShopView
          var labelObject = UnityEngine.Object.Instantiate(_textTemplate.gameObject, parent);
          labelObject.name = name;
          labelObject.layer = uiLayer;
          label = labelObject.GetComponent<TextMeshProUGUI>();
          if (label == null)
          {
            Log.Warning("[CycleBinDebug] Failed to get TextMeshProUGUI from template instantiation");
            UnityEngine.Object.Destroy(labelObject);
            // Fallback to direct creation
            return CreateActionButtonLabelFallback(parent, name, labelText, out button);
          }
        }
        else
        {
          Log.Warning("[CycleBinDebug] No text template available, using fallback creation");
          return CreateActionButtonLabelFallback(parent, name, labelText, out button);
        }

        ConfigureActionLabel(label, labelText);

        var labelRect = label.rectTransform;
        // Parent is already set by Instantiate

        button = label.gameObject.AddComponent<Button>();
        ConfigureActionButton(button, label);

        return label;
      }

      private static TextMeshProUGUI CreateActionButtonLabelFallback(RectTransform parent, string name, string labelText, out Button button)
      {
        int uiLayer = parent.gameObject.layer;

        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.layer = uiLayer;
        var label = labelObject.GetComponent<TextMeshProUGUI>();

        ConfigureActionLabel(label, labelText);

        var labelRect = label.rectTransform;
        labelRect.SetParent(parent, false);

        button = labelObject.AddComponent<Button>();
        ConfigureActionButton(button, label);

        return label;
      }

      private static void ConfigureActionLabel(TextMeshProUGUI label, string text)
      {
        label.text = text;
        label.margin = Vector4.zero;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = false;
        label.fontSize = Mathf.Max(ActionLabelMinFontSize, label.fontSize * ActionLabelFontScale);
        label.color = Color.white;
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

      private static void ConfigureActionButton(Button button, TextMeshProUGUI label)
      {
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = label;
        button.interactable = true;

        // Configure navigation to prevent unwanted focus changes
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        var colors = button.colors;
        colors.normalColor = ActionButtonNormalColor;
        colors.highlightedColor = ActionButtonHighlightedColor;
        colors.pressedColor = ActionButtonPressedColor;
        colors.selectedColor = ActionButtonHighlightedColor;
        colors.disabledColor = ActionButtonDisabledColor;
        button.colors = colors;
      }

      private void SubscribeMonitoredDisplays()
      {
        foreach (var display in _monitoredDisplays)
        {
          if (display == null)
          {
            continue;
          }

          SuppressOriginalDoubleClick(display);
          display.onDisplayDoubleClicked -= OnSourceDisplayDoubleClicked;
          display.onDisplayDoubleClicked += OnSourceDisplayDoubleClicked;
        }
      }

      private void UnsubscribeMonitoredDisplays()
      {
        foreach (var display in _monitoredDisplays)
        {
          if (display == null)
          {
            continue;
          }

          display.onDisplayDoubleClicked -= OnSourceDisplayDoubleClicked;
          RestoreOriginalDoubleClick(display);
        }
      }

      private void SuppressOriginalDoubleClick(InventoryDisplay display)
      {
        if (DoubleClickEventField == null || _suppressedDoubleClickHandlers.ContainsKey(display))
        {
          return;
        }

        var existing = DoubleClickEventField.GetValue(display) as Delegate;
        if (existing == null)
        {
          return;
        }

        _suppressedDoubleClickHandlers[display] = existing;
        DoubleClickEventField.SetValue(display, null);
      }

      private void RestoreOriginalDoubleClick(InventoryDisplay display)
      {
        if (DoubleClickEventField == null)
        {
          return;
        }

        if (_suppressedDoubleClickHandlers.TryGetValue(display, out var original) && original != null)
        {
          DoubleClickEventField.SetValue(display, original);
        }

        _suppressedDoubleClickHandlers.Remove(display);
      }

      private readonly struct ContractValidationResult
      {
        public ContractValidationResult(bool isValid, string category, ItemValueLevel quality, bool isFirstItem, string? errorMessage, ItemValueLevel? rewardLevel)
        {
          IsValid = isValid;
          Category = category;
          Quality = quality;
          IsFirstItem = isFirstItem;
          ErrorMessage = errorMessage;
          RewardLevel = rewardLevel;
        }

        public bool IsValid { get; }
        public string Category { get; }
        public ItemValueLevel Quality { get; }
        public bool IsFirstItem { get; }
        public string? ErrorMessage { get; }
        public ItemValueLevel? RewardLevel { get; }

        public static ContractValidationResult Invalid(string category, ItemValueLevel quality, bool isFirstItem, string? errorMessage)
        {
          return new ContractValidationResult(false, category, quality, isFirstItem, errorMessage, null);
        }
      }

      private ContractValidationResult EvaluateItemForContract(Item item)
      {
        if (item == null)
        {
          return ContractValidationResult.Invalid(string.Empty, ItemValueLevel.White, true, Localizations.I18n.ItemIsNullKey.ToPlainText());
        }

        if (_contractInventory == null)
        {
          return ContractValidationResult.Invalid(string.Empty, ItemValueLevel.White, true, Localizations.I18n.ContractInventoryNotAvailableKey.ToPlainText());
        }

        string category = LotteryService.GetItemCategory(item.TypeID) ?? string.Empty;
        ItemValueLevel quality = QualityUtils.GetCachedItemValueLevel(item);
        int currentCount = _contractInventory.GetItemCount();
        bool isFirstItem = currentCount == 0;
        ItemValueLevel? rewardLevel = null;

        if (string.IsNullOrEmpty(category) || !RewardCategories.Contains(category))
        {
          return ContractValidationResult.Invalid(category, quality, isFirstItem, Localizations.I18n.ItemNotValidForContractKey.ToPlainText());
        }

        if (currentCount >= ContractSize)
        {
          return ContractValidationResult.Invalid(category, quality, isFirstItem, Localizations.I18n.ContractFullKey.ToPlainText());
        }

        if (isFirstItem)
        {
          int nextLevelValue = (int)quality + 1;
          if (!Enum.IsDefined(typeof(ItemValueLevel), nextLevelValue))
          {
            return ContractValidationResult.Invalid(category, quality, true, "该等级不存在高一级的物品，无法进行汰换。");
          }

          rewardLevel = (ItemValueLevel)nextLevelValue;

          if (!LotteryService.HasCategoryItemAtLevel(RewardCategories, rewardLevel.Value))
          {
            return ContractValidationResult.Invalid(category, quality, true, "目标类型中不存在该等级的升级物品。");
          }
        }
        else if (!MatchesContractRequirements(quality))
        {
          return ContractValidationResult.Invalid(category, quality, false, Localizations.I18n.ItemQualityMismatchKey.ToPlainText());
        }
        else
        {
          rewardLevel = _rewardQuality;
        }

        return new ContractValidationResult(true, category, quality, isFirstItem, null, rewardLevel);
      }

      private bool MatchesContractRequirements(ItemValueLevel quality)
      {
        if (!_targetQuality.HasValue)
        {
          return false;
        }

        return _targetQuality.Value == quality;
      }

      private void ResetContractRequirements()
      {
        _targetQuality = null;
        _rewardQuality = null;
      }

      private void OnSourceDisplayDoubleClicked(InventoryDisplay display, InventoryEntry entry, PointerEventData data)
      {
        if (!_visible || _busy || _transferring)
        {
          return;
        }

        data?.Use();

        var item = entry?.GetItem();
        if (item == null)
        {
          return;
        }

        AddItemToContractAsync(item).Forget();
      }

      private void OnContractDoubleClicked(InventoryDisplay display, InventoryEntry entry, PointerEventData data)
      {
        if (_busy || _transferring)
        {
          return;
        }

        data?.Use();

        var item = entry?.GetItem();
        if (item == null)
        {
          return;
        }

        ReturnItem(item);
        UpdateActionButtonsState();
      }

      private async UniTask AddItemToContractAsync(Item item)
      {
        if (item == null)
        {
          return;
        }

        if (_transferring)
        {
          return;
        }

        var validation = EvaluateItemForContract(item);
        if (!validation.IsValid)
        {
          if (!string.IsNullOrEmpty(validation.ErrorMessage))
          {
            NotificationText.Push(validation.ErrorMessage);
          }
          return;
        }

        var origin = item.InInventory;
        if (origin == null)
        {
          Log.Warning("Cycle bin could not resolve the source inventory for the item.");
          return;
        }

        _transferring = true;

        try
        {
          Item itemToMove = item;
          Item? stackSource = null;

          if (item.StackCount > 1)
          {
            Item? splitItem = null;

            try
            {
              splitItem = await item.Split(1);
            }
            catch (Exception ex)
            {
              Log.Error($"Cycle bin failed to split item stack: {ex.Message}");
            }

            if (splitItem == null)
            {
              return;
            }

            itemToMove = splitItem;
            stackSource = item;
          }
          else
          {
            if (!origin.RemoveItem(item))
            {
              Log.Warning("Cycle bin failed to remove item from source inventory.");
              return;
            }

            item.Detach();
          }

          itemToMove.StackCount = 1;

          if (_contractInventory == null)
          {
            return;
          }

          if (!_contractInventory.AddItem(itemToMove))
          {
            Log.Warning("Cycle bin failed to add item into contract inventory.");

            if (stackSource != null)
            {
              stackSource.Combine(itemToMove);
            }
            else if (itemToMove != null && !origin.AddItem(itemToMove))
            {
              GiveToPlayer(itemToMove);
            }

            return;
          }

          if (itemToMove != null)
          {
            _itemOrigins[itemToMove] = new OriginItemContext(origin, stackSource);
          }

          if (validation.IsFirstItem)
          {
            _targetQuality = validation.Quality;
            _rewardQuality = validation.RewardLevel;
            if (!_rewardQuality.HasValue && _targetQuality.HasValue)
            {
              int nextLevelValue = (int)_targetQuality.Value + 1;
              if (Enum.IsDefined(typeof(ItemValueLevel), nextLevelValue))
              {
                _rewardQuality = (ItemValueLevel)nextLevelValue;
              }
            }
          }

          ItemUIUtilities.Select((ItemDisplay)null!);
        }
        catch (Exception ex)
        {
          Log.Error($"[CycleBinDebug] Exception during transfer: {ex.Message}");
          throw;
        }
        finally
        {
          _transferring = false;
          UpdateActionButtonsState();
        }
      }

      private void ReturnItem(Item item)
      {
        if (_contractInventory == null)
        {
          return;
        }

        if (!_contractInventory.RemoveItem(item))
        {
          return;
        }

        if (!_itemOrigins.TryGetValue(item, out var originContext) || originContext.Inventory == null)
        {
          GiveToPlayer(item);
          _itemOrigins.Remove(item);
          return;
        }

        bool restored = false;

        if (originContext.StackSource != null)
        {
          try
          {
            originContext.StackSource.Combine(item);
            restored = true;
          }
          catch (Exception ex)
          {
            Log.Warning($"Cycle bin failed to combine returned item: {ex.Message}");
            restored = false;
          }
        }

        if (!restored)
        {
          if (!originContext.Inventory.AddItem(item))
          {
            GiveToPlayer(item);
          }
        }

        _itemOrigins.Remove(item);

        if (_contractInventory.GetItemCount() == 0)
        {
          ResetContractRequirements();
        }
      }

      private void ReturnAllItems()
      {
        if (_contractInventory == null)
        {
          return;
        }

        var items = _contractInventory.Content.Where(i => i != null).ToList();
        foreach (var item in items)
        {
          ReturnItem(item);
        }

        ResetContractRequirements();
        UpdateActionButtonsState();
      }

      private static void GiveToPlayer(Item item)
      {
        if (!ItemUtilities.SendToPlayerCharacterInventory(item))
        {
          ItemUtilities.SendToPlayerStorage(item);
        }
      }

      private void UpdateActionButtonsState()
      {
        if (_contractInventory == null)
        {
          if (_confirmButton != null)
          {
            _confirmButton.interactable = false;
          }

          if (_clearButton != null)
          {
            _clearButton.interactable = false;
          }

          if (_confirmButtonLabel != null)
          {
            _confirmButtonLabel.text = Localizations.I18n.TrashBinConfirmKey.ToPlainText();
          }

          if (_clearButtonLabel != null)
          {
            _clearButtonLabel.text = Localizations.I18n.TrashBinClearKey.ToPlainText();
          }

          return;
        }

        int itemCount = _contractInventory.GetItemCount();
        bool hasItems = itemCount > 0;
        bool contractFull = itemCount >= ContractSize;
        bool canConfirm = !_busy && !_transferring && contractFull && _rewardQuality.HasValue;
        bool canClear = !_busy && !_transferring && hasItems;

        if (_confirmButton != null)
        {
          _confirmButton.interactable = canConfirm;
        }

        if (_confirmButtonLabel != null)
        {
          var confirmText = Localizations.I18n.TrashBinConfirmKey.ToPlainText();
          confirmText = $"{confirmText} ({Mathf.Min(itemCount, ContractSize)}/{ContractSize})";
          _confirmButtonLabel.text = confirmText;
        }

        if (_clearButton != null)
        {
          _clearButton.interactable = canClear;
        }

        if (_clearButtonLabel != null)
        {
          var clearText = Localizations.I18n.TrashBinClearKey.ToPlainText();
          _clearButtonLabel.text = clearText;
        }
      }

      private async UniTask CompleteContractAsync()
      {
        if (_busy || _contractInventory == null)
        {
          return;
        }

        var items = _contractInventory.Content.Where(i => i != null).ToList();
        if (items.Count < ContractSize)
        {
          return;
        }

        _busy = true;
        UpdateActionButtonsState();

        try
        {
          var rewardLevel = _rewardQuality;

          foreach (var item in items)
          {
            _contractInventory.RemoveItem(item);
            _itemOrigins.Remove(item);
            item.Detach();
            UnityEngine.Object.Destroy(item.gameObject);
          }

          Item? reward = null;

          if (rewardLevel.HasValue)
          {
            reward = await LotteryService.PickRandomItemByCategoriesAndQualityAsync(RewardCategories, rewardLevel.Value);
          }

          if (reward == null)
          {
            var fallbackLevel = DetermineTargetValueLevel(items);
            reward = await TryCreateRewardItem(fallbackLevel);
          }

          ResetContractRequirements();

          if (reward == null)
          {
            NotificationText.Push(Localizations.I18n.RecyclingFailedKey.ToPlainText());
            return;
          }

          // Play reward animation
          await CycleBinAnimation.PlayAsync(reward);

          if (!ItemUtilities.SendToPlayerCharacterInventory(reward))
          {
            ItemUtilities.SendToPlayerStorage(reward);
          }

          string message = Localizations.I18n.PickNotificationFormatKey.ToPlainText()
              .Replace("{itemDisplayName}", reward.DisplayName);
          NotificationText.Push(message);
          AudioManager.Post("UI/buy");
        }
        catch (Exception ex)
        {
          Log.Error($"Cycle bin contract failed: {ex.Message}");
        }
        finally
        {
          _busy = false;
          UpdateActionButtonsState();
        }
      }

      private static ItemValueLevel DetermineTargetValueLevel(IEnumerable<Item> items)
      {
        var levels = items
          .Where(i => i != null)
          .Select(QualityUtils.GetCachedItemValueLevel)
          .Select(level => (int)level)
          .ToList();

        if (levels.Count == 0)
        {
          return ItemValueLevel.Green;
        }

        float average = (float)levels.Average();
        int targetValue = Mathf.Clamp(Mathf.RoundToInt(average) + 1, (int)ItemValueLevel.White, (int)ItemValueLevel.Red);
        return (ItemValueLevel)targetValue;
      }

      private static async UniTask<Item?> TryCreateRewardItem(ItemValueLevel targetLevel)
      {
        var rewardList = await LotteryService.PickRandomItemsByQualityAsync(targetLevel, 1);
        if (rewardList != null && rewardList.Count > 0)
        {
          return rewardList[0];
        }

        Log.Warning("Cycle bin value-level-based reward failed – falling back to default lottery pool.");
        var fallback = await LotteryService.PickRandomItemAsync(LotteryService.ItemTypeIdsCache);
        if (fallback != null)
        {
          fallback.StackCount = 1;
        }

        return fallback;
      }
    }
  }
}
