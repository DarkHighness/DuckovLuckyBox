using System;
using Duckov.Options.UI;
using Duckov.UI;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using HarmonyLib;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovLuckyBox.UI.Component
{
  // The slider component for settings UI read and write values
  // from the OptionsManager, which does not meet our needs.
  // Therefore, we create a new class to override the necessary methods.
  public class SliderComponent : MonoBehaviour
  {
    private SettingItem? item = null;

    private TextMeshProUGUI? label = null;
    private Slider? slider = null;
    private TMP_InputField? inputField = null;
    private bool isIntegral = false;

    public bool Setup(SettingItem item, OptionsUIEntry_Slider baseComponent)
    {
      this.item = item;

      label = AccessTools.Field(typeof(OptionsUIEntry_Slider), "label").GetValue(baseComponent) as TextMeshProUGUI;
      slider = AccessTools.Field(typeof(OptionsUIEntry_Slider), "slider").GetValue(baseComponent) as Slider;
      inputField = AccessTools.Field(typeof(OptionsUIEntry_Slider), "valueField").GetValue(baseComponent) as TMP_InputField;

      if (label == null || slider == null || inputField == null)
      {
        return false;
      }

      slider.minValue = item.MinValue;
      slider.maxValue = item.MaxValue;

      isIntegral = Mathf.RoundToInt(item.Step) == item.Step;
      slider.wholeNumbers = isIntegral;

      slider.onValueChanged.AddListener(OnSliderValueChanged);
      inputField.onEndEdit.AddListener(OnInputFieldEndEdit);

      LocalizationManager.OnSetLanguage += OnLanguageChanged;

      RefreshLabels();
      RefreshValues();

      return true;
    }

    private void OnDestroy()
    {
      slider?.onValueChanged.RemoveListener(OnSliderValueChanged);

      inputField?.onEndEdit.RemoveListener(OnInputFieldEndEdit);
      LocalizationManager.OnSetLanguage -= OnLanguageChanged;
    }

    private void OnLanguageChanged(SystemLanguage language)
    {
      RefreshLabels();
    }

    private float ToSteppedValue(float value)
    {
      if (item != null)
      {
        float steppedValue = Mathf.Round((value - item.MinValue) / item.Step) * item.Step + item.MinValue;
        return Mathf.Clamp(steppedValue, item.MinValue, item.MaxValue);
      }
      return value;
    }

    private void OnSliderValueChanged(float value)
    {
      if (item != null)
      {
        float steppedValue = ToSteppedValue(value);
        item.Value = steppedValue;
        RefreshValues();
      }
    }

    private void OnInputFieldEndEdit(string text)
    {
      if (item != null)
      {
        if (float.TryParse(text, out float value))
        {
          float steppedValue = ToSteppedValue(value);
          item.Value = steppedValue;
          RefreshValues();
        }
        else
        {
          NotificationText.Push(Localizations.I18n.InvalidNumberInputKey.ToPlainText());
          // Revert to the current value
          RefreshValues();
        }
      }
    }


    private void RefreshLabels()
    {
      if (label != null && item != null)
      {
        label.text = item.Label.ToPlainText();
      }
    }

    private void RefreshValues()
    {
      if (item != null && slider != null && inputField != null)
      {
        float steppedValue = ToSteppedValue(item.GetAsFloat());
        slider.SetValueWithoutNotify(steppedValue);

        string format = isIntegral ? "F0" : "F2";
        inputField.SetTextWithoutNotify(steppedValue.ToString(format));
      }
    }
  }
}