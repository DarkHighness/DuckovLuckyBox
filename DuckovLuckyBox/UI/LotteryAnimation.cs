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

namespace DuckovLuckyBox.UI
{
    /// <summary>
    /// Manages lottery animation UI and playback
    /// </summary>
    public class LotteryAnimation
    {
        private static LotteryAnimation? _instance;
        public static LotteryAnimation Instance => _instance ??= new LotteryAnimation();

        private LotteryAnimation() { }

        private bool _isInitialized = false;

        private Canvas? _canvas;

        private RectTransform? _overlayRoot;
        private RectTransform? _viewport;
        private RectTransform? _itemsContainer;
        private Graphic? _centerPointer;
        private TextMeshProUGUI? _resultText;
        private CanvasGroup? _canvasGroup;
        private Sprite? _fallbackSprite;
        private bool _isAnimating;
        private bool _skipRequested;

        // Configuration constants
        private static readonly Vector2 IconSize = new Vector2(160f, 160f);
        private const float ItemSpacing = 16f;
        private const float SlotPadding = 24f;
        private static readonly float SlotFullWidth = IconSize.x + SlotPadding + ItemSpacing;
        private const int MinimumSlotsBeforeFinal = 140; // Minimum slots before final - adapted for 20 slots/s speed (requires more slots)
        private const int SlotsAfterFinal = 20; // Extra slots after final for visual buffer

        // Physics-based animation parameters (CSGO-style) - using slot as the unit
        private const float BaseInitialVelocityInSlots = 50f; // Base initial scroll speed (slots/second) - scroll 50 slots per second initially
        private const float MaxAnimationDurationInSeconds = 10f; // Maximum animation duration (seconds) - force stop if final slot not reached by this time

        // Animation precision and timing constants
        private const int AnimationStepsPerSecond = 100; // Samples per second for higher precision (every 0.01s)
        private const float VelocityAt7Seconds = 1f; // Velocity at 7 seconds (slots/second)
        private const float DecelerationDuration = 7.0f; // Duration of deceleration phase (seconds)
        private const float TotalCurveDuration = 8.5f; // Total duration of velocity curve (seconds)

        // Randomness parameters for animation variation
        // InitialVelocityRandomRange controls how much the animation can vary within the final slot
        // Larger values = more variation in stopping position within final slot
        private const float InitialVelocityRandomRange = 10.0f; // ±N slots/second variation on initial velocity for randomness within final slot

        // Per-animation state
        private float _currentAnimationInitialVelocity = BaseInitialVelocityInSlots;

        // Pre-calculated velocity curve for smooth deceleration
        private float[]? _velocityCurve = null;

        // Visual effect constants
        private const float GlowPulseSpeed = 3f; // Glow pulse frequency (Hz)
        private const float HighlightIntensity = 1.5f; // Highlight glow intensity multiplier

        private const float FadeDuration = 0.25f;
        private const float SkippedFadeDuration = 0.1f;
        private const float CelebrateDuration = 0.5f;
        private const float PointerThickness = 12f;

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color FinalFrameColor = new Color(0.95f, 0.8f, 0.35f, 1f);
        private static readonly Color SlotFrameColor = new Color(1f, 1f, 1f, 0.25f);

        /// <summary>
        /// Initializes the lottery animation UI with a full-screen canvas overlay
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

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

            // Main vertical line pointer (rounded capsule with inner stripe and subtle shadow)
            var pointerLineObj = new GameObject("PointerLine", typeof(RectTransform));
            var pointerLineRect = pointerLineObj.GetComponent<RectTransform>();
            pointerLineRect.SetParent(pointerContainer, false);
            pointerLineRect.anchorMin = new Vector2(0.5f, 0.5f);
            pointerLineRect.anchorMax = new Vector2(0.5f, 0.5f);
            pointerLineRect.pivot = new Vector2(0.5f, 0.5f);
            pointerLineRect.sizeDelta = new Vector2(PointerThickness * 1.1f, viewportHeight + 64f);
            pointerLineRect.anchoredPosition = Vector2.zero;

            // Rounded capsule for the pointer body
            var pointerLineGraphic = pointerLineObj.AddComponent<RoundedRectGraphic>();
            pointerLineGraphic.raycastTarget = false;
            pointerLineGraphic.cornerSegments = 8;
            pointerLineGraphic.cornerRadius = pointerLineRect.sizeDelta.x * 0.5f; // capsule ends
            pointerLineGraphic.color = new Color(1f, 1f, 1f, 0.95f); // near-opaque white

            // Subtle outline and shadow for polish
            var pointerLineOutline = pointerLineObj.AddComponent<Outline>();
            pointerLineOutline.effectColor = new Color(0f, 0f, 0f, 0.25f);
            pointerLineOutline.effectDistance = new Vector2(1f, -1f);
            pointerLineOutline.useGraphicAlpha = true;

            var pointerLineShadow = pointerLineObj.AddComponent<Shadow>();
            pointerLineShadow.effectColor = new Color(0f, 0f, 0f, 0.15f);
            pointerLineShadow.effectDistance = new Vector2(0f, -4f);

            // Thin vertical highlight stripe on the pointer body
            var stripeObj = new GameObject("PointerInnerStripe", typeof(RectTransform));
            var stripeRect = stripeObj.GetComponent<RectTransform>();
            stripeRect.SetParent(pointerLineRect, false);
            stripeRect.anchorMin = new Vector2(0.5f, 0.5f);
            stripeRect.anchorMax = new Vector2(0.5f, 0.5f);
            stripeRect.pivot = new Vector2(0.5f, 0.5f);
            stripeRect.sizeDelta = new Vector2(pointerLineRect.sizeDelta.x * 0.22f, pointerLineRect.sizeDelta.y * 0.82f);
            stripeRect.anchoredPosition = Vector2.zero;

            var stripe = stripeObj.AddComponent<RoundedRectGraphic>();
            stripe.raycastTarget = false;
            stripe.cornerSegments = 6;
            stripe.cornerRadius = stripeRect.sizeDelta.x * 0.5f;
            stripe.color = new Color(1f, 1f, 1f, 0.12f); // subtle vertical highlight

            // Top arrowhead (procedural triangle) - points up
            var topArrowObj = new GameObject("TopArrow", typeof(RectTransform));
            var topArrowRect = topArrowObj.GetComponent<RectTransform>();
            topArrowRect.SetParent(pointerContainer, false);
            topArrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            topArrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            topArrowRect.pivot = new Vector2(0.5f, 0.5f);
            topArrowRect.sizeDelta = new Vector2(PointerThickness * 2.5f, PointerThickness * 2.5f);
            topArrowRect.anchoredPosition = new Vector2(0f, viewportHeight * 0.5f + 32f);

            var topArrowGraphic = topArrowObj.AddComponent<TriangleGraphic>();
            topArrowGraphic.raycastTarget = false;
            topArrowGraphic.color = new Color(1f, 1f, 1f, 0.9f);
            topArrowGraphic.direction = TriangleGraphic.TriangleDirection.Up;

            var topArrowOutline = topArrowObj.AddComponent<Outline>();
            topArrowOutline.effectColor = new Color(1f, 1f, 1f, 0.4f);
            topArrowOutline.effectDistance = new Vector2(0.5f, -0.5f);
            topArrowOutline.useGraphicAlpha = false;
            // Subtle drop shadow for depth
            var topArrowShadow = topArrowObj.AddComponent<Shadow>();
            topArrowShadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            topArrowShadow.effectDistance = new Vector2(0f, -3f);

            // Inner highlight: smaller triangle inset
            var topInnerObj = new GameObject("TopArrowInner", typeof(RectTransform));
            var topInnerRect = topInnerObj.GetComponent<RectTransform>();
            topInnerRect.SetParent(topArrowRect, false);
            topInnerRect.anchorMin = Vector2.zero;
            topInnerRect.anchorMax = Vector2.one;
            topInnerRect.pivot = new Vector2(0.5f, 0.5f);
            // Inset by 15% to create a subtle highlight
            float inset = 0.15f;
            topInnerRect.sizeDelta = Vector2.zero;
            topInnerRect.anchoredPosition = Vector2.zero;
            topInnerRect.localScale = Vector3.one * (1f - inset);

            var topInner = topInnerObj.AddComponent<TriangleGraphic>();
            topInner.raycastTarget = false;
            topInner.color = new Color(1f, 1f, 1f, 0.12f); // light inner highlight (subtler)
            topInner.direction = TriangleGraphic.TriangleDirection.Up;

            // Bottom arrowhead (procedural triangle) - points down
            var bottomArrowObj = new GameObject("BottomArrow", typeof(RectTransform));
            var bottomArrowRect = bottomArrowObj.GetComponent<RectTransform>();
            bottomArrowRect.SetParent(pointerContainer, false);
            bottomArrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            bottomArrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            bottomArrowRect.pivot = new Vector2(0.5f, 0.5f);
            bottomArrowRect.sizeDelta = new Vector2(PointerThickness * 2.5f, PointerThickness * 2.5f);
            bottomArrowRect.anchoredPosition = new Vector2(0f, -viewportHeight * 0.5f - 32f);

            var bottomArrowGraphic = bottomArrowObj.AddComponent<TriangleGraphic>();
            bottomArrowGraphic.raycastTarget = false;
            bottomArrowGraphic.color = new Color(1f, 1f, 1f, 0.9f);
            bottomArrowGraphic.direction = TriangleGraphic.TriangleDirection.Down;

            var bottomArrowOutline = bottomArrowObj.AddComponent<Outline>();
            bottomArrowOutline.effectColor = new Color(1f, 1f, 1f, 0.4f);
            bottomArrowOutline.effectDistance = new Vector2(0.5f, -0.5f);
            bottomArrowOutline.useGraphicAlpha = false;
            // Subtle drop shadow for depth
            var bottomArrowShadow = bottomArrowObj.AddComponent<Shadow>();
            bottomArrowShadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            bottomArrowShadow.effectDistance = new Vector2(0f, 3f);

            // Inner highlight: smaller triangle inset
            var bottomInnerObj = new GameObject("BottomArrowInner", typeof(RectTransform));
            var bottomInnerRect = bottomInnerObj.GetComponent<RectTransform>();
            bottomInnerRect.SetParent(bottomArrowRect, false);
            bottomInnerRect.anchorMin = Vector2.zero;
            bottomInnerRect.anchorMax = Vector2.one;
            bottomInnerRect.pivot = new Vector2(0.5f, 0.5f);
            bottomInnerRect.sizeDelta = Vector2.zero;
            bottomInnerRect.anchoredPosition = Vector2.zero;
            bottomInnerRect.localScale = Vector3.one * (1f - inset);

            var bottomInner = bottomInnerObj.AddComponent<TriangleGraphic>();
            bottomInner.raycastTarget = false;
            bottomInner.color = new Color(1f, 1f, 1f, 0.12f); // light inner highlight (subtler)
            bottomInner.direction = TriangleGraphic.TriangleDirection.Down;

            _centerPointer = pointerLineGraphic;  // Store the main line for visibility control

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

            _isInitialized = true;
        }

        /// <summary>
        /// Plays the lottery animation
        /// </summary>
        public async UniTask PlayAsync(IEnumerable<int> candidateTypeIds, int finalTypeId, string finalDisplayName, Sprite? finalIcon)
        {
            // Check if animation is enabled in settings
            var enableAnimationValue = Core.Settings.SettingManager.Instance.EnableAnimation.Value;
            if (enableAnimationValue is bool enabled && !enabled)
            {
                return;
            }

            // Auto-initialize if not already initialized
            if (!_isInitialized)
            {
                Log.Debug("[LotteryAnimation] Auto-initializing");
                Initialize();
            }

            if (_isAnimating)
            {
                Log.Warning("Lottery animation is already in progress.");
                return;
            }

            // Generate random initial velocity for this animation to create variation within final slot
            _currentAnimationInitialVelocity = BaseInitialVelocityInSlots + UnityEngine.Random.Range(-InitialVelocityRandomRange, InitialVelocityRandomRange);
            Log.Debug($"[LotteryAnimation] Initial velocity: {_currentAnimationInitialVelocity} slots/s (base: {BaseInitialVelocityInSlots})");

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

                // If skip requested, skip the following animations
                if (!_skipRequested)
                {
                    // Celebration on final slot
                    await AnimateCelebration(plan, finalTypeId, sfxGroup);

                    // Reveal result and hold
                    await RevealResult(finalDisplayName);
                }
                else
                {
                    if (_resultText != null)
                    {
                        _resultText.text = string.Empty;
                    }
                }

                // Fade out
                await FadeCanvasGroup(_canvasGroup, 1f, 0f, !_skipRequested ? FadeDuration : SkippedFadeDuration);
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

        private bool TryBuildAnimationPlan(IEnumerable<int> candidateTypeIds, int finalTypeId, Sprite? finalIcon, out AnimationPlan plan)
        {
            plan = default!;

            if (_itemsContainer == null) return false;
            if (_viewport == null) return false;

            // Step 1: Generate velocity curve based on current animation's initial velocity
            _velocityCurve = GenerateVelocityCurve();

            // Step 2: Calculate total distance with high precision
            // We need to know exactly which slot we'll end up in and where within that slot
            float totalDistanceInPixels = CalculateTotalDistanceInPixels(_velocityCurve);

            // Calculate which slot and position within slot
            float slotWidth = SlotFullWidth;
            int completeSlots = Mathf.FloorToInt(totalDistanceInPixels / slotWidth);
            float remainingPixelsInFinalSlot = totalDistanceInPixels - (completeSlots * slotWidth);

            Log.Debug($"[Animation] Total distance: {totalDistanceInPixels}px = {completeSlots} complete slots + {remainingPixelsInFinalSlot}px");

            // Step 3: The stopping position within the final slot is already determined by the velocity curve
            // remainingPixelsInFinalSlot (from -100px to +100px relative to slot center) is the stopping offset
            // This variation comes from the randomized initial velocity, so no additional randomization needed here

            // Step 4: Calculate final slot index
            // The animation will stop in the slot at: startSlotIndex + completeSlots
            const int startSlotIndex = 20;
            int finalSlotIndex = startSlotIndex + completeSlots;

            // Total slots needed = final slot index + buffer slots after + 1
            int totalSlotsNeeded = finalSlotIndex + SlotsAfterFinal + 1;

            // Check if weighted lottery is enabled
            var enableWeightedLottery = Core.Settings.SettingManager.Instance.EnableWeightedLottery.GetAsBool();

            // Step 5: Build complete slot sequence
            var slotSequence = BuildSlotSequenceWithFixedFinal(candidateTypeIds, finalTypeId, enableWeightedLottery, totalSlotsNeeded, finalSlotIndex);
            if (slotSequence.Count == 0)
            {
                return false;
            }

            ClearItems();

            var totalWidth = slotSequence.Count * slotWidth;
            _itemsContainer.sizeDelta = new Vector2(totalWidth, 200f);

            var slots = new List<Slot>(slotSequence.Count);

            // Step 6: Create UI slots for each item in the sequence
            for (int i = 0; i < slotSequence.Count; i++)
            {
                var typeId = slotSequence[i];
                var isFinal = i == finalSlotIndex;

                var sprite = isFinal ? (finalIcon ?? RecycleService.GetItemIcon(typeId)) : RecycleService.GetItemIcon(typeId);
                var slot = CreateSlot(typeId, sprite, RecycleService.GetDisplayName(typeId), RecycleService.GetItemQualityColor(typeId));

                // Position: container is centered, so position is offset from center
                var positionX = i * slotWidth - totalWidth * 0.5f + slotWidth * 0.5f;
                slot.Rect.anchoredPosition = new Vector2(positionX, 0f);

                slots.Add(slot);
            }

            // Step 7: Calculate final offset
            // The animation will stop at: finalSlotCenter + offsetWithinSlot
            // offsetWithinSlot = remainingPixelsInFinalSlot - slotWidth/2 (convert from [0, slotWidth] to [-slotWidth/2, slotWidth/2])
            var finalSlotCenterX = slots[finalSlotIndex].Rect.anchoredPosition.x;
            float slotHalfWidth = slotWidth * 0.5f;
            float offsetWithinSlot = remainingPixelsInFinalSlot - slotHalfWidth;
            var finalStopPositionX = finalSlotCenterX + offsetWithinSlot;
            var endOffset = -finalStopPositionX;

            Log.Debug($"[Animation] Final slot center: {finalSlotCenterX}, Remaining in slot: {remainingPixelsInFinalSlot}px, Offset from center: {offsetWithinSlot}px, Stop at: {finalStopPositionX}, endOffset: {endOffset}");

            // Step 8: Calculate start offset
            var startItemPos = slots[startSlotIndex].Rect.anchoredPosition.x;
            var startOffset = -startItemPos;

            plan = new AnimationPlan(slots, finalSlotIndex, startOffset, endOffset);
            return true;
        }

        /// <summary>
        /// Build slot sequence with final item at a specific index
        /// </summary>
        private List<int> BuildSlotSequenceWithFixedFinal(IEnumerable<int> candidateTypeIds, int finalTypeId, bool useWeightedLottery, int totalSlots, int finalSlotIndex)
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
                    var selectedId = LotteryService.SampleWeightedItems(weightedItemsCache);
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

        private Slot CreateSlot(int typeId, Sprite? sprite, string displayName, Color frameColor)
        {
            // Root acts as the slot container and is what we position inside the items container
            var root = new GameObject($"LotterySlot_{typeId}", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(_itemsContainer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(IconSize.x + SlotPadding, IconSize.y + SlotPadding);

            // Background slightly larger than the frame to act as a border
            // Increase padding to make the border thicker. Change color below to set border color.
            const float borderPadding = 16f; // increased border thickness
            var backgroundObj = new GameObject("LotterySlotBackground", typeof(RectTransform));
            var bgRect = backgroundObj.GetComponent<RectTransform>();
            bgRect.SetParent(rect, false);
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = rect.sizeDelta + new Vector2(borderPadding, borderPadding);

            // Use a rounded rect graphic for the border so mods don't need external sprites.
            var bgGraphic = backgroundObj.AddComponent<RoundedRectGraphic>();
            bgGraphic.raycastTarget = false;
            bgGraphic.cornerRadius = Mathf.Min(rect.sizeDelta.x, rect.sizeDelta.y) * 0.12f; // adaptive radius
            bgGraphic.cornerSegments = 6;
            bgGraphic.color = new Color(0f, 0f, 0f, 0.8f); // black with 80% opacity

            // Frame (the colored background for quality) sits above the white background
            var frameObj = new GameObject("LotterySlotFrame", typeof(RectTransform));
            var frameRect = frameObj.GetComponent<RectTransform>();
            frameRect.SetParent(rect, false);
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.sizeDelta = rect.sizeDelta; // Match root size (icon + padding)

            var frameGraphic = frameObj.AddComponent<RoundedRectGraphic>();
            // Match the frame size to rect
            frameGraphic.cornerRadius = Mathf.Min(frameRect.sizeDelta.x, frameRect.sizeDelta.y) * 0.1f;
            frameGraphic.cornerSegments = 6;
            // Force opaque alpha so the border underneath remains visible
            frameGraphic.color = new Color(frameColor.r, frameColor.g, frameColor.b, 1f);
            frameGraphic.raycastTarget = false;

            var frameMask = frameObj.AddComponent<Mask>();
            frameMask.showMaskGraphic = true;

            // Icon is clipped by the frame mask
            var iconObject = new GameObject("LotterySlotIcon", typeof(RectTransform), typeof(Image));
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(frameRect, false);
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

            // Return the root RectTransform as Slot.Rect (used for positioning/scaling). The frame remains the visual frame.
            return new Slot(rect, frameGraphic, icon, iconOutline, displayName);
        }

        /// <summary>
        /// Generate velocity curve for smooth deceleration from initial to minimum velocity over target duration
        /// Custom curve: velocity at 7s is 0.1 slots/s, velocity at 8s is 0 (complete stop)
        /// Uses the current animation's initial velocity for variation
        /// </summary>
        private float[] GenerateVelocityCurve()
        {
            // AnimationStepsPerSecond samples per second for higher precision
            int totalSteps = (int)(TotalCurveDuration * AnimationStepsPerSecond);
            var curve = new float[totalSteps];

            for (int i = 0; i < totalSteps; i++)
            {
                // Current sample point corresponding time
                float timeInSeconds = (float)i / AnimationStepsPerSecond;

                if (timeInSeconds < DecelerationDuration)
                {
                    // First 7 seconds: decelerate from current initial velocity to 0.1, using ease-out quartic curve for faster deceleration
                    float t = timeInSeconds / DecelerationDuration; // 0.0 to 1.0
                    float easeT = 1f - Mathf.Pow(1f - t, 4f);
                    curve[i] = Mathf.Lerp(_currentAnimationInitialVelocity, VelocityAt7Seconds, easeT);
                }
                else
                {
                    // 7-8 seconds: linearly decelerate from 1 to 0.0 (complete stop)
                    float t = (timeInSeconds - DecelerationDuration) / (TotalCurveDuration - DecelerationDuration); // 0.0 to 1.0
                    curve[i] = Mathf.Lerp(VelocityAt7Seconds, 0f, t);
                }
            }

            return curve;
        }

        /// <summary>
        /// Calculate total distance traveled based on velocity curve
        /// </summary>
        private float CalculateTotalDistanceInSlots(float[] velocityCurve)
        {
            float timeStepInSeconds = 1.0f / AnimationStepsPerSecond; // Each sample point time interval
            float totalDistance = 0f;

            for (int i = 0; i < velocityCurve.Length; i++)
            {
                // Distance = velocity × time
                totalDistance += velocityCurve[i] * timeStepInSeconds;
            }

            return totalDistance;
        }

        /// <summary>
        /// Calculate total distance in pixels (slots × SlotFullWidth)
        /// </summary>
        private float CalculateTotalDistanceInPixels(float[] velocityCurve)
        {
            float distanceInSlots = CalculateTotalDistanceInSlots(velocityCurve);
            return distanceInSlots * SlotFullWidth;
        }

        /// <summary>
        /// Physics-based rolling animation (CSGO-style smooth deceleration)
        /// </summary>
        private async UniTask PerformPhysicsBasedRoll(AnimationPlan plan, ChannelGroup sfxGroup)
        {
            if (_itemsContainer == null) return;

            // Regenerate velocity curve based on current initial velocity for this animation
            _velocityCurve = GenerateVelocityCurve();

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

            while (true)
            {
                // Check for skip request
                if (Input.GetMouseButtonDown(0))
                {
                    _skipRequested = true;

                    // Get the SFX group and play skip sound
                    var result = sfxGroup.stop();
                    if (result != FMOD.RESULT.OK)
                    {
                        Log.Warning($"Failed to stop rolling sound: {result}");
                    }

                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
                float deltaTime = Time.deltaTime;
                elapsedTime += deltaTime;

                // DEBUG: Log elapsed time and distance to target
                float distanceToTarget = Mathf.Abs(targetPositionInPixels - currentPositionInPixels);
                Log.Debug($"[Animation] Elapsed: {elapsedTime:F2}s, Distance to target: {distanceToTarget:F1}px ({distanceToTarget / SlotFullWidth:F2} slots)");

                // Look up current velocity from pre-generated velocity curve
                // Use FloorToInt: round down to nearest sample point
                // This avoids array bounds and ensures we use "current or previous" velocity value
                int curveIndex = Mathf.FloorToInt(elapsedTime * AnimationStepsPerSecond); // One sample every time step
                curveIndex = Mathf.Clamp(curveIndex, 0, _velocityCurve.Length - 1);
                float currentVelocityInSlots = _velocityCurve[curveIndex] * velocityDirection;

                // Convert slot/s to pixel/s, then calculate displacement
                float velocityInPixels = currentVelocityInSlots * SlotFullWidth;
                float movementInPixels = velocityInPixels * deltaTime;
                float nextPositionInPixels = currentPositionInPixels + movementInPixels;

                // Primary stopping condition: Check if next position reaches or overshoots target
                if ((velocityDirection == -1 && nextPositionInPixels <= targetPositionInPixels) ||
                    (velocityDirection == 1 && nextPositionInPixels >= targetPositionInPixels))
                {
                    // Clamp to target position and stop
                    currentPositionInPixels = targetPositionInPixels;
                    _itemsContainer.anchoredPosition = new Vector2(currentPositionInPixels, 0f);
                    Log.Debug($"[Animation] Reached target position at {elapsedTime:F2}s (next: {nextPositionInPixels:F1}, target: {targetPositionInPixels:F1})");
                    break;
                }

                currentPositionInPixels = nextPositionInPixels;

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

                // Secondary condition: Force stop after extended duration if target not reached
                // This allows for slight timing variations but prevents infinite loops
                if (elapsedTime >= MaxAnimationDurationInSeconds)
                {
                    Log.Warning($"[Animation] Force stopped after {elapsedTime:F2}s - target position not reached (pos: {currentPositionInPixels:F1}, target: {targetPositionInPixels:F1})");
                    break;
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

        private int FindCenteredSlotIndex(AnimationPlan plan, float currentOffset)
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

        private async UniTask AnimateCelebration(AnimationPlan plan, int finalTypeId, ChannelGroup sfxGroup)
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

            // Play high-quality lottery sound if enabled
            var finalItemQuality = RecycleService.GetItemQuality(finalTypeId);

            if (finalItemQuality.IsHighQuality())
            {
                SoundUtils.PlayHighQualitySound(sfxGroup, Constants.Sound.HIGH_QUALITY_LOTTERY_SOUND);
            }
        }

        /// <summary>
        /// Continuous glow effect on final slot
        /// </summary>
        private async void StartContinuousGlow(Slot slot)
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

        private void ResetResultText()
        {
            if (_resultText == null) return;

            var textColor = _resultText.color;
            textColor.a = 0f;
            _resultText.color = textColor;
            _resultText.text = string.Empty;
        }

        private async UniTask RevealResult(string finalDisplayName)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.DeltaTime, PlayerLoopTiming.Update, default);

            if (_resultText != null)
            {
                _resultText.text = finalDisplayName;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.75f), DelayType.DeltaTime, PlayerLoopTiming.Update, default);
        }

        private Sprite EnsureFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            var texture = Texture2D.whiteTexture;
            _fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return _fallbackSprite;
        }

        private void ClearItems()
        {
            if (_itemsContainer == null) return;

            for (int i = _itemsContainer.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_itemsContainer.GetChild(i).gameObject);
            }
        }

        private async UniTask FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
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

        /// <summary>
        /// Destroys the Lottery animation UI
        /// </summary>
        public void Destroy()
        {
            if (_overlayRoot != null)
            {
                UnityEngine.Object.Destroy(_overlayRoot.gameObject);
                _overlayRoot = null;
            }

            if (_canvas != null)
            {
                UnityEngine.Object.Destroy(_canvas.gameObject);
                _canvas = null;
            }

            _viewport = null;
            _itemsContainer = null;
            _centerPointer = null;
            _resultText = null;
            _canvasGroup = null;
            _fallbackSprite = null;
            _isAnimating = false;
            _skipRequested = false;
            _velocityCurve = null;
            _isInitialized = false;
            _instance = null;
        }

        private readonly struct Slot
        {
            public RectTransform Rect { get; }
            public Graphic Frame { get; }
            public Image Icon { get; }
            public Outline IconOutline { get; }
            public string DisplayName { get; }

            public Slot(RectTransform rect, Graphic frame, Image icon, Outline iconOutline, string displayName)
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

/// <summary>
/// Simple MaskableGraphic that draws a filled rounded rectangle by generating vertices.
/// Useful for mods where adding external 9-sliced sprites is inconvenient.
/// </summary>
internal class RoundedRectGraphic : MaskableGraphic
{
    public float cornerRadius = 8f;
    public int cornerSegments = 6;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var rect = GetPixelAdjustedRect();
        float hw = rect.width * 0.5f;
        float hh = rect.height * 0.5f;

        float r = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(hw, hh));
        int seg = Mathf.Max(1, cornerSegments);

        // Center is (0,0) since vertices are relative to RectTransform pivot by default
        var points = new List<Vector2>();

        if (r <= 0f)
        {
            // Simple rectangle
            points.Add(new Vector2(hw, hh));
            points.Add(new Vector2(-hw, hh));
            points.Add(new Vector2(-hw, -hh));
            points.Add(new Vector2(hw, -hh));
        }
        else
        {
            // corner centers relative to rect center
            var tr = new Vector2(hw - r, hh - r);
            var tl = new Vector2(-hw + r, hh - r);
            var bl = new Vector2(-hw + r, -hh + r);
            var br = new Vector2(hw - r, -hh + r);

            // Top-right corner: angles 0 -> 90
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Deg2Rad * (0f + (90f * i / seg));
                points.Add(tr + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }

            // Top-left corner: 90 -> 180
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Deg2Rad * (90f + (90f * i / seg));
                points.Add(tl + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }

            // Bottom-left corner: 180 -> 270
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Deg2Rad * (180f + (90f * i / seg));
                points.Add(bl + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }

            // Bottom-right corner: 270 -> 360
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Deg2Rad * (270f + (90f * i / seg));
                points.Add(br + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
        }

        // Add center vertex
        UIVertex centerV = UIVertex.simpleVert;
        centerV.position = Vector3.zero;
        centerV.color = color;
        centerV.uv0 = new Vector2(0.5f, 0.5f);
        vh.AddVert(centerV);
        int centerIndex = 0;

        // Add perimeter vertices
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            UIVertex v = UIVertex.simpleVert;
            v.position = new Vector3(p.x, p.y, 0f);
            v.color = color;
            // UV mapping normalized to rect
            v.uv0 = new Vector2((p.x + hw) / (rect.width == 0 ? 1f : rect.width), (p.y + hh) / (rect.height == 0 ? 1f : rect.height));
            vh.AddVert(v);
        }

        int vertsCount = points.Count + 1; // including center
        // Add triangles as a fan from center
        for (int i = 0; i < points.Count; i++)
        {
            int a = centerIndex;
            int b = 1 + i;
            int c = 1 + ((i + 1) % points.Count);
            vh.AddTriangle(a, b, c);
        }
    }
}

/// <summary>
/// Procedural triangle graphic. Direction can be Up or Down.
/// </summary>
internal class TriangleGraphic : MaskableGraphic
{
    public enum TriangleDirection { Up, Down }

    public TriangleDirection direction = TriangleDirection.Up;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var rect = GetPixelAdjustedRect();
        float hw = rect.width * 0.5f;
        float hh = rect.height * 0.5f;

        Vector2 p0, p1, p2;
        if (direction == TriangleDirection.Up)
        {
            p0 = new Vector2(0f, hh); // top center
            p1 = new Vector2(-hw, -hh); // bottom-left
            p2 = new Vector2(hw, -hh); // bottom-right
        }
        else
        {
            p0 = new Vector2(0f, -hh); // bottom center
            p1 = new Vector2(-hw, hh); // top-left
            p2 = new Vector2(hw, hh); // top-right
        }

        UIVertex v0 = UIVertex.simpleVert;
        v0.position = new Vector3(p0.x, p0.y, 0f);
        v0.color = color;
        v0.uv0 = new Vector2((p0.x + hw) / (rect.width == 0 ? 1f : rect.width), (p0.y + hh) / (rect.height == 0 ? 1f : rect.height));

        UIVertex v1 = UIVertex.simpleVert;
        v1.position = new Vector3(p1.x, p1.y, 0f);
        v1.color = color;
        v1.uv0 = new Vector2((p1.x + hw) / (rect.width == 0 ? 1f : rect.width), (p1.y + hh) / (rect.height == 0 ? 1f : rect.height));

        UIVertex v2 = UIVertex.simpleVert;
        v2.position = new Vector3(p2.x, p2.y, 0f);
        v2.color = color;
        v2.uv0 = new Vector2((p2.x + hw) / (rect.width == 0 ? 1f : rect.width), (p2.y + hh) / (rect.height == 0 ? 1f : rect.height));

        vh.AddVert(v0);
        vh.AddVert(v1);
        vh.AddVert(v2);

        vh.AddTriangle(0, 1, 2);
    }
}
