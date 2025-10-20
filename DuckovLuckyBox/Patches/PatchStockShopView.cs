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
        private const float LuckyRollDefaultWindowWidth = 960f;
        private const float LuckyRollDefaultWindowHeight = 180f;
        private static Vector2 _luckyRollWindowSize = new Vector2(LuckyRollDefaultWindowWidth, LuckyRollDefaultWindowHeight);
        private static float LuckyRollWindowWidth => _luckyRollWindowSize.x;
        private static float LuckyRollWindowHeight => _luckyRollWindowSize.y;
        private static readonly Vector2 LuckyRollIconSize = new Vector2(128f, 128f);
        private const float LuckyRollPointerThickness = 12f;
        private static Vector2 LuckyRollPointerSize => new Vector2(LuckyRollPointerThickness, LuckyRollWindowHeight + 64f);
        private const float LuckyRollItemSpacing = 24f;
        private const float LuckyRollSlotPadding = 32f;
        private static readonly float LuckyRollSlotFullWidth = LuckyRollIconSize.x + LuckyRollSlotPadding + LuckyRollItemSpacing;
        private const int LuckyRollLeadSlots = 8;
        private const int LuckyRollTrailSlots = 12;
        private const int LuckyRollBaseCycles = 5;
        private const int LuckyRollMinimumSlots = 32;
        private const float LuckyRollFadeDuration = 0.25f;
        private const float LuckyRollRevealHold = 0.75f;
        private const float LuckyRollSpinDuration = 4.2f;
        private const float LuckyRollTotalDuration = 4.4f;
        private const float LuckyRollPreviewHold = 0.3f;
        private const float LuckyRollPreviewTransitionDuration = 0.45f;
        private const float LuckyRollFollowSmoothTime = 0.12f;
        private const float LuckyRollCelebrateDuration = 0.4f;
        private const float LuckyRollCelebrateScale = 1.18f;
        private static readonly Color ActionButtonNormalColor = new Color(1f, 1f, 1f, 0.8f);
        private static readonly Color ActionButtonHighlightedColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color ActionButtonPressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color ActionButtonDisabledColor = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color LuckyRollOverlayColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color LuckyRollPointerColor = new Color(1f, 0.95f, 0.6f, 0.95f);
        private static readonly Color LuckyRollFinalFrameColor = new Color(0.95f, 0.8f, 0.35f, 1f);
        private static readonly Color LuckyRollSlotFrameColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly AnimationCurve LuckyRollSpinCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0.25f, 1.8f),
            new Keyframe(0.18f, 0.14f, 2.35f, 2.35f),
            new Keyframe(0.48f, 0.62f, 1.4f, 1.4f),
            new Keyframe(0.78f, 0.9f, 0.55f, 0.55f),
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
                Log.Error($"Failed to send item to player inventory: {selectedItemTypeId}. Send to the player storage.");
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
            if (!TryGetStockShop(stockShopView, out var stockShop)) return;
            if (!TryInvokeRefresh(stockShop)) return;
            AudioManager.Post(SFX_BUY);
            Log.Debug("Stock refreshed");
        }


        private static async UniTask<bool> OnStorePickButtonClicked(StockShopView stockShopView)
        {
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
                    Log.Error($"Failed to send item to player inventory: {pickedItem.ItemTypeID}. Send to the player storage.");
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

            _luckyRollViewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            _luckyRollViewport.SetParent(_luckyRollOverlay, false);
            _luckyRollViewport.anchorMin = new Vector2(0.5f, 0.5f);
            _luckyRollViewport.anchorMax = new Vector2(0.5f, 0.5f);
            _luckyRollViewport.pivot = new Vector2(0.5f, 0.5f);
            _luckyRollViewport.sizeDelta = new Vector2(LuckyRollWindowWidth, LuckyRollWindowHeight);

            _luckyRollItemsContainer = new GameObject("Items", typeof(RectTransform)).GetComponent<RectTransform>();
            _luckyRollItemsContainer.SetParent(_luckyRollViewport, false);
            _luckyRollItemsContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _luckyRollItemsContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _luckyRollItemsContainer.pivot = new Vector2(0.5f, 0.5f);

            _luckyRollPointer = new GameObject("Pointer", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _luckyRollPointer.rectTransform.SetParent(_luckyRollOverlay, false);
            _luckyRollPointer.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _luckyRollPointer.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _luckyRollPointer.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _luckyRollPointer.rectTransform.sizeDelta = LuckyRollPointerSize;
            _luckyRollPointer.rectTransform.anchoredPosition = Vector2.zero;
            _luckyRollPointer.sprite = EnsureFallbackSprite();
            _luckyRollPointer.type = Image.Type.Simple;
            _luckyRollPointer.color = LuckyRollPointerColor;
            _luckyRollPointer.raycastTarget = false;

            _luckyRollResultText = UnityEngine.Object.Instantiate(merchantNameText, _luckyRollOverlay);
            _luckyRollResultText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _luckyRollResultText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _luckyRollResultText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _luckyRollResultText.rectTransform.anchoredPosition = new Vector2(0f, -LuckyRollWindowHeight * 0.75f);
            _luckyRollResultText.enableAutoSizing = false;
            _luckyRollResultText.fontSize = Mathf.Max(26f, merchantNameText.fontSize * 0.9f);
            _luckyRollResultText.alignment = TextAlignmentOptions.Center;
            _luckyRollResultText.raycastTarget = false;
            ResetLuckyRollResultText();

            UpdateLuckyRollWindowSize(merchantNameText);
        }

        private static void UpdateLuckyRollWindowSize(TextMeshProUGUI merchantNameText)
        {
            if (merchantNameText == null) return;

            var references = new RectTransform?[]
            {
                merchantNameText.rectTransform.parent as RectTransform,
                merchantNameText.rectTransform,
                merchantNameText.canvas != null ? merchantNameText.canvas.transform as RectTransform : null
            };

            var width = LuckyRollDefaultWindowWidth;
            var height = LuckyRollDefaultWindowHeight;

            foreach (var rectTransform in references)
            {
                if (rectTransform == null) continue;
                var rect = rectTransform.rect;
                if (rect.width > 0f)
                {
                    width = rect.width;
                    break;
                }
            }

            foreach (var rectTransform in references)
            {
                if (rectTransform == null) continue;
                var rect = rectTransform.rect;
                if (rect.height > 0f)
                {
                    height = rect.height;
                    break;
                }
            }

            width = Mathf.Max(width, LuckyRollIconSize.x + LuckyRollSlotPadding);
            height = Mathf.Max(height, LuckyRollIconSize.y + LuckyRollSlotPadding);

            _luckyRollWindowSize = new Vector2(width, height);
            ApplyLuckyRollWindowSize();
        }

        private static void ApplyLuckyRollWindowSize()
        {
            if (_luckyRollViewport != null)
            {
                _luckyRollViewport.sizeDelta = _luckyRollWindowSize;
            }

            if (_luckyRollPointer != null)
            {
                _luckyRollPointer.rectTransform.sizeDelta = LuckyRollPointerSize;
            }

            if (_luckyRollResultText != null)
            {
                _luckyRollResultText.rectTransform.anchoredPosition = new Vector2(0f, -LuckyRollWindowHeight * 0.75f);
            }
        }

        private static async UniTask PlayLuckyBoxAnimation(IEnumerable<int> candidateTypeIds, int finalTypeId, string finalDisplayName, Sprite? finalIcon)
        {
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
                itemsContainer.anchoredPosition = Vector2.zero;
                overlay.gameObject.SetActive(true);
                canvasGroup.blocksRaycasts = true;

                ResetLuckyRollResultText();

                await FadeCanvasGroup(canvasGroup, 0f, 1f, LuckyRollFadeDuration);

                if (LuckyRollPreviewHold > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(LuckyRollPreviewHold), DelayType.DeltaTime, PlayerLoopTiming.Update, default);
                }

                await AnimateRollPhase(0f, plan.StartOffset, LuckyRollPreviewTransitionDuration, LuckyRollPreviewCurve);

                await AnimateRollPhase(plan.StartOffset, plan.FinalOffset, LuckyRollSpinDuration, LuckyRollSpinCurve);

                itemsContainer.anchoredPosition = new Vector2(-plan.FinalOffset, 0f);
                await AnimateLuckyRollCelebration(plan);

                await RevealLuckyRollResult(finalDisplayName);
                await FadeCanvasGroup(canvasGroup, 1f, 0f, LuckyRollFadeDuration);
            }
            finally
            {
                _isAnimating = false;

                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                overlay.gameObject.SetActive(false);

                ResetLuckyRollResultText();
                ClearLuckyRollItems();
                _luckyRollItemsContainer.anchoredPosition = Vector2.zero;
            }
        }

        private static bool TryBuildAnimationPlan(IEnumerable<int> candidateTypeIds, int finalTypeId, Sprite? finalIcon, out LuckyRollAnimationPlan plan)
        {
            plan = default!;

            if (_luckyRollItemsContainer == null) return false;

            var sequenceData = BuildLuckyRollSequence(candidateTypeIds, finalTypeId);
            var sequence = sequenceData.Slots;
            var finalIndex = sequenceData.FinalIndex;
            if (sequence.Count == 0 || finalIndex < 0 || finalIndex >= sequence.Count)
            {
                return false;
            }

            ClearLuckyRollItems();

            var slotWidth = LuckyRollSlotFullWidth;
            var totalWidth = sequence.Count * slotWidth;
            _luckyRollItemsContainer.sizeDelta = new Vector2(totalWidth, LuckyRollWindowHeight);

            var slots = new List<LuckyRollSlot>(sequence.Count);
            var hasFinalSlot = false;
            LuckyRollSlot finalSlot = default;

            for (int i = 0; i < sequence.Count; i++)
            {
                var typeId = sequence[i];
                var isFinalSlot = i == finalIndex;
                var sprite = isFinalSlot ? (finalIcon ?? GetItemIcon(typeId)) : GetItemIcon(typeId);
                var slot = CreateLuckyRollSlot(typeId, sprite, GetDisplayNameForType(typeId));

                var positionX = -totalWidth * 0.5f + slotWidth * 0.5f + i * slotWidth;
                slot.Rect.anchoredPosition = new Vector2(positionX, 0f);

                slots.Add(slot);

                if (isFinalSlot)
                {
                    finalSlot = slot;
                    hasFinalSlot = true;
                }
            }

            var finalSlotIndex = Mathf.Clamp(finalIndex, 0, slots.Count - 1);
            if (!hasFinalSlot)
            {
                finalSlot = slots[finalSlotIndex];
            }

            var centerOffset = finalSlot.Rect.anchoredPosition.x;
            if (!Mathf.Approximately(centerOffset, 0f))
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var rect = slots[i].Rect;
                    var position = rect.anchoredPosition;
                    rect.anchoredPosition = new Vector2(position.x - centerOffset, position.y);
                }
            }

            finalSlot = slots[finalSlotIndex];
            var finalOffset = finalSlot.Rect.anchoredPosition.x;
            var leadSlotCount = Mathf.Max(sequenceData.LeadCount, 1);
            var startOffset = finalOffset + slotWidth * leadSlotCount;

            plan = new LuckyRollAnimationPlan(slots, finalSlotIndex, startOffset, finalOffset);
            return true;
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

            var elapsed = 0f;
            while (elapsed < LuckyRollCelebrateDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / LuckyRollCelebrateDuration);
                t = Mathf.SmoothStep(0f, 1f, t);
                frame.color = Color.Lerp(initialColor, targetColor, t);
                iconTransform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            }

            frame.color = targetColor;
            iconTransform.localScale = targetScale;
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
            await UniTask.Delay(TimeSpan.FromSeconds(LuckyRollRevealHold), DelayType.DeltaTime, PlayerLoopTiming.Update, default);

            if (_luckyRollResultText != null)
            {
                _luckyRollResultText.text = finalDisplayName;
                var textColor = _luckyRollResultText.color;
                textColor.a = 1f;
                _luckyRollResultText.color = textColor;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(LuckyRollRevealHold), DelayType.DeltaTime, PlayerLoopTiming.Update, default);
        }

        private static async UniTask AnimateRollPhase(float from, float to, float duration, AnimationCurve curve)
        {
            if (_luckyRollItemsContainer == null) return;

            if (duration <= 0f || Mathf.Approximately(from, to))
            {
                _luckyRollItemsContainer.anchoredPosition = new Vector2(-to, 0f);
                await UniTask.Yield(PlayerLoopTiming.Update);
                return;
            }

            var elapsed = 0f;
            var current = from;
            var velocity = 0f;
            while (elapsed < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / duration);
                var targetProgress = curve?.Evaluate(t) ?? t;
                var target = Mathf.Lerp(from, to, targetProgress);

                current = Mathf.SmoothDamp(current, target, ref velocity, LuckyRollFollowSmoothTime, Mathf.Infinity, Time.deltaTime);
                _luckyRollItemsContainer.anchoredPosition = new Vector2(-current, 0f);
            }

            _luckyRollItemsContainer.anchoredPosition = new Vector2(-to, 0f);
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

            var visibleEstimate = Mathf.Max(3, Mathf.CeilToInt(LuckyRollWindowWidth / LuckyRollSlotFullWidth));
            var leadTarget = Mathf.Max(LuckyRollLeadSlots, visibleEstimate * 2);
            var trailTarget = Mathf.Max(LuckyRollTrailSlots, visibleEstimate * 2);

            var leadSlots = new List<int>(leadTarget + LuckyRollMinimumSlots);
            var trailSlots = new List<int>(trailTarget + LuckyRollMinimumSlots);

            int? lastLead = null;
            while (leadSlots.Count < leadTarget)
            {
                var id = SampleNext(lastLead);
                leadSlots.Add(id);
                lastLead = id;
            }

            int? lastTrail = null;
            while (trailSlots.Count < trailTarget)
            {
                var id = SampleNext(lastTrail);
                trailSlots.Add(id);
                lastTrail = id;
            }

            while (leadSlots.Count + 1 + trailSlots.Count < LuckyRollMinimumSlots)
            {
                if (leadSlots.Count <= trailSlots.Count)
                {
                    var id = SampleNext(lastLead);
                    leadSlots.Add(id);
                    lastLead = id;
                }
                else
                {
                    var id = SampleNext(lastTrail);
                    trailSlots.Add(id);
                    lastTrail = id;
                }
            }

            // Inject a partially shuffled pass so long sequences feel less predictable.
            foreach (var id in pool.OrderBy(_ => random.Next()))
            {
                if (leadSlots.Count + 1 + trailSlots.Count >= LuckyRollMinimumSlots * 2) break;
                if (random.NextDouble() < 0.5)
                {
                    leadSlots.Add(id);
                    lastLead = id;
                }
                else
                {
                    trailSlots.Add(id);
                    lastTrail = id;
                }
            }

            var sequence = new List<int>(leadSlots.Count + 1 + trailSlots.Count);
            sequence.AddRange(leadSlots);
            sequence.Add(finalTypeId);
            sequence.AddRange(trailSlots);

            var finalIndex = leadSlots.Count;
            return new LuckyRollSequenceData(sequence, finalIndex, leadSlots.Count);
        }

        private static LuckyRollSlot CreateLuckyRollSlot(int typeId, Sprite sprite, string displayName)
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
            frame.color = LuckyRollSlotFrameColor;
            frame.raycastTarget = false;

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

        private static Sprite GetItemIcon(int typeId)
        {
            var entry = ItemAssetsCollection.Instance.entries.FirstOrDefault(e => e != null && e.typeID == typeId);
            var sprite = entry?.prefab?.Icon;
            return sprite ?? EnsureFallbackSprite();
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