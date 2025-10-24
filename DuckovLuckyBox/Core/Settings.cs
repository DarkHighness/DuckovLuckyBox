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
        Hotkey
    }

    public class SettingItem
    {
        public string Key { get; internal set; } = string.Empty;
        public string Label { get; internal set; } = string.Empty;
        public string Description { get; internal set; } = string.Empty;
        public Type Type { get; internal set; }
        public Category Category { get; internal set; }
        public System.Action<object> OnValueChanged { get; set; } = delegate { };

        // For Number type settings
        public float MinValue { get; internal set; } = 0f;
        public float MaxValue { get; internal set; } = 100f;
        public float Step { get; internal set; } = 1f;

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

    /// <summary>
    /// Centralized default values for all settings
    /// </summary>
    public static class DefaultSettings
    {
        // General Settings
        public const bool EnableAnimation = true;
        public static readonly Hotkey SettingsHotkey = new Hotkey(KeyCode.F1, false, false, false);
        public const bool EnableDestroyButton = true;
        public const bool EnableLotteryButton = true;
        public const bool EnableDebug = false;

        // Pricing Settings
        public const long RefreshStockPrice = 100L;
        public const long StorePickPrice = 100L;
        public const long StreetPickPrice = 100L;

        // Price Range Settings
        public const float PriceMinValue = 0f;
        public const float PriceMaxValue = 5000f;
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
            DefaultValue = DefaultSettings.EnableAnimation,
        };

        public SettingItem SettingsHotkey { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.SettingsHotkey",
            Label = Localizations.I18n.SettingsHotkeyKey,
            Description = "DuckovLuckyBox.Settings.SettingsHotkey.Description",
            Type = Type.Hotkey,
            Category = Category.General,
            DefaultValue = DefaultSettings.SettingsHotkey,
        };

        public SettingItem EnableDestroyButton { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableDestroyButton",
            Label = Localizations.I18n.SettingsEnableDestroyButtonKey,
            Description = "DuckovLuckyBox.Settings.EnableDestroyButton.Description",
            Type = Type.Toggle,
            Category = Category.General,
            DefaultValue = DefaultSettings.EnableDestroyButton,
        };

        public SettingItem EnableLotteryButton { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableLotteryButton",
            Label = Localizations.I18n.SettingsEnableLotteryButtonKey,
            Description = "DuckovLuckyBox.Settings.EnableLotteryButton.Description",
            Type = Type.Toggle,
            Category = Category.General,
            DefaultValue = DefaultSettings.EnableLotteryButton,
        };

        public SettingItem EnableDebug { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.EnableDebug",
            Label = Localizations.I18n.SettingsEnableDebugKey,
            Description = "DuckovLuckyBox.Settings.EnableDebug.Description",
            Type = Type.Toggle,
            Category = Category.General,
            DefaultValue = DefaultSettings.EnableDebug,
        };

        public SettingItem RefreshStockPrice { get; set; } = new SettingItem
        {
            Key = "DuckovLuckyBox.Settings.RefreshStockPrice",
            Label = Localizations.I18n.SettingsRefreshStockPriceKey,
            Description = "DuckovLuckyBox.Settings.RefreshStockPrice.Description",
            Type = Type.Number,
            Category = Category.Pricing,
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
            DefaultValue = DefaultSettings.StreetPickPrice,
            MinValue = DefaultSettings.PriceMinValue,
            MaxValue = DefaultSettings.PriceMaxValue,
            Step = DefaultSettings.PriceStep,
        };

        public IEnumerable<SettingItem> AllSettings
        {
            get
            {
                yield return EnableAnimation;
                yield return SettingsHotkey;
                yield return EnableDestroyButton;
                yield return EnableLotteryButton;
                yield return EnableDebug;
                yield return RefreshStockPrice;
                yield return StorePickPrice;
                yield return StreetPickPrice;
            }
        }

        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        public void ResetToDefaults()
        {
            EnableAnimation.Value = EnableAnimation.DefaultValue;
            SettingsHotkey.Value = SettingsHotkey.DefaultValue;
            EnableDestroyButton.Value = EnableDestroyButton.DefaultValue;
            EnableLotteryButton.Value = EnableLotteryButton.DefaultValue;
            EnableDebug.Value = EnableDebug.DefaultValue;
            RefreshStockPrice.Value = RefreshStockPrice.DefaultValue;
            StorePickPrice.Value = StorePickPrice.DefaultValue;
            StreetPickPrice.Value = StreetPickPrice.DefaultValue;

            Log.Info("All settings have been reset to default values.");
        }

        private static ConfigManager? _configManager;

        public static SettingManager Instance { get; } = new SettingManager();

        public static void InitializeConfig(MonoBehaviour host)
        {
            if (_configManager == null)
            {
                _configManager = new ConfigManager(host);
                _configManager.Initialize();
            }
        }

        public static void CleanupConfig()
        {
            _configManager?.Cleanup();
            _configManager = null;
        }
    }

}
