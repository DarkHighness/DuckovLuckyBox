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
        private const int MinimumSlots = 150;
        private const float AnimationDuration = 7.0f; // matches the BGM
        private const float FadeDuration = 0.25f;
        private const float CelebrateDuration = 0.4f;
        private const float PointerThickness = 12f;

        private static readonly AnimationCurve AnimationCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3.0f),        // Start very fast
            new Keyframe(0.2f, 0.5f, 2.0f, 2.0f),  // High speed, starting to slow
            new Keyframe(0.5f, 0.8f, 1.0f, 1.0f),  // Continue slowing down
            new Keyframe(0.75f, 0.93f, 0.4f, 0.4f), // Slow approach
            new Keyframe(0.9f, 0.985f, 0.15f, 0.15f), // Very slow near end
            new Keyframe(1f, 1f, 0f, 0f));         // Stop smoothly (pure deceleration curve)

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color PointerColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color FinalFrameColor = new Color(0.95f, 0.8f, 0.35f, 1f);
        private static readonly Color SlotFrameColor = new Color(1f, 1f, 1f, 0.25f);

        /// <summary>
        /// Initializes the lottery animation UI
        /// </summary>
        public static void Initialize(Canvas canvas, TextMeshProUGUI templateText)
        {
            if (_overlayRoot != null) return;
            if (canvas == null)
            {
                Log.Warning("Cannot initialize lottery animation: canvas is null");
                return;
            }

            // Create full-screen overlay
            _overlayRoot = new GameObject("LotteryAnimationOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)).GetComponent<RectTransform>();
            _overlayRoot.SetParent(canvas.transform, false);
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
            var canvasRect = canvas.GetComponent<RectTransform>();
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

            // Create center pointer
            _centerPointer = new GameObject("LotteryPointer", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _centerPointer.rectTransform.SetParent(_overlayRoot, false);
            _centerPointer.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _centerPointer.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _centerPointer.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _centerPointer.rectTransform.sizeDelta = new Vector2(PointerThickness, viewportHeight + 64f);
            _centerPointer.rectTransform.anchoredPosition = Vector2.zero;
            _centerPointer.sprite = EnsureFallbackSprite();
            _centerPointer.type = Image.Type.Simple;
            _centerPointer.color = PointerColor;
            _centerPointer.raycastTarget = false;

            // Create result text
            if (templateText != null)
            {
                _resultText = UnityEngine.Object.Instantiate(templateText, _overlayRoot);
                _resultText.gameObject.name = "LotteryResultText";
                _resultText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _resultText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _resultText.rectTransform.pivot = new Vector2(0.5f, 1f);
                _resultText.rectTransform.anchoredPosition = new Vector2(0f, -viewportHeight * 0.75f);
                _resultText.enableAutoSizing = false;
                _resultText.fontSize = Mathf.Max(26f, templateText.fontSize * 0.9f);
                _resultText.alignment = TextAlignmentOptions.Center;
                _resultText.raycastTarget = false;
                ResetResultText();
            }

            Log.Debug("Lottery animation UI initialized");
        }

        /// <summary>
        /// Plays the lottery animation
        /// </summary>
        public static async UniTask PlayAsync(IEnumerable<int> candidateTypeIds, int finalTypeId, string finalDisplayName, Sprite? finalIcon)
        {
            // Check if animation is enabled in settings
            var enableAnimationValue = Core.Settings.Settings.Instance.EnableAnimation.Value;
            if (enableAnimationValue is bool enabled && !enabled)
            {
                Log.Debug("Lottery animation is disabled in settings. Skipping animation.");
                return;
            }

            if (_overlayRoot == null || _itemsContainer == null || _centerPointer == null || _canvasGroup == null)
            {
                Log.Warning("Lottery animation UI is not initialized.");
                return;
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
                Utils.PlaySound(Constants.Sound.ROLLING_SOUND, sfxGroup);

                // Single continuous roll animation
                await PerformContinuousRoll(plan, AnimationDuration, AnimationCurve);

                // If skip was requested, jump directly to celebration
                if (!_skipRequested)
                {
                    // Celebration on final slot
                    await AnimateCelebration(plan);

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

                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
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
                _itemsContainer.anchoredPosition = Vector2.zero;
            }
        }

        private static bool TryBuildAnimationPlan(IEnumerable<int> candidateTypeIds, int finalTypeId, Sprite? finalIcon, out AnimationPlan plan)
        {
            plan = default!;

            if (_itemsContainer == null) return false;
            if (_viewport == null) return false;

            var baseSequenceData = BuildSequence(candidateTypeIds, finalTypeId);
            var baseSequence = baseSequenceData.Slots;
            if (baseSequence.Count == 0)
            {
                return false;
            }

            ClearItems();

            var slotWidth = SlotFullWidth;
            var viewportWidth = _viewport.rect.width;
            var viewportHalfWidth = viewportWidth * 0.5f;

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
                baselineFinalItemIndex = displaySequence.Count - 1;
            }

            // Add some items after the final item so there are visible items after it
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
            _itemsContainer.sizeDelta = new Vector2(totalWidth, 200f);

            var slots = new List<Slot>(displaySequence.Count);
            int finalItemIndex = baselineFinalItemIndex;

            // Create UI slots for each item in the sequence
            for (int i = 0; i < displaySequence.Count; i++)
            {
                var typeId = displaySequence[i];
                var isFinal = (i == finalItemIndex);

                var sprite = isFinal ? (finalIcon ?? LotteryService.GetItemIcon(typeId)) : LotteryService.GetItemIcon(typeId);
                var slot = CreateSlot(typeId, sprite, LotteryService.GetDisplayName(typeId), LotteryService.GetItemQualityColor(typeId));

                // Position: container is centered, so position is offset from center
                var positionX = (float)i * slotWidth - totalWidth * 0.5f + slotWidth * 0.5f;
                slot.Rect.anchoredPosition = new Vector2(positionX, 0f);

                slots.Add(slot);
            }

            var finalItemPos = slots[finalItemIndex].Rect.anchoredPosition.x;
            var endOffset = -finalItemPos;

            var firstItemPos = slots[0].Rect.anchoredPosition.x;
            var startOffset = -viewportHalfWidth - firstItemPos;

            plan = new AnimationPlan(slots, finalItemIndex, startOffset, endOffset);
            return true;
        }

        private static SequenceData BuildSequence(IEnumerable<int> candidateTypeIds, int finalTypeId)
        {
            var pool = candidateTypeIds?.ToList() ?? new List<int>();
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

            int? lastId = null;
            while (sequence.Count < MinimumSlots - 1)
            {
                var id = SampleNext(lastId);
                sequence.Add(id);
                lastId = id;
            }

            var finalIndex = sequence.Count;
            sequence.Add(finalTypeId);

            return new SequenceData(sequence, finalIndex, 0);
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

        private static async UniTask PerformContinuousRoll(AnimationPlan plan, float duration, UnityEngine.AnimationCurve curve)
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

        private static async UniTask AnimateCelebration(AnimationPlan plan)
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

        private readonly struct SequenceData
        {
            public SequenceData(List<int> slots, int finalIndex, int leadCount)
            {
                Slots = slots;
                FinalIndex = finalIndex;
                LeadCount = leadCount;
            }

            public List<int> Slots { get; }
            public int FinalIndex { get; }
            public int LeadCount { get; }
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
                    var safeIndex = Mathf.Clamp(FinalSlotIndex, 0, Mathf.Max(0, Slots.Count - 1));
                    return Slots[safeIndex];
                }
            }
        }
    }
}
