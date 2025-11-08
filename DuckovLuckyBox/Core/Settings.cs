using System;
using System.Collections.Generic;
using UnityEngine;

namespace DuckovLuckyBox.Core.Settings
{

    public enum Category
    {
        General,
        Pricing
    }
    public enum Type
    {
        Toggle,
        Number,
        Hotkey,
        Text
    }

    public enum StorageType
    {
        Bool,
        Long,
        Float,
        String,
        Hotkey
    }

    public class SettingItem
    {
        public string Key { get; internal set; } = string.Empty;
        public string Label { get; internal set; } = string.Empty;
        public string Description { get; internal set; } = string.Empty;
        public Type Type { get; internal set; }
        public Category Category { get; internal set; }
        public StorageType StorageType { get; internal set; } = StorageType.Bool; // Default storage type
        public event System.Action<object> OnValueChanged = delegate { };

        // For Number type settings
        public float MinValue { get; internal set; } = -10000f;
        public float MaxValue { get; internal set; } = 10000f;
        public float Step { get; internal set; } = 1f;

        public object Value
        {
            set
            {
                if (!_hasValue || !IsEqual(_value, value))
                {
                    Log.Debug($"Setting '{Key}' changing value from {_value} to: {value}");

                    _value = TransformValueToType(value, StorageType);
                    _hasValue = true;
                    OnValueChanged?.Invoke(_value);
                    return;
                }

                Log.Debug($"Setting '{Key}' value unchanged. Current value: {_value}, New value: {value}");
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

        private bool IsEqual(object a, object b)
        {
            // Check for nulls
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            switch (StorageType)
            {
                case StorageType.Float:
                    // Convert both to float for comparison with epsilon to handle floating point precision
                    float fa = System.Convert.ToSingle(a);
                    float fb = System.Convert.ToSingle(b);
                    return Mathf.Approximately(fa, fb);
                case StorageType.Long:
                    long la = System.Convert.ToInt64(a);
                    long lb = System.Convert.ToInt64(b);
                    return la == lb;
                case StorageType.Bool:
                    return System.Convert.ToBoolean(a) == System.Convert.ToBoolean(b);
                case StorageType.String:
                    return System.Convert.ToString(a) == System.Convert.ToString(b);
                case StorageType.Hotkey:
                    if (a is Hotkey ha && b is Hotkey hb)
                    {
                        return ha.Key == hb.Key && ha.Ctrl == hb.Ctrl && ha.Shift == hb.Shift && ha.Alt == hb.Alt;
                    }
                    return false;
                default:
                    // Convert to the same type for comparison
                    var ta = TransformValueToType(a, StorageType);
                    var tb = TransformValueToType(b, StorageType);
                    return ta.Equals(tb);
            }
        }

        private object TransformValueToType(object value, StorageType storageType)
        {
            switch (storageType)
            {
                case StorageType.Bool:
                    return System.Convert.ToBoolean(value);
                case StorageType.Long:
                    return System.Convert.ToInt64(value);
                case StorageType.Float:
                    return System.Convert.ToSingle(value);
                case StorageType.String:
                    return System.Convert.ToString(value) ?? string.Empty;
                case StorageType.Hotkey:
                    return value; // Assuming Hotkey is already the correct type
                default:
                    return value;
            }
        }

        // Utility methods
        public void ResetToDefault()
        {
            _value = _defaultValue;
        }

        public bool IsDefault()
        {
            return EqualityComparer<object>.Default.Equals(_value, _defaultValue);
        }

        public bool GetAsBool()
        {
            if (_value is bool b)
                return b;
            if (_value is string s)
            {
                if (bool.TryParse(s, out bool result))
                    return result;
            }
            if (_value is int i)
                return i != 0;
            if (_value is long l)
                return l != 0L;
            if (_value is float f)
                return !Mathf.Approximately(f, 0f);
            if (_value is double d)
                return !Mathf.Approximately((float)d, 0f);

            throw new System.InvalidCastException($"Cannot cast setting value of type {_value.GetType()} to bool.");
        }

        public float GetAsFloat()
        {
            if (_value is float f)
                return f;
            if (_value is double d)
                return System.Convert.ToSingle(d);
            if (_value is int || _value is long)
                return System.Convert.ToSingle(_value);

            throw new System.InvalidCastException($"Cannot cast setting value of type {_value.GetType()} to float.");
        }

        public Hotkey GetAsHotkey()
        {
            if (_value is Hotkey h)
                return h;

            throw new System.InvalidCastException($"Cannot cast setting value of type {_value.GetType()} to Hotkey.");
        }

        public int GetAsInt()
        {
            if (_value is int i)
                return i;
            if (_value is long l)
                return (int)l;
            if (_value is float || _value is double)
                return System.Convert.ToInt32(_value);

            throw new System.InvalidCastException($"Cannot cast setting value of type {_value.GetType()} to int.");
        }

        public long GetAsLong()
        {
            if (_value is long l)
                return l;
            if (_value is int i)
                return i;
            if (_value is float || _value is double)
                return System.Convert.ToInt64(_value);

            throw new System.InvalidCastException($"Cannot cast setting value of type {_value.GetType()} to long.");
        }

        public string GetAsString()
        {
            if (_value is string s)
                return s;

            throw new System.InvalidCastException($"Cannot cast setting value of type {_value.GetType()} to string.");
        }

        private object _value = null!;
        private object _defaultValue = null!;
        private bool _hasValue;
    }

    /// <summary>
    /// Centralized default values for all settings
    /// </summary>
    public static class DefaultSettings
    {
        // General Settings
        public const bool EnableAnimation = true;
        public const bool EnableTripleLotteryAnimation = true;
        public const bool EnableDestroyButton = Constants.ModId != Constants.AnimationOnlyModId;
        public const bool EnableMeltButton = Constants.ModId != Constants.AnimationOnlyModId;
        public const bool EnableRefreshStock = Constants.ModId != Constants.AnimationOnlyModId;
        public const bool EnableStorePick = Constants.ModId != Constants.AnimationOnlyModId;
        public const bool EnableStreetPick = Constants.ModId != Constants.AnimationOnlyModId;
        public const bool EnableRecycle = Constants.ModId != Constants.AnimationOnlyModId;
        public const bool EnableDebug = false;
        public const bool EnableUseToCreateItemPatch = true;
        public const bool EnableUseToCreateItemWeightedLottery = true;
        public const bool EnableWeightedLottery = true;

        public const bool EnablePatchUseTime = true;
        public const float PatchedUseTime = 0.3f;

        public const bool EnableHighQualitySound = true;
        public const string HighQualitySoundFilePath = "";

        // Pricing Settings
        public const long RefreshStockPrice = 100L;
        public const long StorePickPrice = 1000L;
        public const long StreetPickPrice = 1000L;
        public const long MeltBasePrice = 100L;

        // Price Range Settings
        public const float PriceMinValue = 0f;
        public const float PriceMaxValue = 10000f;
        public const float PriceStep = 100f;
    }

    public class SettingManager
    {
        public SettingItem EnableAnimation { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableAnimation",
            Label = Localizations.I18n.SettingsEnableAnimationKey,
            Description = "DuckovLuckyBox.Settings.EnableAnimation.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableAnimation,
        };

        public SettingItem EnableTripleLotteryAnimation { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableTripleLotteryAnimation",
            Label = Localizations.I18n.SettingsEnableTripleLotteryAnimationKey,
            Description = "DuckovLuckyBox.Settings.EnableTripleLotteryAnimation.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableTripleLotteryAnimation,
        };

        public SettingItem EnableDestroyButton { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableDestroyButton",
            Label = Localizations.I18n.SettingsEnableDestroyButtonKey,
            Description = "DuckovLuckyBox.Settings.EnableDestroyButton.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableDestroyButton,
        };

        public SettingItem EnableMeltButton { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableMeltButton",
            Label = Localizations.I18n.SettingsEnableMeltButtonKey,
            Description = "DuckovLuckyBox.Settings.EnableMeltButton.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableMeltButton,
        };

        public SettingItem EnableDebug { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableDebug",
            Label = Localizations.I18n.SettingsEnableDebugKey,
            Description = "DuckovLuckyBox.Settings.EnableDebug.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableDebug,
        };

        public SettingItem EnableUseToCreateItemPatch { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableUseToCreateItemPatch",
            Label = Localizations.I18n.SettingsEnableUseToCreateItemPatchKey,
            Description = "DuckovLuckyBox.Settings.EnableUseToCreateItemPatch.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableUseToCreateItemPatch,
        };

        public SettingItem EnableUseToCreateItemWeightedLottery { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableUseToCreateItemWeightedLottery",
            Label = Localizations.I18n.SettingsEnableUseToCreateItemWeightedLotteryKey,
            Description = "DuckovLuckyBox.Settings.EnableUseToCreateItemWeightedLottery.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableUseToCreateItemWeightedLottery,
        };

        public SettingItem EnablePatchUseTime { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnablePatchUseTime",
            Label = Localizations.I18n.SettingsEnablePatchUseTimeKey,
            Description = Localizations.I18n.SettingsEnablePatchUseTimeDescriptionKey,
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnablePatchUseTime,
        };

        public SettingItem PatchedUseTime { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.PatchedUseTime",
            Label = Localizations.I18n.SettingsPatchedUseTimeKey,
            Description = Localizations.I18n.SettingsPatchedUseTimeDescriptionKey,
            Type = Type.Number,
            Category = Category.General,
            StorageType = StorageType.Float,
            DefaultValue = DefaultSettings.PatchedUseTime,
            MinValue = 0f,
            MaxValue = 6f,
            Step = 0.1f,
        };

        public SettingItem EnableWeightedLottery { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableWeightedLottery",
            Label = Localizations.I18n.SettingsEnableWeightedLotteryKey,
            Description = "DuckovLuckyBox.Settings.EnableWeightedLottery.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableWeightedLottery,
        };

        public SettingItem EnableHighQualitySound { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableHighQualitySound",
            Label = Localizations.I18n.SettingsEnableHighQualitySoundKey,
            Description = "DuckovLuckyBox.Settings.EnableHighQualitySound.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableHighQualitySound,
        };

        public SettingItem EnableRefreshStock { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableRefreshStock",
            Label = Localizations.I18n.SettingsEnableRefreshStockKey,
            Description = "DuckovLuckyBox.Settings.EnableRefreshStock.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableRefreshStock,
        };

        public SettingItem EnableStorePick { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableStorePick",
            Label = Localizations.I18n.SettingsEnableStorePickKey,
            Description = "DuckovLuckyBox.Settings.EnableStorePick.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableStorePick,
        };

        public SettingItem EnableStreetPick { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableStreetPick",
            Label = Localizations.I18n.SettingsEnableStreetPickKey,
            Description = "DuckovLuckyBox.Settings.EnableStreetPick.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableStreetPick,
        };

        public SettingItem EnableRecycle { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableRecycle",
            Label = Localizations.I18n.SettingsEnableRecycleKey,
            Description = "DuckovLuckyBox.Settings.EnableRecycle.Description",
            Type = Type.Toggle,
            Category = Category.General,
            StorageType = StorageType.Bool,
            DefaultValue = DefaultSettings.EnableRecycle,
        };

        public SettingItem HighQualitySoundFilePath { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.HighQualitySoundFilePath",
            Label = Localizations.I18n.SettingsHighQualitySoundFilePathKey,
            Description = "Custom sound file path for high-quality items (leave empty to use default)",
            Type = Type.Text,
            Category = Category.General,
            StorageType = StorageType.String,
            DefaultValue = DefaultSettings.HighQualitySoundFilePath,
        };

        public SettingItem RefreshStockPrice { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.RefreshStockPrice",
            Label = Localizations.I18n.SettingsRefreshStockPriceKey,
            Description = "DuckovLuckyBox.Settings.RefreshStockPrice.Description",
            Type = Type.Number,
            Category = Category.Pricing,
            StorageType = StorageType.Long,
            DefaultValue = DefaultSettings.RefreshStockPrice,
            MinValue = DefaultSettings.PriceMinValue,
            MaxValue = DefaultSettings.PriceMaxValue,
            Step = DefaultSettings.PriceStep,
        };

        public SettingItem StorePickPrice { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.StorePickPrice",
            Label = Localizations.I18n.SettingsStorePickPriceKey,
            Description = "DuckovLuckyBox.Settings.StorePickPrice.Description",
            Type = Type.Number,
            Category = Category.Pricing,
            StorageType = StorageType.Long,
            DefaultValue = DefaultSettings.StorePickPrice,
            MinValue = DefaultSettings.PriceMinValue,
            MaxValue = DefaultSettings.PriceMaxValue,
            Step = DefaultSettings.PriceStep,
        };

        public SettingItem StreetPickPrice { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.StreetPickPrice",
            Label = Localizations.I18n.SettingsStreetPickPriceKey,
            Description = "DuckovLuckyBox.Settings.StreetPickPrice.Description",
            Type = Type.Number,
            Category = Category.Pricing,
            StorageType = StorageType.Long,
            DefaultValue = DefaultSettings.StreetPickPrice,
            MinValue = DefaultSettings.PriceMinValue,
            MaxValue = DefaultSettings.PriceMaxValue,
            Step = DefaultSettings.PriceStep,
        };

        public SettingItem MeltBasePrice { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.MeltBasePrice",
            Label = Localizations.I18n.SettingsMeltBasePriceKey,
            Description = "DuckovLuckyBox.Settings.MeltBasePrice.Description",
            Type = Type.Number,
            Category = Category.Pricing,
            StorageType = StorageType.Long,
            DefaultValue = DefaultSettings.MeltBasePrice,
            MinValue = 0f,
            MaxValue = 10000f,
            Step = 100f,
        };

        public bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        public IEnumerable<SettingItem> AllSettings
        {
            get
            {
                yield return EnableAnimation;
                yield return EnableTripleLotteryAnimation;
                yield return EnableDestroyButton;
                yield return EnableMeltButton;
                yield return EnableDebug;
                yield return EnableUseToCreateItemPatch;
                yield return EnablePatchUseTime;
                yield return PatchedUseTime;
                yield return EnableWeightedLottery;
                yield return EnableUseToCreateItemWeightedLottery;
                yield return EnableHighQualitySound;
                yield return EnableRefreshStock;
                yield return EnableStorePick;
                yield return EnableStreetPick;
                yield return EnableRecycle;
                yield return HighQualitySoundFilePath;
                yield return RefreshStockPrice;
                yield return StorePickPrice;
                yield return StreetPickPrice;
                yield return MeltBasePrice;
            }
        }

        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        public void ResetToDefaults()
        {
            foreach (var setting in AllSettings)
            {
                setting.ResetToDefault();
            }

            Log.Info("All settings have been reset to default values.");
        }

        private static ConfigManager? _configManager;

        public static SettingManager Instance { get; } = new SettingManager();

        public static void InitializeConfig(MonoBehaviour host)
        {
            if (_configManager == null)
            {
                _configManager = new ConfigManager(host);
                _configManager.Initialize(
                  // setting the isInitialized flag after loading is complete
                  () => { Instance._isInitialized = true; }
                );
            }
        }

        public static void CleanupConfig()
        {
            _configManager?.Cleanup();
            _configManager = null;
        }
    }

}
