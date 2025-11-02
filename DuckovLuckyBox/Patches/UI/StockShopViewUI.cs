using System;
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
        /// <summary>
        /// Component to detect double clicks on buttons
        /// </summary>
        private class DoubleClickDetector
        {
            private Dictionary<string, float> _lastClickTimes = new Dictionary<string, float>();
            private Dictionary<string, int> _clickCounts = new Dictionary<string, int>();
            private const float DoubleClickThreshold = 0.3f;

            /// <summary>
            /// Event triggered on single click
            /// </summary>
            public event Action<string>? OnSingleClick;

            /// <summary>
            /// Event triggered on double click
            /// </summary>
            public event Action<string>? OnDoubleClick;

            /// <summary>
            /// Handle a button click for the given identifier
            /// </summary>
            public void HandleClick(string identifier)
            {
                if (!_lastClickTimes.ContainsKey(identifier)) _lastClickTimes[identifier] = -DoubleClickThreshold;
                if (!_clickCounts.ContainsKey(identifier)) _clickCounts[identifier] = 0;

                float currentTime = Time.unscaledTime;
                float timeSinceLastClick = currentTime - _lastClickTimes[identifier];
                _lastClickTimes[identifier] = currentTime;

                if (timeSinceLastClick < DoubleClickThreshold)
                {
                    _clickCounts[identifier]++;
                    if (_clickCounts[identifier] == 2)
                    {
                        // Double click
                        OnDoubleClick?.Invoke(identifier);
                        _clickCounts[identifier] = 0;
                    }
                }
                else
                {
                    _clickCounts[identifier] = 1;
                    // Start async task to handle single click after threshold
                    HandleSingleClickAsync(identifier).Forget();
                }
            }

            private async UniTask HandleSingleClickAsync(string identifier)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(DoubleClickThreshold), DelayType.UnscaledDeltaTime);
                if (_clickCounts.TryGetValue(identifier, out var clickCount) && clickCount == 1)
                {
                    // Single click
                    OnSingleClick?.Invoke(identifier);
                    _clickCounts[identifier] = 0;
                }
            }
        }

        private static StockShopViewUI? _instance;
        public static StockShopViewUI Instance
        {
            get
            {
                _instance ??= new StockShopViewUI();
                return _instance;
            }
        }

        private bool isInitialized = false;
        private bool isOpen = false;
        private StockShop? _currentStockShop = null;
        private StockShopView? _currentStockShopView = null;
        private StockShopActionManager? _actionManager = null;
        private DoubleClickDetector? _doubleClickDetector = null;
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

            if (_doubleClickDetector == null)
            {
                _doubleClickDetector = new DoubleClickDetector();
                _doubleClickDetector.OnSingleClick += OnSingleClick;
                _doubleClickDetector.OnDoubleClick += OnDoubleClick;
                Log.Debug("Double click detector initialized");
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

            // Hide container if no actions are enabled
            bool anyActionEnabled = SettingManager.Instance.EnableRefreshStock.GetAsBool() ||
                                    SettingManager.Instance.EnableStorePick.GetAsBool() ||
                                    SettingManager.Instance.EnableStreetPick.GetAsBool() ||
                                    SettingManager.Instance.EnableRecycle.GetAsBool();
            _actionsContainer?.gameObject.SetActive(anyActionEnabled);
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
                _actionsContainer.anchoredPosition = new Vector2(0f, 40f);

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
                string actionName = action.GetType().Name;
                bool isEnabled = false;

                switch (actionName)
                {
                    case nameof(RefreshStockAction):
                        isEnabled = SettingManager.Instance.EnableRefreshStock.GetAsBool();
                        break;
                    case nameof(StorePickAction):
                        isEnabled = SettingManager.Instance.EnableStorePick.GetAsBool();
                        break;
                    case nameof(StreetPickAction):
                        isEnabled = SettingManager.Instance.EnableStreetPick.GetAsBool();
                        break;
                    case nameof(RecycleAction):
                        isEnabled = SettingManager.Instance.EnableRecycle.GetAsBool();
                        break;
                    default:
                        isEnabled = true; // Default to enabled for unknown actions
                        break;
                }

                // Always create the button, just set active based on setting
                var actionText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
                ConfigureActionLabel(actionText);

                var button = actionText.gameObject.AddComponent<Button>();
                ConfigureActionButton(button, actionText);

                // Set active based on setting
                actionText.gameObject.SetActive(isEnabled);

                _actionTexts[actionName] = actionText;
                _actionButtons[actionName] = button;

                // Bind click event
                button.onClick.AddListener(() => _doubleClickDetector?.HandleClick(actionName));
            }

            UpdateButtonTexts();

            Log.Debug($"Created {_actionTexts.Count} action buttons");
        }

        private void OnSingleClick(string actionName)
        {
            ExecuteActionAsync(actionName, false).Forget();
        }

        private void OnDoubleClick(string actionName)
        {
            ExecuteActionAsync(actionName, true).Forget();
        }

        private async UniTaskVoid ExecuteActionAsync(string actionName, bool isDoubleClick)
        {
            if (_actionManager == null) return;
            if (_currentStockShopView != null)
            {
                await _actionManager.ExecuteAsync(actionName, _currentStockShopView, isDoubleClick);
            }
            else
            {
                Log.Warning("Cannot execute action: current StockShopView is null");
            }

            UpdateButtonTexts();
        }


        private void UpdateButtonTexts()
        {
            UpdateRefreshStockButtonText();
            UpdateStorePickButtonText();
            UpdateStreetPickButtonText();
            UpdateRecycleButtonText();
        }

        private void UpdateRefreshStockButtonText()
        {
            if (!_actionTexts.TryGetValue(nameof(RefreshStockAction), out var refreshText)) return;

            long refreshPrice = SettingManager.Instance.RefreshStockPrice.Value as long? ?? DefaultSettings.RefreshStockPrice;
            var freeText = Localizations.I18n.FreeKey.ToPlainText();
            var baseText = Localizations.I18n.RefreshStockKey.ToPlainText();
            var priceText = refreshPrice > 0 ? $" (${refreshPrice})" : $" ({freeText})";
            var fullText = $"{baseText}{priceText}";
            refreshText.text = fullText;
        }

        private void UpdateStorePickButtonText()
        {
            if (!_actionTexts.TryGetValue(nameof(StorePickAction), out var storePickText)) return;

            long storePickPrice = SettingManager.Instance.StorePickPrice.Value as long? ?? DefaultSettings.StorePickPrice;
            var freeText = Localizations.I18n.FreeKey.ToPlainText();
            var baseText = Localizations.I18n.StorePickKey.ToPlainText();
            var priceText = storePickPrice > 0 ? $" (${storePickPrice})" : $" ({freeText})";
            var doubleClickText = SettingManager.Instance.EnableTripleLotteryAnimation.GetAsBool()
                ? $" ({Localizations.I18n.DoubleClickToTripleLotteryKey.ToPlainText()})"
                : string.Empty;

            var fullText = $"{baseText}{priceText}{doubleClickText}";
            storePickText.text = fullText;
        }

        private void UpdateStreetPickButtonText()
        {
            if (!_actionTexts.TryGetValue(nameof(StreetPickAction), out var streetPickText)) return;

            long streetPickPrice = SettingManager.Instance.StreetPickPrice.Value as long? ?? DefaultSettings.StreetPickPrice;
            var freeText = Localizations.I18n.FreeKey.ToPlainText();
            var baseText = Localizations.I18n.StreetPickKey.ToPlainText();
            var priceText = streetPickPrice > 0 ? $" (${streetPickPrice})" : $" ({freeText})";
            var doubleClickText = SettingManager.Instance.EnableTripleLotteryAnimation.GetAsBool()
                ? $" ({Localizations.I18n.DoubleClickToTripleLotteryKey.ToPlainText()})"
                : string.Empty;
            var fullText = $"{baseText}{priceText}{doubleClickText}";
            streetPickText.text = fullText;
        }

        private void UpdateRecycleButtonText()
        {
            if (!_actionTexts.TryGetValue(nameof(RecycleAction), out var recycleText)) return;

            // Display "Close Recycle" when the recycle view is open, otherwise display "Open Recycle".
            var text = RecycleSessionUI.Instance.IsOpen
                ? Localizations.I18n.CloseKey.ToPlainText() + " " + Localizations.I18n.RecycleKey.ToPlainText()
                : Localizations.I18n.OpenKey.ToPlainText() + " " + Localizations.I18n.RecycleKey.ToPlainText();
            recycleText.text = text;
        }

        private void SubscribeToPriceChanges()
        {
            if (_priceChangeSubscribed) return;

            var settings = SettingManager.Instance;
            settings.RefreshStockPrice.OnValueChanged += _ => UpdateRefreshStockButtonText();
            settings.StorePickPrice.OnValueChanged += _ => UpdateStorePickButtonText();
            settings.StreetPickPrice.OnValueChanged += _ => UpdateStreetPickButtonText();
            settings.EnableRefreshStock.OnValueChanged += OnEnableRefreshStockChanged;
            settings.EnableStorePick.OnValueChanged += OnEnableStorePickChanged;
            settings.EnableStreetPick.OnValueChanged += OnEnableStreetPickChanged;
            settings.EnableRecycle.OnValueChanged += OnEnableRecycleChanged;

            _priceChangeSubscribed = true;
            Log.Debug("Subscribed to price change events");
        }

        private void OnEnableRefreshStockChanged(object value)
        {
            bool enabled = value is bool b && b;
            if (_actionTexts.TryGetValue(nameof(RefreshStockAction), out var text))
            {
                text.gameObject.SetActive(enabled);
            }
            UpdateContainerVisibility();
            ForceRebuildLayout();
        }

        private void OnEnableStorePickChanged(object value)
        {
            bool enabled = value is bool b && b;
            if (_actionTexts.TryGetValue(nameof(StorePickAction), out var text))
            {
                text.gameObject.SetActive(enabled);
            }
            UpdateContainerVisibility();
            ForceRebuildLayout();
        }

        private void OnEnableStreetPickChanged(object value)
        {
            bool enabled = value is bool b && b;
            if (_actionTexts.TryGetValue(nameof(StreetPickAction), out var text))
            {
                text.gameObject.SetActive(enabled);
            }
            UpdateContainerVisibility();
            ForceRebuildLayout();
        }

        private void OnEnableRecycleChanged(object value)
        {
            bool enabled = value is bool b && b;
            if (_actionTexts.TryGetValue(nameof(RecycleAction), out var text))
            {
                text.gameObject.SetActive(enabled);
            }
            UpdateContainerVisibility();
            ForceRebuildLayout();
        }

        private void UpdateContainerVisibility()
        {
            bool anyActionEnabled = SettingManager.Instance.EnableRefreshStock.GetAsBool() ||
                                    SettingManager.Instance.EnableStorePick.GetAsBool() ||
                                    SettingManager.Instance.EnableStreetPick.GetAsBool() ||
                                    SettingManager.Instance.EnableRecycle.GetAsBool();
            _actionsContainer?.gameObject.SetActive(anyActionEnabled);
        }

        private void ConfigureActionLabel(TextMeshProUGUI label)
        {
            label.text = string.Empty;
            label.margin = Vector4.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.fontSize = Mathf.Max(ActionLabelMinFontSize, label.fontSize * ActionLabelFontScale);
            label.raycastTarget = true;

            var rectTransform = label.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // Increase button width for better click area
            rectTransform.sizeDelta = new Vector2(Mathf.Max(200f, label.preferredWidth + ActionLabelExtraWidth), ActionLabelPreferredHeight);

            var layoutElement = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = ActionLabelPreferredHeight;
            layoutElement.preferredWidth = Mathf.Max(200f, label.preferredWidth + ActionLabelExtraWidth);
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

            // Update container visibility based on individual settings
            UpdateContainerVisibility();
        }

        private void ForceRebuildLayout()
        {
            if (_actionsContainer != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_actionsContainer);
            }
        }
    }
}