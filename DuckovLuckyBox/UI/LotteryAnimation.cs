using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FMOD;
using FMODUnity;
using DuckovLuckyBox.Core;
using Unity.VisualScripting;

namespace DuckovLuckyBox.UI
{
    /// <summary>
    /// Manages lottery animation UI and playback
    /// </summary>
    public static class LotteryAnimation
    {
        private static RectTransform? _overlayRoot;
        private static RectTransform? _viewport;
        private static RectTransform? _itemsContainer;
        private static Image? _centerPointer;
        private static TextMeshProUGUI? _resultText;
        private static CanvasGroup? _canvasGroup;
        private static Sprite? _fallbackSprite;
        private static bool _isAnimating;
        private static bool _skipRequested;

        // Configuration constants
        private static readonly Vector2 IconSize = new Vector2(160f, 160f);
        private const float ItemSpacing = 16f;
        private const float SlotPadding = 24f;
        private static readonly float SlotFullWidth = IconSize.x + SlotPadding + ItemSpacing;
        private const int MinimumSlotsBeforeFinal = 100; // Minimum slots before final to ensure smooth deceleration
        private const int SlotsAfterFinal = 10; // Extra slots after final for visual buffer
        private const float AnimationDuration = 7.0f; // matches the BGM
        private const float FadeDuration = 0.25f;
        private const float CelebrateDuration = 0.4f;
        private const float PointerThickness = 12f;

        private static readonly AnimationCurve AnimationCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 1.8f, 1.8f),       // Start with smooth acceleration
            new Keyframe(0.12f, 0.25f, 1.6f, 1.6f), // Continue acceleration
            new Keyframe(0.25f, 0.50f, 1.2f, 1.2f), // Peak speed reached
            new Keyframe(0.45f, 0.75f, 0.6f, 0.6f), // Begin deceleration
            new Keyframe(0.75f, 0.92f, 0.2f, 0.2f), // Final gentle deceleration
            new Keyframe(1f, 1f, 0f, 0f));          // Complete stop

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color FinalFrameColor = new Color(0.95f, 0.8f, 0.35f, 1f);
        private static readonly Color SlotFrameColor = new Color(1f, 1f, 1f, 0.25f);

        private static Canvas? _canvas;

        /// <summary>
        /// Initializes the lottery animation UI with a full-screen canvas overlay
        /// </summary>
        public static void Initialize()
        {
            if (_overlayRoot != null) return;

            // Create full-screen canvas if it doesn't exist
            if (_canvas == null)
            {
                var canvasObj = new GameObject("LotteryAnimationCanvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
                _canvas = canvasObj.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = short.MaxValue;

                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            // Create full-screen overlay
            _overlayRoot = new GameObject("LotteryAnimationOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)).GetComponent<RectTransform>();
            _overlayRoot.SetParent(_canvas.transform, false);
            _overlayRoot.anchorMin = Vector2.zero;
            _overlayRoot.anchorMax = Vector2.one;
            _overlayRoot.offsetMin = Vector2.zero;
            _overlayRoot.offsetMax = Vector2.zero;
            _overlayRoot.gameObject.SetActive(false);

            _canvasGroup = _overlayRoot.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            var overlayImage = _overlayRoot.GetComponent<Image>();
            overlayImage.color = OverlayColor;
            overlayImage.raycastTarget = true;

            // Create viewport in center of screen
            var canvasRect = _canvas.GetComponent<RectTransform>();
            var canvasSize = canvasRect.rect.size;
            var viewportHeight = Mathf.Min(200f, canvasSize.y * 0.3f);
            var viewportWidth = canvasSize.x * 0.8f;

            _viewport = new GameObject("LotteryViewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            _viewport.SetParent(_overlayRoot, false);
            _viewport.anchorMin = new Vector2(0.5f, 0.5f);
            _viewport.anchorMax = new Vector2(0.5f, 0.5f);
            _viewport.pivot = new Vector2(0.5f, 0.5f);
            _viewport.sizeDelta = new Vector2(viewportWidth, viewportHeight);

            // Create black background layer behind items
            var itemsBackground = new GameObject("LotteryItemsBackground", typeof(RectTransform), typeof(Image));
            var bgRect = itemsBackground.GetComponent<RectTransform>();
            bgRect.SetParent(_viewport, false);
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(viewportWidth * 2f, viewportHeight);

            var bgImage = itemsBackground.GetComponent<Image>();
            bgImage.sprite = EnsureFallbackSprite();
            bgImage.type = Image.Type.Simple;
            bgImage.color = new Color(0f, 0f, 0f, 0.5f);
            bgImage.raycastTarget = false;

            _itemsContainer = new GameObject("LotteryItemsContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            _itemsContainer.SetParent(_viewport, false);
            _itemsContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _itemsContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _itemsContainer.pivot = new Vector2(0.5f, 0.5f);

            // Create center pointer with improved visuals - vertical line with diamond accent
            var pointerContainer = new GameObject("LotteryPointerContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            pointerContainer.SetParent(_overlayRoot, false);
            pointerContainer.anchorMin = new Vector2(0.5f, 0.5f);
            pointerContainer.anchorMax = new Vector2(0.5f, 0.5f);
            pointerContainer.pivot = new Vector2(0.5f, 0.5f);
            pointerContainer.sizeDelta = Vector2.zero;
            pointerContainer.anchoredPosition = Vector2.zero;

            // Main vertical line pointer
            var pointerLineObj = new GameObject("PointerLine", typeof(RectTransform), typeof(Image));
            var pointerLineRect = pointerLineObj.GetComponent<RectTransform>();
            pointerLineRect.SetParent(pointerContainer, false);
            pointerLineRect.anchorMin = new Vector2(0.5f, 0.5f);
            pointerLineRect.anchorMax = new Vector2(0.5f, 0.5f);
            pointerLineRect.pivot = new Vector2(0.5f, 0.5f);
            pointerLineRect.sizeDelta = new Vector2(PointerThickness * 1.5f, viewportHeight + 80f);
            pointerLineRect.anchoredPosition = Vector2.zero;

            var pointerLineImage = pointerLineObj.GetComponent<Image>();
            pointerLineImage.sprite = EnsureFallbackSprite();
            pointerLineImage.type = Image.Type.Simple;
            pointerLineImage.color = new Color(1f, 1f, 1f, 0.7f);  // White with transparency
            pointerLineImage.raycastTarget = false;

            // Add outline effect to the pointer line - white glow
            var pointerLineOutline = pointerLineObj.AddComponent<Outline>();
            pointerLineOutline.effectColor = new Color(1f, 1f, 1f, 0.5f);  // White glow
            pointerLineOutline.effectDistance = new Vector2(1.5f, -1.5f);
            pointerLineOutline.useGraphicAlpha = false;

            // Top diamond accent
            var topDiamondObj = new GameObject("TopDiamond", typeof(RectTransform), typeof(Image));
            var topDiamondRect = topDiamondObj.GetComponent<RectTransform>();
            topDiamondRect.SetParent(pointerContainer, false);
            topDiamondRect.anchorMin = new Vector2(0.5f, 0.5f);
            topDiamondRect.anchorMax = new Vector2(0.5f, 0.5f);
            topDiamondRect.pivot = new Vector2(0.5f, 0.5f);
            topDiamondRect.sizeDelta = new Vector2(PointerThickness * 2.5f, PointerThickness * 2.5f);
            topDiamondRect.anchoredPosition = new Vector2(0f, viewportHeight * 0.5f + 32f);
            topDiamondRect.rotation = Quaternion.Euler(0f, 0f, 45f);  // Rotate 45 degrees to make diamond

            var topDiamondImage = topDiamondObj.GetComponent<Image>();
            topDiamondImage.sprite = EnsureFallbackSprite();
            topDiamondImage.type = Image.Type.Simple;
            topDiamondImage.color = new Color(1f, 1f, 1f, 0.8f);  // White diamond
            topDiamondImage.raycastTarget = false;

            // Add outline to top diamond - subtle white outline
            var topDiamondOutline = topDiamondObj.AddComponent<Outline>();
            topDiamondOutline.effectColor = new Color(1f, 1f, 1f, 0.4f);  // Subtle white outline
            topDiamondOutline.effectDistance = new Vector2(0.5f, -0.5f);
            topDiamondOutline.useGraphicAlpha = false;

            // Bottom diamond accent
            var bottomDiamondObj = new GameObject("BottomDiamond", typeof(RectTransform), typeof(Image));
            var bottomDiamondRect = bottomDiamondObj.GetComponent<RectTransform>();
            bottomDiamondRect.SetParent(pointerContainer, false);
            bottomDiamondRect.anchorMin = new Vector2(0.5f, 0.5f);
            bottomDiamondRect.anchorMax = new Vector2(0.5f, 0.5f);
            bottomDiamondRect.pivot = new Vector2(0.5f, 0.5f);
            bottomDiamondRect.sizeDelta = new Vector2(PointerThickness * 2.5f, PointerThickness * 2.5f);
            bottomDiamondRect.anchoredPosition = new Vector2(0f, -viewportHeight * 0.5f - 32f);
            bottomDiamondRect.rotation = Quaternion.Euler(0f, 0f, 45f);  // Rotate 45 degrees to make diamond

            var bottomDiamondImage = bottomDiamondObj.GetComponent<Image>();
            bottomDiamondImage.sprite = EnsureFallbackSprite();
            bottomDiamondImage.type = Image.Type.Simple;
            bottomDiamondImage.color = new Color(1f, 1f, 1f, 0.8f);  // White diamond
            bottomDiamondImage.raycastTarget = false;

            // Add outline to bottom diamond - subtle white outline
            var bottomDiamondOutline = bottomDiamondObj.AddComponent<Outline>();
            bottomDiamondOutline.effectColor = new Color(1f, 1f, 1f, 0.4f);  // Subtle white outline
            bottomDiamondOutline.effectDistance = new Vector2(0.5f, -0.5f);
            bottomDiamondOutline.useGraphicAlpha = false;

            _centerPointer = pointerLineImage;  // Store the main line for visibility control

            // Create result text
            _resultText = new GameObject("LotteryResultText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            _resultText.rectTransform.SetParent(_overlayRoot, false);
            _resultText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _resultText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _resultText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _resultText.rectTransform.anchoredPosition = new Vector2(0f, -viewportHeight * 0.75f);
            _resultText.fontSize = 36;
            _resultText.alignment = TextAlignmentOptions.Center;
            _resultText.raycastTarget = false;
            ResetResultText();

            Log.Debug("Lottery animation UI initialized with full-screen canvas");
        }

        /// <summary>
        /// Plays the lottery animation
        /// </summary>
        public static async UniTask PlayAsync(IEnumerable<int> candidateTypeIds, int finalTypeId, string finalDisplayName, Sprite? finalIcon)
        {
            // Check if animation is enabled in settings
            var enableAnimationValue = Core.Settings.SettingManager.Instance.EnableAnimation.Value;
            if (enableAnimationValue is bool enabled && !enabled)
            {
                Log.Debug("Lottery animation is disabled in settings. Skipping animation.");
                return;
            }

            // Auto-initialize if not already initialized
            if (_overlayRoot == null || _itemsContainer == null || _centerPointer == null || _canvasGroup == null)
            {
                Initialize();
                Log.Debug("Lottery animation UI auto-initialized.");
            }

            if (_isAnimating) return;

            if (!TryBuildAnimationPlan(candidateTypeIds, finalTypeId, finalIcon, out var plan))
            {
                Log.Warning("Failed to prepare lottery animation plan.");
                return;
            }

            _isAnimating = true;
            _skipRequested = false;

            try
            {
                if (_overlayRoot == null || _canvasGroup == null)
                {
                    Log.Error("Lottery animation failed to initialize properly.");
                    return;
                }

                _overlayRoot.gameObject.SetActive(true);
                _canvasGroup.blocksRaycasts = true;

                // Show Pointer at the start of animation
                if (_centerPointer != null)
                {
                    var pointerColor = _centerPointer.color;
                    pointerColor.a = 0.95f;
                    _centerPointer.color = pointerColor;
                }

                // Show result text at the start - it will update in real-time during animation
                if (_resultText != null)
                {
                    _resultText.text = ""; // Start with empty text
                    var textColor = _resultText.color;
                    textColor.a = 1f; // Make text visible from the start
                    _resultText.color = textColor;
                }

                // Fade in
                await FadeCanvasGroup(_canvasGroup, 0f, 1f, FadeDuration);

                // Play rolling sound
                RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup sfxGroup);
                SoundUtils.PlaySound(Constants.Sound.ROLLING_SOUND, sfxGroup);

                // Single continuous roll animation
                await PerformContinuousRoll(plan, AnimationDuration, AnimationCurve);

                // If skip was requested, jump directly to celebration
                if (!_skipRequested)
                {
                    // Celebration on final slot
                    await AnimateCelebration(plan, finalTypeId, sfxGroup);

                    // Reveal result and hold
                    await RevealResult(finalDisplayName);
                }
                else
                {
                    // Skip requested - show final result immediately
                    if (_resultText != null)
                    {
                        _resultText.text = finalDisplayName;
                    }
                    await UniTask.Delay(TimeSpan.FromSeconds(0.1f), DelayType.DeltaTime, PlayerLoopTiming.Update, default);
                }

                // Fade out
                await FadeCanvasGroup(_canvasGroup, 1f, 0f, FadeDuration);
            }
            finally
            {
                _isAnimating = false;

                if (_canvasGroup != null)
                    _canvasGroup.alpha = 0f;
                if (_canvasGroup != null)
                    _canvasGroup.blocksRaycasts = false;
                if (_overlayRoot != null)
                    _overlayRoot.gameObject.SetActive(false);

                // Hide Pointer after animation ends
                if (_centerPointer != null)
                {
                    var pointerColor = _centerPointer.color;
                    pointerColor.a = 0f;
                    _centerPointer.color = pointerColor;
                }

                ResetResultText();
                ClearItems();
                if (_itemsContainer != null)
                    _itemsContainer.anchoredPosition = Vector2.zero;
            }
        }

        private static bool TryBuildAnimationPlan(IEnumerable<int> candidateTypeIds, int finalTypeId, Sprite? finalIcon, out AnimationPlan plan)
        {
            plan = default!;

            if (_itemsContainer == null) return false;
            if (_viewport == null) return false;

            var slotWidth = SlotFullWidth;
            var viewportWidth = _viewport.rect.width;

            // Check if weighted lottery is enabled
            var enableWeightedLottery = Core.Settings.SettingManager.Instance.EnableWeightedLottery.GetAsBool();

            // Build complete slot sequence with finalSlot guaranteed to be beyond threshold
            var slotSequence = BuildSlotSequence(candidateTypeIds, finalTypeId, enableWeightedLottery, out int finalSlotIndex);
            if (slotSequence.Count == 0)
            {
                return false;
            }

            ClearItems();

            var totalWidth = slotSequence.Count * slotWidth;
            _itemsContainer.sizeDelta = new Vector2(totalWidth, 200f);

            var slots = new List<Slot>(slotSequence.Count);

            // Create UI slots for each item in the sequence
            for (int i = 0; i < slotSequence.Count; i++)
            {
                var typeId = slotSequence[i];
                var isFinal = i == finalSlotIndex;

                var sprite = isFinal ? (finalIcon ?? LotteryService.GetItemIcon(typeId)) : LotteryService.GetItemIcon(typeId);
                var slot = CreateSlot(typeId, sprite, LotteryService.GetDisplayName(typeId), LotteryService.GetItemQualityColor(typeId));

                // Position: container is centered, so position is offset from center
                var positionX = (float)i * slotWidth - totalWidth * 0.5f + slotWidth * 0.5f;
                slot.Rect.anchoredPosition = new Vector2(positionX, 0f);

                slots.Add(slot);
            }

            var finalItemPos = slots[finalSlotIndex].Rect.anchoredPosition.x;
            var endOffset = -finalItemPos;

            // Start the animation with about 10 slots already visible
            // This avoids the initial empty space on the left
            var startSlotIndex = Mathf.Max(0, Mathf.Min(10, slots.Count / 4));
            var startItemPos = slots[startSlotIndex].Rect.anchoredPosition.x;
            var startOffset = -startItemPos + viewportWidth * 0.2f; // Offset to show items from left

            plan = new AnimationPlan(slots, finalSlotIndex, startOffset, endOffset);
            return true;
        }

        /// <summary>
        /// Build slot sequence with finalSlot guaranteed to be at index >= MinimumSlotsBeforeFinal
        /// Supports both uniform random and weighted random selection based on item quality
        /// </summary>
        private static List<int> BuildSlotSequence(IEnumerable<int> candidateTypeIds, int finalTypeId, bool useWeightedLottery, out int finalSlotIndex)
        {
            var pool = candidateTypeIds?.ToList() ?? new List<int>();
            if (!pool.Contains(finalTypeId)) pool.Add(finalTypeId);
            if (pool.Count == 0) pool.Add(finalTypeId);

            // Pre-convert to weighted items once if using weighted lottery to avoid repeated conversions
            var weightedItemsCache = useWeightedLottery ? LotteryService.ConvertToQualityWeightedItems(pool) : null;

            var sequence = new List<int>();
            int? lastId = null;
            int lastIdCount = 0;  // Track consecutive count of the same item

            int SampleNext(int? previous, int consecutiveCount)
            {
                if (pool.Count == 1) return pool[0];

                int pick;

                // Always force different - no consecutive items allowed
                if (useWeightedLottery && weightedItemsCache != null)
                {
                    // Use weighted random selection based on cached weighted items
                    var selectedId = LotteryService.PickRandomItemWeighted(weightedItemsCache);

                    // If weighted selection failed, use first item as fallback
                    if (selectedId < 0)
                    {
                        pick = pool[0];
                        Log.Warning("Weighted lottery selection failed, using fallback item.");
                    }
                    else
                    {
                        pick = selectedId;
                    }
                }
                else
                {
                    // Use uniform random selection
                    pick = pool[UnityEngine.Random.Range(0, pool.Count)];
                }

                // If got the same item as previous, retry with weighted selection until different
                if (previous.HasValue && pool.Count > 1 && pick == previous.Value)
                {
                    return SampleNext(previous, consecutiveCount);  // Retry until different
                }

                return pick;
            }

            // Build slots before final (ensure minimum threshold)
            var prefixCount = MinimumSlotsBeforeFinal + UnityEngine.Random.Range(0, 20);
            while (sequence.Count < prefixCount)
            {
                var id = SampleNext(lastId, lastIdCount);
                sequence.Add(id);

                // Update consecutive count
                if (lastId.HasValue && id == lastId.Value)
                {
                    lastIdCount++;
                }
                else
                {
                    lastIdCount = 1;
                }

                lastId = id;
            }

            // Place final item
            finalSlotIndex = sequence.Count;
            sequence.Add(finalTypeId);
            lastId = finalTypeId;
            lastIdCount = 1;

            // Add buffer slots after final for visual completeness
            for (int i = 0; i < SlotsAfterFinal; i++)
            {
                var id = SampleNext(lastId, lastIdCount);
                sequence.Add(id);

                // Update consecutive count
                if (lastId.HasValue && id == lastId.Value)
                {
                    lastIdCount++;
                }
                else
                {
                    lastIdCount = 1;
                }

                lastId = id;
            }

            return sequence;
        }

        private static Slot CreateSlot(int typeId, Sprite? sprite, string displayName, Color frameColor)
        {
            var root = new GameObject($"LotterySlot_{typeId}", typeof(RectTransform), typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(_itemsContainer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(IconSize.x + SlotPadding, IconSize.y + SlotPadding);

            var frame = root.GetComponent<Image>();
            frame.sprite = EnsureFallbackSprite();
            frame.type = Image.Type.Simple;
            frame.color = frameColor;
            frame.raycastTarget = false;
            frame.useSpriteMesh = true;

            var frameMask = root.AddComponent<Mask>();
            frameMask.showMaskGraphic = true;

            var iconObject = new GameObject("LotterySlotIcon", typeof(RectTransform), typeof(Image));
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 0f);
            iconRect.sizeDelta = IconSize;

            var icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite ?? EnsureFallbackSprite();
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.useSpriteMesh = true;
            iconObject.name = $"LotterySlotIcon_{displayName}";

            var iconOutline = iconObject.AddComponent<Outline>();
            iconOutline.effectColor = new Color(1f, 1f, 1f, 0f);
            iconOutline.effectDistance = new Vector2(2f, -2f);
            iconOutline.useGraphicAlpha = false;

            return new Slot(rect, frame, icon, iconOutline, displayName);
        }

        private static async UniTask PerformContinuousRoll(AnimationPlan plan, float duration, AnimationCurve curve)
        {
            if (_itemsContainer == null) return;

            _itemsContainer.anchoredPosition = new Vector2(plan.StartOffset, 0f);

            int lastHighlightedIndex = -1;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                // Check for mouse click to skip animation
                if (Input.GetMouseButtonDown(0))
                {
                    _skipRequested = true;
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / duration);
                var progress = curve?.Evaluate(t) ?? t;

                var currentOffset = Mathf.Lerp(plan.StartOffset, plan.FinalOffset, progress);
                _itemsContainer.anchoredPosition = new Vector2(currentOffset, 0f);

                int currentIndex = FindCenteredSlotIndex(plan, currentOffset);
                if (currentIndex != lastHighlightedIndex)
                {
                    if (lastHighlightedIndex >= 0 && lastHighlightedIndex < plan.Slots.Count)
                    {
                        var prevSlot = plan.Slots[lastHighlightedIndex];
                        prevSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 0f);
                    }

                    if (currentIndex >= 0 && currentIndex < plan.Slots.Count)
                    {
                        var currentSlot = plan.Slots[currentIndex];
                        currentSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 1f);

                        if (_resultText != null)
                        {
                            _resultText.text = currentSlot.DisplayName;
                        }
                    }

                    lastHighlightedIndex = currentIndex;
                }
            }

            // Jump to final position
            _itemsContainer.anchoredPosition = new Vector2(plan.FinalOffset, 0f);

            if (lastHighlightedIndex >= 0 && lastHighlightedIndex < plan.Slots.Count && lastHighlightedIndex != plan.FinalSlotIndex)
            {
                var prevSlot = plan.Slots[lastHighlightedIndex];
                prevSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 0f);
            }
            var finalSlot = plan.FinalSlot;
            finalSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 1f);

            if (_resultText != null)
            {
                _resultText.text = finalSlot.DisplayName;
            }
        }

        private static int FindCenteredSlotIndex(AnimationPlan plan, float currentOffset)
        {
            float minDistance = float.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < plan.Slots.Count; i++)
            {
                var slot = plan.Slots[i];
                var slotWorldX = slot.Rect.anchoredPosition.x + currentOffset;
                var distance = Mathf.Abs(slotWorldX);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private static async UniTask AnimateCelebration(AnimationPlan plan, int finalTypeId, ChannelGroup sfxGroup)
        {
            var slot = plan.FinalSlot;
            var frame = slot.Frame;

            var initialColor = SlotFrameColor;
            var targetColor = FinalFrameColor;

            frame.color = initialColor;

            var elapsed = 0f;
            while (elapsed < CelebrateDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / CelebrateDuration);
                t = Mathf.SmoothStep(0f, 1f, t);
                frame.color = Color.Lerp(initialColor, targetColor, t);
            }

            frame.color = targetColor;

            // Play high-quality lottery sound if enabled and the final item has high quality (quality >= 5 and < 99)
            var enableHighQualitySound = Core.Settings.SettingManager.Instance.EnableHighQualitySound.GetAsBool();
            var finalItemQuality = LotteryService.GetItemQuality(finalTypeId);

            if (enableHighQualitySound && finalItemQuality >= 5 && finalItemQuality < 99)
            {
                var customSoundPath = Core.Settings.SettingManager.Instance.HighQualitySoundFilePath.GetAsString();

                if (!string.IsNullOrEmpty(customSoundPath) && System.IO.File.Exists(customSoundPath))
                {
                    // Play custom sound from file path
                    Log.Debug($"Playing custom high-quality lottery sound from: {customSoundPath}");
                    SoundUtils.PlaySoundFromFile(customSoundPath, sfxGroup);
                }
                else
                {
                    // Play default sound
                    Log.Debug($"Playing default high-quality lottery sound for item {finalTypeId} with quality {finalItemQuality}");
                    SoundUtils.PlaySound(Constants.Sound.HIGH_QUALITY_LOTTERY_SOUND, sfxGroup);
                }
            }
        }

        private static void ResetResultText()
        {
            if (_resultText == null) return;

            var textColor = _resultText.color;
            textColor.a = 0f;
            _resultText.color = textColor;
            _resultText.text = string.Empty;
        }

        private static async UniTask RevealResult(string finalDisplayName)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.DeltaTime, PlayerLoopTiming.Update, default);

            if (_resultText != null)
            {
                _resultText.text = finalDisplayName;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.75f), DelayType.DeltaTime, PlayerLoopTiming.Update, default);
        }

        private static Sprite EnsureFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            var texture = Texture2D.whiteTexture;
            _fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return _fallbackSprite;
        }

        private static void ClearItems()
        {
            if (_itemsContainer == null) return;

            for (int i = _itemsContainer.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_itemsContainer.GetChild(i).gameObject);
            }
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

        private readonly struct Slot
        {
            public RectTransform Rect { get; }
            public Image Frame { get; }
            public Image Icon { get; }
            public Outline IconOutline { get; }
            public string DisplayName { get; }

            public Slot(RectTransform rect, Image frame, Image icon, Outline iconOutline, string displayName)
            {
                Rect = rect;
                Frame = frame;
                Icon = icon;
                IconOutline = iconOutline;
                DisplayName = displayName;
            }
        }

        private sealed class AnimationPlan
        {
            public AnimationPlan(List<Slot> slots, int finalSlotIndex, float startOffset, float finalOffset)
            {
                Slots = slots;
                FinalSlotIndex = finalSlotIndex;
                StartOffset = startOffset;
                FinalOffset = finalOffset;
            }

            public List<Slot> Slots { get; }
            public int FinalSlotIndex { get; }
            public float StartOffset { get; }
            public float FinalOffset { get; }

            public Slot FinalSlot
            {
                get
                {
                    // FinalSlotIndex should always be valid since it's set during BuildSlotSequence
                    // and is guaranteed to be within Slots.Count
                    if (FinalSlotIndex >= 0 && FinalSlotIndex < Slots.Count)
                    {
                        return Slots[FinalSlotIndex];
                    }

                    // Fallback to last slot if something goes wrong (should never happen)
                    Log.Warning($"Invalid FinalSlotIndex: {FinalSlotIndex}, Slots.Count: {Slots.Count}. Returning last slot.");
                    return Slots[Slots.Count - 1];
                }
            }
        }
    }
}
