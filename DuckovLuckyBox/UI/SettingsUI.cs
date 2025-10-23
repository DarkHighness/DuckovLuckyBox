using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SodaCraft.Localizations;

namespace DuckovLuckyBox.Core.Settings.UI
{
  public class SettingsUI : MonoBehaviour
  {
    private GameObject? settingsPanel;
    private Canvas? canvas;
    private RectTransform? panelTransform;
    private Font defaultFont = null!;
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

      // Load assets in first frame to get them ready
      yield return null;

      // Ensure inputs can reach the runtime-generated UI.
      if (EventSystem.current == null)
      {
        var eventSystemObj = new GameObject("DuckovLuckyBox.UI.EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
      }

      // Load built-in assets so text and toggles render even when the host project has no prefabs.
      defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

      // Spread UI creation across frames to avoid stutters
      CreateSettingPanel();
      yield return null;

      CreateSettings();
      yield return null;

      initialized = true;
      initializing = false;
      Log.Debug("Settings UI Initialized (async complete).");

      // Display after initialization
      ShowSettingsUI();
    }

    private void CreateSettingPanel()
    {
      // Create canvas
      GameObject canvasObj = new GameObject("DuckovLuckyBox.UI.SettingsUICanvas");
      canvas = canvasObj.AddComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.sortingOrder = short.MaxValue;

      var scaler = canvasObj.AddComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920f, 1080f);

      canvasObj.AddComponent<GraphicRaycaster>();

      // Create settings panel - Match game's dark cyberpunk style
      settingsPanel = new GameObject("DuckovLuckyBox.UI.SettingsPanel");
      settingsPanel.transform.SetParent(canvas.transform, false);

      // Create fullscreen blocker with semi-transparent dark overlay (Material Design scrim)
      // Place it as child of settingsPanel so it's hidden when panel is hidden
      GameObject blockerObj = new GameObject("DuckovLuckyBox.UI.InputBlocker");
      blockerObj.transform.SetParent(settingsPanel.transform, false);
      Image blockerImage = blockerObj.AddComponent<Image>();
      blockerImage.color = new Color(0f, 0f, 0f, 0.5f); // Simple black overlay at 50% opacity

      RectTransform blockerRect = blockerObj.GetComponent<RectTransform>();
      blockerRect.anchorMin = Vector2.zero;
      blockerRect.anchorMax = Vector2.one;
      blockerRect.offsetMin = Vector2.zero;
      blockerRect.offsetMax = Vector2.zero;
      blockerRect.SetAsFirstSibling(); // Ensure blocker is behind the panel content
      blockerObj.AddComponent<GraphicRaycaster>();

      // Base background - Material Design surface color (pure white)
      Image backgroundImage = settingsPanel.AddComponent<Image>();
      backgroundImage.color = new Color(1f, 1f, 1f, 1f); // Pure white background

      panelTransform = settingsPanel.GetComponent<RectTransform>();
      panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
      panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
      panelTransform.pivot = new Vector2(0.5f, 0.5f);
      panelTransform.anchoredPosition = Vector2.zero;
      panelTransform.sizeDelta = new Vector2(600f, 480f); // Material Design card proportions

      var verticalLayout = settingsPanel.AddComponent<VerticalLayoutGroup>();
      verticalLayout.childAlignment = TextAnchor.UpperLeft;
      verticalLayout.childForceExpandHeight = false;
      verticalLayout.childForceExpandWidth = true;
      verticalLayout.spacing = 8f; // Consistent spacing
      verticalLayout.padding = new RectOffset(24, 24, 24, 24); // Material Design padding

      settingsPanel.SetActive(false);
    }

    private void CreateSettings()
    {
      CreateSettingsTitle();

      // group settings by category
      var categorizedSettings = new Dictionary<Category, List<SettingItem>>();
      foreach (var setting in Settings.Instance.AllSettings)
      {
        if (!categorizedSettings.ContainsKey(setting.Category))
        {
          categorizedSettings[setting.Category] = new List<SettingItem>();
        }
        categorizedSettings[setting.Category].Add(setting);
      }

      // create UI for each category
      foreach (var category in categorizedSettings.Keys)
      {
        CreateSettingCategory(category, categorizedSettings[category]);
      }
    }

    private void CreateSettingsTitle()
    {
      // Material Design: Dark gray title text (87% opacity on white)
      Text titleComponent = CreateText(settingsPanel!.transform, "DuckovLuckyBox.UI.SettingsTitle", Constants.I18n.SettingsPanelTitleKey.ToPlainText(), 24, new Color(0f, 0f, 0f, 0.87f), TextAnchor.MiddleLeft);
      titleComponent.fontStyle = FontStyle.Bold;
      var layout = titleComponent.gameObject.AddComponent<LayoutElement>();
      layout.preferredHeight = 48f;
    }

    private void CreateSettingCategory(Category category, List<SettingItem> settings)
    {
      string categoryDisplayName = category switch
      {
        Category.General => Constants.I18n.SettingsCategoryGeneralKey.ToPlainText(),
        Category.Pricing => Constants.I18n.SettingsCategoryPricingKey.ToPlainText(),
        _ => category.ToString()
      };

      // Material Design: Medium gray category text (60% opacity)
      Text categoryText = CreateText(settingsPanel!.transform, $"DuckovLuckyBox.UI.SettingCategory.{category}", $"{categoryDisplayName}", 14, new Color(0f, 0f, 0f, 0.6f), TextAnchor.MiddleLeft);
      categoryText.fontStyle = FontStyle.Bold;
      var layout = categoryText.gameObject.AddComponent<LayoutElement>();
      layout.preferredHeight = 32f;

      // Create settings under this category
      foreach (var setting in settings)
      {
        CreateSettingItem(setting);
      }
    }

    private void CreateSettingItem(SettingItem setting)
    {
      GameObject settingItem = new GameObject($"DuckovLuckyBox.UI.SettingItem.{setting.Key}");
      settingItem.transform.SetParent(settingsPanel!.transform, false);

      // Material Design: Light gray background for list items (hover state)
      Image itemBackground = settingItem.AddComponent<Image>();
      itemBackground.color = new Color(0.96f, 0.96f, 0.96f, 1f); // #F5F5F5

      var rowLayout = settingItem.AddComponent<HorizontalLayoutGroup>();
      rowLayout.childAlignment = TextAnchor.MiddleLeft;
      rowLayout.spacing = 16f;
      rowLayout.padding = new RectOffset(16, 16, 12, 12);
      rowLayout.childForceExpandHeight = false;
      rowLayout.childForceExpandWidth = false;

      string labelText = setting.Label.ToPlainText();
      // Material Design: Dark gray text (87% opacity)
      Text labelComponent = CreateText(settingItem.transform, $"DuckovLuckyBox.UI.SettingItem.{setting.Key}.Label", labelText, 14, new Color(0f, 0f, 0f, 0.87f), TextAnchor.MiddleLeft);
      var labelLayout = labelComponent.gameObject.AddComponent<LayoutElement>();
      labelLayout.minWidth = 380f;
      labelLayout.flexibleWidth = 1f;

      if (setting.Type == Type.Toggle)
      {
        CreateToggle(settingItem.transform, setting);
      }
      else if (setting.Type == Type.Number)
      {
        CreateNumberInput(settingItem.transform, setting);
      }
      else if (setting.Type == Type.Hotkey)
      {
        CreateHotkeyInput(settingItem.transform, setting);
      }

      var itemLayout = settingItem.AddComponent<LayoutElement>();
      itemLayout.preferredHeight = 48f; // Material Design list item height
    }

    private Text CreateText(Transform parent, string objectName, string content, int fontSize, Color color, TextAnchor alignment)
    {
      GameObject textObj = new GameObject(objectName);
      textObj.transform.SetParent(parent, false);
      Text text = textObj.AddComponent<Text>();
      text.text = content;
      text.fontSize = fontSize;
      text.color = color;
      text.font = defaultFont;
      text.alignment = alignment;
      text.horizontalOverflow = HorizontalWrapMode.Overflow;
      text.verticalOverflow = VerticalWrapMode.Overflow;
      return text;
    }

    private void CreateToggle(Transform parent, SettingItem setting)
    {
      GameObject toggleRoot = new GameObject($"DuckovLuckyBox.UI.Toggle.{setting.Key}");
      toggleRoot.transform.SetParent(parent, false);

      RectTransform toggleRect = toggleRoot.AddComponent<RectTransform>();
      toggleRect.sizeDelta = new Vector2(52f, 32f); // Material Design switch size

      // Background for toggle - Material Design switch track
      GameObject backgroundObj = new GameObject("Background");
      backgroundObj.transform.SetParent(toggleRoot.transform, false);
      Image backgroundImage = backgroundObj.AddComponent<Image>();
      backgroundImage.color = new Color(0.62f, 0.62f, 0.62f, 0.5f); // Gray track when OFF

      RectTransform backgroundRect = backgroundObj.GetComponent<RectTransform>();
      backgroundRect.anchorMin = Vector2.zero;
      backgroundRect.anchorMax = Vector2.one;
      backgroundRect.offsetMin = Vector2.zero;
      backgroundRect.offsetMax = Vector2.zero;

      // Checkmark for toggle - Material Design primary color when ON
      GameObject checkmarkObj = new GameObject("Checkmark");
      checkmarkObj.transform.SetParent(backgroundObj.transform, false);
      Image checkmarkImage = checkmarkObj.AddComponent<Image>();
      checkmarkImage.color = new Color(0.13f, 0.59f, 0.95f, 1f); // Material Blue 600 (#2196F3)

      RectTransform checkmarkRect = checkmarkObj.GetComponent<RectTransform>();
      checkmarkRect.anchorMin = new Vector2(0.15f, 0.15f);
      checkmarkRect.anchorMax = new Vector2(0.85f, 0.85f);
      checkmarkRect.offsetMin = Vector2.zero;
      checkmarkRect.offsetMax = Vector2.zero;

      Toggle toggle = toggleRoot.AddComponent<Toggle>();
      toggle.targetGraphic = backgroundImage;
      toggle.graphic = checkmarkImage;
      toggle.isOn = setting.Value is bool boolean && boolean;

      Log.Debug($"CreateToggle - Setting: {setting.Key}, Value: {setting.Value}, Type: {setting.Value?.GetType().Name ?? "null"}, Toggle.isOn: {toggle.isOn}");

      // Material Design color scheme
      var colors = toggle.colors;
      colors.normalColor = new Color(0.62f, 0.62f, 0.62f, 0.5f); // Gray when OFF
      colors.highlightedColor = new Color(0.13f, 0.59f, 0.95f, 0.12f); // Light blue tint
      colors.pressedColor = new Color(0.13f, 0.59f, 0.95f, 0.24f); // Darker blue tint
      colors.selectedColor = new Color(0.13f, 0.59f, 0.95f, 1f); // Blue when ON
      colors.disabledColor = new Color(0.62f, 0.62f, 0.62f, 0.26f); // Lighter gray when disabled
      toggle.colors = colors;

      toggle.onValueChanged.AddListener(value =>
      {
        Log.Debug($"Toggle changed - Setting: {setting.Key}, New value: {value}");
        setting.Value = value;
        Log.Debug($"Setting updated - Setting: {setting.Key}, Stored value: {setting.Value}");
      });

      var layoutElement = toggleRoot.AddComponent<LayoutElement>();
      layoutElement.preferredWidth = 52f;
      layoutElement.preferredHeight = 32f;
    }

    private void CreateNumberInput(Transform parent, SettingItem setting)
    {
      GameObject inputRoot = new GameObject($"DuckovLuckyBox.UI.NumberInput.{setting.Key}");
      inputRoot.transform.SetParent(parent, false);

      RectTransform inputRect = inputRoot.AddComponent<RectTransform>();
      inputRect.sizeDelta = new Vector2(100f, 32f);

      // Background for input field - Material Design filled text field
      Image inputBackground = inputRoot.AddComponent<Image>();
      inputBackground.color = new Color(0.96f, 0.96f, 0.96f, 1f); // Light gray background

      // Create InputField
      GameObject textObj = new GameObject("Text");
      textObj.transform.SetParent(inputRoot.transform, false);
      Text textComponent = textObj.AddComponent<Text>();
      textComponent.font = defaultFont;
      textComponent.fontSize = 14;
      textComponent.color = new Color(0f, 0f, 0f, 0.87f); // Material Design primary text
      textComponent.alignment = TextAnchor.MiddleCenter;
      textComponent.supportRichText = false;

      RectTransform textRect = textObj.GetComponent<RectTransform>();
      textRect.anchorMin = Vector2.zero;
      textRect.anchorMax = Vector2.one;
      textRect.offsetMin = new Vector2(8, 0);
      textRect.offsetMax = new Vector2(-8, 0);

      InputField inputField = inputRoot.AddComponent<InputField>();
      inputField.textComponent = textComponent;
      inputField.targetGraphic = inputBackground;
      inputField.contentType = InputField.ContentType.IntegerNumber;

      // Set initial value
      long currentValue = setting.Value is long longVal ? longVal : 0L;
      inputField.text = currentValue.ToString();

      Log.Debug($"CreateNumberInput - Setting: {setting.Key}, Value: {setting.Value}, Type: {setting.Value?.GetType().Name ?? "null"}, InputField.text: {inputField.text}");

      // Material Design color scheme for text field
      var colors = inputField.colors;
      colors.normalColor = new Color(0.96f, 0.96f, 0.96f, 1f); // Light gray
      colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f); // Slightly darker
      colors.pressedColor = new Color(0.90f, 0.90f, 0.90f, 1f); // Even darker
      colors.selectedColor = new Color(0.13f, 0.59f, 0.95f, 0.12f); // Light blue tint when selected
      colors.disabledColor = new Color(0.96f, 0.96f, 0.96f, 0.38f); // Faded when disabled
      inputField.colors = colors;

      inputField.onEndEdit.AddListener(value =>
      {
        if (long.TryParse(value, out long newValue))
        {
          // Ensure non-negative values
          newValue = System.Math.Max(0, newValue);
          inputField.text = newValue.ToString();

          Log.Debug($"NumberInput changed - Setting: {setting.Key}, New value: {newValue}");
          setting.Value = newValue;
          Log.Debug($"Setting updated - Setting: {setting.Key}, Stored value: {setting.Value}");
        }
        else
        {
          // Invalid input, reset to current value
          long resetValue = setting.Value is long longVal ? longVal : 0L;
          inputField.text = resetValue.ToString();
          Log.Warning($"Invalid number input for {setting.Key}, reset to {resetValue}");
        }
      });

      var layoutElement = inputRoot.AddComponent<LayoutElement>();
      layoutElement.preferredWidth = 100f;
      layoutElement.preferredHeight = 32f;
    }

    private void CreateHotkeyInput(Transform parent, SettingItem setting)
    {
      GameObject buttonRoot = new GameObject($"DuckovLuckyBox.UI.HotkeyInput.{setting.Key}");
      buttonRoot.transform.SetParent(parent, false);

      RectTransform buttonRect = buttonRoot.AddComponent<RectTransform>();
      buttonRect.sizeDelta = new Vector2(120f, 32f);

      // Create button
      Button button = buttonRoot.AddComponent<Button>();
      Image buttonImage = buttonRoot.AddComponent<Image>();
      buttonImage.color = new Color(0.13f, 0.59f, 0.95f, 1f); // Material Blue

      // Button text
      GameObject textObj = new GameObject("Text");
      textObj.transform.SetParent(buttonRoot.transform, false);
      Text textComponent = textObj.AddComponent<Text>();
      textComponent.font = defaultFont;
      textComponent.fontSize = 14;
      textComponent.color = Color.white; // White text on blue button
      textComponent.alignment = TextAnchor.MiddleCenter;
      textComponent.supportRichText = false;

      RectTransform textRect = textObj.GetComponent<RectTransform>();
      textRect.anchorMin = Vector2.zero;
      textRect.anchorMax = Vector2.one;
      textRect.offsetMin = new Vector2(4, 0);
      textRect.offsetMax = new Vector2(-4, 0);

      button.targetGraphic = buttonImage;

      // Set initial value
      KeyCode currentKey = setting.Value is KeyCode keyCode ? keyCode : DefaultSettings.SettingsHotkey;
      textComponent.text = currentKey.ToString();

      Log.Debug($"CreateHotkeyInput - Setting: {setting.Key}, Value: {setting.Value}, Type: {setting.Value?.GetType().Name ?? "null"}, Button.text: {textComponent.text}");

      // Material Design color scheme for button
      var colors = button.colors;
      colors.normalColor = new Color(0.13f, 0.59f, 0.95f, 1f); // Material Blue
      colors.highlightedColor = new Color(0.10f, 0.53f, 0.89f, 1f); // Darker blue
      colors.pressedColor = new Color(0.08f, 0.47f, 0.83f, 1f); // Even darker
      colors.selectedColor = new Color(0.13f, 0.59f, 0.95f, 1f); // Same as normal
      colors.disabledColor = new Color(0.13f, 0.59f, 0.95f, 0.38f); // Faded
      button.colors = colors;

      bool isWaitingForKey = false;

      button.onClick.AddListener(() =>
      {
        if (!isWaitingForKey)
        {
          isWaitingForKey = true;
          textComponent.text = Constants.I18n.SettingsPressAnyKeyKey.ToPlainText();
          StartCoroutine(WaitForKeyPress(textComponent, setting, () => isWaitingForKey = false));
        }
      });

      var layoutElement = buttonRoot.AddComponent<LayoutElement>();
      layoutElement.preferredWidth = 120f;
      layoutElement.preferredHeight = 32f;
    }

    private IEnumerator WaitForKeyPress(Text textComponent, SettingItem setting, System.Action onComplete)
    {
      // Wait for any key press
      while (!Input.anyKeyDown)
      {
        yield return null;
      }

      // Find which key was pressed
      foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
      {
        if (Input.GetKeyDown(keyCode))
        {
          // Filter out mouse buttons if desired
          if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6)
          {
            continue;
          }

          Log.Debug($"HotkeyInput changed - Setting: {setting.Key}, New key: {keyCode}");
          setting.Value = keyCode;
          textComponent.text = keyCode.ToString();
          Log.Debug($"Setting updated - Setting: {setting.Key}, Stored value: {setting.Value}");
          break;
        }
      }

      onComplete?.Invoke();
    }
  }

}