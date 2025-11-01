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
        private sealed class LaneUI
        {
            public LaneUI(string name, RectTransform root, RectTransform viewport, RectTransform itemsContainer, GameObject pointerRoot, Graphic pointerGraphic, Color pointerActiveColor, TextMeshProUGUI resultText, float laneHeight)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Lane name cannot be null or empty.", nameof(name));
                }

                Name = name;
                Root = root ?? throw new ArgumentNullException(nameof(root));
                Viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
                ItemsContainer = itemsContainer ?? throw new ArgumentNullException(nameof(itemsContainer));
                PointerRoot = pointerRoot ?? throw new ArgumentNullException(nameof(pointerRoot));
                PointerGraphic = pointerGraphic ?? throw new ArgumentNullException(nameof(pointerGraphic));
                PointerActiveColor = pointerActiveColor;
                PointerInactiveColor = new Color(pointerActiveColor.r, pointerActiveColor.g, pointerActiveColor.b, 0f);
                ResultText = resultText ?? throw new ArgumentNullException(nameof(resultText));
                LaneHeight = Mathf.Max(laneHeight, 1f);

                if (ResultText != null)
                {
                    ResultText.text = string.Empty;
                    var color = ResultText.color;
                    color.a = 0f;
                    ResultText.color = color;
                }
            }

            public string Name { get; }
            public RectTransform Root { get; }
            public RectTransform Viewport { get; }
            public RectTransform ItemsContainer { get; }
            public GameObject PointerRoot { get; }
            public Graphic PointerGraphic { get; }
            public TextMeshProUGUI ResultText { get; } = null!;
            public float LaneHeight { get; }

            private Color PointerActiveColor { get; }
            private Color PointerInactiveColor { get; }

            public void ResetVisualState()
            {
                SetPointerVisible(false);
                if (ItemsContainer != null)
                {
                    ItemsContainer.anchoredPosition = Vector2.zero;
                }

                if (ResultText != null)
                {
                    ResultText.text = string.Empty;
                    var color = ResultText.color;
                    color.a = 0f;
                    ResultText.color = color;
                }
            }

            public void SetPointerVisible(bool visible)
            {
                PointerRoot?.SetActive(visible);

                if (PointerGraphic != null)
                {
                    PointerGraphic.color = visible ? PointerActiveColor : PointerInactiveColor;
                }
            }

            public void SetResultVisibility(bool visible)
            {
                if (ResultText == null) return;

                var color = ResultText.color;
                color.a = visible ? 1f : 0f;
                ResultText.color = color;
            }

            public void SetResultText(string text)
            {
                if (ResultText == null) return;
                ResultText.text = text;
            }
        }

        private readonly struct LaneResult
        {
            public LaneResult(int typeId, string displayName, Sprite? icon, bool isRewardLane)
            {
                TypeId = typeId;
                DisplayName = displayName;
                Icon = icon;
                IsRewardLane = isRewardLane;
            }

            public int TypeId { get; }
            public string DisplayName { get; }
            public Sprite? Icon { get; }
            public bool IsRewardLane { get; }
        }

        public readonly struct LaneFinalData
        {
            public LaneFinalData(int typeId, string displayName, Sprite? icon, bool isRewardLane = true)
            {
                TypeId = typeId;
                DisplayName = displayName;
                Icon = icon;
                IsRewardLane = isRewardLane;
            }

            public int TypeId { get; }
            public string DisplayName { get; }
            public Sprite? Icon { get; }
            public bool IsRewardLane { get; }
        }

        private sealed class AnimationPlan
        {
            public AnimationPlan(List<Slot> slots, int finalSlotIndex, float startOffset, float finalOffset, float[] velocityCurve, bool reverseDirection, float initialVelocityInSlots)
            {
                Slots = slots;
                FinalSlotIndex = finalSlotIndex;
                StartOffset = startOffset;
                FinalOffset = finalOffset;
                VelocityCurve = velocityCurve;
                ReverseDirection = reverseDirection;
                InitialVelocityInSlots = initialVelocityInSlots;
            }

            public List<Slot> Slots { get; }
            public int FinalSlotIndex { get; }
            public float StartOffset { get; }
            public float FinalOffset { get; }
            public float[] VelocityCurve { get; }
            public bool ReverseDirection { get; }
            public float InitialVelocityInSlots { get; }

            public Slot FinalSlot
            {
                get
                {
                    if (FinalSlotIndex >= 0 && FinalSlotIndex < Slots.Count)
                    {
                        return Slots[FinalSlotIndex];
                    }

                    Log.Warning($"Invalid FinalSlotIndex: {FinalSlotIndex}, Slots.Count: {Slots.Count}. Returning last slot.");
                    return Slots[Slots.Count - 1];
                }
            }
        }

        private static LotteryAnimation? _instance;
        public static LotteryAnimation Instance => _instance ??= new LotteryAnimation();

        private LotteryAnimation() { }

        private bool _isInitialized;

        private Canvas? _canvas;
        private RectTransform? _overlayRoot;
        private CanvasGroup? _canvasGroup;

    private readonly List<LaneUI> _lanes = new List<LaneUI>();
        private int _currentLaneCount = 0;

        private Sprite? _fallbackSprite;
        private bool _isAnimating;
        private bool _skipRequested;

        // Configuration constants
        private static readonly Vector2 IconSize = new Vector2(160f, 160f);
        private const float ItemSpacing = 16f;
        private const float SlotPadding = 24f;
        private static readonly float SlotFullWidth = IconSize.x + SlotPadding + ItemSpacing;
        private const int SlotsAfterFinal = 20;

        private const float BaseInitialVelocityInSlots = 50f;
        private const float MaxAnimationDurationInSeconds = 10f;
        private const int AnimationStepsPerSecond = 100;
        private const float VelocityAt7Seconds = 1f;
        private const float DecelerationDuration = 7.0f;
        private const float TotalCurveDuration = 8.5f;
        private const float InitialVelocityRandomRange = 10.0f;

        private const float GlowPulseSpeed = 3f;
        private const float HighlightIntensity = 1.5f;

        private const float FadeDuration = 0.25f;
        private const float SkippedFadeDuration = 0.1f;
        private const float CelebrateDuration = 0.5f;
        private const float PointerThickness = 12f;
        private const float ViewportHorizontalPadding = 120f;
        private const float LaneVerticalSpacing = 48f;
        private const float MinLaneHeight = 140f;
        private const float MaxLaneHeight = 240f;

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color FinalFrameColor = new Color(0.95f, 0.8f, 0.35f, 1f);
        private static readonly Color SlotFrameColor = new Color(1f, 1f, 1f, 0.25f);

        /// <summary>
        /// Initializes the lottery animation UI with a full-screen canvas overlay
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

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

            _isInitialized = true;
        }

        /// <summary>
        /// Plays the lottery animation across one to three lanes depending on the provided results.
        /// </summary>
        public async UniTask PlayAsync(IEnumerable<int> candidateTypeIds, IReadOnlyList<LaneFinalData> finalLaneData)
        {
            var enableAnimationValue = Core.Settings.SettingManager.Instance.EnableAnimation.Value;
            if (enableAnimationValue is bool enabled && !enabled)
            {
                return;
            }

            if (finalLaneData == null || finalLaneData.Count == 0)
            {
                Log.Warning("[LotteryAnimation] No final lane data provided. Skipping animation.");
                return;
            }

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

            var candidatePool = candidateTypeIds?.ToList() ?? new List<int>();
            int requestedLaneCount = Mathf.Clamp(finalLaneData.Count, 1, 3);

            EnsureLanes(requestedLaneCount);

            if (_overlayRoot == null || _canvasGroup == null || _lanes.Count == 0)
            {
                Log.Error("Lottery animation failed to initialize properly.");
                return;
            }

            var laneResults = BuildLaneResults(candidatePool, finalLaneData);
            if (laneResults.Count == 0)
            {
                Log.Warning("[LotteryAnimation] No lane results could be prepared. Skipping animation.");
                return;
            }
            if (laneResults.Count < _lanes.Count)
            {
                EnsureLanes(laneResults.Count);
            }

            if (laneResults.Count > _lanes.Count)
            {
                laneResults = laneResults.Take(_lanes.Count).ToList();
            }

            if (_lanes.Count == 0 || laneResults.Count == 0)
            {
                Log.Warning("[LotteryAnimation] No lanes available to animate. Skipping animation.");
                return;
            }

            var laneDirections = new List<bool>(_lanes.Count);
            for (int i = 0; i < _lanes.Count; i++)
            {
                laneDirections.Add(UnityEngine.Random.value > 0.5f);
            }

            if (_lanes.Count > 1 && laneDirections.TrueForAll(d => d == laneDirections[0]))
            {
                laneDirections[0] = !laneDirections[0];
            }

            var laneInitialVelocities = new List<float>(_lanes.Count);
            var laneDecelerationDurations = new List<float>(_lanes.Count);
            var laneVelocityAtDecelerations = new List<float>(_lanes.Count);
            var laneTotalCurveDurations = new List<float>(_lanes.Count);
            for (int i = 0; i < _lanes.Count; i++)
            {
                float initialVelocity = BaseInitialVelocityInSlots + UnityEngine.Random.Range(-InitialVelocityRandomRange, InitialVelocityRandomRange);
                initialVelocity += i * 1.5f;
                initialVelocity = Mathf.Max(10f, initialVelocity);
                laneInitialVelocities.Add(initialVelocity);

                float decelDuration = DecelerationDuration + UnityEngine.Random.Range(-0.75f, 0.75f);
                float clampedDecelDuration = Mathf.Clamp(decelDuration, 5.5f, 8.5f);
                laneDecelerationDurations.Add(clampedDecelDuration);

                float velocityAtDecelEnd = VelocityAt7Seconds + UnityEngine.Random.Range(-0.4f, 0.4f);
                float clampedVelocityAtDecelEnd = Mathf.Clamp(velocityAtDecelEnd, 0.2f, 2.0f);
                laneVelocityAtDecelerations.Add(clampedVelocityAtDecelEnd);

                float totalCurveDuration = TotalCurveDuration + UnityEngine.Random.Range(-0.5f, 0.5f);
                float clampedTotalCurveDuration = Mathf.Clamp(totalCurveDuration, clampedDecelDuration + 0.5f, clampedDecelDuration + 2.5f);
                laneTotalCurveDurations.Add(clampedTotalCurveDuration);
            }

            var activeLanes = new List<(LaneUI lane, AnimationPlan plan, LaneResult result, int originalIndex)>(laneResults.Count);

            for (int i = 0; i < laneResults.Count && i < _lanes.Count; i++)
            {
                var lane = _lanes[i];
                var laneResult = laneResults[i];
                bool reverseDirection = laneDirections[Mathf.Min(i, laneDirections.Count - 1)];
                float initialVelocityInSlots = laneInitialVelocities[Mathf.Min(i, laneInitialVelocities.Count - 1)];
                float decelerationDuration = laneDecelerationDurations[Mathf.Min(i, laneDecelerationDurations.Count - 1)];
                float velocityAtDecelEnd = laneVelocityAtDecelerations[Mathf.Min(i, laneVelocityAtDecelerations.Count - 1)];
                float totalCurveDuration = laneTotalCurveDurations[Mathf.Min(i, laneTotalCurveDurations.Count - 1)];

                Log.Debug($"[LotteryAnimation] Lane {lane.Name} initial velocity: {initialVelocityInSlots:F2} slots/s, decelDuration: {decelerationDuration:F2}s, endVel: {velocityAtDecelEnd:F2} slots/s, totalDuration: {totalCurveDuration:F2}s, direction: {(reverseDirection ? "RightToLeft" : "LeftToRight")}");

                if (!TryBuildAnimationPlan(lane, candidatePool, laneResult.TypeId, laneResult.Icon, initialVelocityInSlots, decelerationDuration, velocityAtDecelEnd, totalCurveDuration, reverseDirection, out var plan))
                {
                    Log.Warning($"Failed to prepare lottery animation plan for lane {lane.Name}. Lane will be skipped.");
                    lane.ResetVisualState();
                    lane.SetPointerVisible(false);
                    lane.SetResultVisibility(false);
                    lane.SetResultText(string.Empty);
                    continue;
                }

                lane.ResetVisualState();
                lane.SetResultVisibility(true);
                lane.SetResultText(string.Empty);
                lane.SetPointerVisible(true);
                activeLanes.Add((lane, plan, laneResult, i));
            }

            if (activeLanes.Count == 0)
            {
                Log.Warning("[LotteryAnimation] No lanes could be prepared successfully. Skipping animation.");
                return;
            }

            _isAnimating = true;
            _skipRequested = false;

            try
            {
                if (_canvas != null && !_canvas.gameObject.activeSelf)
                {
                    _canvas.gameObject.SetActive(true);
                }

                _overlayRoot.gameObject.SetActive(true);
                _canvasGroup.blocksRaycasts = true;

                await FadeCanvasGroup(_canvasGroup, 0f, 1f, FadeDuration);

                RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup sfxGroup);
                SoundUtils.PlaySound(Constants.Sound.ROLLING_SOUND, sfxGroup);

                int primaryLaneIndex = activeLanes.FindIndex(entry => entry.originalIndex == 1);
                if (primaryLaneIndex < 0)
                {
                    primaryLaneIndex = 0;
                }

                var rollTasks = new List<UniTask>(activeLanes.Count);
                for (int i = 0; i < activeLanes.Count; i++)
                {
                    bool canHandleSkip = i == primaryLaneIndex;
                    var entry = activeLanes[i];
                    rollTasks.Add(PerformPhysicsBasedRoll(entry.lane, entry.plan, canHandleSkip, sfxGroup));
                }

                await UniTask.WhenAll(rollTasks);

                if (!_skipRequested)
                {
                    var celebrationTasks = new List<UniTask>(activeLanes.Count);
                    for (int i = 0; i < activeLanes.Count; i++)
                    {
                        var entry = activeLanes[i];
                        celebrationTasks.Add(AnimateCelebration(entry.lane, entry.plan, entry.result.TypeId, entry.result.IsRewardLane, sfxGroup));
                    }

                    await UniTask.WhenAll(celebrationTasks);

                    var revealTasks = new List<UniTask>(activeLanes.Count);
                    for (int i = 0; i < activeLanes.Count; i++)
                    {
                        var entry = activeLanes[i];
                        revealTasks.Add(RevealResult(entry.lane, entry.result.DisplayName));
                    }

                    await UniTask.WhenAll(revealTasks);
                }
                else
                {
                    foreach (var entry in activeLanes)
                    {
                        entry.lane.SetResultText(string.Empty);
                    }
                }

                await FadeCanvasGroup(_canvasGroup, 1f, 0f, !_skipRequested ? FadeDuration : SkippedFadeDuration);
            }
            finally
            {
                _isAnimating = false;

                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                    _canvasGroup.blocksRaycasts = false;
                }

                _overlayRoot?.gameObject.SetActive(false);

                _canvas?.gameObject.SetActive(false);

                foreach (var lane in _lanes)
                {
                    lane.ResetVisualState();
                    ClearItems(lane);
                }

                _skipRequested = false;
            }
        }

        private void EnsureLanes(int laneCount)
        {
            if (_overlayRoot == null || _canvas == null)
            {
                return;
            }

            laneCount = Mathf.Clamp(laneCount, 1, 3);

            if (_currentLaneCount == laneCount && _lanes.Count == laneCount)
            {
                foreach (var lane in _lanes)
                {
                    lane.ResetVisualState();
                    ClearItems(lane);
                }
                return;
            }

            foreach (var lane in _lanes)
            {
                if (lane.Root != null)
                {
                    UnityEngine.Object.Destroy(lane.Root.gameObject);
                }
            }

            _lanes.Clear();

            var canvasRect = _canvas.GetComponent<RectTransform>();
            var canvasSize = canvasRect.rect.size;

            float laneHeight = CalculateLaneHeight(laneCount, canvasSize.y);
            float totalHeight = laneCount * laneHeight + LaneVerticalSpacing * (laneCount - 1);
            float topOffset = laneCount == 1 ? 0f : (totalHeight - laneHeight) * 0.5f;

            for (int i = 0; i < laneCount; i++)
            {
                float verticalOffset = laneCount == 1 ? 0f : topOffset - i * (laneHeight + LaneVerticalSpacing);
                var lane = CreateLane($"LotteryLane_{i}", laneHeight, canvasSize.x, verticalOffset);
                lane.ResetVisualState();
                ClearItems(lane);
                _lanes.Add(lane);
            }

            _currentLaneCount = laneCount;
        }

        private float CalculateLaneHeight(int laneCount, float canvasHeight)
        {
            float totalSpacing = LaneVerticalSpacing * (laneCount - 1);
            float minimumRequired = laneCount * MinLaneHeight + totalSpacing;
            float availableHeight = Mathf.Max(canvasHeight * 0.75f, minimumRequired);
            float laneHeight = (availableHeight - totalSpacing) / laneCount;
            return Mathf.Clamp(laneHeight, MinLaneHeight, MaxLaneHeight);
        }

        private LaneUI CreateLane(string laneName, float laneHeight, float canvasWidth, float verticalOffset)
        {
            if (_overlayRoot == null)
            {
                throw new InvalidOperationException("Overlay root is not initialized.");
            }

            var laneRoot = new GameObject(laneName, typeof(RectTransform)).GetComponent<RectTransform>();
            laneRoot.SetParent(_overlayRoot, false);
            laneRoot.anchorMin = new Vector2(0f, 0.5f);
            laneRoot.anchorMax = new Vector2(1f, 0.5f);
            laneRoot.pivot = new Vector2(0.5f, 0.5f);
            laneRoot.sizeDelta = new Vector2(0f, laneHeight);
            laneRoot.anchoredPosition = new Vector2(0f, verticalOffset);

            float clampedPadding = Mathf.Clamp(ViewportHorizontalPadding, 0f, canvasWidth * 0.25f);

            var viewport = new GameObject($"{laneName}_Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(laneRoot, false);
            viewport.anchorMin = new Vector2(0f, 0.5f);
            viewport.anchorMax = new Vector2(1f, 0.5f);
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.sizeDelta = new Vector2(-2f * clampedPadding, laneHeight);
            viewport.anchoredPosition = Vector2.zero;

            var backgroundObj = new GameObject($"{laneName}_Background", typeof(RectTransform), typeof(Image));
            var bgRect = backgroundObj.GetComponent<RectTransform>();
            bgRect.SetParent(viewport, false);
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(0f, laneHeight);

            var bgImage = backgroundObj.GetComponent<Image>();
            bgImage.sprite = EnsureFallbackSprite();
            bgImage.type = Image.Type.Simple;
            bgImage.color = new Color(0f, 0f, 0f, 0.5f);
            bgImage.raycastTarget = false;

            var itemsContainer = new GameObject($"{laneName}_ItemsContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            itemsContainer.SetParent(viewport, false);
            itemsContainer.anchorMin = new Vector2(0.5f, 0.5f);
            itemsContainer.anchorMax = new Vector2(0.5f, 0.5f);
            itemsContainer.pivot = new Vector2(0.5f, 0.5f);

            var pointerContainer = new GameObject($"{laneName}_Pointer", typeof(RectTransform)).GetComponent<RectTransform>();
            pointerContainer.SetParent(laneRoot, false);
            pointerContainer.anchorMin = new Vector2(0.5f, 0.5f);
            pointerContainer.anchorMax = new Vector2(0.5f, 0.5f);
            pointerContainer.pivot = new Vector2(0.5f, 0.5f);
            pointerContainer.sizeDelta = Vector2.zero;
            pointerContainer.anchoredPosition = Vector2.zero;

            var pointerGraphic = BuildPointerVisuals(pointerContainer, laneHeight, out var pointerActiveColor);
            pointerContainer.gameObject.SetActive(false);

            var resultText = new GameObject($"{laneName}_ResultText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var resultRect = resultText.rectTransform;
            resultRect.SetParent(laneRoot, false);
            resultRect.anchorMin = new Vector2(0.5f, 0.5f);
            resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.pivot = new Vector2(0.5f, 1f);
            float pointerArrowSize = PointerThickness * 2.5f;
            float pointerTipY = -laneHeight * 0.5f - 32f - pointerArrowSize * 0.5f;
            resultRect.anchoredPosition = new Vector2(0f, pointerTipY - 20f);
            resultText.fontSize = 32f;
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.raycastTarget = false;

            return new LaneUI(laneName, laneRoot, viewport, itemsContainer, pointerContainer.gameObject, pointerGraphic, pointerActiveColor, resultText, laneHeight);
        }

        private Graphic BuildPointerVisuals(RectTransform pointerContainer, float laneHeight, out Color pointerActiveColor)
        {
            var pointerLineObj = new GameObject("PointerLine", typeof(RectTransform));
            var pointerLineRect = pointerLineObj.GetComponent<RectTransform>();
            pointerLineRect.SetParent(pointerContainer, false);
            pointerLineRect.anchorMin = new Vector2(0.5f, 0.5f);
            pointerLineRect.anchorMax = new Vector2(0.5f, 0.5f);
            pointerLineRect.pivot = new Vector2(0.5f, 0.5f);
            pointerLineRect.sizeDelta = new Vector2(PointerThickness * 1.1f, laneHeight + 64f);
            pointerLineRect.anchoredPosition = Vector2.zero;

            var pointerLineGraphic = pointerLineObj.AddComponent<RoundedRectGraphic>();
            pointerLineGraphic.raycastTarget = false;
            pointerLineGraphic.cornerSegments = 8;
            pointerLineGraphic.cornerRadius = pointerLineRect.sizeDelta.x * 0.5f;
            pointerActiveColor = new Color(1f, 1f, 1f, 0.95f);
            pointerLineGraphic.color = pointerActiveColor;

            var pointerLineOutline = pointerLineObj.AddComponent<Outline>();
            pointerLineOutline.effectColor = new Color(0f, 0f, 0f, 0.25f);
            pointerLineOutline.effectDistance = new Vector2(1f, -1f);
            pointerLineOutline.useGraphicAlpha = true;

            var pointerLineShadow = pointerLineObj.AddComponent<Shadow>();
            pointerLineShadow.effectColor = new Color(0f, 0f, 0f, 0.15f);
            pointerLineShadow.effectDistance = new Vector2(0f, -4f);

            var stripeObj = new GameObject("PointerInnerStripe", typeof(RectTransform));
            var stripeRect = stripeObj.GetComponent<RectTransform>();
            stripeRect.SetParent(pointerLineRect, false);
            stripeRect.anchorMin = new Vector2(0.5f, 0.5f);
            stripeRect.anchorMax = new Vector2(0.5f, 0.5f);
            stripeRect.pivot = new Vector2(0.5f, 0.5f);
            stripeRect.sizeDelta = new Vector2(pointerLineRect.sizeDelta.x * 0.22f, pointerLineRect.sizeDelta.y * 0.82f);

            var stripe = stripeObj.AddComponent<RoundedRectGraphic>();
            stripe.raycastTarget = false;
            stripe.cornerSegments = 6;
            stripe.cornerRadius = stripeRect.sizeDelta.x * 0.5f;
            stripe.color = new Color(1f, 1f, 1f, 0.12f);

            var topArrowObj = new GameObject("TopArrow", typeof(RectTransform));
            var topArrowRect = topArrowObj.GetComponent<RectTransform>();
            topArrowRect.SetParent(pointerContainer, false);
            topArrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            topArrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            topArrowRect.pivot = new Vector2(0.5f, 0.5f);
            topArrowRect.sizeDelta = new Vector2(PointerThickness * 2.5f, PointerThickness * 2.5f);
            topArrowRect.anchoredPosition = new Vector2(0f, laneHeight * 0.5f + 32f);

            var topArrowGraphic = topArrowObj.AddComponent<TriangleGraphic>();
            topArrowGraphic.raycastTarget = false;
            topArrowGraphic.color = new Color(1f, 1f, 1f, 0.9f);
            topArrowGraphic.direction = TriangleGraphic.TriangleDirection.Up;

            var topArrowOutline = topArrowObj.AddComponent<Outline>();
            topArrowOutline.effectColor = new Color(1f, 1f, 1f, 0.4f);
            topArrowOutline.effectDistance = new Vector2(0.5f, -0.5f);
            topArrowOutline.useGraphicAlpha = false;

            var topArrowShadow = topArrowObj.AddComponent<Shadow>();
            topArrowShadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            topArrowShadow.effectDistance = new Vector2(0f, -3f);

            var topInnerObj = new GameObject("TopArrowInner", typeof(RectTransform));
            var topInnerRect = topInnerObj.GetComponent<RectTransform>();
            topInnerRect.SetParent(topArrowRect, false);
            topInnerRect.anchorMin = Vector2.zero;
            topInnerRect.anchorMax = Vector2.one;
            topInnerRect.pivot = new Vector2(0.5f, 0.5f);
            topInnerRect.sizeDelta = Vector2.zero;
            topInnerRect.anchoredPosition = Vector2.zero;
            topInnerRect.localScale = Vector3.one * 0.85f;

            var topInner = topInnerObj.AddComponent<TriangleGraphic>();
            topInner.raycastTarget = false;
            topInner.color = new Color(1f, 1f, 1f, 0.12f);
            topInner.direction = TriangleGraphic.TriangleDirection.Up;

            var bottomArrowObj = new GameObject("BottomArrow", typeof(RectTransform));
            var bottomArrowRect = bottomArrowObj.GetComponent<RectTransform>();
            bottomArrowRect.SetParent(pointerContainer, false);
            bottomArrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            bottomArrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            bottomArrowRect.pivot = new Vector2(0.5f, 0.5f);
            bottomArrowRect.sizeDelta = new Vector2(PointerThickness * 2.5f, PointerThickness * 2.5f);
            bottomArrowRect.anchoredPosition = new Vector2(0f, -laneHeight * 0.5f - 32f);

            var bottomArrowGraphic = bottomArrowObj.AddComponent<TriangleGraphic>();
            bottomArrowGraphic.raycastTarget = false;
            bottomArrowGraphic.color = new Color(1f, 1f, 1f, 0.9f);
            bottomArrowGraphic.direction = TriangleGraphic.TriangleDirection.Down;

            var bottomArrowOutline = bottomArrowObj.AddComponent<Outline>();
            bottomArrowOutline.effectColor = new Color(1f, 1f, 1f, 0.4f);
            bottomArrowOutline.effectDistance = new Vector2(0.5f, -0.5f);
            bottomArrowOutline.useGraphicAlpha = false;

            var bottomArrowShadow = bottomArrowObj.AddComponent<Shadow>();
            bottomArrowShadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            bottomArrowShadow.effectDistance = new Vector2(0f, 3f);

            var bottomInnerObj = new GameObject("BottomArrowInner", typeof(RectTransform));
            var bottomInnerRect = bottomInnerObj.GetComponent<RectTransform>();
            bottomInnerRect.SetParent(bottomArrowRect, false);
            bottomInnerRect.anchorMin = Vector2.zero;
            bottomInnerRect.anchorMax = Vector2.one;
            bottomInnerRect.pivot = new Vector2(0.5f, 0.5f);
            bottomInnerRect.sizeDelta = Vector2.zero;
            bottomInnerRect.anchoredPosition = Vector2.zero;
            bottomInnerRect.localScale = Vector3.one * 0.85f;

            var bottomInner = bottomInnerObj.AddComponent<TriangleGraphic>();
            bottomInner.raycastTarget = false;
            bottomInner.color = new Color(1f, 1f, 1f, 0.12f);
            bottomInner.direction = TriangleGraphic.TriangleDirection.Down;

            return pointerLineGraphic;
        }

        private List<LaneResult> BuildLaneResults(List<int> candidatePool, IReadOnlyList<LaneFinalData> finalLaneData)
        {
            var results = new List<LaneResult>(finalLaneData.Count);

            for (int i = 0; i < finalLaneData.Count; i++)
            {
                var laneData = finalLaneData[i];

                if (!candidatePool.Contains(laneData.TypeId))
                {
                    candidatePool.Add(laneData.TypeId);
                }

                string display = !string.IsNullOrWhiteSpace(laneData.DisplayName)
                    ? laneData.DisplayName
                    : RecycleService.GetDisplayName(laneData.TypeId) ?? string.Empty;

                var icon = laneData.Icon ?? RecycleService.GetItemIcon(laneData.TypeId);

                results.Add(new LaneResult(laneData.TypeId, display, icon, laneData.IsRewardLane));
            }

            return results;
        }

        private bool TryBuildAnimationPlan(LaneUI lane, List<int> candidateTypeIds, int finalTypeId, Sprite? finalIcon, float initialVelocityInSlots, float decelerationDuration, float velocityAtEndOfDeceleration, float totalCurveDuration, bool reverseDirection, out AnimationPlan plan)
        {
            plan = null!;

            if (lane.ItemsContainer == null || lane.Viewport == null)
            {
                return false;
            }

            var velocityCurve = GenerateVelocityCurve(initialVelocityInSlots, decelerationDuration, velocityAtEndOfDeceleration, totalCurveDuration);
            float totalDistanceInPixels = CalculateTotalDistanceInPixels(velocityCurve);

            float slotWidth = SlotFullWidth;
            float viewportWidth = lane.Viewport.rect.width;
            int minSlotsForViewport = Mathf.CeilToInt(viewportWidth / slotWidth) + 2;
            int completeSlots = Mathf.FloorToInt(totalDistanceInPixels / slotWidth);
            float remainingPixelsInFinalSlot = totalDistanceInPixels - (completeSlots * slotWidth);

            Log.Debug($"[Animation:{lane.Name}] Total distance: {totalDistanceInPixels}px = {completeSlots} complete slots + {remainingPixelsInFinalSlot}px");

            int startSlotIndex = Mathf.Max(20, minSlotsForViewport);
            int finalSlotIndex = startSlotIndex + completeSlots;

            int slotsAfterFinal = Mathf.Max(SlotsAfterFinal, minSlotsForViewport);
            int totalSlotsNeeded = finalSlotIndex + slotsAfterFinal + 1;

            bool enableWeightedLottery = Core.Settings.SettingManager.Instance.EnableWeightedLottery.GetAsBool();

            var slotSequence = BuildSlotSequenceWithFixedFinal(candidateTypeIds, finalTypeId, enableWeightedLottery, totalSlotsNeeded, finalSlotIndex);
            if (slotSequence.Count == 0)
            {
                return false;
            }

            if (startSlotIndex >= slotSequence.Count || finalSlotIndex >= slotSequence.Count)
            {
                Log.Warning($"[Animation:{lane.Name}] Slot sequence too short (count={slotSequence.Count}, startIndex={startSlotIndex}, finalIndex={finalSlotIndex}).");
                return false;
            }

            ClearItems(lane);

            var totalWidth = slotSequence.Count * slotWidth;
            lane.ItemsContainer.sizeDelta = new Vector2(totalWidth, lane.LaneHeight);

            var slots = new List<Slot>(slotSequence.Count);

            for (int i = 0; i < slotSequence.Count; i++)
            {
                var typeId = slotSequence[i];
                bool isFinal = i == finalSlotIndex;

                var sprite = isFinal ? (finalIcon ?? RecycleService.GetItemIcon(typeId)) : RecycleService.GetItemIcon(typeId);
                var slot = CreateSlot(lane, typeId, sprite, RecycleService.GetDisplayName(typeId), RecycleService.GetItemQualityColor(typeId));

                var positionX = i * slotWidth - totalWidth * 0.5f + slotWidth * 0.5f;
                if (reverseDirection)
                {
                    positionX = -positionX;
                }
                slot.Rect.anchoredPosition = new Vector2(positionX, 0f);

                slots.Add(slot);
            }

            var finalSlotCenterX = slots[finalSlotIndex].Rect.anchoredPosition.x;
            float slotHalfWidth = slotWidth * 0.5f;
            float offsetWithinSlot = remainingPixelsInFinalSlot - slotHalfWidth;
            if (reverseDirection)
            {
                offsetWithinSlot = -offsetWithinSlot;
            }
            float finalStopPositionX = finalSlotCenterX + offsetWithinSlot;
            float endOffset = -finalStopPositionX;

            Log.Debug($"[Animation:{lane.Name}] Final slot center: {finalSlotCenterX}, Remaining in slot: {remainingPixelsInFinalSlot}px, Offset: {offsetWithinSlot}px, Stop: {finalStopPositionX}, endOffset: {endOffset}");

            var startItemPos = slots[startSlotIndex].Rect.anchoredPosition.x;
            float startOffset = -startItemPos;

            plan = new AnimationPlan(slots, finalSlotIndex, startOffset, endOffset, velocityCurve, reverseDirection, initialVelocityInSlots);
            return true;
        }

        private static List<int> BuildSlotSequenceWithFixedFinal(IEnumerable<int> candidateTypeIds, int finalTypeId, bool useWeightedLottery, int totalSlots, int finalSlotIndex)
        {
            var pool = candidateTypeIds?.ToList() ?? new List<int>();
            if (!pool.Contains(finalTypeId)) pool.Add(finalTypeId);
            if (pool.Count == 0) pool.Add(finalTypeId);

            var weightedItemsCache = useWeightedLottery ? LotteryService.ConvertToQualityWeightedItems(pool) : null;

            var sequence = new List<int>();
            int? lastId = null;
            int lastIdCount = 0;

            int SampleNext(int? previous, int consecutiveCount)
            {
                if (pool.Count == 1) return pool[0];

                int pick;

                if (useWeightedLottery && weightedItemsCache != null && weightedItemsCache.Count > 0)
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

            for (int i = 0; i < totalSlots; i++)
            {
                int id;

                if (i == finalSlotIndex)
                {
                    id = finalTypeId;
                }
                else
                {
                    id = SampleNext(lastId, lastIdCount);
                }

                sequence.Add(id);

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

        private Slot CreateSlot(LaneUI lane, int typeId, Sprite? sprite, string displayName, Color frameColor)
        {
            var root = new GameObject($"LotterySlot_{typeId}", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(lane.ItemsContainer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(IconSize.x + SlotPadding, IconSize.y + SlotPadding);

            const float borderPadding = 16f;
            var backgroundObj = new GameObject("LotterySlotBackground", typeof(RectTransform));
            var bgRect = backgroundObj.GetComponent<RectTransform>();
            bgRect.SetParent(rect, false);
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = rect.sizeDelta + new Vector2(borderPadding, borderPadding);

            var bgGraphic = backgroundObj.AddComponent<RoundedRectGraphic>();
            bgGraphic.raycastTarget = false;
            bgGraphic.cornerRadius = Mathf.Min(rect.sizeDelta.x, rect.sizeDelta.y) * 0.12f;
            bgGraphic.cornerSegments = 6;
            bgGraphic.color = new Color(0f, 0f, 0f, 0.8f);

            var frameObj = new GameObject("LotterySlotFrame", typeof(RectTransform));
            var frameRect = frameObj.GetComponent<RectTransform>();
            frameRect.SetParent(rect, false);
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.sizeDelta = rect.sizeDelta;

            var frameGraphic = frameObj.AddComponent<RoundedRectGraphic>();
            frameGraphic.cornerRadius = Mathf.Min(frameRect.sizeDelta.x, frameRect.sizeDelta.y) * 0.1f;
            frameGraphic.cornerSegments = 6;
            frameGraphic.color = new Color(frameColor.r, frameColor.g, frameColor.b, 1f);
            frameGraphic.raycastTarget = false;

            var frameMask = frameObj.AddComponent<Mask>();
            frameMask.showMaskGraphic = true;

            var iconObject = new GameObject("LotterySlotIcon", typeof(RectTransform), typeof(Image));
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(frameRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
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

            return new Slot(rect, frameGraphic, icon, iconOutline, displayName);
        }

        private float[] GenerateVelocityCurve(float initialVelocityInSlots, float decelerationDuration, float velocityAtEndOfDeceleration, float totalCurveDuration)
        {
            float clampedInitialVelocity = Mathf.Max(0.1f, initialVelocityInSlots);
            float clampedDecelerationDuration = Mathf.Max(0.1f, decelerationDuration);
            float clampedVelocityAtEnd = Mathf.Clamp(velocityAtEndOfDeceleration, 0f, clampedInitialVelocity);
            float clampedTotalDuration = Mathf.Max(clampedDecelerationDuration + 0.1f, totalCurveDuration);

            int totalSteps = Mathf.Max(1, (int)(clampedTotalDuration * AnimationStepsPerSecond));
            var curve = new float[totalSteps];

            for (int i = 0; i < totalSteps; i++)
            {
                float timeInSeconds = (float)i / AnimationStepsPerSecond;

                if (timeInSeconds < clampedDecelerationDuration)
                {
                    float t = timeInSeconds / clampedDecelerationDuration;
                    float easeT = 1f - Mathf.Pow(1f - t, 4f);
                    curve[i] = Mathf.Lerp(clampedInitialVelocity, clampedVelocityAtEnd, easeT);
                }
                else
                {
                    float remainingDuration = Mathf.Max(0.001f, clampedTotalDuration - clampedDecelerationDuration);
                    float t = (timeInSeconds - clampedDecelerationDuration) / remainingDuration;
                    curve[i] = Mathf.Lerp(clampedVelocityAtEnd, 0f, Mathf.Clamp01(t));
                }
            }

            return curve;
        }

        private float CalculateTotalDistanceInSlots(float[] velocityCurve)
        {
            float timeStepInSeconds = 1.0f / AnimationStepsPerSecond;
            float totalDistance = 0f;

            for (int i = 0; i < velocityCurve.Length; i++)
            {
                totalDistance += velocityCurve[i] * timeStepInSeconds;
            }

            return totalDistance;
        }

        private float CalculateTotalDistanceInPixels(float[] velocityCurve)
        {
            float distanceInSlots = CalculateTotalDistanceInSlots(velocityCurve);
            return distanceInSlots * SlotFullWidth;
        }

        private async UniTask PerformPhysicsBasedRoll(LaneUI lane, AnimationPlan plan, bool canHandleSkipInput, ChannelGroup sfxGroup)
        {
            if (lane.ItemsContainer == null) return;

            var velocityCurve = plan.VelocityCurve;

            float currentPositionInPixels = plan.StartOffset;
            float targetPositionInPixels = plan.FinalOffset;
            lane.ItemsContainer.anchoredPosition = new Vector2(currentPositionInPixels, 0f);

            int lastHighlightedIndex = -1;
            float elapsedTime = 0f;

            float velocityDirection = targetPositionInPixels < currentPositionInPixels ? -1f : 1f;

            string directionLabel = plan.ReverseDirection ? "RightToLeft" : "LeftToRight";
            Log.Debug($"[Animation:{lane.Name}] Start ({directionLabel}) - Current: {currentPositionInPixels}, Target: {targetPositionInPixels}, Distance: {(targetPositionInPixels - currentPositionInPixels) / SlotFullWidth} slots, InitialVel: {plan.InitialVelocityInSlots:F2} slots/s");

            while (true)
            {
                if (!_skipRequested && canHandleSkipInput && Input.GetMouseButtonDown(0))
                {
                    _skipRequested = true;
                    var result = sfxGroup.stop();
                    if (result != FMOD.RESULT.OK)
                    {
                        Log.Warning($"Failed to stop rolling sound: {result}");
                    }
                }

                if (_skipRequested)
                {
                    currentPositionInPixels = targetPositionInPixels;
                    lane.ItemsContainer.anchoredPosition = new Vector2(currentPositionInPixels, 0f);
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
                float deltaTime = Time.deltaTime;
                elapsedTime += deltaTime;

                float distanceToTarget = Mathf.Abs(targetPositionInPixels - currentPositionInPixels);
                Log.Debug($"[Animation:{lane.Name}] Elapsed: {elapsedTime:F2}s, Distance to target: {distanceToTarget:F1}px ({distanceToTarget / SlotFullWidth:F2} slots)");

                int curveIndex = Mathf.FloorToInt(elapsedTime * AnimationStepsPerSecond);
                curveIndex = Mathf.Clamp(curveIndex, 0, velocityCurve.Length - 1);
                float currentVelocityInSlots = velocityCurve[curveIndex] * velocityDirection;

                float velocityInPixels = currentVelocityInSlots * SlotFullWidth;
                float movementInPixels = velocityInPixels * deltaTime;
                float nextPositionInPixels = currentPositionInPixels + movementInPixels;

                if ((velocityDirection == -1 && nextPositionInPixels <= targetPositionInPixels) ||
                    (velocityDirection == 1 && nextPositionInPixels >= targetPositionInPixels))
                {
                    currentPositionInPixels = targetPositionInPixels;
                    lane.ItemsContainer.anchoredPosition = new Vector2(currentPositionInPixels, 0f);
                    Log.Debug($"[Animation:{lane.Name}] Reached target position at {elapsedTime:F2}s (next: {nextPositionInPixels:F1}, target: {targetPositionInPixels:F1})");
                    break;
                }

                currentPositionInPixels = nextPositionInPixels;
                lane.ItemsContainer.anchoredPosition = new Vector2(currentPositionInPixels, 0f);

                int currentIndex = FindCenteredSlotIndex(plan, currentPositionInPixels);
                if (currentIndex != lastHighlightedIndex)
                {
                    if (lastHighlightedIndex >= 0 && lastHighlightedIndex < plan.Slots.Count)
                    {
                        var prevSlot = plan.Slots[lastHighlightedIndex];
                        prevSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 0f);
                        prevSlot.Rect.localScale = Vector3.one;
                    }

                    if (currentIndex >= 0 && currentIndex < plan.Slots.Count)
                    {
                        var currentSlot = plan.Slots[currentIndex];
                        currentSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 1f);
                        currentSlot.Rect.localScale = Vector3.one * 1.05f;
                        lane.SetResultText(currentSlot.DisplayName);
                    }

                    lastHighlightedIndex = currentIndex;
                }

                if (elapsedTime >= MaxAnimationDurationInSeconds)
                {
                    Log.Warning($"[Animation:{lane.Name}] Force stopped after {elapsedTime:F2}s - target position not reached (pos: {currentPositionInPixels:F1}, target: {targetPositionInPixels:F1})");
                    break;
                }
            }

            if (lastHighlightedIndex >= 0 && lastHighlightedIndex < plan.Slots.Count && lastHighlightedIndex != plan.FinalSlotIndex)
            {
                var prevSlot = plan.Slots[lastHighlightedIndex];
                prevSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 0f);
                prevSlot.Rect.localScale = Vector3.one;
            }

            var finalSlot = plan.FinalSlot;
            finalSlot.IconOutline.effectColor = new Color(1f, 1f, 1f, 1f);
            finalSlot.Rect.localScale = Vector3.one * 1.05f;

            lane.SetResultText(finalSlot.DisplayName);
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

        private async UniTask AnimateCelebration(LaneUI lane, AnimationPlan plan, int finalTypeId, bool isRewardLane, ChannelGroup sfxGroup)
        {
            var slot = plan.FinalSlot;
            var frame = slot.Frame;
            var icon = slot.Icon;

            var initialFrameColor = SlotFrameColor;
            var targetFrameColor = FinalFrameColor;

            frame.color = initialFrameColor;

            const int pulseCount = 3;
            const float pulseDuration = CelebrateDuration / pulseCount;

            for (int pulse = 0; pulse < pulseCount; pulse++)
            {
                var elapsed = 0f;
                while (elapsed < pulseDuration)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / pulseDuration);

                    frame.color = Color.Lerp(initialFrameColor, targetFrameColor, t);

                    float scaleProgress = Mathf.Sin(t * Mathf.PI);
                    float scale = 1.05f + scaleProgress * 0.15f;
                    slot.Rect.localScale = Vector3.one * scale;

                    float glowIntensity = Mathf.Lerp(1f, HighlightIntensity, scaleProgress);
                    slot.IconOutline.effectColor = new Color(1f, 1f, 1f, glowIntensity);

                    icon.color = Color.Lerp(Color.white, new Color(1.2f, 1.2f, 1.2f), scaleProgress * 0.5f);
                }
            }

            frame.color = targetFrameColor;
            slot.Rect.localScale = Vector3.one * 1.1f;
            slot.IconOutline.effectColor = new Color(1f, 1f, 1f, HighlightIntensity);
            icon.color = Color.white;

            StartContinuousGlow(slot, isRewardLane);

            if (isRewardLane)
            {
                var finalItemQuality = RecycleService.GetItemQuality(finalTypeId);
                if (finalItemQuality.IsHighQuality())
                {
                    SoundUtils.PlayHighQualitySound(sfxGroup, Constants.Sound.HIGH_QUALITY_LOTTERY_SOUND);
                }
            }
        }

        private async void StartContinuousGlow(Slot slot, bool isRewardLane)
        {
            if (!isRewardLane)
            {
                return;
            }

            float startTime = Time.time;
            const float glowDuration = 2f;

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

        private void ClearItems(LaneUI lane)
        {
            if (lane.ItemsContainer == null) return;

            for (int i = lane.ItemsContainer.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(lane.ItemsContainer.GetChild(i).gameObject);
            }
        }

        private async UniTask RevealResult(LaneUI lane, string finalDisplayName)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.DeltaTime, PlayerLoopTiming.Update, default);
            lane.SetResultText(finalDisplayName);
            await UniTask.Delay(TimeSpan.FromSeconds(0.75f), DelayType.DeltaTime, PlayerLoopTiming.Update, default);
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

        private Sprite EnsureFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            var texture = Texture2D.whiteTexture;
            _fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return _fallbackSprite;
        }

        /// <summary>
        /// Destroys the Lottery animation UI
        /// </summary>
        public void Destroy()
        {
            foreach (var lane in _lanes)
            {
                if (lane.Root != null)
                {
                    UnityEngine.Object.Destroy(lane.Root.gameObject);
                }
            }

            _lanes.Clear();
            _currentLaneCount = 0;

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

            _canvasGroup = null;
            _fallbackSprite = null;
            _isAnimating = false;
            _skipRequested = false;
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
