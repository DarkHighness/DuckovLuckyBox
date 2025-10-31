using System;
using System.Collections.Generic;
using System.Linq;
using Duckov.Options.UI;
using TMPro;
using UnityEngine;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using HarmonyLib;

namespace DuckovLuckyBox.UI
{
    public class Dropdown : MonoBehaviour
    {
        private SettingItem? settingItem;
        private SortedDictionary<string, object> options = new SortedDictionary<string, object>();
        private UnityEngine.UI.Dropdown? unityDropdown;
        private bool initDone = false;

        public void Init(SettingItem settingItem, string description, SortedDictionary<string, object> options)
        {
            if (settingItem == null)
            {
                Log.Error("SettingItem cannot be null");
                return;
            }

            if (options == null || options.Count == 0)
            {
                Log.Error("Dropdown options cannot be null or empty");
                return;
            }

            this.settingItem = settingItem;
            this.options = options;

            // Try to get Unity Dropdown component directly
            unityDropdown = GetComponent<UnityEngine.UI.Dropdown>();
            if (unityDropdown == null)
            {
                // Fallback to reflection if direct get fails
                try
                {
                    var dropdownField = AccessTools.Field(typeof(OptionsUIEntry_Dropdown), "dropdown");
                    unityDropdown = (UnityEngine.UI.Dropdown)dropdownField.GetValue(this);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to get dropdown field: {ex.Message}");
                    return;
                }
            }

            if (unityDropdown == null)
            {
                Log.Error("Failed to get Unity Dropdown component");
                return;
            }

            // Setup dropdown options
            SetupDropdownOptions();

            // Set current value
            SetDropdownValue(settingItem.Value);

            // Add event listener
            unityDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

            // Subscribe to SettingItem's OnValueChanged event
            settingItem.OnValueChanged += OnSettingValueChanged;

            // Set label
            SetLabel(description);

            initDone = true;
            Log.Info($"Dropdown initialized: {description}, options count: {options.Count}");
        }

        private void SetupDropdownOptions()
        {
            if (unityDropdown == null) return;

            unityDropdown.ClearOptions();
            var optionDataList = new List<UnityEngine.UI.Dropdown.OptionData>();
            foreach (var option in options)
            {
                optionDataList.Add(new UnityEngine.UI.Dropdown.OptionData(option.Key));
            }
            unityDropdown.AddOptions(optionDataList);
        }

        private void OnDropdownValueChanged(int index)
        {
            if (!initDone || settingItem == null || index < 0 || index >= options.Count)
                return;

            try
            {
                var selectedValue = options.Values.ElementAt(index);
                settingItem.Value = selectedValue;
                Log.Debug($"Dropdown value changed: {settingItem.Key} = {selectedValue}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error setting dropdown value: {ex.Message}");
            }
        }

        private void OnSettingValueChanged(object value)
        {
            if (!initDone)
                return;

            SetDropdownValue(value);
        }

        private void SetDropdownValue(object value)
        {
            if (unityDropdown == null) return;

            int selectedIndex = 0;
            var values = options.Values.ToList();
            for (int i = 0; i < values.Count; i++)
            {
                if (Equals(values[i], value))
                {
                    selectedIndex = i;
                    break;
                }
            }

            unityDropdown.SetValueWithoutNotify(selectedIndex);
        }

        private void SetLabel(string description)
        {
            // Try to get label directly
            var label = GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.SetText(description);
                return;
            }

            // Fallback to reflection
            try
            {
                var labelField = AccessTools.Field(typeof(OptionsUIEntry_Dropdown), "label");
                label = (TextMeshProUGUI)labelField.GetValue(this);
                if (label != null)
                {
                    label.SetText(description);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to set label: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (settingItem != null)
            {
                settingItem.OnValueChanged -= OnSettingValueChanged;
            }

            if (unityDropdown != null)
            {
                unityDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }
        }
    }
}