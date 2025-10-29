using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using DuckovLuckyBox.Core;
using ItemStatsSystem;

namespace DuckovLuckyBox.UI
{
    /// <summary>
    /// Manages Recycle reward animation UI and playback
    /// </summary>
    public class RecycleAnimation
    {
        private static RecycleAnimation? _instance;
        public static RecycleAnimation Instance => _instance ??= new RecycleAnimation();

        private RecycleAnimation() { }

        private bool _isInitialized = false;
        private RectTransform? _overlayRoot;
        private CanvasGroup? _canvasGroup;
        private Image? _itemIcon;
        private TextMeshProUGUI? _itemText;
        private Canvas? _canvas;
        private bool _isAnimating;

        // Animation constants
        private const float FadeInDuration = 0.3f;
        private const float BounceDuration = 0.8f;
        private const float HoldDuration = 1.5f;
        private const float FadeOutDuration = 0.5f;
        private const float IconSize = 200f;
        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.7f);

        /// <summary>
        /// Initializes the Recycle animation UI with a full-screen canvas overlay
        /// </summary>
        public void Initialize()
        {
            Log.Debug("[RecycleAnimation] Initializing animation UI");

            if (_isInitialized)
            {
                Log.Debug("[RecycleAnimation] Already initialized, skipping");
                return;
            }

            // Create full-screen canvas if it doesn't exist
            if (_canvas == null)
            {
                Log.Debug("[RecycleAnimation] Creating animation canvas");
                var canvasObj = new GameObject("RecycleAnimationCanvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
                _canvas = canvasObj.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = short.MaxValue - 1; // Slightly lower than LotteryAnimation

                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            // Create full-screen overlay
            Log.Debug("[RecycleAnimation] Creating overlay root");
            _overlayRoot = new GameObject("RecycleAnimationOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)).GetComponent<RectTransform>();
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

            // Create item icon in center
            Log.Debug("[RecycleAnimation] Creating item icon");
            var iconObj = new GameObject("RecycleItemIcon", typeof(RectTransform), typeof(Image));
            _itemIcon = iconObj.GetComponent<Image>();
            _itemIcon.rectTransform.SetParent(_overlayRoot, false);
            _itemIcon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _itemIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _itemIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _itemIcon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
            _itemIcon.raycastTarget = false;

            // Create item text below icon
            Log.Debug("[RecycleAnimation] Creating item text");
            _itemText = new GameObject("RecycleItemText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            _itemText.rectTransform.SetParent(_overlayRoot, false);
            _itemText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _itemText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _itemText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _itemText.rectTransform.anchoredPosition = new Vector2(0f, -IconSize * 0.75f);
            _itemText.fontSize = 36;
            _itemText.alignment = TextAlignmentOptions.Center;
            _itemText.raycastTarget = false;

            _isInitialized = true;
            Log.Debug("[RecycleAnimation] Initialization completed");
        }

        /// <summary>
        /// Plays the Recycle reward animation
        /// </summary>
        public async UniTask PlayAsync(Item item)
        {
            Log.Debug($"[RecycleAnimation] Starting animation for item: {item?.DisplayName ?? "null"}");

            if (item == null)
            {
                Log.Warning("RecycleAnimation: Item is null.");
                return;
            }

            // Auto-initialize if not already initialized
            if (!_isInitialized)
            {
                Log.Debug("[RecycleAnimation] Auto-initializing");
                Initialize();
            }

            if (_isAnimating)
            {
                Log.Debug("[RecycleAnimation] Animation already in progress, skipping");
                return;
            }

            _isAnimating = true;
            Log.Debug("[RecycleAnimation] Animation started");

            try
            {
                // Set item icon and text
                Log.Debug("[RecycleAnimation] Setting item icon and text");
                var itemIcon = RecycleService.GetItemIcon(item.TypeID) ?? EnsureFallbackSprite();
                if (_itemIcon != null)
                {
                    _itemIcon.sprite = itemIcon;
                    _itemIcon.color = Color.white;
                }

                if (_itemText != null)
                {
                    _itemText.text = item.DisplayName;
                    _itemText.color = Color.white;
                }

                // Set background color based on item quality
                Color backgroundColor = RecycleService.GetItemQualityColor(item.TypeID);
                Log.Debug($"[RecycleAnimation] Setting background color: {backgroundColor}");

                var overlayImage = _overlayRoot?.GetComponent<Image>();
                if (overlayImage != null)
                {
                    overlayImage.color = backgroundColor;
                }

                // Show overlay
                Log.Debug("[RecycleAnimation] Showing overlay");
                _overlayRoot?.gameObject.SetActive(true);

                // Fade in
                Log.Debug($"[RecycleAnimation] Fading in (duration: {FadeInDuration}s)");
                await FadeCanvasGroup(_canvasGroup, 0f, 1f, FadeInDuration);

                // Play sound effect
                Log.Debug("[RecycleAnimation] Playing sound effect");
                await PlayRewardSoundEffect(item);

                // Bounce animation
                Log.Debug($"[RecycleAnimation] Performing bounce animation (duration: {BounceDuration}s)");
                await PerformBounceAnimation();

                // Hold for a moment
                Log.Debug($"[RecycleAnimation] Holding for {HoldDuration}s");
                await UniTask.Delay(TimeSpan.FromSeconds(HoldDuration));

                // Fade out
                Log.Debug($"[RecycleAnimation] Fading out (duration: {FadeOutDuration}s)");
                await FadeCanvasGroup(_canvasGroup, 1f, 0f, FadeOutDuration);

                // Hide overlay
                Log.Debug("[RecycleAnimation] Hiding overlay");
                _overlayRoot?.gameObject.SetActive(false);

                Log.Debug("[RecycleAnimation] Animation completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[RecycleAnimation] Animation failed: {ex.Message}");
                throw;
            }
            finally
            {
                _isAnimating = false;
                Log.Debug("[RecycleAnimation] Animation finished");
            }
        }

        private async UniTask PlayRewardSoundEffect(Item item)
        {
            if (item == null) return;

            var itemQuality = QualityUtils.GetCachedItemValueLevel(item);
            Log.Debug($"[RecycleAnimation] Playing sound for item quality: {itemQuality}");

            RuntimeManager.CoreSystem.createChannelGroup("RecycleSFX", out var sfxGroup);

            if (itemQuality.IsHighQuality())
            {
                Log.Debug("[RecycleAnimation] Playing high-quality sound");
                // Play high-quality sound for high-value items
                SoundUtils.PlayHighQualitySound(sfxGroup, Constants.Sound.HIGH_QUALITY_LOTTERY_SOUND);
            }
            else
            {
                Log.Debug("[RecycleAnimation] Playing normal sound");
                // Play normal reward sound for regular items
                SoundUtils.PlaySound(Constants.Sound.LOTTERY_SOUND, sfxGroup);
            }

            // Small delay for sound to start
            await UniTask.Delay(TimeSpan.FromMilliseconds(100));
        }

        private async UniTask PerformBounceAnimation()
        {
            if (_itemIcon == null) return;

            var rect = _itemIcon.rectTransform;

            // Initial scale
            rect.localScale = Vector3.one * 0.5f;

            // Bounce phases: scale up to 1.2, then down to 1.0
            float elapsed = 0f;

            while (elapsed < BounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / BounceDuration);

                if (t <= 0.5f)
                {
                    // Scale up to 1.2
                    float scaleT = t * 2f; // 0 to 1 over first half
                    float scale = Mathf.Lerp(0.5f, 1.2f, EaseOutBounce(scaleT));
                    rect.localScale = Vector3.one * scale;
                }
                else
                {
                    // Scale down to 1.0
                    float scaleT = (t - 0.5f) * 2f; // 0 to 1 over second half
                    float scale = Mathf.Lerp(1.2f, 1.0f, EaseOut(scaleT));
                    rect.localScale = Vector3.one * scale;
                }

                await UniTask.Yield();
            }

            // Ensure final scale
            rect.localScale = Vector3.one;
        }

        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2f / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }

        private static float EaseOut(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private async UniTask FadeCanvasGroup(CanvasGroup? group, float from, float to, float duration)
        {
            if (group == null) return;

            group.alpha = from;
            if (Mathf.Approximately(duration, 0f))
            {
                group.alpha = to;
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(from, to, t);
                await UniTask.Yield();
            }

            group.alpha = to;
        }

        private Sprite EnsureFallbackSprite()
        {
            // Create a simple white square as fallback
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Destroys the Recycle animation UI
        /// </summary>
        public void Destroy()
        {
            Log.Debug("[RecycleAnimation] Destroying animation UI");

            if (_overlayRoot != null)
            {
                Log.Debug("[RecycleAnimation] Destroying overlay root");
                UnityEngine.Object.Destroy(_overlayRoot.gameObject);
                _overlayRoot = null;
            }

            if (_canvas != null)
            {
                Log.Debug("[RecycleAnimation] Destroying canvas");
                UnityEngine.Object.Destroy(_canvas.gameObject);
                _canvas = null;
            }

            _canvasGroup = null;
            _itemIcon = null;
            _itemText = null;
            _isAnimating = false;
            _isInitialized = false;
            _instance = null;

            Log.Debug("[RecycleAnimation] Destroy completed");
        }
    }
}