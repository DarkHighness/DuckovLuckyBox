using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SodaCraft.Localizations;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;

namespace DuckovLuckyBox.UI
{
    public class SettingsUI : MonoBehaviour
    {
        private GameObject? settingsPanel;
        private Canvas? canvas;
        private bool initialized = false;
        private bool initializing = false;

        public bool IsVisible => settingsPanel != null && settingsPanel.activeSelf;

        void Awake()
        {
        }

        public void ToggleSettingsUI()
        {
            if (!initialized && !initializing)
            {
                StartCoroutine(InitializeAsync());
                return;
            }

            if (IsVisible)
            {
                HideSettingsUI();
            }
            else
            {
                ShowSettingsUI();
            }
        }

        public void HideSettingsUI()
        {
            if (!initialized && !initializing)
            {
                StartCoroutine(InitializeAsync());
                return;
            }

            settingsPanel!.SetActive(false);
            Log.Debug("Settings UI hidden.");
        }

        public void ShowSettingsUI()
        {
            if (!initialized && !initializing)
            {
                StartCoroutine(InitializeAsync());
                return;
            }

            settingsPanel!.SetActive(true);
            Log.Debug("Settings UI shown.");
        }

        public void OnDestroy()
        {
            if (canvas != null)
            {
                DestroyImmediate(canvas.gameObject);
                canvas = null;
            }

            if (settingsPanel != null)
            {
                DestroyImmediate(settingsPanel);
                settingsPanel = null;
            }

            initialized = false;
            initializing = false;
        }

        private IEnumerator InitializeAsync()
        {
            if (initializing || initialized)
                yield break;

            initializing = true;
            Log.Debug("Initializing Settings UI (async)...");

            yield return null;

            CreateCanvas();
            yield return null;

            CreateSettingsPanel();
            yield return null;

            CreateSettings();
            yield return null;

            initialized = true;
            initializing = false;
            Log.Debug("Settings UI Initialized (async complete).");

            ShowSettingsUI();
        }

        private void CreateCanvas()
        {
            GameObject canvasObj = new GameObject("DuckovLuckyBox.UI.SettingsUICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        private void CreateSettingsPanel()
        {
            settingsPanel = new GameObject("DuckovLuckyBox.UI.SettingsPanel");
            settingsPanel.transform.SetParent(canvas!.transform, false);

            // Fullscreen blocker
            GameObject blockerObj = new GameObject("DuckovLuckyBox.UI.InputBlocker");
            blockerObj.transform.SetParent(settingsPanel.transform, false);
            Image blockerImage = blockerObj.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.7f);

            RectTransform blockerRect = blockerObj.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            blockerRect.SetAsFirstSibling();
            blockerObj.AddComponent<GraphicRaycaster>();

            // Panel background - matching game style
            Image panelBackground = settingsPanel.AddComponent<Image>();
            panelBackground.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(900f, 850f);

            var verticalLayout = settingsPanel.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childAlignment = TextAnchor.UpperLeft;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.spacing = 12f;
            verticalLayout.padding = new RectOffset(24, 24, 24, 24);

            settingsPanel.SetActive(false);
        }

        private void CreateSettings()
        {
            CreateTitle();

            var settings = SettingManager.Instance;

            // General category
            CreateCategoryLabel(Localizations.I18n.SettingsCategoryGeneralKey);
            CreateToggleSetting(settings.EnableAnimation);
            CreateHotkeySetting(settings.SettingsHotkey);
            CreateToggleSetting(settings.EnableDestroyButton);
            CreateToggleSetting(settings.EnableLotteryButton);
            CreateToggleSetting(settings.EnableDebug);
            CreateToggleSetting(settings.EnableUseToCreateItemPatch);
            CreateToggleSetting(settings.EnableWeightedLottery);
            CreateToggleSetting(settings.EnableHighQualitySound);
            CreateToggleSetting(settings.EnableStockShopActions);
            CreateTextSetting(settings.HighQualitySoundFilePath);

            // Pricing category
            CreateCategoryLabel(Localizations.I18n.SettingsCategoryPricingKey);
            CreateSliderSetting(settings.RefreshStockPrice);
            CreateSliderSetting(settings.StorePickPrice);
            CreateSliderSetting(settings.StreetPickPrice);
            CreateSliderSetting(settings.MeltBasePrice);

            // Reset button
            CreateResetButton();
        }

        private void CreateTitle()
        {
            GameObject titleObj = new GameObject("DuckovLuckyBox.UI.Title");
            titleObj.transform.SetParent(settingsPanel!.transform, false);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = Localizations.I18n.SettingsPanelTitleKey.ToPlainText();
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.9f, 0.6f);
            titleText.alignment = TextAlignmentOptions.Center;

            var layout = titleObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 50f;
        }

        private void CreateCategoryLabel(string categoryKey)
        {
            GameObject categoryObj = new GameObject($"DuckovLuckyBox.UI.Category.{categoryKey}");
            categoryObj.transform.SetParent(settingsPanel!.transform, false);

            TextMeshProUGUI categoryText = categoryObj.AddComponent<TextMeshProUGUI>();
            categoryText.text = categoryKey.ToPlainText();
            categoryText.fontSize = 20;
            categoryText.fontStyle = FontStyles.Bold;
            categoryText.color = new Color(0.8f, 0.8f, 0.8f);
            categoryText.alignment = TextAlignmentOptions.Left;

            var layout = categoryObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 32f;
        }

        private void CreateToggleSetting(SettingItem setting)
        {
            GameObject entryObj = new GameObject($"DuckovLuckyBox.UI.Entry.{setting.Key}");
            entryObj.transform.SetParent(settingsPanel!.transform, false);

            // Background
            Image entryBg = entryObj.AddComponent<Image>();
            entryBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            var horizontalLayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.padding = new RectOffset(16, 16, 8, 8);
            horizontalLayout.spacing = 16f;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(entryObj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = setting.Label.ToPlainText();
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 400f;
            labelLayout.flexibleWidth = 1f;

            // Toggle (using Slider like the game does)
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(entryObj.transform, false);

            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(200f, 32f);

            Slider toggleSlider = toggleObj.AddComponent<Slider>();
            toggleSlider.wholeNumbers = true;
            toggleSlider.minValue = 0f;
            toggleSlider.maxValue = 1f;

            // Background for slider
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Fill area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(toggleObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.7f, 0.3f, 1f);

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // Handle
            GameObject handleAreaObj = new GameObject("Handle Slide Area");
            handleAreaObj.transform.SetParent(toggleObj.transform, false);
            RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;

            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 0f);

            // Setup slider
            toggleSlider.fillRect = fillRect;
            toggleSlider.handleRect = handleRect;
            toggleSlider.targetGraphic = handleImage;

            // Set initial value
            bool currentValue = setting.Value is bool b && b;
            toggleSlider.SetValueWithoutNotify(currentValue ? 1f : 0f);

            // Add listener
            toggleSlider.onValueChanged.AddListener(value =>
            {
                bool newValue = value > 0f;
                setting.Value = newValue;
                Log.Debug($"Toggle changed - Setting: {setting.Key}, New value: {newValue}");
            });

            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 200f;
            toggleLayout.preferredHeight = 32f;

            var entryLayout = entryObj.AddComponent<LayoutElement>();
            entryLayout.preferredHeight = 48f;
        }

        private void CreateTextSetting(SettingItem setting)
        {
            GameObject entryObj = new GameObject($"DuckovLuckyBox.UI.Entry.{setting.Key}");
            entryObj.transform.SetParent(settingsPanel!.transform, false);

            // Background
            Image entryBg = entryObj.AddComponent<Image>();
            entryBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            var verticalLayout = entryObj.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childAlignment = TextAnchor.UpperLeft;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.spacing = 8f;
            verticalLayout.padding = new RectOffset(16, 16, 8, 8);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(entryObj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = setting.Label.ToPlainText();
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 30f;

            // Input field
            GameObject inputFieldObj = new GameObject("InputField");
            inputFieldObj.transform.SetParent(entryObj.transform, false);

            Image inputFieldBg = inputFieldObj.AddComponent<Image>();
            inputFieldBg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();
            inputField.contentType = TMP_InputField.ContentType.Standard;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputFieldObj.transform, false);
            TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 16;
            inputText.color = Color.white;
            inputText.alignment = TextAlignmentOptions.Left;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 0);
            textRect.offsetMax = new Vector2(-12, 0);

            inputField.textComponent = inputText;
            inputField.targetGraphic = inputFieldBg;

            // Set initial value
            string currentValue = setting.Value is string s ? s : string.Empty;
            inputField.SetTextWithoutNotify(currentValue);

            // Add listener
            inputField.onEndEdit.AddListener(text =>
            {
                setting.Value = text;
                Log.Debug($"TextSetting changed - Setting: {setting.Key}, New value: {text}");
            });

            var inputFieldLayout = inputFieldObj.AddComponent<LayoutElement>();
            inputFieldLayout.preferredHeight = 50f;

            var entryLayout = entryObj.AddComponent<LayoutElement>();
            entryLayout.preferredHeight = 110f;
        }

        private void CreateSliderSetting(SettingItem setting)
        {
            float minValue = setting.MinValue;
            float maxValue = setting.MaxValue;
            float step = setting.Step;

            GameObject entryObj = new GameObject($"DuckovLuckyBox.UI.Entry.{setting.Key}");
            entryObj.transform.SetParent(settingsPanel!.transform, false);

            // Background
            Image entryBg = entryObj.AddComponent<Image>();
            entryBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            var horizontalLayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.padding = new RectOffset(16, 16, 8, 8);
            horizontalLayout.spacing = 16f;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(entryObj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = setting.Label.ToPlainText();
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 300f;
            labelLayout.flexibleWidth = 1f;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(entryObj.transform, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(200f, 32f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.wholeNumbers = step >= 1f;
            slider.minValue = minValue;
            slider.maxValue = maxValue;

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Fill area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.4f, 0.6f, 0.8f, 1f);

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // Handle
            GameObject handleAreaObj = new GameObject("Handle Slide Area");
            handleAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;

            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 0f);

            // Setup slider
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            // Value field
            GameObject valueFieldObj = new GameObject("ValueField");
            valueFieldObj.transform.SetParent(entryObj.transform, false);

            Image valueFieldBg = valueFieldObj.AddComponent<Image>();
            valueFieldBg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            TMP_InputField valueField = valueFieldObj.AddComponent<TMP_InputField>();
            valueField.contentType = TMP_InputField.ContentType.IntegerNumber;

            GameObject valueTextObj = new GameObject("Text");
            valueTextObj.transform.SetParent(valueFieldObj.transform, false);
            TextMeshProUGUI valueText = valueTextObj.AddComponent<TextMeshProUGUI>();
            valueText.fontSize = 18;
            valueText.color = Color.white;
            valueText.alignment = TextAlignmentOptions.Center;

            RectTransform valueTextRect = valueTextObj.GetComponent<RectTransform>();
            valueTextRect.anchorMin = Vector2.zero;
            valueTextRect.anchorMax = Vector2.one;
            valueTextRect.offsetMin = new Vector2(8, 0);
            valueTextRect.offsetMax = new Vector2(-8, 0);

            valueField.textComponent = valueText;
            valueField.targetGraphic = valueFieldBg;

            // Set initial value
            long currentValue = setting.Value is long l ? l : (long)minValue;
            slider.SetValueWithoutNotify(currentValue);
            valueField.SetTextWithoutNotify(currentValue.ToString());

            // Add listeners
            slider.onValueChanged.AddListener(value =>
            {
                // Round to nearest step
                long rawValue = (long)value;
                long stepValue = (long)step;
                long newValue = (rawValue / stepValue) * stepValue;

                setting.Value = newValue;
                valueField.SetTextWithoutNotify(newValue.ToString());

                // Update slider if rounding changed the value
                if (newValue != rawValue)
                {
                    slider.SetValueWithoutNotify(newValue);
                }

                Log.Debug($"Slider changed - Setting: {setting.Key}, New value: {newValue}");
            });

            valueField.onEndEdit.AddListener(text =>
            {
                if (long.TryParse(text, out long newValue))
                {
                    // Clamp to min/max
                    newValue = Math.Max((long)minValue, Math.Min((long)maxValue, newValue));

                    // Round to nearest step
                    long stepValue = (long)step;
                    newValue = (newValue / stepValue) * stepValue;

                    setting.Value = newValue;
                    slider.SetValueWithoutNotify(newValue);
                    valueField.SetTextWithoutNotify(newValue.ToString());
                    Log.Debug($"ValueField changed - Setting: {setting.Key}, New value: {newValue}");
                }
                else
                {
                    long resetValue = setting.Value is long l ? l : (long)minValue;
                    valueField.SetTextWithoutNotify(resetValue.ToString());
                }
            });

            var sliderLayout = sliderObj.AddComponent<LayoutElement>();
            sliderLayout.preferredWidth = 200f;
            sliderLayout.preferredHeight = 32f;

            var valueFieldLayout = valueFieldObj.AddComponent<LayoutElement>();
            valueFieldLayout.preferredWidth = 80f;
            valueFieldLayout.preferredHeight = 32f;

            var entryLayout = entryObj.AddComponent<LayoutElement>();
            entryLayout.preferredHeight = 48f;
        }

        private void CreateHotkeySetting(SettingItem setting)
        {
            GameObject entryObj = new GameObject($"DuckovLuckyBox.UI.Entry.{setting.Key}");
            entryObj.transform.SetParent(settingsPanel!.transform, false);

            // Background
            Image entryBg = entryObj.AddComponent<Image>();
            entryBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            var horizontalLayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.padding = new RectOffset(16, 16, 8, 8);
            horizontalLayout.spacing = 16f;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(entryObj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = setting.Label.ToPlainText();
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 400f;
            labelLayout.flexibleWidth = 1f;

            // Button
            GameObject buttonObj = new GameObject("Button");
            buttonObj.transform.SetParent(entryObj.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(150f, 32f);

            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.5f, 0.7f, 1f);

            // Button text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;

            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = new Vector2(4, 0);
            buttonTextRect.offsetMax = new Vector2(-4, 0);

            button.targetGraphic = buttonImage;

            // Set initial value
            var currentHotkey = setting.Value as Hotkey ?? DefaultSettings.SettingsHotkey;
            buttonText.text = currentHotkey.ToString();

            bool isWaitingForKey = false;

            button.onClick.AddListener(() =>
            {
                if (!isWaitingForKey)
                {
                    isWaitingForKey = true;
                    buttonText.text = Localizations.I18n.SettingsPressAnyKeyKey.ToPlainText();
                    StartCoroutine(WaitForKeyPress(buttonText, setting, () => isWaitingForKey = false));
                }
            });

            var buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 150f;
            buttonLayout.preferredHeight = 32f;

            var entryLayout = entryObj.AddComponent<LayoutElement>();
            entryLayout.preferredHeight = 48f;
        }

        private IEnumerator WaitForKeyPress(TextMeshProUGUI textComponent, SettingItem setting, Action onComplete)
        {
            // Wait for any key press
            while (!Input.anyKeyDown)
            {
                yield return null;
            }

            // Capture modifier keys state
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            // Find which key was pressed
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    // Filter out mouse buttons
                    if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6)
                    {
                        continue;
                    }

                    // Skip modifier keys themselves as the main key
                    if (keyCode == KeyCode.LeftControl || keyCode == KeyCode.RightControl ||
                        keyCode == KeyCode.LeftShift || keyCode == KeyCode.RightShift ||
                        keyCode == KeyCode.LeftAlt || keyCode == KeyCode.RightAlt)
                    {
                        continue;
                    }

                    var hotkey = new Hotkey(keyCode, ctrlPressed, shiftPressed, altPressed);

                    Log.Debug($"HotkeyInput changed - Setting: {setting.Key}, New hotkey: {hotkey}");
                    setting.Value = hotkey;
                    textComponent.text = hotkey.ToString();
                    Log.Debug($"Setting updated - Setting: {setting.Key}, Stored value: {setting.Value}");
                    break;
                }
            }

            onComplete?.Invoke();
        }

        private void CreateResetButton()
        {
            GameObject buttonContainerObj = new GameObject("DuckovLuckyBox.UI.ResetButtonContainer");
            buttonContainerObj.transform.SetParent(settingsPanel!.transform, false);

            // Add some spacing above the button
            var containerLayout = buttonContainerObj.AddComponent<LayoutElement>();
            containerLayout.preferredHeight = 50f;

            // Center the button using horizontal layout
            var horizontalLayout = buttonContainerObj.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.childForceExpandWidth = false;

            // Reset button
            GameObject buttonObj = new GameObject("ResetButton");
            buttonObj.transform.SetParent(buttonContainerObj.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200f, 40f);

            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.8f, 0.3f, 0.3f, 1f); // Red-ish color to indicate reset action

            // Button text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = Localizations.I18n.SettingsResetToDefaultKey.ToPlainText();
            buttonText.fontSize = 18;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;

            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = new Vector2(8, 0);
            buttonTextRect.offsetMax = new Vector2(-8, 0);

            button.targetGraphic = buttonImage;

            // Add click event
            button.onClick.AddListener(() =>
            {
                Log.Info("Reset to default button clicked.");
                SettingManager.Instance.ResetToDefaults();

                // Refresh the UI by reinitializing
                OnDestroy();
                StartCoroutine(InitializeAsync());
            });

            var buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 200f;
            buttonLayout.preferredHeight = 40f;
        }
    }
}
