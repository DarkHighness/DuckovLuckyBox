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
            public static readonly string ModNameKey = "ModName_DuckovLuckyBox";
            public static readonly string RefreshStockKey = "UI_RefreshStock";
            public static readonly string StorePickKey = "UI_StorePick";
            public static readonly string StreetPickKey = "UI_StreetPick";
            public static readonly string RecycleKey = "UI_Recycle";
            public static readonly string ConfirmKey = "UI_Confirm";
            public static readonly string ClearKey = "UI_Clear";
            public static readonly string OpenKey = "UI_Open";
            public static readonly string CloseKey = "UI_Close";
            public static readonly string PickNotificationFormatKey = "Notification_PickOneFormat";
            public static readonly string InventoryFullAndSendToStorageKey = "Notification_InventoryFullAndSendToStorage";
            public static readonly string NotEnoughMoneyFormatKey = "Notification_NotEnoughMoneyFormat";

            // Settings UI I18n keys
            public static readonly string InvalidNumberInputKey = "UI_InvalidInput";
            public static readonly string SettingsPanelTitleKey = "UI_SettingsPanelTitle";
            public static readonly string SettingsCategoryGeneralKey = "UI_SettingsCategoryGeneral";
            public static readonly string SettingsCategoryPricingKey = "UI_SettingsCategoryPricing";
            public static readonly string SettingsEnableAnimationKey = "UI_SettingsEnableAnimation";
            public static readonly string SettingsPressAnyKeyKey = "UI_SettingsPressAnyKey";
            public static readonly string SettingsRefreshStockPriceKey = "UI_SettingsRefreshStockPrice";
            public static readonly string SettingsStorePickPriceKey = "UI_SettingsStorePickPrice";
            public static readonly string SettingsStreetPickPriceKey = "UI_SettingsStreetPickPrice";
            public static readonly string SettingsMeltBasePriceKey = "UI_SettingsMeltBasePrice";
            public static readonly string SettingsResetToDefaultKey = "UI_SettingsResetToDefault";
            public static readonly string FreeKey = "UI_Free";
            public static readonly string SettingsEnableDestroyButtonKey = "UI_SettingsEnableDestroyButton";
            public static readonly string SettingsEnableMeltButtonKey = "UI_SettingsEnableLotteryButton";
            public static readonly string SettingsEnableDebugKey = "UI_SettingsEnableDebug";
            public static readonly string SettingsEnableUseToCreateItemPatchKey = "UI_SettingsEnableUseToCreateItemPatch";
            public static readonly string SettingsEnableWeightedLotteryKey = "UI_SettingsEnableWeightedLottery";
            public static readonly string SettingsEnableHighQualitySoundKey = "UI_SettingsEnableHighQualitySound";
            public static readonly string SettingsHighQualitySoundFilePathKey = "UI_SettingsHighQualitySoundFile";
            public static readonly string SettingsEnableRefreshStockKey = "UI_SettingsEnableRefreshStock";
            public static readonly string SettingsEnableStorePickKey = "UI_SettingsEnableStorePick";
            public static readonly string SettingsEnableStreetPickKey = "UI_SettingsEnableStreetPick";
            public static readonly string SettingsEnableRecycleKey = "UI_SettingsEnableRecycle";
            public static readonly string SettingsEnableTripleLotteryAnimationKey = "UI_SettingsEnableTripleLotteryAnimation";
            public static readonly string SettingsEnablePatchUseTimeKey = "UI_SettingsEnablePatchUseTime";
            public static readonly string SettingsPatchedUseTimeKey = "UI_SettingsPatchedUseTime";
            public static readonly string SettingsEnablePatchUseTimeDescriptionKey = "UI_SettingsEnablePatchUseTimeDescription";
            public static readonly string SettingsPatchedUseTimeDescriptionKey = "UI_SettingsPatchedUseTimeDescription";
            public static readonly string RecyclingFailedKey = "UI_RecyclingFailed";
            public static readonly string ItemIsNullKey = "UI_ItemIsNull";
            public static readonly string ContractInventoryNotAvailableKey = "UI_ContractInventoryNotAvailable";
            public static readonly string ItemNotValidForContractKey = "UI_ItemNotValidForContract";
            public static readonly string ContractFullKey = "UI_ContractFull";
            public static readonly string NoRewardAvailableKey = "UI_NoRewardAvailable";
            public static readonly string ItemQualityMismatchKey = "UI_ItemQualityMismatch";
            public static readonly string BulletMustBeStackableKey = "UI_BulletMustBeStackable";
            public static readonly string BulletMustBeFullStackKey = "UI_BulletMustBeFullStack";
            public static readonly string RecycleTooltipEmptyKey = "UI_RecycleTooltipEmpty";
            public static readonly string RecycleTooltipNeedCountKey = "UI_RecycleTooltipNeedCount";
            public static readonly string RecycleTooltipConfirmKey = "UI_RecycleTooltipConfirm";
            public static readonly string RecycleTooltipClearKey = "UI_RecycleTooltipClear";
            public static readonly string RecycleCannotMixKey = "UI_RecycleCannotMix";
            public static readonly string RecycleNoHigherLevelKey = "UI_RecycleNoHigherLevel";
            public static readonly string RecycleNoTargetUpgradeKey = "UI_RecycleNoTargetUpgrade";
            public static readonly string DoubleClickToTripleLotteryKey = "UI_DoubleClickToTripleLottery";

            // Item Operation Menu I18n keys
            public static readonly string ItemMenuDestroyKey = "UI_ItemMenuDestroy";
            public static readonly string ItemMenuMeltKey = "UI_ItemMenuLottery";
            public static readonly string LotteryResultFormatKey = "Notification_LotteryResultFormat";

            // Melt Operation I18n keys
            public static readonly string MeltResultFormatKey = "Notification_MeltResultFormat";
            public static readonly string MeltLevelUpNotificationKey = "Notification_MeltLevelUp";
            public static readonly string MeltLevelDownNotificationKey = "Notification_MeltLevelDown";
            public static readonly string MeltLevelSameNotificationKey = "Notification_MeltLevelSame";
            public static readonly string MeltSameItemNotificationKey = "Notification_MeltSameItem";
            public static readonly string MeltDestroyedNotificationKey = "Notification_MeltDestroyed";
            public static readonly string MeltCostFormatKey = "Notification_MeltCostFormat";
            public static readonly string MeltMutatedNotificationKey = "Notification_MeltMutated";
        }

        private readonly Dictionary<SystemLanguage, Dictionary<string, string>> _localizedStrings = new Dictionary<SystemLanguage, Dictionary<string, string>> {
        { SystemLanguage.English, new Dictionary<string, string> {
            { I18n.ModNameKey,  Constants.ModId != Constants.AnimationOnlyModId ? "Lucky Box" : "Lucky Animation" },
            { I18n.RefreshStockKey, "Refresh" },
            { I18n.StorePickKey, "Picked from the merchant." },
            { I18n.RecycleKey, "Recycle" },
            { I18n.ConfirmKey, "Confirm" },
            { I18n.ClearKey, "Clear" },
            { I18n.OpenKey, "Open" },
            { I18n.CloseKey, "Close" },
            { I18n.PickNotificationFormatKey, "You picked up one {itemDisplayName}." },
            { I18n.StreetPickKey, "Picked from the street." },
            { I18n.InventoryFullAndSendToStorageKey, "Inventory full — sending item to storage." },
            { I18n.NotEnoughMoneyFormatKey, "Not enough coins! Need {price} coins." },
            { I18n.InvalidNumberInputKey, "Invalid input. Please enter a valid number." },
            { I18n.SettingsPanelTitleKey, $"{Constants.ModName} SETTINGS" },
            { I18n.SettingsCategoryGeneralKey, "General" },
            { I18n.SettingsCategoryPricingKey, "Pricing" },
            { I18n.SettingsEnableAnimationKey, "Enable animation" },
            { I18n.SettingsPressAnyKeyKey, "Press any key..." },
            { I18n.SettingsRefreshStockPriceKey, "Refresh stock price" },
            { I18n.SettingsStorePickPriceKey, "Merchant roll price" },
            { I18n.SettingsStreetPickPriceKey, "Street roll price" },
            { I18n.SettingsMeltBasePriceKey, "Melt base price" },
            { I18n.SettingsResetToDefaultKey, "Reset to default" },
            { I18n.FreeKey, "Free!" },
            { I18n.SettingsEnableDestroyButtonKey, "Enable destroy action" },
            { I18n.SettingsEnableMeltButtonKey, "Enable melt action" },
            { I18n.SettingsEnableDebugKey, "Enable debug mode" },
            { I18n.SettingsEnableUseToCreateItemPatchKey, "Enable in-game lottery patch" },
            { I18n.SettingsEnableWeightedLotteryKey, "Enable weighted lottery" },
            { I18n.SettingsEnableHighQualitySoundKey, "Enable high-quality item sound" },
            { I18n.SettingsHighQualitySoundFilePathKey, "High-quality item sound file" },
            { I18n.SettingsEnableRefreshStockKey, "Enable refresh stock" },
            { I18n.SettingsEnableStorePickKey, "Enable store pick" },
            { I18n.SettingsEnableStreetPickKey, "Enable street pick" },
            { I18n.SettingsEnableRecycleKey, "Enable recycle" },
            { I18n.SettingsEnableTripleLotteryAnimationKey, "Enable triple lottery animation" },
            { I18n.SettingsEnablePatchUseTimeKey, "Enable patch use time" },
            { I18n.SettingsPatchedUseTimeKey, "Patched use time" },
            { I18n.SettingsEnablePatchUseTimeDescriptionKey, "Enable patching of item use time for use-to-create items" },
            { I18n.SettingsPatchedUseTimeDescriptionKey, "Use time to set for patched items (seconds)" },
            { I18n.DoubleClickToTripleLotteryKey, "Double click to roll multiple times." },
            { I18n.RecyclingFailedKey, "Recycling failed." },
            { I18n.ItemIsNullKey, "Item is null." },
            { I18n.ContractInventoryNotAvailableKey, "Contract inventory not available." },
            { I18n.ItemNotValidForContractKey, "Item not valid for this contract." },
            { I18n.ContractFullKey, "Contract is full." },
            { I18n.BulletMustBeStackableKey, "Bullets must be stackable." },
            { I18n.BulletMustBeFullStackKey, "Bullets must be submitted as a full group of {groupSize}." },
            { I18n.RecycleTooltipEmptyKey, "Place items into the recycle box to start recycling." },
            { I18n.RecycleTooltipNeedCountKey, "Requires {needed} items to recycle. Current: {current}/{needed}" },
            { I18n.RecycleTooltipConfirmKey, "Confirm recycling {count} items to receive a higher-tier item." },
            { I18n.RecycleTooltipClearKey, "Clear all {count} items; items will be returned to their origin." },
            { I18n.RecycleCannotMixKey, "Cannot mix bullets with non-bullet items in a contract." },
            { I18n.RecycleNoHigherLevelKey, "No higher-tier item exists for this level; cannot recycle." },
            { I18n.RecycleNoTargetUpgradeKey, "No upgrade item found for this level in target categories." },
            { I18n.NoRewardAvailableKey, "No reward available for this quality." },
            { I18n.ItemQualityMismatchKey, "Item quality does not match contract requirements." },
            { I18n.ItemMenuDestroyKey, "Destroy" },
            { I18n.ItemMenuMeltKey, "Melt" },
            { I18n.LotteryResultFormatKey, "You got {itemDisplayName}!" },
            { I18n.MeltResultFormatKey, "Melt complete! Total: {meltCount}, Up: {levelUpCount}, Down: {levelDownCount}, Same: {sameLevelCount}, Destroyed: {destroyedCount}" },
            { I18n.MeltLevelUpNotificationKey, "{originalItem} upgraded to {newItem}!" },
            { I18n.MeltLevelDownNotificationKey, "{originalItem} downgraded to {newItem}..." },
            { I18n.MeltLevelSameNotificationKey, "{originalItem} stayed as {newItem}." },
            { I18n.MeltSameItemNotificationKey, "{originalItem} stayed the same!" },
            { I18n.MeltDestroyedNotificationKey, "{originalItem} was destroyed!" },
            { I18n.MeltCostFormatKey, "Melt cost: {basePrice} (base) × {level} (level) × {count} (qty) = {totalCost}" },
            { I18n.MeltMutatedNotificationKey, "Hmm — something seems to have changed subtly." }
        } },
        { SystemLanguage.ChineseSimplified, new Dictionary<string, string> {
            { I18n.ModNameKey, Constants.ModId != Constants.AnimationOnlyModId ? "幸运方块" : "抽奖动画" },
            { I18n.RefreshStockKey, "刷新" },
            { I18n.StorePickKey, "商人那拾一个" },
            { I18n.RecycleKey, "汰换合同" },
            { I18n.ConfirmKey, "确认" },
            { I18n.ClearKey, "清空" },
            { I18n.OpenKey, "打开" },
            { I18n.CloseKey, "关闭" },
            { I18n.PickNotificationFormatKey, "俺拾到了 {itemDisplayName}！" },
            { I18n.StreetPickKey, "路边拾一个" },
            { I18n.InventoryFullAndSendToStorageKey, "俺背不动了，寄回仓库啦。" },
            { I18n.NotEnoughMoneyFormatKey, "钱不够啦！需要 {price} 个铜板。" },
            { I18n.InvalidNumberInputKey, "输入无效，请输入有效数字。" },
            { I18n.SettingsPanelTitleKey, "幸运方块 设置" },
            { I18n.SettingsCategoryGeneralKey, "常规" },
            { I18n.SettingsCategoryPricingKey, "价格" },
            { I18n.SettingsEnableAnimationKey, "启用动画" },
            { I18n.SettingsPressAnyKeyKey, "按任意键..." },
            { I18n.SettingsRefreshStockPriceKey, "刷新库存价格" },
            { I18n.SettingsStorePickPriceKey, "商人抽奖价格" },
            { I18n.SettingsStreetPickPriceKey, "街边抽奖价格" },
            { I18n.SettingsMeltBasePriceKey, "熔炼基础价格" },
            { I18n.SettingsResetToDefaultKey, "恢复默认" },
            { I18n.FreeKey, "免费！" },
            { I18n.SettingsEnableDestroyButtonKey, "启用销毁动作" },
            { I18n.SettingsEnableMeltButtonKey, "启用熔炼动作" },
            { I18n.SettingsEnableDebugKey, "启用调试模式" },
            { I18n.SettingsEnableUseToCreateItemPatchKey, "启用道具抽奖动画补丁" },
            { I18n.SettingsEnableWeightedLotteryKey, "启用权重抽奖" },
            { I18n.SettingsEnableHighQualitySoundKey, "启用高价值物品音效" },
            { I18n.SettingsHighQualitySoundFilePathKey, "高价值音效文件" },
            { I18n.SettingsEnableRefreshStockKey, "启用刷新库存" },
            { I18n.SettingsEnableStorePickKey, "启用商人抽奖" },
            { I18n.SettingsEnableStreetPickKey, "启用街边抽奖" },
            { I18n.SettingsEnableRecycleKey, "启用汰换合同" },
            { I18n.SettingsEnableTripleLotteryAnimationKey, "启用三连抽动画" },
            { I18n.SettingsEnablePatchUseTimeKey, "启用使用时间补丁" },
            { I18n.SettingsPatchedUseTimeKey, "补丁使用时间" },
            { I18n.SettingsEnablePatchUseTimeDescriptionKey, "为使用创建物品的使用时间应用补丁" },
            { I18n.SettingsPatchedUseTimeDescriptionKey, "为补丁物品设置的使用时间（秒）" },
            { I18n.DoubleClickToTripleLotteryKey, "双击可连抽更多次。" },
            { I18n.RecyclingFailedKey, "回收失败。" },
            { I18n.ItemIsNullKey, "物品为空。" },
            { I18n.ContractInventoryNotAvailableKey, "合同库存不可用。" },
            { I18n.ItemNotValidForContractKey, "物品不符合该合同。" },
            { I18n.ContractFullKey, "合同已满。" },
            { I18n.BulletMustBeStackableKey, "子弹必须为可堆叠物品。" },
            { I18n.BulletMustBeFullStackKey, "子弹必须一次提交整组（{groupSize}发）。" },
            { I18n.RecycleTooltipEmptyKey, "将物品放入汰换箱以开始汰换。" },
            { I18n.RecycleTooltipNeedCountKey, "需要 {needed} 个物品才能完成汰换。 当前: {current}/{needed}" },
            { I18n.RecycleTooltipConfirmKey, "确认汰换 {count} 个物品以获得更高阶物品。" },
            { I18n.RecycleTooltipClearKey, "清除所有 {count} 个物品；物品将返回原位。" },
            { I18n.RecycleCannotMixKey, "子弹不能与非子弹物品混合放入汰换合同。" },
            { I18n.RecycleNoHigherLevelKey, "该等级不存在更高一级的物品，无法汰换。" },
            { I18n.RecycleNoTargetUpgradeKey, "目标类别中不存在该等级可升级的物品。" },
            { I18n.NoRewardAvailableKey, "此品质无可获得奖励。" },
            { I18n.ItemQualityMismatchKey, "物品品质不符合合同要求。" },
            { I18n.ItemMenuDestroyKey, "销毁" },
            { I18n.ItemMenuMeltKey, "熔炼" },
            { I18n.LotteryResultFormatKey, "你抽中了 {itemDisplayName}！" },
            { I18n.MeltResultFormatKey, "熔炼完成！ 总数: {meltCount}, 升级: {levelUpCount}, 降级: {levelDownCount}, 不变: {sameLevelCount}, 损毁: {destroyedCount}" },
            { I18n.MeltLevelUpNotificationKey, "{originalItem} 升级为 {newItem}！" },
            { I18n.MeltLevelDownNotificationKey, "{originalItem} 降级为 {newItem}..." },
            { I18n.MeltLevelSameNotificationKey, "{originalItem} 保持为 {newItem}。" },
            { I18n.MeltSameItemNotificationKey, "{originalItem} 没有变化！" },
            { I18n.MeltDestroyedNotificationKey, "{originalItem} 被损毁了！" },
            { I18n.MeltCostFormatKey, "熔炼费用: {basePrice} × {level} × {count} = {totalCost}" },
            { I18n.MeltMutatedNotificationKey, "看起来在转换过程中出现了些微变化。" }
        } },
        { SystemLanguage.ChineseTraditional, new Dictionary<string, string> {
            { I18n.ModNameKey, Constants.ModId != Constants.AnimationOnlyModId ? "幸運方塊" : "抽獎動畫" },
            { I18n.RefreshStockKey, "刷新" },
            { I18n.StorePickKey, "商人那拾一個" },
            { I18n.RecycleKey, "汰換合約" },
            { I18n.ConfirmKey, "確認" },
            { I18n.ClearKey, "清空" },
            { I18n.OpenKey, "打開" },
            { I18n.CloseKey, "關閉" },
            { I18n.PickNotificationFormatKey, "俺拾到了 {itemDisplayName}！" },
            { I18n.StreetPickKey, "路邊拾一個" },
            { I18n.InventoryFullAndSendToStorageKey, "俺背不動了，寄回倉庫啦。" },
            { I18n.NotEnoughMoneyFormatKey, "錢不夠啦！需要 {price} 個銅板。" },
            { I18n.InvalidNumberInputKey, "輸入無效，請輸入有效數字。" },
            { I18n.SettingsPanelTitleKey, "幸運方塊 設定" },
            { I18n.SettingsCategoryGeneralKey, "常規" },
            { I18n.SettingsCategoryPricingKey, "價格" },
            { I18n.SettingsEnableAnimationKey, "啟用動畫" },
            { I18n.SettingsPressAnyKeyKey, "按任意鍵..." },
            { I18n.SettingsRefreshStockPriceKey, "刷新庫存價格" },
            { I18n.SettingsStorePickPriceKey, "商人抽獎價格" },
            { I18n.SettingsStreetPickPriceKey, "街邊抽獎價格" },
            { I18n.SettingsMeltBasePriceKey, "熔煉基礎價格" },
            { I18n.SettingsResetToDefaultKey, "恢復預設" },
            { I18n.FreeKey, "免費！" },
            { I18n.SettingsEnableDestroyButtonKey, "啟用銷毀動作" },
            { I18n.SettingsEnableMeltButtonKey, "啟用熔煉動作" },
            { I18n.SettingsEnableDebugKey, "啟用偵錯模式" },
            { I18n.SettingsEnableUseToCreateItemPatchKey, "啟用道具抽獎動畫補丁" },
            { I18n.SettingsEnableWeightedLotteryKey, "啟用權重抽獎" },
            { I18n.SettingsEnableHighQualitySoundKey, "啟用高品質物品音效" },
            { I18n.SettingsHighQualitySoundFilePathKey, "高品質音效檔案" },
            { I18n.SettingsEnableRefreshStockKey, "啟用刷新庫存" },
            { I18n.SettingsEnableStorePickKey, "啟用商人抽獎" },
            { I18n.SettingsEnableStreetPickKey, "啟用街邊抽獎" },
            { I18n.SettingsEnableRecycleKey, "啟用汰換合約" },
            { I18n.SettingsEnableTripleLotteryAnimationKey, "啟用三連抽動畫" },
            { I18n.SettingsEnablePatchUseTimeKey, "啟用使用時間補丁" },
            { I18n.SettingsPatchedUseTimeKey, "補丁使用時間" },
            { I18n.SettingsEnablePatchUseTimeDescriptionKey, "為使用建立物品的使用時間套用補丁" },
            { I18n.SettingsPatchedUseTimeDescriptionKey, "為補丁物品設定的使用時間（秒）" },
            { I18n.DoubleClickToTripleLotteryKey, "雙擊可連抽更多次。" },
            { I18n.RecyclingFailedKey, "回收失敗。" },
            { I18n.ItemIsNullKey, "物品為空。" },
            { I18n.ContractInventoryNotAvailableKey, "合約庫存不可用。" },
            { I18n.ItemNotValidForContractKey, "物品不符合該合約。" },
            { I18n.ContractFullKey, "合約已滿。" },
            { I18n.BulletMustBeStackableKey, "子彈必須為可堆疊物品。" },
            { I18n.BulletMustBeFullStackKey, "子彈必須一次提交整組（{groupSize}發）。" },
            { I18n.RecycleTooltipEmptyKey, "將物品放入汰換箱以開始汰換。" },
            { I18n.RecycleTooltipNeedCountKey, "需要 {needed} 個物品才能完成汰換。 當前: {current}/{needed}" },
            { I18n.RecycleTooltipConfirmKey, "確認汰換 {count} 個物品以獲得更高階物品。" },
            { I18n.RecycleTooltipClearKey, "清除所有 {count} 個物品；物品將返回原位。" },
            { I18n.RecycleCannotMixKey, "子彈不能與非子彈物品混合放入汰換合約。" },
            { I18n.RecycleNoHigherLevelKey, "該等級不存在更高一級的物品，無法汰換。" },
            { I18n.RecycleNoTargetUpgradeKey, "目標類別中不存在該等級可升級的物品。" },
            { I18n.NoRewardAvailableKey, "此品質無可獲取的獎勵。" },
            { I18n.ItemQualityMismatchKey, "物品品質不符合合約要求。" },
            { I18n.ItemMenuDestroyKey, "銷毀" },
            { I18n.ItemMenuMeltKey, "熔煉" },
            { I18n.LotteryResultFormatKey, "你抽中了 {itemDisplayName}！" },
            { I18n.MeltResultFormatKey, "熔煉完成！ 總數: {meltCount}, 升級: {levelUpCount}, 降級: {levelDownCount}, 不變: {sameLevelCount}, 損毀: {destroyedCount}" },
            { I18n.MeltLevelUpNotificationKey, "{originalItem} 升級為 {newItem}！" },
            { I18n.MeltLevelDownNotificationKey, "{originalItem} 降級為 {newItem}..." },
            { I18n.MeltLevelSameNotificationKey, "{originalItem} 保持為 {newItem}。" },
            { I18n.MeltSameItemNotificationKey, "{originalItem} 沒有變化！" },
            { I18n.MeltDestroyedNotificationKey, "{originalItem} 被損毀了！" },
            { I18n.MeltCostFormatKey, "熔煉費用: {basePrice} × {level} × {count} = {totalCost}" },
            { I18n.MeltMutatedNotificationKey, "在轉換過程中似乎出現了細微變化。" }
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
