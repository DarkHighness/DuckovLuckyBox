using Duckov.Options.UI;
using DuckovLuckyBox.Core.Settings;
using HarmonyLib;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

namespace DuckovLuckyBox.UI.Component
{

    // The dropdown component for settings UI read and write values
    // from the OptionsManager, which does not meet our needs.
    // Therefore, we create a new class to override the necessary methods.
    public class ToggleComponent : MonoBehaviour
    {
        private SettingItem? item = null;

        private TextMeshProUGUI? label = null;
        private Slider? slider = null;
        private TMP_InputField? inputField = null;

        public bool Setup(SettingItem item, OptionsUIEntry_Slider baseComponent)
        {
            this.item = item;

            label = AccessTools.Field(typeof(OptionsUIEntry_Slider), "label").GetValue(baseComponent) as TextMeshProUGUI;
            slider = AccessTools.Field(typeof(OptionsUIEntry_Slider), "slider").GetValue(baseComponent) as Slider;
            inputField = AccessTools.Field(typeof(OptionsUIEntry_Slider), "valueField").GetValue(baseComponent) as TMP_InputField;

            if (label == null || slider == null)
            {
                return false;
            }

            // Hide the input field for toggle
            inputField?.gameObject.SetActive(false);

            slider.wholeNumbers = true;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.onValueChanged.AddListener(OnToggleValueChanged);

            LocalizationManager.OnSetLanguage += OnLanguageChanged;

            RefreshLabels();
            RefreshValues();

            return true;
        }

        private void OnDestroy()
        {
            slider?.onValueChanged.RemoveListener(OnToggleValueChanged);

            LocalizationManager.OnSetLanguage -= OnLanguageChanged;
        }

        private void OnToggleValueChanged(float value)
        {
            if (item != null)
            {
                bool boolValue = Mathf.Approximately(value, 1f);
                item.Value = boolValue;
                RefreshValues();
            }
        }

        private void RefreshLabels()
        {
            if (item != null && label != null)
            {
                label.text = item.Label.ToPlainText();
            }
        }

        private void RefreshValues()
        {
            if (item != null && slider != null)
            {
                bool boolValue = item.GetAsBool();
                slider.SetValueWithoutNotify(boolValue ? 1f : 0f);
            }
        }

        private void OnLanguageChanged(SystemLanguage language)
        {
            RefreshLabels();
        }
    }
}