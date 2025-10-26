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
        private const int MinimumSlotsBeforeFinal = 140; // Minimum slots before final - adapted for 20 slots/s speed (requires more slots)
        private const int SlotsAfterFinal = 20; // Extra slots after final for visual buffer

        // Physics-based animation parameters (CSGO-style) - using slot as the unit
        private const float InitialVelocityInSlots = 20f; // Initial scroll speed (slots/second) - scroll 20 slots per second initially
        private const float MinVelocityInSlots = 0.01f; // Minimum velocity before final positioning (slots/second) - decelerate to 0.01 slots/s at the end
        private const float TargetAnimationDurationInSeconds = 6.8f; // Target animation total duration (seconds) - physics-based rolling phase must last this long (decelerate to 0 after 6.8s)

        // Pre-calculated velocity curve for smooth deceleration
        private static float[]? _velocityCurve = null;

        // Visual effect constants
        private const float GlowPulseSpeed = 3f; // Glow pulse frequency (Hz)
        private const float HighlightIntensity = 1.5f; // Highlight glow intensity multiplier

        private const float FadeDuration = 0.25f;
        private const float CelebrateDuration = 0.5f;
        private const float PointerThickness = 12f;

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

                // Physics-based continuous roll animation (CSGO-style)
                await PerformPhysicsBasedRoll(plan, sfxGroup);

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
                _overlayRoot?.gameObject.SetActive(false);

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

            // Generate velocity curve if not already generated
            if (_velocityCurve == null)
            {
                _velocityCurve = GenerateVelocityCurve();
                Log.Debug($"[BuildPlan] Generated velocity curve with {_velocityCurve.Length} steps");
            }

            // Calculate total distance based on velocity curve
            float totalDistanceInSlots = CalculateTotalDistanceInSlots(_velocityCurve);

            // Start slot index (used to fill the left side of viewport)
            const int startSlotIndex = 20;

            // Final slot index = start slot + distance calculated from velocity curve
            // Use RoundToInt instead of CeilToInt because we want to match the calculated distance as precisely as possible
            // If distance is 70.3 slots, final slot should be 90 (20 + 70), not 91
            int finalSlotIndex = startSlotIndex + Mathf.RoundToInt(totalDistanceInSlots);

            // Total slots needed = final slot index + buffer slots after + 1
            // +1 because index starts from 0, so index N means N+1 total slots are needed
            int totalSlotsNeeded = finalSlotIndex + SlotsAfterFinal + 1;

            Log.Debug($"[BuildPlan] Start slot: {startSlotIndex}, Total distance: {totalDistanceInSlots:F2} slots");
            Log.Debug($"[BuildPlan] Final slot index: {finalSlotIndex}, Total slots needed: {totalSlotsNeeded}");

            var slotWidth = SlotFullWidth;
            var viewportWidth = _viewport.rect.width;

            // Check if weighted lottery is enabled
            var enableWeightedLottery = Core.Settings.SettingManager.Instance.EnableWeightedLottery.GetAsBool();

            // Build complete slot sequence
            var slotSequence = BuildSlotSequenceWithFixedFinal(candidateTypeIds, finalTypeId, enableWeightedLottery, totalSlotsNeeded, finalSlotIndex);
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
                var positionX = i * slotWidth - totalWidth * 0.5f + slotWidth * 0.5f;
                slot.Rect.anchoredPosition = new Vector2(positionX, 0f);

                slots.Add(slot);
            }

            var finalItemPos = slots[finalSlotIndex].Rect.anchoredPosition.x;
            var endOffset = -finalItemPos;

            // Start position: place start slot in the center
            var startItemPos = slots[startSlotIndex].Rect.anchoredPosition.x;
            var startOffset = -startItemPos;

            Log.Debug($"[BuildPlan] Start offset: {startOffset:F2}, End offset: {endOffset:F2}");
            Log.Debug($"[BuildPlan] Actual distance to travel: {(endOffset - startOffset) / SlotFullWidth:F2} slots");

            plan = new AnimationPlan(slots, finalSlotIndex, startOffset, endOffset);
            return true;
        }

        /// <summary>
        /// Build slot sequence with final item at a specific index
        /// </summary>
        private static List<int> BuildSlotSequenceWithFixedFinal(IEnumerable<int> candidateTypeIds, int finalTypeId, bool useWeightedLottery, int totalSlots, int finalSlotIndex)
        {
            var pool = candidateTypeIds?.ToList() ?? new List<int>();
            if (!pool.Contains(finalTypeId)) pool.Add(finalTypeId);
            if (pool.Count == 0) pool.Add(finalTypeId);

            // Pre-convert to weighted items once if using weighted lottery
            var weightedItemsCache = useWeightedLottery ? LotteryService.ConvertToQualityWeightedItems(pool) : null;

            var sequence = new List<int>();
            int? lastId = null;
            int lastIdCount = 0;

            int SampleNext(int? previous, int consecutiveCount)
            {
                if (pool.Count == 1) return pool[0];

                int pick;

                if (useWeightedLottery && weightedItemsCache != null)
                {
                    var selectedId = LotteryService.PickRandomItemWeighted(weightedItemsCache);
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
                    pick = pool[UnityEngine.Random.Range(0, pool.Count)];
                }

                if (previous.HasValue && pool.Count > 1 && pick == previous.Value)
                {
                    return SampleNext(previous, consecutiveCount);
                }

                return pick;
            }

            // Build all slots
            for (int i = 0; i < totalSlots; i++)
            {
                int id;

                if (i == finalSlotIndex)
                {
                    // Place final item at specific index
                    id = finalTypeId;
                }
                else
                {
                    // Fill with random items
                    id = SampleNext(lastId, lastIdCount);
                }

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

        /// <summary>
        /// Generate velocity curve for smooth deceleration from initial to minimum velocity over target duration
        /// Custom curve: velocity at 6s is 0.1 slots/s, velocity at 6.8s is 0 (complete stop)
        /// </summary>
        private static float[] GenerateVelocityCurve()
        {
            const int stepsPerSecond = 20; // 20 samples per second (every 0.05s)
            // 6.8 * 20 = 136
            int totalSteps = (int)(TargetAnimationDurationInSeconds * stepsPerSecond);
            var curve = new float[totalSteps];

            // At 6s (index 120) should be 0.1
            // At 6.8s (index 135, the last one) should be 0.0 (complete stop)
            const float velocityAt6s = 0.1f;

            for (int i = 0; i < totalSteps; i++)
            {
                // Current sample point corresponding time
                // i=0 → 0.00s, i=1 → 0.05s, ..., i=120 → 6.00s, i=135 → 6.75s
                float timeInSeconds = (float)i / stepsPerSecond;

                if (timeInSeconds < 6.0f)
                {
                    // First 6 seconds: decelerate from 20 to 0.1, using ease-out cubic curve
                    float t = timeInSeconds / 6.0f; // 0.0 to 1.0
                    float easeT = 1f - Mathf.Pow(1f - t, 3f);
                    curve[i] = Mathf.Lerp(InitialVelocityInSlots, velocityAt6s, easeT);
                }
                else
                {
                    // 6-6.8 seconds: linearly decelerate from 0.1 to 0.0 (complete stop)
                    float t = (timeInSeconds - 6.0f) / 0.8f; // 0.0 to 1.0
                    curve[i] = Mathf.Lerp(velocityAt6s, 0f, t);
                }
            }

            // Debug: output velocity at key time points
            Log.Debug($"[VelocityCurve] Steps: {totalSteps}, 0.0s: {curve[0]:F2}, 3.0s: {curve[60]:F2}, 6.0s: {curve[120]:F2}, 6.75s: {curve[135]:F2}");

            return curve;
        }

        /// <summary>
        /// Calculate total distance traveled based on velocity curve
        /// </summary>
        private static float CalculateTotalDistanceInSlots(float[] velocityCurve)
        {
            const float timeStepInSeconds = 0.05f; // Each sample point is 0.05 seconds apart
            float totalDistance = 0f;

            for (int i = 0; i < velocityCurve.Length; i++)
            {
                // Distance = velocity × time
                totalDistance += velocityCurve[i] * timeStepInSeconds;
            }

            return totalDistance;
        }

        /// <summary>
        /// Physics-based rolling animation (CSGO-style smooth deceleration)
        /// </summary>
        private static async UniTask PerformPhysicsBasedRoll(AnimationPlan plan, ChannelGroup sfxGroup)
        {
            if (_itemsContainer == null) return;

            // Generate velocity curve if not already generated
            if (_velocityCurve == null)
            {
                _velocityCurve = GenerateVelocityCurve();
                Log.Debug($"[Animation] Generated velocity curve with {_velocityCurve.Length} steps");
            }

            // Convert pixel position to slot unit for physics calculation
            float currentPositionInPixels = plan.StartOffset;
            float targetPositionInPixels = plan.FinalOffset;

            _itemsContainer.anchoredPosition = new Vector2(currentPositionInPixels, 0f);

            int lastHighlightedIndex = -1;
            float elapsedTime = 0f; // Track elapsed time

            // Add debug info
            Log.Debug($"[Animation] Start - Current: {currentPositionInPixels}, Target: {targetPositionInPixels}, Distance: {(targetPositionInPixels - currentPositionInPixels) / SlotFullWidth} slots");

            // Determine velocity direction
            float velocityDirection = targetPositionInPixels < currentPositionInPixels ? -1f : 1f;
            Log.Debug($"[Animation] Velocity direction: {velocityDirection} (container moves from {currentPositionInPixels} to {targetPositionInPixels})");

            while (true)
            {
                // Check for skip request
                if (Input.GetMouseButtonDown(0))
                {
                    _skipRequested = true;
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
                float deltaTime = Time.deltaTime;
                elapsedTime += deltaTime;

                // Force stop after target duration (regardless of whether target position is reached)
                if (elapsedTime >= TargetAnimationDurationInSeconds)
                {
                    break;
                }

                // Look up current velocity from pre-generated velocity curve
                // Use FloorToInt: round down to nearest sample point
                // Example: time 0.12s → index Floor(0.12 * 20) = Floor(2.4) = 2 → use velocity at 0.10s
                // This avoids array bounds and ensures we use "current or previous" velocity value
                int curveIndex = Mathf.FloorToInt(elapsedTime * 20f); // One sample every 0.05s
                curveIndex = Mathf.Clamp(curveIndex, 0, _velocityCurve.Length - 1);
                float currentVelocityInSlots = _velocityCurve[curveIndex] * velocityDirection;

                // Calculate distance to target
                float distanceInPixels = targetPositionInPixels - currentPositionInPixels;
                float distanceInSlots = distanceInPixels / SlotFullWidth;

                if (Core.Settings.SettingManager.Instance.EnableDebug.GetAsBool())
                {
                    // Detailed debug: output every 0.1s or every frame after 5.8s
                    bool shouldLog = (Mathf.FloorToInt(elapsedTime * 10) % 10 == 0) || (elapsedTime > 5.8f);
                    if (shouldLog && deltaTime > 0)
                    {
                        Log.Debug($"[Animation] Time: {elapsedTime:F3}s, Velocity: {currentVelocityInSlots:F4} slots/s, Distance: {distanceInSlots:F3} slots, Pos: {currentPositionInPixels:F2}, Target: {targetPositionInPixels:F2}, CurveIdx: {curveIndex}");
                    }
                }

                // Convert slot/s to pixel/s, then calculate displacement
                float velocityInPixels = currentVelocityInSlots * SlotFullWidth;
                float movementInPixels = velocityInPixels * deltaTime;
                currentPositionInPixels += movementInPixels;

                // Set base position first
                _itemsContainer.anchoredPosition = new Vector2(currentPositionInPixels, 0f);

                // Update highlighting with smooth transitions
                int currentIndex = FindCenteredSlotIndex(plan, currentPositionInPixels);
                if (currentIndex != lastHighlightedIndex)
                {
                    // Remove previous highlight
                    if (lastHighlightedIndex >= 0 && lastHighlightedIndex < plan.Slots.Count)
                    {
                        var prevSlot = plan.Slots[lastHighlightedIndex];
                        prevSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 0f);

                        // Reset scale
                        prevSlot.Rect.localScale = Vector3.one;
                    }

                    // Apply new highlight
                    if (currentIndex >= 0 && currentIndex < plan.Slots.Count)
                    {
                        var currentSlot = plan.Slots[currentIndex];
                        currentSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 1f);

                        // Slight scale up for emphasis
                        currentSlot.Rect.localScale = Vector3.one * 1.05f;

                        if (_resultText != null)
                        {
                            _resultText.text = currentSlot.DisplayName;
                        }
                    }

                    lastHighlightedIndex = currentIndex;
                }
            }

            // Ensure final position and highlight
            if (lastHighlightedIndex >= 0 && lastHighlightedIndex < plan.Slots.Count && lastHighlightedIndex != plan.FinalSlotIndex)
            {
                var prevSlot = plan.Slots[lastHighlightedIndex];
                prevSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 0f);
                prevSlot.Rect.localScale = Vector3.one;
            }

            var finalSlot = plan.FinalSlot;
            finalSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 1f);
            finalSlot.Rect.localScale = Vector3.one * 1.05f;

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
            var icon = slot.Icon;

            var initialFrameColor = SlotFrameColor;
            var targetFrameColor = FinalFrameColor;

            frame.color = initialFrameColor;

            // Multi-stage celebration effect
            const int pulseCount = 3;
            const float pulseDuration = CelebrateDuration / pulseCount;

            for (int pulse = 0; pulse < pulseCount; pulse++)
            {
                // Scale pulse
                var elapsed = 0f;
                while (elapsed < pulseDuration)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / pulseDuration);

                    // Frame color transition
                    frame.color = Color.Lerp(initialFrameColor, targetFrameColor, t);

                    // Pulsing scale effect
                    float scaleProgress = Mathf.Sin(t * Mathf.PI);
                    float scale = 1.05f + scaleProgress * 0.15f; // Pulse from 1.05 to 1.20 and back
                    slot.Rect.localScale = Vector3.one * scale;

                    // Glow intensity on icon outline
                    float glowIntensity = Mathf.Lerp(1f, HighlightIntensity, scaleProgress);
                    slot.IconOutline.effectColor = new Color(1f, 1f, 1f, glowIntensity);

                    // Icon brightness pulse
                    icon.color = Color.Lerp(Color.white, new Color(1.2f, 1.2f, 1.2f), scaleProgress * 0.5f);
                }
            }

            // Final state
            frame.color = targetFrameColor;
            slot.Rect.localScale = Vector3.one * 1.1f;
            slot.IconOutline.effectColor = new Color(1f, 1f, 1f, HighlightIntensity);
            icon.color = Color.white;

            // Continuous glow effect
            StartContinuousGlow(slot);

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

        /// <summary>
        /// Continuous glow effect on final slot
        /// </summary>
        private static async void StartContinuousGlow(Slot slot)
        {
            float startTime = Time.time;
            const float glowDuration = 2f; // Glow for 2 seconds

            while (Time.time - startTime < glowDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);

                float elapsed = Time.time - startTime;
                float glowCycle = Mathf.Sin(elapsed * GlowPulseSpeed * Mathf.PI);
                float glowAlpha = Mathf.Lerp(1f, HighlightIntensity, (glowCycle + 1f) * 0.5f);

                if (slot.IconOutline != null)
                {
                    slot.IconOutline.effectColor = new Color(1f, 1f, 0.8f, glowAlpha);
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
                    // FinalSlotIndex is calculated based on velocity curve distance
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
