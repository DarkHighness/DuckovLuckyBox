using Duckov;
using Duckov.UI;
using Duckov.Economy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Duckov.Economy;
using SodaCraft.Localizations;
using HarmonyLib;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using ItemStatsSystem;
using System.Linq;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;
using DuckovLuckyBox.Patches.StockShopActions;

namespace DuckovLuckyBox.Patches
{
    /// <summary>
    /// Lottery context for store pick operations
    /// </summary>
    public class StoreLotteryContext : ILotteryContext
    {
        private readonly StockShop _stockShop;
        private readonly IList<StockShop.Entry> _stockEntries;
        private const string SFX_BUY = "UI/buy";

        public StoreLotteryContext(StockShop stockShop, IList<StockShop.Entry> stockEntries)
        {
            _stockShop = stockShop;
            _stockEntries = stockEntries;
        }

        public async UniTask<bool> OnBeforeLotteryAsync()
        {
            if (_stockShop == null || _stockShop.Busy) return false;

            // Mark shop as buying
            try { AccessTools.Field(typeof(StockShop), "buying").SetValue(_stockShop, true); } catch { }

            return await UniTask.FromResult(true);
        }

        public void OnLotterySuccess(Item resultItem, bool sentToStorage)
        {
            if (_stockShop == null || resultItem == null) return;

            try
            {
                // Decrement stock of the entry matching the result item's TypeID
                if (_stockEntries != null && _stockEntries.Count > 0)
                {
                    var matchingEntry = _stockEntries.FirstOrDefault(entry => entry?.ItemTypeID == resultItem.TypeID);
                    if (matchingEntry != null)
                    {
                        matchingEntry.CurrentStock = Math.Max(0, matchingEntry.CurrentStock - 1);
                    }
                    else
                    {
                        Log.Error($"No matching stock entry found for item TypeID {resultItem.TypeID}");
                    }
                }

                // Fire shop events
                var onAfterItemSoldField = AccessTools.Field(typeof(StockShop), "OnAfterItemSold");
                if (onAfterItemSoldField?.GetValue(null) is Action<StockShop> onAfterItemSold)
                    onAfterItemSold(_stockShop);
                var onItemPurchasedField = AccessTools.Field(typeof(StockShop), "OnItemPurchased");
                if (onItemPurchasedField?.GetValue(null) is Action<StockShop, Item> onItemPurchased)
                    onItemPurchased(_stockShop, resultItem);

                // Show notification
                var messageTemplate = Localizations.I18n.PickNotificationFormatKey.ToPlainText();
                var message = messageTemplate.Replace("{itemDisplayName}", resultItem.DisplayName);
                if (sentToStorage)
                {
                    message += " " + Localizations.I18n.InventoryFullAndSendToStorageKey.ToPlainText();
                }
                NotificationText.Push(message);
                AudioManager.Post(SFX_BUY);
            }
            finally
            {
                try { AccessTools.Field(typeof(StockShop), "buying").SetValue(_stockShop, false); } catch { }
            }
        }

        public void OnLotteryFailed()
        {
            try { AccessTools.Field(typeof(StockShop), "buying").SetValue(_stockShop, false); } catch { }
        }
    }

    [HarmonyPatch(typeof(StockShopView), "Setup")]
    public class PatchStockShopView_Setup
    {
        private static StockShop? _currentStockShop = null;
        private static StockShopActionManager? _actionManager = null;
        private static Dictionary<string, TextMeshProUGUI> _actionTexts = new Dictionary<string, TextMeshProUGUI>();
        private static Dictionary<string, Button> _actionButtons = new Dictionary<string, Button>();
        private static RectTransform? _actionsContainer;
        private static bool _priceChangeSubscribed = false;
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

        public static void Postfix(StockShopView __instance, TextMeshProUGUI ___merchantNameText, StockShop ___target)
        {
            if (___merchantNameText == null) return;
            _currentStockShop = ___target;

            InitializeActionManager();
            EnsureUIElements(___merchantNameText);
            SubscribeToPriceChanges();
            UpdateButtonTexts();
        }

        private static void InitializeActionManager()
        {
            if (_actionManager == null)
            {
                _actionManager = new StockShopActionManager();
                Log.Debug("Stock shop action manager initialized");
            }
        }

        private static void EnsureUIElements(TextMeshProUGUI merchantNameText)
        {
            if (_actionTexts.Count > 0) return;

            EnsureActionContainer(merchantNameText);
            CreateActionButtons(merchantNameText);

            // Hide if disabled
            if (!SettingManager.Instance.EnableStockShopActions.GetAsBool())
            {
                _actionsContainer?.gameObject.SetActive(false);
            }
        }

        private static void EnsureActionContainer(TextMeshProUGUI merchantNameText)
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

        private static void CreateActionButtons(TextMeshProUGUI merchantNameText)
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

        private static async UniTaskVoid ExecuteActionAsync(string actionName)
        {
            if (_actionManager == null) return;

            var view = FindStockShopView();
            if (view != null)
            {
                await _actionManager.ExecuteAsync(actionName, view);
            }
        }

        private static StockShopView? FindStockShopView()
        {
            // Find the currently active StockShopView
            var allViews = UnityEngine.Object.FindObjectsOfType<StockShopView>();
            return allViews.Length > 0 ? allViews[0] : null;
        }

        private static void UpdateButtonTexts()
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

            if (_actionTexts.TryGetValue(nameof(CycleBinAction), out var cycleBinText))
            {
                cycleBinText.text = Localizations.I18n.TrashBinKey.ToPlainText();
            }
        }

        private static void SubscribeToPriceChanges()
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

        private static void OnEnableStockShopActionsChanged(object value)
        {
            bool enabled = value is bool b && b;
            _actionsContainer?.gameObject.SetActive(enabled);
        }

        private static void ConfigureActionLabel(TextMeshProUGUI label, string text)
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

        private static void ConfigureActionButton(Button button, TextMeshProUGUI label)
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

        private static RectOffset CreateActionsPadding()
        {
            return new RectOffset(
              ActionsLayoutPaddingHorizontal,
              ActionsLayoutPaddingHorizontal,
              ActionsLayoutPaddingTop,
              ActionsLayoutPaddingBottom);
        }
    }

    [HarmonyPatch(typeof(StockShopView), "OnOpen")]
    public static class PatchStockShopView_OnOpen
    {
        public static void Postfix(StockShopView __instance)
        {
            CycleBinAction.OnViewOpened(__instance);
        }
    }

    [HarmonyPatch(typeof(StockShopView), "OnClose")]
    public static class PatchStockShopView_OnClose
    {
        public static void Postfix(StockShopView __instance)
        {
            CycleBinAction.OnViewClosed(__instance);
        }
    }

    [HarmonyPatch(typeof(StockShopView), "OnSelectionChanged")]
    public static class PatchStockShopView_OnSelectionChanged
    {
        public static bool Prefix(StockShopView __instance)
        {
            // Prevent selection changes when Cycle Bin view is open or has items
            if (CycleBinAction.IsOpen && CycleBinAction.HasItems)
            {
                return false; // Skip original method
            }
            return true; // Proceed with original method
        }
    }
}
