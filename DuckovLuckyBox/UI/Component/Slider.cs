using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;

namespace DuckovLuckyBox.UI
{
    public class Slider : MonoBehaviour
    {
        public object Value
        {
            get
            {
                if (settingItem == null)
                {
                    Log.Error("SettingItem is null");
                    return "";
                }

                try
                {
                    return settingItem.Value;
                }
                catch (Exception ex)
                {
                    Log.Error($"Error getting config value: {ex.Message} key={settingItem.Key}");
                    return settingItem.DefaultValue ?? "";
                }
            }
            set
            {
                if (settingItem == null)
                {
                    Log.Error("SettingItem is null");
                    return;
                }

                try
                {
                    settingItem.Value = value;
                    Log.Debug("Setting config value");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error saving config value: {ex.Message}");
                }
            }
        }

        private void Awake()
        {
            if (this.slider != null)
                this.slider.onValueChanged.AddListener(new UnityAction<float>(this.OnSliderValueChanged));

            if (this.valueField != null)
                this.valueField.onEndEdit.AddListener(new UnityAction<string>(this.OnFieldEndEdit));
        }

        private void Start()
        {
            this.RefreshValues();
        }

        private void OnSettingValueChanged(object value)
        {
            if (!initDone)
                return;

            RefreshValues();
        }

        private void OnDestroy()
        {
            if (settingItem != null)
            {
                settingItem.OnValueChanged -= OnSettingValueChanged;
            }
        }

        private void OnFieldEndEdit(string arg0)
        {
            if (!initDone || string.IsNullOrEmpty(arg0) || settingItem == null)
                return;

            try
            {
                object? parsedValue = ParseValue(arg0, settingItem.Type);
                if (parsedValue != null)
                {
                    this.Value = parsedValue;
                }
                else
                {
                    Log.Warning("Invalid input value");
                    this.RefreshValues();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing input field value: {ex.Message}");
                this.RefreshValues();
            }

            this.RefreshValues();
        }

        private void OnEnable()
        {
            if (!initDone)
                return;

            this.RefreshValues();
        }

        private void OnSliderValueChanged(float value)
        {
            if (!initDone)
                return;

            this.Value = value;
            this.RefreshValues();
        }

        private void RefreshValues()
        {
            if (!initDone || settingItem == null)
                return;

            try
            {
                object currentValue = this.Value;
                UpdateUI(currentValue, settingItem.Type);
            }
            catch (Exception ex)
            {
                Log.Error($"Error refreshing UI values: {ex.Message}");
            }
        }

        private object? ParseValue(string input, DuckovLuckyBox.Core.Settings.Type valueType)
        {
            switch (valueType)
            {
                case DuckovLuckyBox.Core.Settings.Type.Number:
                    if (long.TryParse(input, out long longValue))
                        return longValue;
                    if (int.TryParse(input, out int intValue))
                        return intValue;
                    if (float.TryParse(input, out float floatValue))
                        return floatValue;
                    break;
                case DuckovLuckyBox.Core.Settings.Type.Text:
                    return input;
                case DuckovLuckyBox.Core.Settings.Type.Toggle:
                    if (bool.TryParse(input, out bool boolValue))
                        return boolValue;
                    string lower = input.ToLower();
                    if (lower == "1" || lower == "yes" || lower == "on")
                        return true;
                    if (lower == "0" || lower == "no" || lower == "off")
                        return false;
                    break;
                case DuckovLuckyBox.Core.Settings.Type.Hotkey:
                    return input;
            }
            return null;
        }

        private void UpdateUI(object value, DuckovLuckyBox.Core.Settings.Type valueType)
        {
            switch (valueType)
            {
                case DuckovLuckyBox.Core.Settings.Type.Number:
                    if (value is long longVal)
                    {
                        valueField?.SetTextWithoutNotify(longVal.ToString());
                        slider?.SetValueWithoutNotify(longVal);
                    }
                    else if (value is int intVal)
                    {
                        valueField?.SetTextWithoutNotify(intVal.ToString());
                        slider?.SetValueWithoutNotify(intVal);
                    }
                    else if (value is float floatVal)
                    {
                        valueField?.SetTextWithoutNotify(floatVal.ToString("F2"));
                        slider?.SetValueWithoutNotify(floatVal);
                    }
                    break;
                case DuckovLuckyBox.Core.Settings.Type.Text:
                    valueField?.SetTextWithoutNotify(value?.ToString() ?? "");
                    slider?.gameObject.SetActive(false);
                    break;
                case DuckovLuckyBox.Core.Settings.Type.Toggle:
                    bool boolVal = Convert.ToBoolean(value);
                    valueField?.SetTextWithoutNotify(boolVal ? "True" : "False");
                    slider?.gameObject.SetActive(false);
                    break;
                case DuckovLuckyBox.Core.Settings.Type.Hotkey:
                    valueField?.SetTextWithoutNotify(value?.ToString() ?? "");
                    slider?.gameObject.SetActive(false);
                    break;
            }
        }

        private bool initDone = false;

        [Space]
        public TextMeshProUGUI? label;

        public UnityEngine.UI.Slider? slider;

        public TMP_InputField? valueField;

        private DuckovLuckyBox.Core.Settings.Type valueType = DuckovLuckyBox.Core.Settings.Type.Text;
        private Vector2? sliderRange = null;

        private SettingItem? settingItem;

        public void Init(SettingItem settingItem, string description, Vector2? sliderRange = null)
        {
            if (settingItem == null)
            {
                Log.Error("SettingItem cannot be null");
                return;
            }

            if (this.valueField == null)
            {
                Log.Error("valueField is null!!");
                return;
            }
            else
            {
                this.valueField.contentType = TMP_InputField.ContentType.Standard;
                this.valueField.characterLimit = 1000;
            }

            if (this.slider == null)
            {
                Log.Error("slider is null!!");
                return;
            }

            this.settingItem = settingItem;
            this.valueType = settingItem.Type;
            this.sliderRange = sliderRange;

            if (this.label != null)
                this.label.SetText(description);

            // Set slider range and visibility
            if (sliderRange.HasValue)
            {
                this.slider.minValue = sliderRange.Value.x;
                this.slider.maxValue = sliderRange.Value.y;

                // Set integer mode based on type
                if (settingItem.Type == DuckovLuckyBox.Core.Settings.Type.Number)
                {
                    var defaultVal = settingItem.DefaultValue;
                    if (defaultVal is int || defaultVal is long)
                    {
                        this.slider.wholeNumbers = true;
                    }
                    else
                    {
                        this.slider.wholeNumbers = false;
                    }
                }
                else
                {
                    this.slider.wholeNumbers = false;
                }

                this.slider.gameObject.SetActive(true);

                // Validate if default value is within range
                try
                {
                    var defaultV = Convert.ToSingle(settingItem.DefaultValue);
                    if (defaultV < sliderRange.Value.x || defaultV > sliderRange.Value.y)
                    {
                        Log.Error($"Default value {defaultV} for config item {settingItem.Key} exceeds the set range [{sliderRange.Value.x}, {sliderRange.Value.y}]");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error validating default value range: {ex.Message}");
                }
            }
            else
            {
                // When no range is provided, hide slider
                // Also remove numerical range restrictions
                if (settingItem.Type == DuckovLuckyBox.Core.Settings.Type.Number)
                {
                    var defaultVal = settingItem.DefaultValue;
                    if (defaultVal is int || defaultVal is long)
                    {
                        this.slider.minValue = long.MinValue;
                        this.slider.maxValue = long.MaxValue;
                        this.slider.wholeNumbers = true;
                    }
                    else
                    {
                        this.slider.minValue = -100000f;
                        this.slider.maxValue = 100000f;
                        this.slider.wholeNumbers = false;
                    }
                }

                this.slider.gameObject.SetActive(true);
                Log.Info($"Slider range restrictions removed - Key: {settingItem.Key}, new range: [{this.slider.minValue}, {this.slider.maxValue}]");
            }

            // Subscribe to SettingItem's OnValueChanged event
            settingItem.OnValueChanged += OnSettingValueChanged;

            initDone = true;

            // Initialize display
            RefreshValues();
        }
    }
}