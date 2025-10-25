using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SodaCraft.Localizations;

namespace DuckovLuckyBox.Core
{
    public class Localizations
    {
        public static class I18n
        {
            public static readonly string RefreshStockKey = "UI_RefreshStock";
            public static readonly string StorePickKey = "UI_StorePick";
            public static readonly string StreetPickKey = "UI_StreetPick";
            public static readonly string PickNotificationFormatKey = "Notification_PickOneFormat";
            public static readonly string InventoryFullAndSendToStorageKey = "Notification_InventoryFullAndSendToStorage";
            public static readonly string NotEnoughMoneyFormatKey = "Notification_NotEnoughMoneyFormat";

            // Settings UI I18n keys
            public static readonly string SettingsPanelTitleKey = "UI_SettingsPanelTitle";
            public static readonly string SettingsCategoryGeneralKey = "UI_SettingsCategoryGeneral";
            public static readonly string SettingsCategoryPricingKey = "UI_SettingsCategoryPricing";
            public static readonly string SettingsEnableAnimationKey = "UI_SettingsEnableAnimation";
            public static readonly string SettingsHotkeyKey = "UI_SettingsHotkey";
            public static readonly string SettingsPressAnyKeyKey = "UI_SettingsPressAnyKey";
            public static readonly string SettingsRefreshStockPriceKey = "UI_SettingsRefreshStockPrice";
            public static readonly string SettingsStorePickPriceKey = "UI_SettingsStorePickPrice";
            public static readonly string SettingsStreetPickPriceKey = "UI_SettingsStreetPickPrice";
            public static readonly string SettingsResetToDefaultKey = "UI_SettingsResetToDefault";
            public static readonly string FreeKey = "UI_Free";
            public static readonly string SettingsEnableDestroyButtonKey = "UI_SettingsEnableDestroyButton";
            public static readonly string SettingsEnableLotteryButtonKey = "UI_SettingsEnableLotteryButton";
            public static readonly string SettingsEnableDebugKey = "UI_SettingsEnableDebug";
            public static readonly string SettingsEnableUseToCreateItemPatchKey = "UI_SettingsEnableUseToCreateItemPatch";

            // Item Operation Menu I18n keys
            public static readonly string ItemMenuDestroyKey = "UI_ItemMenuDestroy";
            public static readonly string ItemMenuLotteryKey = "UI_ItemMenuLottery";
            public static readonly string LotteryResultFormatKey = "Notification_LotteryResultFormat";
        }

        private readonly Dictionary<SystemLanguage, Dictionary<string, string>> _localizedStrings = new Dictionary<SystemLanguage, Dictionary<string, string>> {
        { SystemLanguage.English, new Dictionary<string, string> {
            { I18n.RefreshStockKey, "Refresh" },
            { I18n.StorePickKey, "Pick One" },
            { I18n.PickNotificationFormatKey, "You picked one {itemDisplayName}." },
            { I18n.StreetPickKey, "Buy one Lucky Box." },
            { I18n.InventoryFullAndSendToStorageKey, "Inventory full, sending to storage." },
            { I18n.NotEnoughMoneyFormatKey, "Not enough money! Need {price} coins." },
            { I18n.SettingsPanelTitleKey, "DuckvoLuckyBox SETTINGS" },
            { I18n.SettingsCategoryGeneralKey, "General" },
            { I18n.SettingsCategoryPricingKey, "Pricing" },
            { I18n.SettingsEnableAnimationKey, "Enable Animation" },
            { I18n.SettingsHotkeyKey, "Settings Hotkey" },
            { I18n.SettingsPressAnyKeyKey, "Press any key..." },
            { I18n.SettingsRefreshStockPriceKey, "Refresh Stock Price" },
            { I18n.SettingsStorePickPriceKey, "Store Pick Price" },
            { I18n.SettingsStreetPickPriceKey, "Street Pick Price" },
            { I18n.SettingsResetToDefaultKey, "Reset to Default" },
            { I18n.FreeKey, "Free!" },
            { I18n.SettingsEnableDestroyButtonKey, "Enable Destroy Button" },
            { I18n.SettingsEnableLotteryButtonKey, "Enable Lottery Button" },
            { I18n.SettingsEnableDebugKey, "Enable Debug Mode" },
            { I18n.SettingsEnableUseToCreateItemPatchKey, "Enable In—Game Lottery Patch" },
            { I18n.ItemMenuDestroyKey, "Destroy" },
            { I18n.ItemMenuLotteryKey, "Lottery" },
            { I18n.LotteryResultFormatKey, "You got {itemDisplayName}!" }
        } },
        { SystemLanguage.ChineseSimplified, new Dictionary<string, string> {
            { I18n.RefreshStockKey, "刷新" },
            { I18n.StorePickKey, "商人那拾一个" },
            { I18n.PickNotificationFormatKey, "俺拾到了 {itemDisplayName}" },
            { I18n.StreetPickKey, "路边拾一个" },
            { I18n.InventoryFullAndSendToStorageKey, "俺拾不动嘞，邮回仓库嘞。" },
            { I18n.NotEnoughMoneyFormatKey, "钱不够嘞！需要 {price} 个铜板。" },
            { I18n.SettingsPanelTitleKey, "幸运\"方块\"设置" },
            { I18n.SettingsCategoryGeneralKey, "常规" },
            { I18n.SettingsCategoryPricingKey, "价格" },
            { I18n.SettingsEnableAnimationKey, "启用动画" },
            { I18n.SettingsHotkeyKey, "设置快捷键" },
            { I18n.SettingsPressAnyKeyKey, "按任意键..." },
            { I18n.SettingsRefreshStockPriceKey, "刷新库存价格" },
            { I18n.SettingsStorePickPriceKey, "商人抽奖价格" },
            { I18n.SettingsStreetPickPriceKey, "街边抽奖价格" },
            { I18n.SettingsResetToDefaultKey, "恢复默认值" },
            { I18n.FreeKey, "免费！" },
            { I18n.SettingsEnableDestroyButtonKey, "启用销毁按钮" },
            { I18n.SettingsEnableLotteryButtonKey, "启用抽奖按钮" },
            { I18n.SettingsEnableDebugKey, "启用调试模式" },
            { I18n.SettingsEnableUseToCreateItemPatchKey, "启用游戏内抽奖补丁" },
            { I18n.ItemMenuDestroyKey, "销毁" },
            { I18n.ItemMenuLotteryKey, "抽奖" },
            { I18n.LotteryResultFormatKey, "你抽中了 {itemDisplayName}！" }
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
