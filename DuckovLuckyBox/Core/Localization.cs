using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SodaCraft.Localizations;

namespace DuckovLuckyBox.Core
{
    public class Localizations
    {
        private readonly Dictionary<SystemLanguage, Dictionary<string, string>> _localizedStrings = new Dictionary<SystemLanguage, Dictionary<string, string>> {
        { SystemLanguage.English, new Dictionary<string, string> {
            { Constants.I18n.RefreshStockKey, "Refresh" },
            { Constants.I18n.StorePickKey, "Pick One" },
            { Constants.I18n.PickNotificationFormatKey, "You picked one {itemDisplayName}." },
            { Constants.I18n.StreetPickKey, "Buy one Lucky Box." },
            { Constants.I18n.InventoryFullAndSendToStorageKey, "Inventory full, sending to storage." },
            { Constants.I18n.SettingsPanelTitleKey, "DuckvoLuckyBox SETTINGS" },
            { Constants.I18n.SettingsCategoryGeneralKey, "General" },
            { Constants.I18n.SettingsEnableAnimationKey, "Enable Animation" }
        } },
        { SystemLanguage.ChineseSimplified, new Dictionary<string, string> {
            { Constants.I18n.RefreshStockKey, "刷新" },
            { Constants.I18n.StorePickKey, "商人那拾一个" },
            { Constants.I18n.PickNotificationFormatKey, "俺拾到了 {itemDisplayName}" },
            { Constants.I18n.StreetPickKey, "路边拾一个" },
            { Constants.I18n.InventoryFullAndSendToStorageKey, "俺拾不动嘞，邮回仓库嘞。" },
            { Constants.I18n.SettingsPanelTitleKey, "幸运“方块”设置" },
            { Constants.I18n.SettingsCategoryGeneralKey, "常规" },
            { Constants.I18n.SettingsEnableAnimationKey, "启用动画" }
        } },
    };

        public static Localizations Instance { get; } = new Localizations();

        private void OnSetLanguage(SystemLanguage language)
        {
            if (!_localizedStrings.ContainsKey(language))
            {
                Log.Warning($"Unsupported language '{language}', defaulting to English.");
                language = SystemLanguage.English;
            }

            foreach (var pair in _localizedStrings[language])
            {
                LocalizationManager.SetOverrideText(pair.Key, pair.Value);
            }
        }

        private void RemoveOverrides()
        {
            foreach (var key in _localizedStrings.Values.SelectMany(dict => dict.Keys))
            {
                LocalizationManager.RemoveOverrideText(key);
            }
        }

        public void Initialize()
        {
            LocalizationManager.OnSetLanguage += OnSetLanguage;
            OnSetLanguage(LocalizationManager.CurrentLanguage);
        }

        public void Destroy()
        {
            LocalizationManager.OnSetLanguage -= OnSetLanguage;
            RemoveOverrides();
        }
    }

}