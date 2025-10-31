using System.Collections.Generic;
using Duckov.Options.UI;
using Duckov.Utilities;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI.Component;
using HarmonyLib;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;

namespace DuckovLuckyBox.UI
{
  public class GameSettingUI
  {
    private static GameSettingUI? instance = null;
    public static GameSettingUI Instance
    {
      get
      {
        instance ??= new GameSettingUI();
        return instance;
      }
    }

    private bool isInitialized = false;
    private bool isInitializing = false;
    private OptionsPanel? target = null;
    private OptionsPanel_TabButton? tabButton = null;
    private GameObject? tabContent = null;
    private OptionsUIEntry_Dropdown? dropdownPrefab = null;
    private OptionsUIEntry_Slider? sliderPrefab = null;
    // toggle has never been used in the game, so we comment it out for now
    // private OptionsUIEntry_Toggle? togglePrefab = null;

    public bool Initialize(OptionsPanel optionsPanel)
    {
      if (isInitializing)
      {
        return false; // Prevent re-entrance
      }

      isInitializing = true;

      if (isInitialized && target == optionsPanel)
      {
        isInitializing = false;
        return false; // Already initialized for this panel
      }

      try
      {
        isInitialized = InitializeInternal(optionsPanel);
      }
      finally
      {
        isInitializing = false;
        if (!isInitialized)
        {
          Cleanup();
        }
      }

      return isInitialized;
    }

    private bool InitializeInternal(OptionsPanel optionsPanel)
    {
      Log.Debug("Initializing GameSettingUI...");

      target = optionsPanel;

      if (!InitializeTab())
      {
        return false;
      }

      if (!CreateSettings())
      {
        return false;
      }

      return true;
    }

    private bool InitializeTab()
    {
      if (target == null)
      {
        return false; // Target panel is not set
      }

      // Create custom UI elements
      if (!CreatePrefabs())
      {
        Log.Error("Failed to create prefabs for the tab content.");
        return false;
      }

      var tabButtons = AccessTools.Field(typeof(OptionsPanel), "tabButtons").GetValue(target) as List<OptionsPanel_TabButton>;
      if (tabButtons == null)
      {
        Log.Error("Failed to access tabButtons field.");
        return false;
      }

      if (tabButtons.Count == 0)
      {
        Log.Error("No tab buttons found in the OptionsPanel.");
        return false;
      }
      // Clone the first tab button as a template
      var tabButtonClone = UnityEngine.Object.Instantiate(tabButtons[0].gameObject, tabButtons[0].gameObject.transform.parent);
      tabButtonClone.name = Constants.ModId + "_TabButton";

      tabButton = tabButtonClone.GetComponent<OptionsPanel_TabButton>();
      if (tabButton == null)
      {
        Log.Error("Failed to get OptionsPanel_TabButton component from cloned object.");
        return false;
      }

      // get the `tab` field of the tabButton
      GameObject? tab = AccessTools.Field(typeof(OptionsPanel_TabButton), "tab")?.GetValue(tabButton) as GameObject;
      if (tab == null)
      {
        Log.Error("Failed to access tab field of the tab button.");
        Cleanup();
        return false;
      }

      // Clone the tab
      tabContent = (GameObject)UnityEngine.Object.Instantiate((UnityEngine.Object)tab, tab.transform.parent);
      tabContent.name = Constants.ModId + "_Tab";

      // Set the tab to the tabButton
      AccessTools.Field(typeof(OptionsPanel_TabButton), "tab")?.SetValue(tabButton, tabContent);

      // Add the tabButton to the tabButtons list
      tabButtons.Add(tabButton);

      // Call the setup method to refresh the tabs
      var setupMethod = AccessTools.Method(typeof(OptionsPanel), "Setup");
      setupMethod?.Invoke(target, null);

      // Change the tab name
      TextMeshProUGUI? tabName = tabButton.GetComponentInChildren<TextMeshProUGUI>(true);
      if (tabName != null)
      {
        // Remove the TextLocalizor component if exists
        var textLocalizor = tabName.GetComponent<TextLocalizor>();
        if (textLocalizor != null)
        {
          UnityEngine.Object.DestroyImmediate(textLocalizor);
        }

        // Set the tab name
        tabName.text = Localizations.I18n.ModNameKey.ToPlainText();
      }

      // Cleanup the content of the tab and add our own UI elements
      tabContent.transform.DestroyAllChildren();

      Log.Debug("GameSettingUI tab created successfully.");

      return true;
    }

    private bool CreateSettings()
    {
      if (tabContent == null)
      {
        Log.Error("Tab content is not set.");
        return false;
      }

      var settings = SettingManager.Instance.AllSettings;
      foreach (var item in settings)
      {
        if (!CreateSetting(item))
        {
          Log.Error($"Failed to create setting UI for {item.Key.ToPlainText()}.");
          return false;
        }

        Log.Debug($"Setting UI for {item.Key.ToPlainText()} created successfully.");
      }

      Log.Debug("All settings created successfully.");

      return true;
    }

    private bool CreatePrefabs()
    {
      // Find OptionsUIEntry_Dropdown, OptionsUIEntry_Slider and OptionsUIEntry_Toggle prefabs from the existing game-wide UI
      var dropdown = UnityEngine.Object.FindObjectOfType<OptionsUIEntry_Dropdown>(true);
      var slider = UnityEngine.Object.FindObjectOfType<OptionsUIEntry_Slider>(true);
      // toggle has never been used in the game, so we comment it out for now
      // var toggle = UnityEngine.Object.FindObjectOfType<OptionsUIEntry_Toggle>(true);

      if (dropdown == null)
      {
        Log.Error("Failed to find OptionsUIEntry_Dropdown prefab.");
        return false;
      }

      if (slider == null)
      {
        Log.Error("Failed to find OptionsUIEntry_Slider prefab.");
        return false;
      }

      Log.Debug($"Clone {dropdown.name} and {slider.name} prefabs for GameSettingUI.");

      // Clone and register the prefabs
      dropdownPrefab = UnityEngine.Object.Instantiate(dropdown);
      dropdownPrefab.name = Constants.ModId + "_DropdownPrefab";
      dropdownPrefab.gameObject.SetActive(false);

      sliderPrefab = UnityEngine.Object.Instantiate(slider);
      sliderPrefab.name = Constants.ModId + "_SliderPrefab";
      sliderPrefab.gameObject.SetActive(false);

      // Dump Hierarchy for debugging
      DebugUtils.DumpGameObjectHierarchy(dropdownPrefab.gameObject, 5, true, false, null);
      DebugUtils.DumpGameObjectHierarchy(sliderPrefab.gameObject, 5, true, false, null);

      Log.Debug("GameSettingUI prefabs created successfully.");

      return true;
    }

    private bool CreateSetting(SettingItem item)
    {
      if (tabContent == null)
      {
        Log.Error("Tab content is not set.");
        return false;
      }

      return item.Type switch
      {
        Type.Number => CreateSliderSetting(item),
        Type.Toggle => CreateToggleSetting(item),
        Type.Hotkey => true, // Hotkey setting is not implemented yet.
        Type.Text => CreateInputSetting(item),
        _ => false,
      };

    }

    private bool CreateSliderSetting(SettingItem item)
    {
      if (sliderPrefab == null)
      {
        Log.Error("Slider prefab is not set.");
        return false;
      }

      var sliderInstance = UnityEngine.Object.Instantiate(sliderPrefab, tabContent!.transform);
      if (sliderInstance == null)
      {
        Log.Error("Failed to instantiate slider prefab for slider setting.");
        return false;
      }

      // Replace the slider with our custom slider
      var customSlider = sliderInstance.gameObject.AddComponent<SliderComponent>();
      var oldSlider = sliderInstance.GetComponent<OptionsUIEntry_Slider>();
      if (oldSlider == null)
      {
        Log.Error("Failed to get OptionsUIEntry_Slider component from slider instance.");
        UnityEngine.Object.Destroy(sliderInstance.gameObject);
        return false;
      }

      if (!customSlider.Setup(item, oldSlider))
      {
        Log.Error($"Failed to setup custom slider for setting {item.Key}.");
        UnityEngine.Object.Destroy(sliderInstance.gameObject);
        return false;
      }

      // Detach the custom slider from the original slider instance
      customSlider.transform.SetParent(tabContent!.transform, false);
      // Activate the custom slider and set its name
      sliderInstance.gameObject.SetActive(true);
      sliderInstance.name = "Settings_" + Constants.ModId + "_Slider_" + item.Key;
      // Move the custom slider to the correct position
      sliderInstance.transform.SetAsLastSibling();

      // Remove the original component
      UnityEngine.Object.DestroyImmediate(oldSlider);

      return true;
    }

    private bool CreateToggleSetting(SettingItem item)
    {
      if (sliderPrefab == null)
      {
        Log.Error("Slider prefab is not set.");
        return false;
      }

      var sliderInstance = UnityEngine.Object.Instantiate(sliderPrefab, tabContent!.transform);
      // Replace the slider with our custom slider
      var customToggle = sliderInstance.gameObject.AddComponent<ToggleComponent>();
      var oldSlider = sliderInstance.GetComponent<OptionsUIEntry_Slider>();
      if (oldSlider == null)
      {
        Log.Error("Failed to get OptionsUIEntry_Slider component from slider instance.");
        UnityEngine.Object.Destroy(sliderInstance.gameObject);
        return false;
      }

      if (!customToggle.Setup(item, oldSlider))
      {
        Log.Error($"Failed to setup custom toggle for setting {item.Key}.");
        UnityEngine.Object.Destroy(sliderInstance.gameObject);
        return false;
      }

      // Detach the custom toggle from the original slider instance
      customToggle.transform.SetParent(tabContent.transform, false);
      // Activate the custom toggle and set its name
      sliderInstance.gameObject.SetActive(true);
      sliderInstance.name = "Settings_" + Constants.ModId + "_Toggle_" + item.Key;
      // Move the custom toggle to the correct position
      sliderInstance.transform.SetAsLastSibling();

      // Remove the original
      UnityEngine.Object.DestroyImmediate(oldSlider);

      return true;
    }

    private bool CreateInputSetting(SettingItem item)
    {
      if (sliderPrefab == null)
      {
        Log.Error("Slider prefab is not set.");
        return false;
      }

      var sliderInstance = UnityEngine.Object.Instantiate(sliderPrefab, tabContent!.transform);
      if (sliderInstance == null)
      {
        Log.Error("Failed to instantiate slider prefab for input setting.");
        return false;
      }

      // Replace the slider with our custom input
      var customInput = sliderInstance.gameObject.AddComponent<InputComponent>();
      var oldSlider = sliderInstance.GetComponent<OptionsUIEntry_Slider>();
      if (oldSlider == null)
      {
        Log.Error("Failed to get OptionsUIEntry_Slider component from slider instance.");
        UnityEngine.Object.Destroy(sliderInstance.gameObject);
        return false;
      }

      if (!customInput.Setup(item, oldSlider))
      {
        Log.Error($"Failed to setup custom input for setting {item.Key}.");
        UnityEngine.Object.Destroy(sliderInstance.gameObject);
        return false;
      }

      // Detach the custom input from the original slider instance
      customInput.transform.SetParent(tabContent!.transform, false);
      // Activate the custom input and set its name
      sliderInstance.gameObject.SetActive(true);
      sliderInstance.name = "Settings_" + Constants.ModId + "_Input_" + item.Key;
      // Move the custom input to the correct position
      sliderInstance.transform.SetAsLastSibling();

      // Remove the original component
      UnityEngine.Object.DestroyImmediate(oldSlider);

      return true;
    }

    private void Cleanup()
    {
      if (tabButton != null)
      {
        UnityEngine.Object.Destroy(tabButton.gameObject);
        tabButton = null;
      }

      if (tabContent != null)
      {
        UnityEngine.Object.Destroy(tabContent);
        tabContent = null;
      }

      isInitialized = false;
      target = null;
    }
  }
}