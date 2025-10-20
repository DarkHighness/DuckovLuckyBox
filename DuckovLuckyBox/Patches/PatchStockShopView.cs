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

namespace DuckovLuckyBox.Patches
{

    [HarmonyPatch(typeof(StockShopView), "Setup")]
    public class PatchStockShopView
    {
        private static TextMeshProUGUI? _refreshStockText;
        private static Button? _refreshButton;
        private static TextMeshProUGUI? _pickOneText;
        private static Button? _storePickButton;
        private static TextMeshProUGUI? _buyLuckyBoxText;
        private static Button? _streetPickButton;
        private static RectTransform? _actionsContainer;
        private static RectTransform? _luckyRollOverlay;
        private static RectTransform? _luckyRollViewport;
        private static RectTransform? _luckyRollItemsContainer;
        private static Image? _luckyRollPointer;
        private static TextMeshProUGUI? _luckyRollResultText;
        private static CanvasGroup? _luckyRollCanvasGroup;
        private static Sprite? _fallbackIconSprite;
        private static bool _isAnimating;
        private static readonly string SFX_BUY = "UI/buy";
        private const float AnchorOffsetExtraPadding = 40f;
        private const float ActionsContainerFallbackWidth = 320f;
        private const float ActionsContainerHeight = 48f;
        private const float ActionsLayoutSpacing = 16f;
        private const int ActionsLayoutPaddingHorizontal = 12;
        private const int ActionsLayoutPaddingTop = 8;
        private const int ActionsLayoutPaddingBottom = 8;
        private const float ActionLabelPreferredHeight = 40f;
        private const float ActionLabelMinWidth = 140f;
        private const float ActionLabelExtraWidth = 24f;
        private const float ActionLabelMinFontSize = 18f;
        private const float ActionLabelFontScale = 0.9f;
        private static readonly Vector2 LuckyRollIconSize = new Vector2(128f, 128f);
        private const float LuckyRollItemSpacing = 24f;
        private const float LuckyRollSlotPadding = 32f;
        private static readonly float LuckyRollSlotFullWidth = LuckyRollIconSize.x + LuckyRollSlotPadding + LuckyRollItemSpacing;
        private const int LuckyRollMinimumSlots = 100;
        private const float LuckyRollAnimationDuration = 5.0f;
        private const float LuckyRollFadeDuration = 0.25f;
        private const float LuckyRollCelebrateDuration = 0.4f;
        private const float LuckyRollCelebrateScale = 1.1f;
        private const float LuckyRollPointerThickness = 12f;
        private static readonly AnimationCurve LuckyRollAnimationCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 3f, 3f),
            new Keyframe(0.3f, 0.6f, 2f, 2f),
            new Keyframe(0.7f, 0.9f, 0.5f, 0.5f),
            new Keyframe(1f, 1f, 0f, 0f));
        private static readonly Color ActionButtonNormalColor = new Color(1f, 1f, 1f, 0.8f);
        private static readonly Color ActionButtonHighlightedColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color ActionButtonPressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color ActionButtonDisabledColor = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color LuckyRollOverlayColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color LuckyRollPointerColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color LuckyRollFinalFrameColor = new Color(0.95f, 0.8f, 0.35f, 1f);
        private static readonly Color LuckyRollSlotFrameColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly AnimationCurve LuckyRollSpinCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 1.5f),
            new Keyframe(0.15f, 0.1f, 1.8f, 1.8f),
            new Keyframe(0.4f, 0.5f, 2.0f, 2.0f),
            new Keyframe(0.7f, 0.85f, 0.8f, 0.8f),
            new Keyframe(1f, 1f, 0f, 0f));
        private static readonly AnimationCurve LuckyRollPreviewCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(0.6f, 0.7f, 1.4f, 1.4f),
            new Keyframe(1f, 1f, 0f, 0f));
        private static List<int>? _itemTypeIdsCache = null;
        public static List<int> ItemTypeIdsCache
        {
            get
            {
                if (_itemTypeIdsCache == null)
                {
                    _itemTypeIdsCache = ItemAssetsCollection.Instance.entries
                    // We predictably exclude items whose display name is in the form of "*Item_*"
                    // to avoid illegal items.
                    .Where(entry => !entry.prefab.DisplayName.StartsWith("*Item_"))
                    .Select(entry => entry.typeID)
                    .ToList();
                }
                return _itemTypeIdsCache;
            }
        }


        public static void Postfix(StockShopView __instance)
        {
            var merchantNameText = GetMerchantNameText(__instance);
            if (merchantNameText == null) return;

            EnsureTexts(merchantNameText);
            EnsureButtons(__instance);
        }

        private static TextMeshProUGUI? GetMerchantNameText(StockShopView instance)
        {
            var field = typeof(StockShopView).GetField("merchantNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                Log.Error("Failed to find merchantNameText field in StockShopView");
                return null;
            }

            var merchantNameText = field.GetValue(instance) as TextMeshProUGUI;
            if (merchantNameText == null)
            {
                Log.Error("Failed to get merchantNameText from StockShopView");
            }

            return merchantNameText;
        }

        private static void EnsureTexts(TextMeshProUGUI merchantNameText)
        {
            if (_refreshStockText != null || _pickOneText != null) return;

            if (_actionsContainer == null)
            {
                var parent = merchantNameText.transform.parent as RectTransform;
                if (parent == null) return;

                _actionsContainer = new GameObject("ExtraActionsContainer", typeof(RectTransform)).GetComponent<RectTransform>();
                _actionsContainer.SetParent(parent, false);

                float anchorOffset = merchantNameText.rectTransform.rect.height;
                if (anchorOffset <= 0f) anchorOffset = merchantNameText.fontSize + AnchorOffsetExtraPadding;
                _actionsContainer.anchorMin = new Vector2(0.5f, 1f);
                _actionsContainer.anchorMax = new Vector2(0.5f, 1f);
                _actionsContainer.pivot = new Vector2(0.5f, 1f);
                _actionsContainer.anchoredPosition = new Vector2(0f, -anchorOffset);

                float width = merchantNameText.rectTransform.rect.width;
                if (width <= 0f) width = ActionsContainerFallbackWidth;
                _actionsContainer.sizeDelta = new Vector2(width, ActionsContainerHeight);

                var layout = _actionsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = false;
                layout.spacing = ActionsLayoutSpacing;
                layout.padding = CreateActionsPadding();
            }

            EnsureLuckyRollUI(merchantNameText);

            _refreshStockText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
            ConfigureActionLabel(_refreshStockText, Constants.I18n.RefreshStockKey.ToPlainText());

            _pickOneText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
            ConfigureActionLabel(_pickOneText, Constants.I18n.StorePickKey.ToPlainText());

            _buyLuckyBoxText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
            ConfigureActionLabel(_buyLuckyBoxText, Constants.I18n.StreetPickKey.ToPlainText());
        }

        private static void EnsureButtons(StockShopView view)
        {
            if (_refreshButton != null || _storePickButton != null || _streetPickButton != null) return;
            if (_refreshStockText == null || _pickOneText == null || _buyLuckyBoxText == null) return;

            _refreshButton = _refreshStockText.gameObject.AddComponent<Button>();
            ConfigureActionButton(_refreshButton, _refreshStockText);

            _storePickButton = _pickOneText.gameObject.AddComponent<Button>();
            ConfigureActionButton(_storePickButton, _pickOneText);

            _streetPickButton = _buyLuckyBoxText.gameObject.AddComponent<Button>();
            ConfigureActionButton(_streetPickButton, _buyLuckyBoxText);

            _refreshButton.onClick.AddListener(() => OnRefreshButtonClicked(view));
            _storePickButton.onClick.AddListener(() => OnStorePickButtonClicked(view).Forget());
            _streetPickButton.onClick.AddListener(() => OnStreetPickButtonClicked(view).Forget());
        }

        private static async UniTask OnStreetPickButtonClicked(StockShopView stockShopView)
        {
            Log.Debug("Street pick button clicked");
            if (_isAnimating) return;

            var selectedIndex = UnityEngine.Random.Range(0, ItemTypeIdsCache.Count);
            var selectedItemTypeId = ItemTypeIdsCache[selectedIndex];
            Item obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
            if (obj == null)
            {
                Log.Error($"Failed to instantiate lucky box item: {selectedItemTypeId}");
                return;
            }

            await PlayLuckyBoxAnimation(ItemTypeIdsCache, selectedItemTypeId, obj.DisplayName, obj.Icon);

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

            var messageTemplate = Constants.I18n.PickNotificationFormatKey.ToPlainText();
            var message = messageTemplate.Replace("{itemDisplayName}", obj.DisplayName);

            if (isSentToStorage)
            {
                var inventoryFullMessage = Constants.I18n.InventoryFullAndSendToStorageKey.ToPlainText();
                message = $"{message}({inventoryFullMessage})";
            }

            NotificationText.Push(message);
            AudioManager.Post(SFX_BUY);
        }

        private static void OnRefreshButtonClicked(StockShopView stockShopView)
        {
            Log.Debug("Refresh button clicked");
            if (!TryGetStockShop(stockShopView, out var stockShop)) return;
            if (!TryInvokeRefresh(stockShop)) return;
            AudioManager.Post(SFX_BUY);
            Log.Debug("Stock refreshed");
        }


        private static async UniTask<bool> OnStorePickButtonClicked(StockShopView stockShopView)
        {
            Log.Debug("Store pick button clicked");
            if (_isAnimating) return false;
            if (!TryGetStockShop(stockShopView, out var stockShop)) return false;
            if (stockShop == null) return false;

            if (stockShop.Busy) return false;

            var itemEntries = stockShop.entries.Where(entry =>
              entry != null &&
              entry.CurrentStock > 0 &&
              entry.Possibility > 0f &&
              entry.Show).ToList();

            if (itemEntries.Count == 0)
            {
                Log.Warning("No available items to pick");
                return false;
            }

            var randomIndex = UnityEngine.Random.Range(0, itemEntries.Count);
            var pickedItem = itemEntries[randomIndex];

            if (!SetBuyingState(stockShop, true)) return false;

            var candidateTypeIds = itemEntries.Select(entry => entry.ItemTypeID).ToList();
            var success = false;

            try
            {
                Item obj = await ItemAssetsCollection.InstantiateAsync(pickedItem.ItemTypeID);
                if (obj == null)
                {
                    Log.Error("Failed to instantiate picked item for " + pickedItem.ItemTypeID);
                    return false;
                }

                await PlayLuckyBoxAnimation(candidateTypeIds, pickedItem.ItemTypeID, obj.DisplayName, obj.Icon);

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

                var messageTemplate = Constants.I18n.PickNotificationFormatKey.ToPlainText();
                var message = messageTemplate.Replace("{itemDisplayName}", obj.DisplayName);

                if (isSentToStorage)
                {
                    var inventoryFullMessage = Constants.I18n.InventoryFullAndSendToStorageKey.ToPlainText();
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

        private static bool TryGetStockShop(StockShopView view, out StockShop? stockShop)
        {

            stockShop = AccessTools.Field(typeof(StockShopView), "target").GetValue(view) as StockShop;
            return stockShop != null;
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

        private static void EnsureLuckyRollUI(TextMeshProUGUI merchantNameText)
        {
            if (_luckyRollOverlay != null) return;

            var canvas = merchantNameText.canvas;
            if (canvas == null) return;

            // Create full-screen overlay
            _luckyRollOverlay = new GameObject("LuckyRollOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)).GetComponent<RectTransform>();
            _luckyRollOverlay.SetParent(canvas.transform, false);
            _luckyRollOverlay.anchorMin = Vector2.zero;
            _luckyRollOverlay.anchorMax = Vector2.one;
            _luckyRollOverlay.offsetMin = Vector2.zero;
            _luckyRollOverlay.offsetMax = Vector2.zero;
            _luckyRollOverlay.gameObject.SetActive(false);

            _luckyRollCanvasGroup = _luckyRollOverlay.GetComponent<CanvasGroup>();
            _luckyRollCanvasGroup.alpha = 0f;
            _luckyRollCanvasGroup.blocksRaycasts = true;
            _luckyRollCanvasGroup.interactable = true;

            var overlayImage = _luckyRollOverlay.GetComponent<Image>();
            overlayImage.color = LuckyRollOverlayColor;
            overlayImage.raycastTarget = true;

            // Create viewport in center of screen
            var canvasRect = canvas.GetComponent<RectTransform>();
            var canvasSize = canvasRect.rect.size;
            var viewportHeight = Mathf.Min(200f, canvasSize.y * 0.3f);
            var viewportWidth = canvasSize.x * 0.8f;

            _luckyRollViewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            _luckyRollViewport.SetParent(_luckyRollOverlay, false);
            _luckyRollViewport.anchorMin = new Vector2(0.5f, 0.5f);
            _luckyRollViewport.anchorMax = new Vector2(0.5f, 0.5f);
            _luckyRollViewport.pivot = new Vector2(0.5f, 0.5f);
            _luckyRollViewport.sizeDelta = new Vector2(viewportWidth, viewportHeight);

            _luckyRollItemsContainer = new GameObject("Items", typeof(RectTransform)).GetComponent<RectTransform>();
            _luckyRollItemsContainer.SetParent(_luckyRollViewport, false);
            _luckyRollItemsContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _luckyRollItemsContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _luckyRollItemsContainer.pivot = new Vector2(0.5f, 0.5f);

            // Create center pointer
            _luckyRollPointer = new GameObject("Pointer", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _luckyRollPointer.rectTransform.SetParent(_luckyRollOverlay, false);
            _luckyRollPointer.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _luckyRollPointer.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _luckyRollPointer.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _luckyRollPointer.rectTransform.sizeDelta = new Vector2(LuckyRollPointerThickness, viewportHeight + 64f);
            _luckyRollPointer.rectTransform.anchoredPosition = Vector2.zero;
            _luckyRollPointer.sprite = EnsureFallbackSprite();
            _luckyRollPointer.type = Image.Type.Simple;
            _luckyRollPointer.color = LuckyRollPointerColor;
            _luckyRollPointer.raycastTarget = false;

            // Create result text
            _luckyRollResultText = UnityEngine.Object.Instantiate(merchantNameText, _luckyRollOverlay);
            _luckyRollResultText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _luckyRollResultText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _luckyRollResultText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _luckyRollResultText.rectTransform.anchoredPosition = new Vector2(0f, -viewportHeight * 0.75f);
            _luckyRollResultText.enableAutoSizing = false;
            _luckyRollResultText.fontSize = Mathf.Max(26f, merchantNameText.fontSize * 0.9f);
            _luckyRollResultText.alignment = TextAlignmentOptions.Center;
            _luckyRollResultText.raycastTarget = false;
            ResetLuckyRollResultText();
        }

        private static async UniTask PlayLuckyBoxAnimation(IEnumerable<int> candidateTypeIds, int finalTypeId, string finalDisplayName, Sprite? finalIcon)
        {
            // Check if animation is enabled in settings
            if (!(Core.Settings.Settings.Instance.EnableAnimation.Value is bool enabled && enabled))
            {
                Log.Debug("Lucky roll animation is disabled. Skipping animation.");
                return;
            }

            if (_luckyRollOverlay == null || _luckyRollItemsContainer == null || _luckyRollPointer == null || _luckyRollCanvasGroup == null)
            {
                Log.Warning("Lucky roll overlay is not ready.");
                return;
            }

            var overlay = _luckyRollOverlay;
            var itemsContainer = _luckyRollItemsContainer;
            var canvasGroup = _luckyRollCanvasGroup;

            if (_isAnimating) return;

            if (!TryBuildAnimationPlan(candidateTypeIds, finalTypeId, finalIcon, out var plan))
            {
                Log.Warning("Failed to prepare lucky roll animation plan.");
                return;
            }

            _isAnimating = true;

            try
            {
                overlay.gameObject.SetActive(true);
                canvasGroup.blocksRaycasts = true;

                // Show Pointer at the start of animation
                if (_luckyRollPointer != null)
                {
                    var pointerColor = _luckyRollPointer.color;
                    pointerColor.a = 0.95f;
                    _luckyRollPointer.color = pointerColor;
                }

                ResetLuckyRollResultText();

                // Fade in
                await FadeCanvasGroup(canvasGroup, 0f, 1f, LuckyRollFadeDuration);

                // Single continuous roll animation
                await PerformContinuousRoll(plan, LuckyRollAnimationDuration, LuckyRollAnimationCurve);

                // Celebration on final slot
                await AnimateLuckyRollCelebration(plan);

                // Reveal result and hold
                await RevealLuckyRollResult(finalDisplayName);

                // Fade out
                await FadeCanvasGroup(canvasGroup, 1f, 0f, LuckyRollFadeDuration);
            }
            finally
            {
                _isAnimating = false;

                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                overlay.gameObject.SetActive(false);

                // Hide Pointer after animation ends
                if (_luckyRollPointer != null)
                {
                    var pointerColor = _luckyRollPointer.color;
                    pointerColor.a = 0f;
                    _luckyRollPointer.color = pointerColor;
                }

                ResetLuckyRollResultText();
                ClearLuckyRollItems();
                _luckyRollItemsContainer.anchoredPosition = Vector2.zero;
            }
        }

        private static bool TryBuildAnimationPlan(IEnumerable<int> candidateTypeIds, int finalTypeId, Sprite? finalIcon, out LuckyRollAnimationPlan plan)
        {
            plan = default!;

            if (_luckyRollItemsContainer == null) return false;
            if (_luckyRollViewport == null) return false;

            var baseSequenceData = BuildLuckyRollSequence(candidateTypeIds, finalTypeId);
            var baseSequence = baseSequenceData.Slots;
            if (baseSequence.Count == 0)
            {
                return false;
            }

            ClearLuckyRollItems();

            var slotWidth = LuckyRollSlotFullWidth;
            var viewportWidth = _luckyRollViewport.rect.width;
            var viewportHalfWidth = viewportWidth * 0.5f;

            // ===== SIMPLIFIED SCROLLING APPROACH =====
            //
            // The base sequence has ~100 random items + 1 final item
            // We need to create a scrolling animation where:
            // 1. Initial state shows items filling the viewport
            // 2. Items scroll from left to right smoothly
            // 3. Final item stops at center WITH items visible after it

            var displaySequence = new List<int>(baseSequence);

            // Find the final item index in the base sequence
            int baselineFinalItemIndex = -1;
            for (int i = 0; i < displaySequence.Count; i++)
            {
                if (displaySequence[i] == finalTypeId)
                {
                    baselineFinalItemIndex = i;
                    break;
                }
            }

            if (baselineFinalItemIndex < 0)
            {
                // Should not happen, but just in case
                baselineFinalItemIndex = displaySequence.Count - 1;
            }

            // Add some items after the final item so there are visible items after it
            // Use the same random pool to generate them
            // If pool size is small, items will be repeated as needed
            int itemsAfterFinal = Mathf.CeilToInt(viewportWidth / slotWidth);
            var random = new System.Random();
            int? lastId = finalTypeId;
            for (int i = 0; i < itemsAfterFinal; i++)
            {
                var pool = candidateTypeIds?.Distinct().Where(id => id > 0).ToList() ?? new List<int>();
                if (pool.Count == 0) pool.Add(finalTypeId);

                int pick;
                if (pool.Count == 1)
                {
                    pick = pool[0];
                }
                else
                {
                    do
                    {
                        pick = pool[random.Next(pool.Count)];
                    } while (lastId.HasValue && pool.Count > 1 && pick == lastId.Value);
                }
                displaySequence.Add(pick);
                lastId = pick;
            }

            var totalWidth = displaySequence.Count * slotWidth;
            _luckyRollItemsContainer.sizeDelta = new Vector2(totalWidth, 200f);

            var slots = new List<LuckyRollSlot>(displaySequence.Count);
            int finalItemIndex = baselineFinalItemIndex;

            // Create UI slots for each item in the sequence
            for (int i = 0; i < displaySequence.Count; i++)
            {
                var typeId = displaySequence[i];
                var isFinal = (i == finalItemIndex); // Only the first occurrence at baselineFinalItemIndex is the target

                var sprite = isFinal ? (finalIcon ?? GetItemIcon(typeId)) : GetItemIcon(typeId);
                var slot = CreateLuckyRollSlot(typeId, sprite, GetDisplayNameForType(typeId), GetItemQualityColor(typeId));

                // Position: container is centered, so position is offset from center
                var positionX = (float)i * slotWidth - totalWidth * 0.5f + slotWidth * 0.5f;
                slot.Rect.anchoredPosition = new Vector2(positionX, 0f);

                slots.Add(slot);
            }

            // ===== CALCULATE OFFSETS =====
            //
            // Container's anchoredPosition is the offset that controls viewport visibility
            //
            // With both container and viewport pivoted at center:
            // - offset = 0: viewport shows items around container center
            // - offset > 0: container moves right, viewport sees earlier items
            // - offset < 0: container moves left, viewport sees later items
            //
            // For left-to-right scrolling animation:
            // - startOffset should be positive (to show early items)
            // - endOffset should be negative (to show final item near end)
            // - As offset decreases, viewport scrolls right (new items enter from left)

            var finalItemPos = slots[finalItemIndex].Rect.anchoredPosition.x;

            // End state: final item at viewport center (where the pointer is)
            // We need offset such that viewport center (0) sees finalItemPos
            // offset = -finalItemPos achieves this
            // With items added after the final item, they will be visible to the right
            var endOffset = -finalItemPos;

            // Start state: show early items filling viewport from left to right
            // We want the first item (or items near start) visible at left edge
            var firstItemPos = slots[0].Rect.anchoredPosition.x;

            // To place first item at left viewport edge:
            // We need: offset + firstItemPos = -viewportHalfWidth
            // offset = -viewportHalfWidth - firstItemPos
            var startOffset = -viewportHalfWidth - firstItemPos;

            plan = new LuckyRollAnimationPlan(slots, finalItemIndex, startOffset, endOffset);
            return true;
        }

        private static async UniTask PerformContinuousRoll(LuckyRollAnimationPlan plan, float duration, AnimationCurve curve)
        {
            if (_luckyRollItemsContainer == null) return;

            // Initialize to start position
            _luckyRollItemsContainer.anchoredPosition = new Vector2(plan.StartOffset, 0f);

            var elapsed = 0f;
            while (elapsed < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / duration);
                var progress = curve?.Evaluate(t) ?? t;

                // Smooth interpolation from start to end position
                var currentOffset = Mathf.Lerp(plan.StartOffset, plan.FinalOffset, progress);
                _luckyRollItemsContainer.anchoredPosition = new Vector2(currentOffset, 0f);
            }

            // Ensure final position is set precisely
            _luckyRollItemsContainer.anchoredPosition = new Vector2(plan.FinalOffset, 0f);
        }

        private static async UniTask AnimateLuckyRollCelebration(LuckyRollAnimationPlan plan)
        {
            var slot = plan.FinalSlot;
            var frame = slot.Frame;
            var iconTransform = slot.Icon.rectTransform;

            var initialColor = LuckyRollSlotFrameColor;
            var targetColor = LuckyRollFinalFrameColor;
            var initialScale = Vector3.one;
            var targetScale = Vector3.one * LuckyRollCelebrateScale;

            frame.color = initialColor;
            iconTransform.localScale = initialScale;

            // Record initial font size and scale of result text
            var initialTextFontSize = _luckyRollResultText?.fontSize ?? 24f;
            var targetTextFontSize = initialTextFontSize * 1.1f;
            var resultTextRectTransform = _luckyRollResultText?.rectTransform;
            var initialTextScale = resultTextRectTransform?.localScale ?? Vector3.one;
            var targetTextScale = initialTextScale * 1.1f;

            var elapsed = 0f;
            while (elapsed < LuckyRollCelebrateDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / LuckyRollCelebrateDuration);
                t = Mathf.SmoothStep(0f, 1f, t);
                frame.color = Color.Lerp(initialColor, targetColor, t);
                iconTransform.localScale = Vector3.Lerp(initialScale, targetScale, t);

                // Scale up result text
                if (_luckyRollResultText != null)
                {
                    _luckyRollResultText.fontSize = Mathf.Lerp(initialTextFontSize, targetTextFontSize, t);
                    if (resultTextRectTransform != null)
                    {
                        resultTextRectTransform.localScale = Vector3.Lerp(initialTextScale, targetTextScale, t);
                    }
                }
            }

            frame.color = targetColor;
            iconTransform.localScale = targetScale;

            // Ensure text reaches target size
            if (_luckyRollResultText != null)
            {
                _luckyRollResultText.fontSize = (int)targetTextFontSize;
                if (resultTextRectTransform != null)
                {
                    resultTextRectTransform.localScale = targetTextScale;
                }
            }
        }

        private static void ResetLuckyRollResultText()
        {
            if (_luckyRollResultText == null) return;

            var textColor = _luckyRollResultText.color;
            textColor.a = 0f;
            _luckyRollResultText.color = textColor;
            _luckyRollResultText.text = string.Empty;
        }

        private static async UniTask RevealLuckyRollResult(string finalDisplayName)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.DeltaTime, PlayerLoopTiming.Update, default);

            if (_luckyRollResultText != null)
            {
                _luckyRollResultText.text = finalDisplayName;
                var textColor = _luckyRollResultText.color;
                textColor.a = 1f;
                _luckyRollResultText.color = textColor;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.75f), DelayType.DeltaTime, PlayerLoopTiming.Update, default);
        }

        private static Sprite EnsureFallbackSprite()
        {
            if (_fallbackIconSprite != null) return _fallbackIconSprite;

            var texture = Texture2D.whiteTexture;
            _fallbackIconSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return _fallbackIconSprite;
        }

        private static LuckyRollSequenceData BuildLuckyRollSequence(IEnumerable<int> candidateTypeIds, int finalTypeId)
        {
            var pool = candidateTypeIds?.Distinct().Where(id => id > 0).ToList() ?? new List<int>();
            if (!pool.Contains(finalTypeId)) pool.Add(finalTypeId);
            if (pool.Count == 0) pool.Add(finalTypeId);

            var random = new System.Random();
            int SampleNext(int? previous)
            {
                if (pool.Count == 1) return pool[0];
                int pick;
                do
                {
                    pick = pool[random.Next(pool.Count)];
                } while (previous.HasValue && pool.Count > 1 && pick == previous.Value);
                return pick;
            }

            var sequence = new List<int>();

            // Build sequence: many random items, then the final item
            // Items are sampled from the pool with repetition allowed
            // If pool is smaller than required slots, items will be repeated
            int? lastId = null;
            while (sequence.Count < LuckyRollMinimumSlots - 1)
            {
                var id = SampleNext(lastId);
                sequence.Add(id);
                lastId = id;
            }

            var finalIndex = sequence.Count;
            sequence.Add(finalTypeId);

            return new LuckyRollSequenceData(sequence, finalIndex, 0);
        }

        private static LuckyRollSlot CreateLuckyRollSlot(int typeId, Sprite sprite, string displayName, Color frameColor)
        {
            var root = new GameObject($"LuckyItem_{typeId}", typeof(RectTransform), typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(_luckyRollItemsContainer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(LuckyRollIconSize.x + LuckyRollSlotPadding, LuckyRollIconSize.y + LuckyRollSlotPadding);


            var frame = root.GetComponent<Image>();
            frame.sprite = EnsureFallbackSprite();
            frame.type = Image.Type.Simple;
            frame.color = frameColor;
            frame.raycastTarget = false;
            // Add rounded corners to frame
            frame.useSpriteMesh = true;

            // Add mask for rounded corner effect
            var frameMask = root.AddComponent<Mask>();
            frameMask.showMaskGraphic = false;

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = LuckyRollIconSize;

            var icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite ?? EnsureFallbackSprite();
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            // Add rounded corners to icon
            icon.useSpriteMesh = true;
            iconObject.name = displayName;

            return new LuckyRollSlot(rect, frame, icon);
        }

        private static void ClearLuckyRollItems()
        {
            if (_luckyRollItemsContainer == null) return;

            for (int i = _luckyRollItemsContainer.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_luckyRollItemsContainer.GetChild(i).gameObject);
            }
        }

        private static Item getItem(int typeId)
        {
            var GetEntryMethod = AccessTools.Method(typeof(ItemAssetsCollection), "GetEntry") ?? throw new InvalidOperationException("Failed to find GetEntry method in ItemAssetsCollection");
            var entry = GetEntryMethod.Invoke(ItemAssetsCollection.Instance, new object[] { typeId }) as ItemAssetsCollection.Entry ?? throw new InvalidOperationException($"Failed to get item entry for typeId {typeId}");
            return entry.prefab;
        }

        private static Sprite GetItemIcon(int typeId)
        {
            var item = getItem(typeId);
            if (item == null) return EnsureFallbackSprite();
            var sprite = item.Icon;
            return sprite ?? EnsureFallbackSprite();
        }

        private static int GetItemQuality(int typeId)
        {
            var item = getItem(typeId);
            return item?.Quality ?? 0;
        }


        private static readonly Color[] ItemQualityColors = new Color[]
        {
            // Reference: https://github.com/shiquda/duckov-fancy-items/blob/7d3094cf40f1b01bbd08f0c8e05d341672b4fb33/fancy-items/Constants/FancyItemsConstants.cs#L56
            new Color(0.5f, 0.5f, 0.5f, 0.5f),      // Quality 0: 灰色
            new Color(0.9f, 0.9f, 0.9f, 0.24f),     // Quality 1: 浅白色
            new Color(0.6f, 0.9f, 0.6f, 0.24f),     // Quality 2: 柔和浅绿
            new Color(0.6f, 0.8f, 1.0f, 0.30f),     // Quality 3: 天蓝浅色
            new Color(1.0f, 0.50f, 1.0f, 0.40f),   // Quality 4: 亮浅紫（提亮，略粉）
            new Color(1.0f, 0.75f, 0.2f, 0.60f),   // Quality 5: 柔亮橙（更偏橙、更暖）
            new Color(1.0f, 0.3f, 0.3f, 0.4f),     // Quality 6+: 明亮红（亮度提升、透明度降低）
        };
        private static Color GetItemQualityColor(int typeId)
        {
            var quality = GetItemQuality(typeId);
            if (quality < 0 || quality >= ItemQualityColors.Length)
            {
                quality = 0;
            }
            return ItemQualityColors[quality];
        }

        private static async UniTask FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            group.alpha = from;
            if (Mathf.Approximately(duration, 0f))
            {
                group.alpha = to;
                await UniTask.Yield(PlayerLoopTiming.Update);
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(from, to, t);
            }

            group.alpha = to;
        }

        private readonly struct LuckyRollSequenceData
        {
            public LuckyRollSequenceData(List<int> slots, int finalIndex, int leadCount)
            {
                Slots = slots;
                FinalIndex = finalIndex;
                LeadCount = leadCount;
            }

            public List<int> Slots { get; }
            public int FinalIndex { get; }
            public int LeadCount { get; }
        }

        private readonly struct LuckyRollSlot
        {
            public RectTransform Rect { get; }
            public Image Frame { get; }
            public Image Icon { get; }

            public LuckyRollSlot(RectTransform rect, Image frame, Image icon)
            {
                Rect = rect;
                Frame = frame;
                Icon = icon;
            }
        }

        private static string GetDisplayNameForType(int typeId)
        {
            var entry = ItemAssetsCollection.Instance.entries.FirstOrDefault(e => e != null && e.typeID == typeId);
            return entry?.prefab?.DisplayName ?? $"#{typeId}";
        }

        private sealed class LuckyRollAnimationPlan
        {
            public LuckyRollAnimationPlan(List<LuckyRollSlot> slots, int finalSlotIndex, float startOffset, float finalOffset)
            {
                Slots = slots;
                FinalSlotIndex = finalSlotIndex;
                StartOffset = startOffset;
                FinalOffset = finalOffset;
            }

            public List<LuckyRollSlot> Slots { get; }
            public int FinalSlotIndex { get; }
            public float StartOffset { get; }
            public float FinalOffset { get; }

            public LuckyRollSlot FinalSlot
            {
                get
                {
                    var safeIndex = Mathf.Clamp(FinalSlotIndex, 0, Mathf.Max(0, Slots.Count - 1));
                    return Slots[safeIndex];
                }
            }
        }
    }
}