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

      // Create fullscreen blocker to prevent input pass-through
      GameObject blockerObj = new GameObject("DuckovLuckyBox.UI.InputBlocker");
      blockerObj.transform.SetParent(canvas.transform, false);
      Image blockerImage = blockerObj.AddComponent<Image>();
      blockerImage.color = new Color(0, 0, 0, 0); // transparent

      RectTransform blockerRect = blockerObj.GetComponent<RectTransform>();
      blockerRect.anchorMin = Vector2.zero;
      blockerRect.anchorMax = Vector2.one;
      blockerRect.offsetMin = Vector2.zero;
      blockerRect.offsetMax = Vector2.zero;
      blockerObj.AddComponent<GraphicRaycaster>();

      // Create settings panel - Sci-Fi Style
      settingsPanel = new GameObject("DuckovLuckyBox.UI.SettingsPanel");
      settingsPanel.transform.SetParent(canvas.transform, false);

      // Base background - Deep indigo/blue
      Image backgroundImage = settingsPanel.AddComponent<Image>();
      backgroundImage.color = new Color(0.08f, 0.12f, 0.2f, 0.98f);

      panelTransform = settingsPanel.GetComponent<RectTransform>();
      panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
      panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
      panelTransform.pivot = new Vector2(0.5f, 0.5f);
      panelTransform.anchoredPosition = Vector2.zero;
      panelTransform.sizeDelta = new Vector2(600f, 560f);

      // Add glow/border effect
      var outline = settingsPanel.AddComponent<Outline>();
      outline.effectColor = new Color(0.2f, 0.6f, 1f, 0.6f); // Cyan glow
      outline.effectDistance = new Vector2(2, 2);

      var verticalLayout = settingsPanel.AddComponent<VerticalLayoutGroup>();
      verticalLayout.childAlignment = TextAnchor.UpperLeft;
      verticalLayout.childForceExpandHeight = false;
      verticalLayout.childForceExpandWidth = true;
      verticalLayout.spacing = 10f;
      verticalLayout.padding = new RectOffset(24, 24, 24, 24);

      // Add border decorations
      CreatePanelBorders(settingsPanel.transform);

      settingsPanel.SetActive(false);
    }

    private void CreatePanelBorders(Transform panelTransform)
    {
      // Top glow line
      CreateBorderLine(panelTransform, "TopBorder", new Vector2(0, 1), new Vector2(1, 1), 3f, new Color(0.2f, 0.7f, 1f, 0.9f));

      // Bottom border line
      CreateBorderLine(panelTransform, "BottomBorder", new Vector2(0, 0), new Vector2(1, 0), 2f, new Color(0.2f, 0.7f, 1f, 0.5f));
    }

    private void CreateBorderLine(Transform panelTransform, string name, Vector2 anchorMin, Vector2 anchorMax, float height, Color color)
    {
      GameObject lineObj = new GameObject(name);
      lineObj.transform.SetParent(panelTransform, false);
      Image lineImage = lineObj.AddComponent<Image>();
      lineImage.color = color;

      RectTransform lineRect = lineObj.GetComponent<RectTransform>();
      lineRect.anchorMin = anchorMin;
      lineRect.anchorMax = anchorMax;

      if (anchorMin.y == 1) // Top
      {
        lineRect.offsetMin = new Vector2(0, -height);
        lineRect.offsetMax = new Vector2(0, 0);
      }
      else // Bottom
      {
        lineRect.offsetMin = new Vector2(0, 0);
        lineRect.offsetMax = new Vector2(0, height);
      }
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
      Text titleComponent = CreateText(settingsPanel!.transform, "DuckovLuckyBox.UI.SettingsTitle", Constants.I18n.SettingsPanelTitleKey.ToPlainText(), 32, new Color(0.2f, 0.7f, 1f, 1f), TextAnchor.MiddleCenter);
      titleComponent.fontStyle = FontStyle.Bold;
      var layout = titleComponent.gameObject.AddComponent<LayoutElement>();
      layout.preferredHeight = 48f;

      // Add separator line after title
      CreateSeparatorLine(settingsPanel!.transform, "TitleSeparator");
    }

    private void CreateSeparatorLine(Transform parent, string name)
    {
      GameObject lineObj = new GameObject(name);
      lineObj.transform.SetParent(parent, false);
      Image lineImage = lineObj.AddComponent<Image>();
      lineImage.color = new Color(0.2f, 0.6f, 1f, 0.4f);

      RectTransform lineRect = lineObj.GetComponent<RectTransform>();
      lineRect.sizeDelta = new Vector2(0, 1);
      var layout = lineObj.AddComponent<LayoutElement>();
      layout.preferredHeight = 1f;
    }

    private void CreateSettingCategory(Category category, List<SettingItem> settings)
    {
      string categoryDisplayName = category == Category.General
        ? Constants.I18n.SettingsCategoryGeneralKey.ToPlainText()
        : category.ToString();

      Text categoryText = CreateText(settingsPanel!.transform, $"DuckovLuckyBox.UI.SettingCategory.{category}", $"▸ {categoryDisplayName}", 18, new Color(0.2f, 0.8f, 1f, 1f), TextAnchor.MiddleLeft);
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

      // Background with subtle sci-fi styling
      Image itemBackground = settingItem.AddComponent<Image>();
      itemBackground.color = new Color(0.1f, 0.15f, 0.25f, 0.8f);

      // Add border effect
      var outline = settingItem.AddComponent<Outline>();
      outline.effectColor = new Color(0.2f, 0.6f, 1f, 0.3f);
      outline.effectDistance = new Vector2(1, 1);

      var rowLayout = settingItem.AddComponent<HorizontalLayoutGroup>();
      rowLayout.childAlignment = TextAnchor.MiddleLeft;
      rowLayout.spacing = 16f;
      rowLayout.padding = new RectOffset(16, 16, 8, 8);
      rowLayout.childForceExpandHeight = false;
      rowLayout.childForceExpandWidth = false;

      string labelText = setting.Label.ToPlainText();
      Text labelComponent = CreateText(settingItem.transform, $"DuckovLuckyBox.UI.SettingItem.{setting.Key}.Label", labelText, 14, new Color(0.8f, 0.9f, 1f, 1f), TextAnchor.MiddleLeft);
      var labelLayout = labelComponent.gameObject.AddComponent<LayoutElement>();
      labelLayout.minWidth = 280f;
      labelLayout.flexibleWidth = 1f;

      if (setting.Type == Type.Toggle)
      {
        CreateToggle(settingItem.transform, setting);
      }

      var itemLayout = settingItem.AddComponent<LayoutElement>();
      itemLayout.preferredHeight = 40f;
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
      toggleRect.sizeDelta = new Vector2(56f, 28f);

      // Background for toggle - OFF state
      GameObject backgroundObj = new GameObject("Background");
      backgroundObj.transform.SetParent(toggleRoot.transform, false);
      Image backgroundImage = backgroundObj.AddComponent<Image>();
      backgroundImage.color = new Color(0.1f, 0.15f, 0.25f, 1f);

      RectTransform backgroundRect = backgroundObj.GetComponent<RectTransform>();
      backgroundRect.anchorMin = Vector2.zero;
      backgroundRect.anchorMax = Vector2.one;
      backgroundRect.offsetMin = Vector2.zero;
      backgroundRect.offsetMax = Vector2.zero;

      // Add glow border to background
      var bgOutline = backgroundObj.AddComponent<Outline>();
      bgOutline.effectColor = new Color(0.15f, 0.4f, 0.7f, 0.6f);
      bgOutline.effectDistance = new Vector2(1, 1);

      // Checkmark for toggle - ON state indicator
      GameObject checkmarkObj = new GameObject("Checkmark");
      checkmarkObj.transform.SetParent(backgroundObj.transform, false);
      Image checkmarkImage = checkmarkObj.AddComponent<Image>();
      checkmarkImage.color = new Color(0.2f, 0.8f, 1f, 1f);

      RectTransform checkmarkRect = checkmarkObj.GetComponent<RectTransform>();
      checkmarkRect.anchorMin = new Vector2(0.15f, 0.15f);
      checkmarkRect.anchorMax = new Vector2(0.85f, 0.85f);
      checkmarkRect.offsetMin = Vector2.zero;
      checkmarkRect.offsetMax = Vector2.zero;

      Toggle toggle = toggleRoot.AddComponent<Toggle>();
      toggle.targetGraphic = backgroundImage;
      toggle.graphic = checkmarkImage;
      toggle.isOn = setting.Value is bool boolean && boolean;

      // Sci-fi color scheme for toggle states
      var colors = toggle.colors;
      colors.normalColor = new Color(0.1f, 0.15f, 0.25f, 1f);
      colors.highlightedColor = new Color(0.15f, 0.25f, 0.35f, 1f);
      colors.pressedColor = new Color(0.08f, 0.12f, 0.2f, 1f);
      colors.selectedColor = new Color(0.1f, 0.3f, 0.5f, 1f);
      colors.disabledColor = new Color(0.08f, 0.1f, 0.15f, 0.5f);
      toggle.colors = colors;

      toggle.onValueChanged.AddListener(value =>
      {
        setting.Value = value;
      });

      var layoutElement = toggleRoot.AddComponent<LayoutElement>();
      layoutElement.preferredWidth = 56f;
      layoutElement.preferredHeight = 28f;
    }
  }

}