using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SodaCraft.Localizations;

namespace DuckovLuckyBox
{
    public class Localizations
    {
        private readonly Dictionary<SystemLanguage, Dictionary<string, string>> _localizedStrings = new Dictionary<SystemLanguage, Dictionary<string, string>> {
        { SystemLanguage.English, new Dictionary<string, string> {
            { Constants.I18n.RefreshStockKey, "Refresh" },
            { Constants.I18n.PickOneKey, "Pick One" },
            { Constants.I18n.PickOneNotificationFormatKey, "You picked one {itemDisplayName}." },
            { Constants.I18n.BuyLuckyBoxText, "Buy one Lucky Box." },
            { Constants.I18n.InventoryFullAndSendToStorageKey, "Inventory full, sending to storage." }
        } },
        { SystemLanguage.ChineseSimplified, new Dictionary<string, string> {
            { Constants.I18n.RefreshStockKey, "刷新" },
            { Constants.I18n.PickOneKey, "商人那拾一个" },
            { Constants.I18n.PickOneNotificationFormatKey, "俺拾到了 {itemDisplayName}" },
            { Constants.I18n.BuyLuckyBoxText, "路边拾一个" },
            { Constants.I18n.InventoryFullAndSendToStorageKey, "俺拾不动嘞，邮回仓库嘞。" }
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