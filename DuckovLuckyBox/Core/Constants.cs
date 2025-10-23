namespace DuckovLuckyBox.Core
{
    public static class Constants
    {
        public static readonly string ModId = "Duckov.LuckyBox";
        public static readonly string ModName = "Duckov Lucky Box";

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
        }

        public static class Sound {
            public static readonly string RollingSoundResourceName = "Resources.rolling.mp3";
            public static FMOD.Sound? ROLLING_SOUND { get; private set; }

            public static void LoadSounds()
            {
                ROLLING_SOUND = Utils.CreateSound(RollingSoundResourceName);
            }
        }
    }
}