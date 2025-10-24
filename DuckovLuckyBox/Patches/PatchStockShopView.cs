using Duckov;
using Duckov.UI;
using Duckov.Economy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Duckov.Economy;
using SodaCraft.Localizations;
using HarmonyLib;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using ItemStatsSystem;
using System.Linq;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;

namespace DuckovLuckyBox.Patches
{

    [HarmonyPatch(typeof(StockShopView), "Setup")]
    public class PatchStockShopView_Setup
    {
        private static TextMeshProUGUI? _refreshStockText;
        private static Button? _refreshButton;
        private static TextMeshProUGUI? _pickOneText;
        private static Button? _storePickButton;
        private static TextMeshProUGUI? _buyLuckyBoxText;
        private static Button? _streetPickButton;
        private static RectTransform? _actionsContainer;
        private static bool _priceChangeSubscribed = false;
        private static readonly string SFX_BUY = "UI/buy";
        private const float AnchorOffsetExtraPadding = 40f;
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

            EnsureTexts(___merchantNameText);
            EnsureButtons(__instance, ___target);
            SubscribeToPriceChanges();
            UpdateButtonTexts();
        }

        private static void EnsureTexts(TextMeshProUGUI merchantNameText)
        {
            if (_refreshStockText != null || _pickOneText != null) return;

            if (_actionsContainer == null)
            {
                var parent = merchantNameText.transform.parent as RectTransform;
                if (parent == null) return;

                // Navigate up the hierarchy to find the appropriate container
                // Goal: place buttons below the item grid
                var grandParent = parent.parent as RectTransform;
                var greatGrandParent = grandParent?.parent as RectTransform;

                // Use the highest available parent (great-grandparent if available, otherwise grandparent, otherwise parent)
                var targetParent = greatGrandParent != null ? greatGrandParent : (grandParent != null ? grandParent : parent);

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

            _pickOneText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
            ConfigureActionLabel(_pickOneText, Localizations.I18n.StorePickKey.ToPlainText());

            _buyLuckyBoxText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
            ConfigureActionLabel(_buyLuckyBoxText, Localizations.I18n.StreetPickKey.ToPlainText());
        }

        private static void EnsureButtons(StockShopView view, StockShop target)
        {
            if (_refreshButton != null || _storePickButton != null || _streetPickButton != null) return;
            if (_refreshStockText == null || _pickOneText == null || _buyLuckyBoxText == null) return;

            _refreshButton = _refreshStockText.gameObject.AddComponent<Button>();
            ConfigureActionButton(_refreshButton, _refreshStockText);

            _storePickButton = _pickOneText.gameObject.AddComponent<Button>();
            ConfigureActionButton(_storePickButton, _pickOneText);

            _streetPickButton = _buyLuckyBoxText.gameObject.AddComponent<Button>();
            ConfigureActionButton(_streetPickButton, _buyLuckyBoxText);

            _refreshButton.onClick.AddListener(() => OnRefreshButtonClicked(target));
            _storePickButton.onClick.AddListener(() => OnStorePickButtonClicked(target).Forget());
            _streetPickButton.onClick.AddListener(() => OnStreetPickButtonClicked().Forget());
        }

        private static void UpdateButtonTexts()
        {
            if (_refreshStockText == null || _pickOneText == null || _buyLuckyBoxText == null) return;

            long refreshPrice = Core.Settings.Settings.Instance.RefreshStockPrice.Value as long? ?? DefaultSettings.RefreshStockPrice;
            long storePickPrice = Core.Settings.Settings.Instance.StorePickPrice.Value as long? ?? DefaultSettings.StorePickPrice;
            long streetPickPrice = Core.Settings.Settings.Instance.StreetPickPrice.Value as long? ?? DefaultSettings.StreetPickPrice;

            var baseRefreshText = Localizations.I18n.RefreshStockKey.ToPlainText();
            var baseStorePickText = Localizations.I18n.StorePickKey.ToPlainText();
            var baseStreetPickText = Localizations.I18n.StreetPickKey.ToPlainText();
            var freeText = Localizations.I18n.FreeKey.ToPlainText();

            _refreshStockText.text = refreshPrice > 0 ? $"{baseRefreshText} (${refreshPrice})" : $"{baseRefreshText} ({freeText})";
            _pickOneText.text = storePickPrice > 0 ? $"{baseStorePickText} (${storePickPrice})" : $"{baseStorePickText} ({freeText})";
            _buyLuckyBoxText.text = streetPickPrice > 0 ? $"{baseStreetPickText} (${streetPickPrice})" : $"{baseStreetPickText} ({freeText})";
        }

        private static void SubscribeToPriceChanges()
        {
            if (_priceChangeSubscribed) return;

            var settings = Core.Settings.Settings.Instance;

            settings.RefreshStockPrice.OnValueChanged += _ => UpdateButtonTexts();
            settings.StorePickPrice.OnValueChanged += _ => UpdateButtonTexts();
            settings.StreetPickPrice.OnValueChanged += _ => UpdateButtonTexts();

            _priceChangeSubscribed = true;
            Log.Debug("Subscribed to price change events");
        }

        private static async UniTask OnStreetPickButtonClicked()
        {
            Log.Debug("Street pick button clicked");

            // Get price from settings and try to pay
            long price = Core.Settings.Settings.Instance.StreetPickPrice.Value as long? ?? DefaultSettings.StreetPickPrice;

            // Skip payment if price is zero
            if (price > 0 && !Pay(price))
            {
                Log.Warning($"Failed to pay {price} for street pick");
                var notEnoughMoneyMessage = Localizations.I18n.NotEnoughMoneyFormatKey.ToPlainText().Replace("{price}", price.ToString());
                NotificationText.Push(notEnoughMoneyMessage);
                return;
            }

            AudioManager.Post(SFX_BUY);

            // Use LotteryService to pick a random item
            var selectedItemTypeId = LotteryService.PickRandomItem(LotteryService.ItemTypeIdsCache);
            if (selectedItemTypeId < 0)
            {
                Log.Error("Failed to pick item for street lottery");
                return;
            }

            Item? obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
            if (obj == null)
            {
                Log.Error($"Failed to instantiate lucky box item: {selectedItemTypeId}");
                return;
            }

            // Play animation
            await LotteryAnimation.PlayAsync(LotteryService.ItemTypeIdsCache, selectedItemTypeId, obj.DisplayName, obj.Icon);

            var isSentToStorage = true;
            if (!ItemUtilities.SendToPlayerCharacterInventory(obj))
            {
                Log.Warning($"Failed to send item to player inventory: {selectedItemTypeId}. Send to the player storage.");
                ItemUtilities.SendToPlayerStorage(obj);
                isSentToStorage = true;
            }
            else
            {
                isSentToStorage = false;
            }

            var messageTemplate = Localizations.I18n.PickNotificationFormatKey.ToPlainText();
            var message = messageTemplate.Replace("{itemDisplayName}", obj.DisplayName);

            if (isSentToStorage)
            {
                var inventoryFullMessage = Localizations.I18n.InventoryFullAndSendToStorageKey.ToPlainText();
                message = $"{message}({inventoryFullMessage})";
            }

            NotificationText.Push(message);
            AudioManager.Post(SFX_BUY);
        }

        private static void OnRefreshButtonClicked(StockShop stockShop)
        {
            Log.Debug("Refresh button clicked");
            if (stockShop == null) return;

            // Get price from settings and try to pay
            long price = Core.Settings.Settings.Instance.RefreshStockPrice.Value as long? ?? DefaultSettings.RefreshStockPrice;

            // Skip payment if price is zero
            if (price > 0 && !Pay(price))
            {
                Log.Warning($"Failed to pay {price} for refresh stock");
                var notEnoughMoneyMessage = Localizations.I18n.NotEnoughMoneyFormatKey.ToPlainText().Replace("{price}", price.ToString());
                NotificationText.Push(notEnoughMoneyMessage);
                return;
            }

            if (!TryInvokeRefresh(stockShop)) return;
            AudioManager.Post(SFX_BUY);
            Log.Debug("Stock refreshed");
        }


        private static async UniTask<bool> OnStorePickButtonClicked(StockShop stockShop)
        {
            Log.Debug("Store pick button clicked");
            if (stockShop == null) return false;

            if (stockShop.Busy) return false;

            var itemEntries = stockShop.entries.Where(entry =>
              entry != null &&
              entry.CurrentStock > 0 &&
              entry.Possibility > 0f &&
              entry.Show).ToList();

            // If the length of itemEntries is too short, we randomly duplicate entries to ensure enough variety in the lottery animation
            const int MIN_ANIMATION_SLOTS = 150;
            while (itemEntries.Count < MIN_ANIMATION_SLOTS)
            {
                var randomEntry = itemEntries[UnityEngine.Random.Range(0, itemEntries.Count)];
                itemEntries.Add(randomEntry);
            }

            if (itemEntries.Count == 0)
            {
                Log.Warning("No available items to pick");
                return false;
            }

            // Get price from settings and try to pay
            long price = Core.Settings.Settings.Instance.StorePickPrice.Value as long? ?? DefaultSettings.StorePickPrice;

            // Skip payment if price is zero
            if (price > 0 && !Pay(price))
            {
                Log.Warning($"Failed to pay {price} for store pick");
                var notEnoughMoneyMessage = Localizations.I18n.NotEnoughMoneyFormatKey.ToPlainText().Replace("{price}", price.ToString());
                NotificationText.Push(notEnoughMoneyMessage);
                return false;
            }

            var randomIndex = UnityEngine.Random.Range(0, itemEntries.Count);
            var pickedItem = itemEntries[randomIndex];

            if (!SetBuyingState(stockShop, true)) return false;

            var candidateTypeIds = itemEntries.Select(entry => entry.ItemTypeID).ToList();
            var success = false;

            try
            {
                Item? obj = await ItemAssetsCollection.InstantiateAsync(pickedItem.ItemTypeID);
                if (obj == null)
                {
                    Log.Error("Failed to instantiate picked item for " + pickedItem.ItemTypeID);
                    return false;
                }

                // Play animation using new service
                await LotteryAnimation.PlayAsync(candidateTypeIds, pickedItem.ItemTypeID, obj.DisplayName, obj.Icon);

                var isSentToStorage = false;
                if (!ItemUtilities.SendToPlayerCharacterInventory(obj))
                {
                    Log.Warning($"Failed to send item to player inventory: {pickedItem.ItemTypeID}. Send to the player storage.");
                    ItemUtilities.SendToPlayerStorage(obj);
                    isSentToStorage = true;
                }

                pickedItem.CurrentStock = Math.Max(0, pickedItem.CurrentStock - 1);

                var onAfterItemSoldField = AccessTools.Field(typeof(StockShop), "OnAfterItemSold");
                if (onAfterItemSoldField?.GetValue(null) is Action<StockShop> onAfterItemSold)
                    onAfterItemSold(stockShop);
                var onItemPurchasedField = AccessTools.Field(typeof(StockShop), "OnItemPurchased");
                if (onItemPurchasedField?.GetValue(null) is Action<StockShop, Item> onItemPurchased)
                    onItemPurchased(stockShop, obj);

                var messageTemplate = Localizations.I18n.PickNotificationFormatKey.ToPlainText();
                var message = messageTemplate.Replace("{itemDisplayName}", obj.DisplayName);

                if (isSentToStorage)
                {
                    var inventoryFullMessage = Localizations.I18n.InventoryFullAndSendToStorageKey.ToPlainText();
                    message = $"{message}({inventoryFullMessage})";
                }

                NotificationText.Push(message);
                AudioManager.Post(SFX_BUY);

                success = true;
            }
            finally
            {
                SetBuyingState(stockShop, false);
            }

            return success;
        }

        private static bool SetBuyingState(StockShop? stockShop, bool buying)
        {
            if (stockShop == null) return false;

            AccessTools.Field(typeof(StockShop), "buying").SetValue(stockShop, buying);
            return true;
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

        private static bool Pay(long money)
        {
            return EconomyManager.Pay(
                new Cost(money), true, true
            );
        }
    }
}
