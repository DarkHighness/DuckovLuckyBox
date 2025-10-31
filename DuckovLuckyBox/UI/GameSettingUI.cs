using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Duckov.Options.UI;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using SodaCraft.Localizations;
using HarmonyLib;

namespace DuckovLuckyBox.UI
{
  public class GameSettingUI
  {
    private static GameSettingUI? _instance;
    public static GameSettingUI Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new GameSettingUI();
        }
        return _instance;
      }
    }

    private const string ModName = "DuckovLuckyBox";

    private bool modTabCreated = false;
    private OptionsPanel_TabButton? modTabButton = null;
    private GameObject? modContent = null;
    private GameObject? dropdownListPrefab;
    private GameObject? inputWithSliderPrefab;
    private Queue<Action> pendingConfigActions = new Queue<Action>();

    private GameSettingUI() { }

    public static void Initialize()
    {
      Instance.InitializeInstance();
    }

    private void InitializeInstance()
    {
      AddSettings();
      TryCreatingModSettingTab();
    }

    private void AddSettings()
    {
      var settings = SettingManager.Instance;

      // General Settings
      AddBoolSetting(settings.EnableAnimation, Localizations.I18n.SettingsEnableAnimationKey.ToPlainText());
      AddBoolSetting(settings.EnableDestroyButton, Localizations.I18n.SettingsEnableDestroyButtonKey.ToPlainText());
      AddBoolSetting(settings.EnableMeltButton, Localizations.I18n.SettingsEnableMeltButtonKey.ToPlainText());
      AddBoolSetting(settings.EnableDebug, Localizations.I18n.SettingsEnableDebugKey.ToPlainText());
      AddBoolSetting(settings.EnableUseToCreateItemPatch, Localizations.I18n.SettingsEnableUseToCreateItemPatchKey.ToPlainText());
      AddBoolSetting(settings.EnableWeightedLottery, Localizations.I18n.SettingsEnableWeightedLotteryKey.ToPlainText());
      AddBoolSetting(settings.EnableHighQualitySound, Localizations.I18n.SettingsEnableHighQualitySoundKey.ToPlainText());
      AddBoolSetting(settings.EnableStockShopActions, Localizations.I18n.SettingsEnableStockShopActionsKey.ToPlainText());

      AddSliderSetting(settings.SettingsHotkey, Localizations.I18n.SettingsHotkeyKey.ToPlainText());
      AddSliderSetting(settings.HighQualitySoundFilePath, Localizations.I18n.SettingsHighQualitySoundFilePathKey.ToPlainText());

      // Pricing Settings
      AddSliderSetting(settings.RefreshStockPrice, Localizations.I18n.SettingsRefreshStockPriceKey.ToPlainText(), new Vector2(settings.RefreshStockPrice.MinValue, settings.RefreshStockPrice.MaxValue));
      AddSliderSetting(settings.StorePickPrice, Localizations.I18n.SettingsStorePickPriceKey.ToPlainText(), new Vector2(settings.StorePickPrice.MinValue, settings.StorePickPrice.MaxValue));
      AddSliderSetting(settings.StreetPickPrice, Localizations.I18n.SettingsStreetPickPriceKey.ToPlainText(), new Vector2(settings.StreetPickPrice.MinValue, settings.StreetPickPrice.MaxValue));
      AddSliderSetting(settings.MeltBasePrice, Localizations.I18n.SettingsMeltBasePriceKey.ToPlainText(), new Vector2(settings.MeltBasePrice.MinValue, settings.MeltBasePrice.MaxValue));
    }

    /// <summary>
    /// Generic method for adding config items (supports delayed initialization)
    /// </summary>
    private void AddConfig(Action configAction)
    {
      if (modTabCreated)
      {
        configAction();
      }
      else
      {
        pendingConfigActions.Enqueue(configAction);
      }
    }

    /// <summary>
    /// Process all pending config items
    /// </summary>
    private void ProcessPendingConfigs()
    {
      while (pendingConfigActions.Count > 0)
      {
        var action = pendingConfigActions.Dequeue();
        action();
      }
    }

    /// <summary>
    /// Add a boolean dropdown setting
    /// </summary>
    private void AddBoolSetting(SettingItem settingItem, string description)
    {
      var options = new SortedDictionary<string, object>
            {
                { "Off", false },
                { "On", true }
            };

      AddDropdownSetting(settingItem, description, options);
    }

    /// <summary>
    /// Add a dropdown setting
    /// </summary>
    private void AddDropdownSetting(SettingItem settingItem, string description, SortedDictionary<string, object> options)
    {
      AddConfig(() =>
      {
        if (dropdownListPrefab == null || modContent == null)
          return;

        // Create title
        GameObject modNameTitleClone = CreateModTitle(ModName);

        // Clone dropdown list prefab
        GameObject inputWithSliderPrefabClone = UnityEngine.Object.Instantiate(dropdownListPrefab, modContent.transform);
        inputWithSliderPrefabClone.transform.SetSiblingIndex(modNameTitleClone.transform.GetSiblingIndex() + 1);
        inputWithSliderPrefabClone.name = "UI_" + ModName + "_" + settingItem.Key;

        // Setup dropdown list
        var dropdownMod = inputWithSliderPrefabClone.AddComponent<Dropdown>();
        dropdownMod.Init(settingItem, description, options);

        Log.Info($"Successfully added dropdown setting: {description}");
      });
    }

    /// <summary>
    /// Add an input field with optional slider
    /// </summary>
    private void AddSliderSetting(SettingItem settingItem, string description, Vector2? sliderRange = null)
    {
      AddConfig(() =>
      {
        if (inputWithSliderPrefab == null || modContent == null)
          return;

        // Create title
        GameObject modNameTitleClone = CreateModTitle(ModName);

        // Clone input slider prefab
        GameObject inputWithSliderPrefabClone = UnityEngine.Object.Instantiate(inputWithSliderPrefab, modContent.transform);
        inputWithSliderPrefabClone.transform.SetSiblingIndex(modNameTitleClone.transform.GetSiblingIndex() + 1);
        inputWithSliderPrefabClone.name = "UI_" + ModName + "_" + settingItem.Key;

        // Setup input slider
        var sliderMod = inputWithSliderPrefabClone.AddComponent<Slider>();
        sliderMod.Init(settingItem, description, sliderRange);

        Log.Info($"Successfully added slider setting: {description}");
      });
    }

    private GameObject CreateModTitle(string modName)
    {
      if (modContent == null)
        throw new System.InvalidOperationException("modContent is null");

      // 检查是否已经存在该mod的标题
      Transform existingTitle = modContent.transform.Find("Title_" + modName);
      if (existingTitle != null)
      {
        return existingTitle.gameObject;
      }

      // 创建新的标题
      GameObject titleObj = new GameObject("Title_" + modName);
      titleObj.transform.SetParent(modContent.transform);

      TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
      titleText.SetText(modName);
      titleText.margin = new Vector4(10, 10, 10, 10);

      return titleObj;
    }

    /// <summary>
    /// Add ModSetting tab to the settings menu
    /// </summary>
    private void CreateModSettingTab()
    {
      Log.Info("Starting to create Mod settings tab");

      // 获取MainMenu场景中的OptionsPanel
      OptionsPanel optionsPanel = UnityEngine.Object.FindObjectsOfType<OptionsPanel>(true)
          .FirstOrDefault(panel => panel.gameObject.scene.name == "DontDestroyOnLoad");

      if (optionsPanel == null)
      {
        Log.Error("OptionsPanel Not Found!!!");
        return;
      }

      Log.Info("OptionsPanel Found");

      // 使用反射获取tabButtons
      var tabButtonsField = AccessTools.Field(optionsPanel.GetType(), "tabButtons");
      var tabButtons = (List<OptionsPanel_TabButton>)tabButtonsField.GetValue(optionsPanel);
      if (tabButtons == null)
      {
        Log.Error("Failed to get tabButtons via reflection!!!!");
        return;
      }

      Log.Info("Successfully got tabButtons via reflection");

      // 复制一个tabButton的游戏对象
      GameObject tabButtonGameObjectClone = UnityEngine.Object.Instantiate(tabButtons[0].gameObject, tabButtons[0].gameObject.transform.parent);
      tabButtonGameObjectClone.name = "modTab";

      OptionsPanel_TabButton modTabButton = tabButtonGameObjectClone.GetComponent<OptionsPanel_TabButton>();
      if (modTabButton == null)
      {
        Log.Error("Failed to get OptionsPanel_TabButton component from cloned GameObject");
        UnityEngine.Object.Destroy(tabButtonGameObjectClone);
        return;
      }

      this.modTabButton = modTabButton;

      // 获取原始tab并克隆
      var tabField = AccessTools.Field(modTabButton.GetType(), "tab");
      var tab = (GameObject)tabField.GetValue(modTabButton);
      if (tab == null)
      {
        Log.Error("Failed to get tab member from modTabButton via reflection");
        UnityEngine.Object.Destroy(tabButtonGameObjectClone);
        return;
      }

      GameObject tabClone = UnityEngine.Object.Instantiate(tab, tab.transform.parent);
      tabClone.name = "modContent";
      modContent = tabClone;

      // 设置克隆的tab到tabButton
      var tabField2 = AccessTools.Field(modTabButton.GetType(), "tab");
      tabField2.SetValue(modTabButton, tabClone);

      // 添加到tabButtons列表
      tabButtons.Add(modTabButton);

      // 调用Setup更新UI
      var setupMethod = AccessTools.Method(optionsPanel.GetType(), "Setup");
      setupMethod.Invoke(optionsPanel, null);

      // 修改标签页名称
      TextMeshProUGUI? tabName = modTabButton.GetComponentInChildren<TextMeshProUGUI>(true);
      if (tabName != null)
      {
        // 移除本地化组件
        var localizor = modTabButton.GetComponentInChildren<TextLocalizor>(true);
        if (localizor != null)
          UnityEngine.Object.Destroy(localizor);

        tabName.SetText("Mod Settings");
      }

      // 清空内容并创建下拉列表预制体
      if (modContent != null)
      {
        // Clear all children
        for (int i = modContent.transform.childCount - 1; i >= 0; i--)
        {
          UnityEngine.Object.DestroyImmediate(modContent.transform.GetChild(i).gameObject);
        }

        // 查找分辨率下拉列表作为模板
        OptionsUIEntry_Dropdown resolutionDropDown = tabClone.transform.parent.GetComponentsInChildren<OptionsUIEntry_Dropdown>(true)
            .FirstOrDefault(dropdown => dropdown.gameObject.name == "UI_Resolution");

        if (resolutionDropDown != null)
        {
          GameObject dropdownListPrefab = UnityEngine.Object.Instantiate(resolutionDropDown.gameObject, modContent.transform);
          dropdownListPrefab.name = "dropDownPrefab";
          dropdownListPrefab.SetActive(false);
          this.dropdownListPrefab = dropdownListPrefab;
          Log.Info("Successfully created dropdown list prefab");
        }
        else
        {
          Log.Error("Resolution dropdown not found as template");
        }
      }

      MakeInputWithSliderPrefab();

      Log.Info("Mod settings tab creation completed");

      // 立即处理等待的配置项
      ProcessPendingConfigs();
    }

    private void MakeInputWithSliderPrefab()
    {
      if (modContent == null)
        return;

      // 从游戏中克隆一个鼠标灵敏度的设置项作为模板
      OptionsUIEntry_Slider mouseSensitivitySlider = UnityEngine.Object.FindObjectsOfType<OptionsUIEntry_Slider>(true)
          .FirstOrDefault(slider => slider.gameObject.name == "UI_MouseSensitivity");

      if (mouseSensitivitySlider != null)
      {
        GameObject UI_MouseSensitivity_Clone = UnityEngine.Object.Instantiate(mouseSensitivitySlider.gameObject, modContent.transform);
        UI_MouseSensitivity_Clone.name = "inputWithSliderPrefab";
        UI_MouseSensitivity_Clone.SetActive(false);

        // 获取引用
        var labelField = AccessTools.Field(mouseSensitivitySlider.GetType(), "label");
        var label = (TextMeshProUGUI)labelField.GetValue(mouseSensitivitySlider);
        var sliderField = AccessTools.Field(mouseSensitivitySlider.GetType(), "slider");
        var slider = (UnityEngine.UI.Slider)sliderField.GetValue(mouseSensitivitySlider);
        var valueFieldField = AccessTools.Field(mouseSensitivitySlider.GetType(), "valueField");
        var valueField = (TMP_InputField)valueFieldField.GetValue(mouseSensitivitySlider);

        UnityEngine.Object.DestroyImmediate(UI_MouseSensitivity_Clone.GetComponent<OptionsUIEntry_Slider>());

        inputWithSliderPrefab = UI_MouseSensitivity_Clone;
        Log.Info("Successfully created input slider prefab");
      }
      else
      {
        Log.Error("Mouse sensitivity slider not found as template");
      }
    }

    private void TryCreatingModSettingTab()
    {
      if (!modTabCreated)
      {
        CreateModSettingTab();
        modTabCreated = true;
      }
    }
  }
}