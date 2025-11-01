using Duckov.Options.UI;
using DuckovLuckyBox.Core.Settings;
using HarmonyLib;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovLuckyBox.UI.Component
{
  public class InputComponent : MonoBehaviour
  {
    private SettingItem? item = null;

    private TextMeshProUGUI? label = null;
    private TMP_InputField? inputField = null;

    public bool Setup(SettingItem item, OptionsUIEntry_Slider baseComponent)
    {
      this.item = item;

      label = AccessTools.Field(typeof(OptionsUIEntry_Slider), "label").GetValue(baseComponent) as TextMeshProUGUI;
      inputField = AccessTools.Field(typeof(OptionsUIEntry_Slider), "valueField").GetValue(baseComponent) as TMP_InputField;

      if (label == null || inputField == null)
      {
        return false;
      }

      // Remove the slider component for input
      var slider = AccessTools.Field(typeof(OptionsUIEntry_Slider), "slider").GetValue(baseComponent) as Slider;
      if (slider != null)
      {
        Destroy(slider.gameObject);
      }

      // Adjust the input field to take the full width
      var inputRect = inputField.GetComponent<RectTransform>();
      inputRect.anchorMin = new Vector2(0, 0);
      inputRect.anchorMax = new Vector2(1, 1);
      inputRect.offsetMin = new Vector2(0, 0);
      inputRect.offsetMax = new Vector2(0, 0);
      // Adjust the width
      inputRect.sizeDelta = new Vector2(0, inputRect.sizeDelta.y);

      // Adjust LayoutElement to allow full width expansion
      var layoutElement = inputField.GetComponent<LayoutElement>();
      if (layoutElement != null)
      {
        layoutElement.preferredWidth = -1;
        layoutElement.flexibleWidth = 1;
      }

      // Adjust the input field format
      inputField.contentType = TMP_InputField.ContentType.Standard;
      inputField.lineType = TMP_InputField.LineType.SingleLine;
      inputField.characterValidation = TMP_InputField.CharacterValidation.None;
      inputField.characterLimit = 0;
      inputField.readOnly = false;

      inputField.onEndEdit.AddListener(OnInputFieldEndEdit);

      LocalizationManager.OnSetLanguage += OnLanguageChanged;

      RefreshLabels();
      RefreshValues();

      return true;
    }

    private void OnDestroy()
    {
      inputField?.onEndEdit.RemoveListener(OnInputFieldEndEdit);
      LocalizationManager.OnSetLanguage -= OnLanguageChanged;
    }

    private void OnInputFieldEndEdit(string value)
    {
      if (item == null)
      {
        return;
      }

      item.Value = value;
      RefreshValues();
    }

    private void OnLanguageChanged(SystemLanguage language)
    {
      RefreshLabels();
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
      if (item != null && inputField != null)
      {
        inputField.SetTextWithoutNotify(item.GetAsString() ?? string.Empty);
      }
    }
  }
}