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

    /// <summary>
    /// Lottery context for street pick operations
    /// </summary>
    public class StreetLotteryContext : ILotteryContext
    {
        private const string SFX_BUY = "UI/buy";

        public async UniTask<bool> OnBeforeLotteryAsync()
        {
            // No special validation needed for street lottery
            return await UniTask.FromResult(true);
        }

        public void OnLotterySuccess(Item resultItem, bool sentToStorage)
        {
            if (resultItem == null) return;

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

        public void OnLotteryFailed()
        {
            // No special cleanup needed for street lottery
        }
    }

    [HarmonyPatch(typeof(StockShopView), "Setup")]
    public class PatchStockShopView_Setup
    {
        private static StockShop? _currentStockShop = null;
        private static TextMeshProUGUI? _refreshStockText;
        private static Button? _refreshButton;
        private static TextMeshProUGUI? _storePickText;
        private static Button? _storePickButton;
        private static TextMeshProUGUI? _streetPickText;
        private static Button? _streetPickButton;
        private static RectTransform? _actionsContainer;
        private static bool _priceChangeSubscribed = false;
        private static readonly string SFX_BUY = "UI/buy";
        private const float ActionsContainerFallbackWidth = 320f;
        private const float ActionsContainerHeight = 240f; // Increased height for vertical layout with larger spacing
        private const float ActionsLayoutSpacing = 24f; // Larger spacing between buttons to prevent overlap
        private const int ActionsLayoutPaddingHorizontal = 0; // No horizontal padding for left alignment
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

            EnsureTexts(___merchantNameText);
            EnsureButtons(__instance);
            SubscribeToPriceChanges();
            UpdateButtonTexts();
        }

        private static void EnsureTexts(TextMeshProUGUI merchantNameText)
        {
            if (_refreshStockText != null || _storePickText != null) return;

            if (_actionsContainer == null)
            {
                var parent = merchantNameText.transform.parent as RectTransform;
                if (parent == null) return;

                // Navigate up the hierarchy to find the appropriate container
                // Goal: place buttons below the item grid
                var grandParent = parent.parent as RectTransform;
                var greatGrandParent = grandParent?.parent as RectTransform;

                // Use the highest available parent (great-grandparent if available, otherwise grandparent, otherwise parent)
                var targetParent = greatGrandParent ?? grandParent ?? parent;

                Log.Debug($"Button container parent hierarchy - Parent: {parent.name}, GrandParent: {grandParent?.name ?? "null"}, GreatGrandParent: {greatGrandParent?.name ?? "null"}");
                Log.Debug($"Using target parent: {targetParent.name}");

                _actionsContainer = new GameObject("ExtraActionsContainer", typeof(RectTransform)).GetComponent<RectTransform>();
                _actionsContainer.SetParent(targetParent, false);

                // Anchor to bottom instead of top to place below items
                _actionsContainer.anchorMin = new Vector2(0.5f, 0f);
                _actionsContainer.anchorMax = new Vector2(0.5f, 0f);
                _actionsContainer.pivot = new Vector2(0.5f, 0f);
                _actionsContainer.anchoredPosition = new Vector2(0f, 20f); // Small offset from bottom

                float width = merchantNameText.rectTransform.rect.width;
                if (width <= 0f) width = ActionsContainerFallbackWidth;
                _actionsContainer.sizeDelta = new Vector2(width, ActionsContainerHeight);

                // Move to the end of sibling list to ensure proper rendering order
                _actionsContainer.SetAsLastSibling();

                var layout = _actionsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlHeight = false; // Don't control height - let buttons keep their fixed size
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = false;
                layout.spacing = ActionsLayoutSpacing;
                layout.padding = CreateActionsPadding();
            }

            // Initialize lottery animation UI
            LotteryAnimation.Initialize(merchantNameText.canvas, merchantNameText);

            _refreshStockText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
            ConfigureActionLabel(_refreshStockText, Localizations.I18n.RefreshStockKey.ToPlainText());

            _storePickText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
            ConfigureActionLabel(_storePickText, Localizations.I18n.StorePickKey.ToPlainText());

            _streetPickText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
            ConfigureActionLabel(_streetPickText, Localizations.I18n.StreetPickKey.ToPlainText());
        }

        private static void EnsureButtons(StockShopView view)
        {
            if (_refreshButton != null || _storePickButton != null || _streetPickButton != null) return;
            if (_refreshStockText == null || _storePickText == null || _streetPickText == null) return;

            _refreshButton = _refreshStockText.gameObject.AddComponent<Button>();
            ConfigureActionButton(_refreshButton, _refreshStockText);

            _storePickButton = _storePickText.gameObject.AddComponent<Button>();
            ConfigureActionButton(_storePickButton, _storePickText);

            _streetPickButton = _streetPickText.gameObject.AddComponent<Button>();
            ConfigureActionButton(_streetPickButton, _streetPickText);

            _refreshButton.onClick.AddListener(() => OnRefreshButtonClicked());
            _storePickButton.onClick.AddListener(() => OnStorePickButtonClicked().Forget());
            _streetPickButton.onClick.AddListener(() => OnStreetPickButtonClicked().Forget());
        }

        private static void UpdateButtonTexts()
        {
            if (_refreshStockText == null || _storePickText == null || _streetPickText == null) return;

            long refreshPrice = SettingManager.Instance.RefreshStockPrice.Value as long? ?? DefaultSettings.RefreshStockPrice;
            long storePickPrice = SettingManager.Instance.StorePickPrice.Value as long? ?? DefaultSettings.StorePickPrice;
            long streetPickPrice = SettingManager.Instance.StreetPickPrice.Value as long? ?? DefaultSettings.StreetPickPrice;

            var baseRefreshText = Localizations.I18n.RefreshStockKey.ToPlainText();
            var baseStorePickText = Localizations.I18n.StorePickKey.ToPlainText();
            var baseStreetPickText = Localizations.I18n.StreetPickKey.ToPlainText();
            var freeText = Localizations.I18n.FreeKey.ToPlainText();

            _refreshStockText.text = refreshPrice > 0 ? $"{baseRefreshText} (${refreshPrice})" : $"{baseRefreshText} ({freeText})";
            _storePickText.text = storePickPrice > 0 ? $"{baseStorePickText} (${storePickPrice})" : $"{baseStorePickText} ({freeText})";
            _streetPickText.text = streetPickPrice > 0 ? $"{baseStreetPickText} (${streetPickPrice})" : $"{baseStreetPickText} ({freeText})";
        }

        private static void SubscribeToPriceChanges()
        {
            if (_priceChangeSubscribed) return;

            var settings = SettingManager.Instance;

            settings.RefreshStockPrice.OnValueChanged += _ => UpdateButtonTexts();
            settings.StorePickPrice.OnValueChanged += _ => UpdateButtonTexts();
            settings.StreetPickPrice.OnValueChanged += _ => UpdateButtonTexts();

            _priceChangeSubscribed = true;
            Log.Debug("Subscribed to price change events");
        }

        private static async UniTask OnStreetPickButtonClicked()
        {
            Log.Debug("Street pick button clicked");

            // Get price from settings
            long price = SettingManager.Instance.StreetPickPrice.Value as long? ?? DefaultSettings.StreetPickPrice;

            try
            {
                var context = new StreetLotteryContext();
                await LotteryService.PerformLotteryWithContextAsync(
                    candidateTypeIds: null, // Use default cache
                    price: price,
                    playAnimation: SettingManager.Instance.EnableAnimation.Value as bool? ?? DefaultSettings.EnableAnimation,
                    context: context);
            }
            catch (Exception ex)
            {
                Log.Error($"Street lottery failed: {ex.Message}");
            }
        }

        private static void OnRefreshButtonClicked()
        {
            Log.Debug("Refresh button clicked");
            if (_currentStockShop == null) return;

            // Get price from settings and try to pay
            long price = SettingManager.Instance.RefreshStockPrice.Value as long? ?? DefaultSettings.RefreshStockPrice;

            // Skip payment if price is zero
            if (price > 0 && !EconomyManager.Pay(new Cost(price), true, true))
            {
                Log.Warning($"Failed to pay {price} for refresh stock");
                var notEnoughMoneyMessage = Localizations.I18n.NotEnoughMoneyFormatKey.ToPlainText().Replace("{price}", price.ToString());
                NotificationText.Push(notEnoughMoneyMessage);
                return;
            }

            if (!TryInvokeRefresh(_currentStockShop)) return;
            AudioManager.Post(SFX_BUY);
            Log.Debug("Stock refreshed");
        }


        private static async UniTask<bool> OnStorePickButtonClicked()
        {
            Log.Debug("Store pick button clicked");
            if (_currentStockShop == null) return false;
            if (_currentStockShop.Busy) return false;

            var itemEntries = _currentStockShop.entries.Where(entry =>
              entry.CurrentStock > 0 &&
              entry.Possibility > 0f &&
              entry.Show).ToList();

            if (itemEntries.Count == 0)
            {
                Log.Warning("No available items to pick");
                return false;
            }

            // Get price from settings
            long price = SettingManager.Instance.StorePickPrice.Value as long? ?? DefaultSettings.StorePickPrice;

            var candidateTypeIds = itemEntries.Select(entry => entry.ItemTypeID).ToList();
            var context = new StoreLotteryContext(_currentStockShop, itemEntries);

            try
            {
                await LotteryService.PerformLotteryWithContextAsync(
                    candidateTypeIds,
                    price,
                    playAnimation: SettingManager.Instance.EnableAnimation.Value as bool? ?? DefaultSettings.EnableAnimation,
                    context);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Store lottery failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryInvokeRefresh(StockShop? stockShop)
        {
            if (stockShop == null) return false;

            AccessTools.Method(typeof(StockShop), "DoRefreshStock").Invoke(stockShop, null);
            return true;
        }

        private static void ConfigureActionLabel(TextMeshProUGUI label, string text)
        {
            label.text = text;
            label.margin = Vector4.zero;
            label.alignment = TextAlignmentOptions.Center; // Center alignment
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
}
