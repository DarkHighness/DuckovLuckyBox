using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovLuckyBox.Core.Settings
{

  public enum Category
  {
    General
  }
  public enum Type
  {
    Toggle
  }

  public class SettingItem
  {
    public string Key { get; internal set; } = string.Empty;
    public string Label { get; internal set; } = string.Empty;
    public string Description { get; internal set; } = string.Empty;
    public Type Type { get; internal set; }
    public Category Category { get; internal set; }
    public System.Action<object> OnValueChanged = delegate { };

    public object Value
    {
      get => _value;
      set
      {
        if (!_hasValue || !EqualityComparer<object>.Default.Equals(_value, value))
        {
          _value = value;
          _hasValue = true;
          OnValueChanged?.Invoke(_value);
        }
      }
    }

    public object DefaultValue
    {
      get => _defaultValue;
      internal set
      {
        _defaultValue = value;

        // Ensure instances expose a usable value before the first explicit assignment.
        if (!_hasValue)
        {
          _value = value;
          _hasValue = true;
        }
      }
    }

    private object _value = null!;
    private object _defaultValue = null!;
    private bool _hasValue;
  }

  public class Settings
  {
    public SettingItem EnableAnimation { get; set; } = new SettingItem
    {
      Key = "DuckovLuckyBox.Settings.EnableAnimation",
      Label = Constants.I18n.SettingsEnableAnimationKey,
      Description = "DuckovLuckyBox.Settings.EnableAnimation.Description",
      Type = Type.Toggle,
      Category = Category.General,
      DefaultValue = true,
    };

    public IEnumerable<SettingItem> AllSettings
    {
      get
      {
        yield return EnableAnimation;
      }
    }

    public static Settings Instance { get; } = new Settings();
  }

}