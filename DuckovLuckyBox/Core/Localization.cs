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
            public static readonly string SettingsEnableStockShopActionsKey = "UI_SettingsEnableStockShopActions";
            public static readonly string SettingsEnableTripleLotteryAnimationKey = "UI_SettingsEnableTripleLotteryAnimation";
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
            { I18n.StorePickKey, "Roll from the merchant." },
            { I18n.RecycleKey, "Recycle" },
            { I18n.ConfirmKey, "Confirm" },
            { I18n.ClearKey, "Clear" },
            { I18n.OpenKey, "Open" },
            { I18n.CloseKey, "Close" },
            { I18n.PickNotificationFormatKey, "You picked one {itemDisplayName}." },
            { I18n.StreetPickKey, "Roll from the street." },
            { I18n.InventoryFullAndSendToStorageKey, "Inventory full, sending to storage." },
            { I18n.NotEnoughMoneyFormatKey, "Not enough money! Need {price} coins." },
            { I18n.InvalidNumberInputKey, "Invalid input. Please enter a valid number." },
            { I18n.SettingsPanelTitleKey, $"{Constants.ModName} SETTINGS" },
            { I18n.SettingsCategoryGeneralKey, "General" },
            { I18n.SettingsCategoryPricingKey, "Pricing" },
            { I18n.SettingsEnableAnimationKey, "Enable Animation" },
            { I18n.SettingsPressAnyKeyKey, "Press any key..." },
            { I18n.SettingsRefreshStockPriceKey, "Refresh Stock Price" },
            { I18n.SettingsStorePickPriceKey, "Roll from the merchant Price" },
            { I18n.SettingsStreetPickPriceKey, "Roll from the street Price" },
            { I18n.SettingsMeltBasePriceKey, "Melt Base Price" },
            { I18n.SettingsResetToDefaultKey, "Reset to Default" },
            { I18n.FreeKey, "Free!" },
            { I18n.SettingsEnableDestroyButtonKey, "Enable Destroy Action" },
            { I18n.SettingsEnableMeltButtonKey, "Enable Melt Action" },
            { I18n.SettingsEnableDebugKey, "Enable Debug Mode" },
            { I18n.SettingsEnableUseToCreateItemPatchKey, "Enable In—Game Lottery Patch" },
            { I18n.SettingsEnableWeightedLotteryKey, "Enable Weighted Lottery" },
            { I18n.SettingsEnableHighQualitySoundKey, "Enable High Quality Item Sound" },
            { I18n.SettingsHighQualitySoundFilePathKey, "High Quality Item Sound File" },
            { I18n.SettingsEnableStockShopActionsKey, "Enable Shop Actions" },
            { I18n.SettingsEnableTripleLotteryAnimationKey, "Enable Triple Lottery Animation" },
            { I18n.RecyclingFailedKey, "Recycling failed." },
            { I18n.ItemIsNullKey, "Item is null." },
            { I18n.ContractInventoryNotAvailableKey, "Contract inventory not available." },
            { I18n.ItemNotValidForContractKey, "Item is not valid for this contract." },
            { I18n.ContractFullKey, "Contract is full." },
            { I18n.BulletMustBeStackableKey, "Bullets must be stackable." },
            { I18n.BulletMustBeFullStackKey, "Bullets must be submitted as a full group of {groupSize}." },
            { I18n.RecycleTooltipEmptyKey, "Place items into the recycle box to start recycling." },
            { I18n.RecycleTooltipNeedCountKey, "Requires {needed} items to complete the recycle. Current: {current}/{needed}" },
            { I18n.RecycleTooltipConfirmKey, "Confirm recycling {count} items to receive a higher-tier item." },
            { I18n.RecycleTooltipClearKey, "Clear all {count} items; items will be returned to their origin." },
            { I18n.RecycleCannotMixKey, "Cannot mix bullets with non-bullet items in a contract." },
            { I18n.RecycleNoHigherLevelKey, "No higher-tier item exists for this level; cannot recycle." },
            { I18n.RecycleNoTargetUpgradeKey, "Target categories do not contain an upgrade item for this level." },
            { I18n.NoRewardAvailableKey, "No reward available for this quality." },
            { I18n.ItemQualityMismatchKey, "Item quality does not match contract requirements." },
            { I18n.ItemMenuDestroyKey, "Destroy" },
            { I18n.ItemMenuMeltKey, "Melt" },
            { I18n.LotteryResultFormatKey, "You got {itemDisplayName}!" },
            { I18n.MeltResultFormatKey, "Melt done! Total: {meltCount}, Level up: {levelUpCount}, Level down: {levelDownCount}, Same level: {sameLevelCount}, Destroyed: {destroyedCount}" },
            { I18n.MeltLevelUpNotificationKey, "{originalItem} upgraded to {newItem}!" },
            { I18n.MeltLevelDownNotificationKey, "{originalItem} downgraded to {newItem}..." },
            { I18n.MeltLevelSameNotificationKey, "{originalItem} stayed as {newItem}." },
            { I18n.MeltSameItemNotificationKey, "{originalItem} stayed the same!" },
            { I18n.MeltDestroyedNotificationKey, "{originalItem} was destroyed!" },
            { I18n.MeltCostFormatKey, "Melt cost: {basePrice} (Base Price) × {level} (Level Fee) × {count} (Quantity) = {totalCost}" },
            { I18n.MeltMutatedNotificationKey, "Wait! Something seems to have changed subtly." }
        } },
        { SystemLanguage.ChineseSimplified, new Dictionary<string, string> {
            { I18n.ModNameKey, Constants.ModId != Constants.AnimationOnlyModId ? "幸运\"方块\"" : "抽奖动画" },
            { I18n.RefreshStockKey, "刷新" },
            { I18n.StorePickKey, "商人那拾一个" },
            { I18n.RecycleKey, "\"汰换合同\"" },
            { I18n.ConfirmKey, "确认" },
            { I18n.ClearKey, "清空" },
            { I18n.OpenKey, "打开" },
            { I18n.CloseKey, "关闭" },
            { I18n.PickNotificationFormatKey, "俺拾到了 {itemDisplayName}" },
            { I18n.StreetPickKey, "路边拾一个" },
            { I18n.InventoryFullAndSendToStorageKey, "俺拾不动嘞，邮回仓库嘞。" },
            { I18n.NotEnoughMoneyFormatKey, "钱不够嘞！需要 {price} 个铜板。" },
            { I18n.InvalidNumberInputKey, "输入无效。请输入一个有效的数字。" },
            { I18n.SettingsPanelTitleKey, "幸运\"方块\"设置" },
            { I18n.SettingsCategoryGeneralKey, "常规" },
            { I18n.SettingsCategoryPricingKey, "价格" },
            { I18n.SettingsEnableAnimationKey, "启用动画" },
            { I18n.SettingsPressAnyKeyKey, "按任意键..." },
            { I18n.SettingsRefreshStockPriceKey, "刷新库存价格" },
            { I18n.SettingsStorePickPriceKey, "商人抽奖价格" },
            { I18n.SettingsStreetPickPriceKey, "街边抽奖价格" },
            { I18n.SettingsMeltBasePriceKey, "熔炼基本价格" },
            { I18n.SettingsResetToDefaultKey, "恢复默认值" },
            { I18n.FreeKey, "免费！" },
            { I18n.SettingsEnableDestroyButtonKey, "启用销毁动作" },
            { I18n.SettingsEnableMeltButtonKey, "启用熔炼动作" },
            { I18n.SettingsEnableDebugKey, "启用调试模式" },
            { I18n.SettingsEnableUseToCreateItemPatchKey, "启用游戏道具抽奖动画" },
            { I18n.SettingsEnableWeightedLotteryKey, "启用权重抽奖" },
            { I18n.SettingsEnableHighQualitySoundKey, "启用高等级物品音效" },
            { I18n.SettingsHighQualitySoundFilePathKey, "高等级物品音效文件" },
            { I18n.SettingsEnableStockShopActionsKey, "启用商店动作" },
            { I18n.SettingsEnableTripleLotteryAnimationKey, "启用三连抽动画" },
            { I18n.RecyclingFailedKey, "回收失败。" },
            { I18n.ItemIsNullKey, "物品为空。" },
            { I18n.ContractInventoryNotAvailableKey, "合同库存不可用。" },
            { I18n.ItemNotValidForContractKey, "物品对该合同无效。" },
            { I18n.ContractFullKey, "合同已满。" },
            { I18n.BulletMustBeStackableKey, "子弹必须为可堆叠物品。" },
            { I18n.BulletMustBeFullStackKey, "子弹必须一次提交整组（{groupSize}发），无法添加到汰换合同。" },
            { I18n.RecycleTooltipEmptyKey, "将物品放入汰换箱中以开始汰换" },
            { I18n.RecycleTooltipNeedCountKey, "需要 {needed} 个物品才能完成汰换\n当前: {current}/{needed}" },
            { I18n.RecycleTooltipConfirmKey, "确认汰换 {count} 个物品\n将获得更高级别的物品" },
            { I18n.RecycleTooltipClearKey, "清除所有 {count} 个物品\n物品将返回原位置" },
            { I18n.RecycleCannotMixKey, "子弹与非子弹物品不能混合放入汰换合同。" },
            { I18n.RecycleNoHigherLevelKey, "该等级不存在高一级的物品，无法进行汰换。" },
            { I18n.RecycleNoTargetUpgradeKey, "目标类型中不存在该等级的升级物品。" },
            { I18n.NoRewardAvailableKey, "此品质无可用奖励。" },
            { I18n.ItemQualityMismatchKey, "物品品质不符合合同要求。" },
            { I18n.ItemMenuDestroyKey, "销毁" },
            { I18n.ItemMenuMeltKey, "熔炼" },
            { I18n.LotteryResultFormatKey, "你抽中了 {itemDisplayName}！" },
            { I18n.MeltResultFormatKey, "熔炼完成！ 总数: {meltCount}, 升级: {levelUpCount}, 降级: {levelDownCount}, 不变: {sameLevelCount}, 损毁: {destroyedCount}" },
            { I18n.MeltLevelUpNotificationKey, "{originalItem} 升级为 {newItem}！" },
            { I18n.MeltLevelDownNotificationKey, "{originalItem} 降级为 {newItem}..." },
            { I18n.MeltLevelSameNotificationKey, "{originalItem} 平级转换为 {newItem}。" },
            { I18n.MeltSameItemNotificationKey, "{originalItem} 未起一丝波澜！" },
            { I18n.MeltDestroyedNotificationKey, "{originalItem} 被损毁了！" },
            { I18n.MeltCostFormatKey, "熔炼费用: {basePrice} (基础价格) × {level} (等级付费) × {count} (数量) = {totalCost}" },
            { I18n.MeltMutatedNotificationKey, "但转换过程中似乎出现了些微妙的变化。" }
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
