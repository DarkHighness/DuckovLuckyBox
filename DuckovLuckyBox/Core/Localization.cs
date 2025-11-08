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
            public static readonly string ModNameKey = $"{Constants.ModId}/ModName/{Constants.ModId}";
            public static readonly string RefreshStockKey = $"{Constants.ModId}/UI/RefreshStock";
            public static readonly string StorePickKey = $"{Constants.ModId}/UI/StorePick";
            public static readonly string StreetPickKey = $"{Constants.ModId}/UI/StreetPick";
            public static readonly string RecycleKey = $"{Constants.ModId}/UI/Recycle";
            public static readonly string ConfirmKey = $"{Constants.ModId}/UI/Confirm";
            public static readonly string ClearKey = $"{Constants.ModId}/UI/Clear";
            public static readonly string OpenKey = $"{Constants.ModId}/UI/Open";
            public static readonly string CloseKey = $"{Constants.ModId}/UI/Close";
            public static readonly string PickNotificationFormatKey = $"{Constants.ModId}/Notification/PickOneFormat";
            public static readonly string InventoryFullAndSendToStorageKey = $"{Constants.ModId}/Notification/InventoryFullAndSendToStorage";
            public static readonly string NotEnoughMoneyFormatKey = $"{Constants.ModId}/Notification/NotEnoughMoneyFormat";

            // Settings UI I18n keys
            public static readonly string InvalidNumberInputKey = $"{Constants.ModId}/UI/InvalidInput";
            public static readonly string SettingsPanelTitleKey = $"{Constants.ModId}/UI/SettingsPanelTitle";
            public static readonly string SettingsCategoryGeneralKey = $"{Constants.ModId}/UI/SettingsCategoryGeneral";
            public static readonly string SettingsCategoryPricingKey = $"{Constants.ModId}/UI/SettingsCategoryPricing";
            public static readonly string SettingsEnableAnimationKey = $"{Constants.ModId}/UI/SettingsEnableAnimation";
            public static readonly string SettingsPressAnyKeyKey = $"{Constants.ModId}/UI/SettingsPressAnyKey";
            public static readonly string SettingsRefreshStockPriceKey = $"{Constants.ModId}/UI/SettingsRefreshStockPrice";
            public static readonly string SettingsStorePickPriceKey = $"{Constants.ModId}/UI/SettingsStorePickPrice";
            public static readonly string SettingsStreetPickPriceKey = $"{Constants.ModId}/UI/SettingsStreetPickPrice";
            public static readonly string SettingsMeltBasePriceKey = $"{Constants.ModId}/UI/SettingsMeltBasePrice";
            public static readonly string SettingsResetToDefaultKey = $"{Constants.ModId}/UI/SettingsResetToDefault";
            public static readonly string FreeKey = $"{Constants.ModId}/UI/Free";
            public static readonly string SettingsEnableDestroyButtonKey = $"{Constants.ModId}/UI/SettingsEnableDestroyButton";
            public static readonly string SettingsEnableMeltButtonKey = $"{Constants.ModId}/UI/SettingsEnableLotteryButton";
            public static readonly string SettingsEnableDebugKey = $"{Constants.ModId}/UI/SettingsEnableDebug";
            public static readonly string SettingsEnableUseToCreateItemPatchKey = $"{Constants.ModId}/UI/SettingsEnableUseToCreateItemPatch";
            public static readonly string SettingsEnableUseToCreateItemWeightedLotteryKey = $"{Constants.ModId}/UI/SettingsEnableUseToCreateItemWeightedLottery";
            public static readonly string SettingsEnableWeightedLotteryKey = $"{Constants.ModId}/UI/SettingsEnableWeightedLottery";
            public static readonly string SettingsEnableHighQualitySoundKey = $"{Constants.ModId}/UI/SettingsEnableHighQualitySound";
            public static readonly string SettingsHighQualitySoundFilePathKey = $"{Constants.ModId}/UI/SettingsHighQualitySoundFile";
            public static readonly string SettingsEnableRefreshStockKey = $"{Constants.ModId}/UI/SettingsEnableRefreshStock";
            public static readonly string SettingsEnableStorePickKey = $"{Constants.ModId}/UI/SettingsEnableStorePick";
            public static readonly string SettingsEnableStreetPickKey = $"{Constants.ModId}/UI/SettingsEnableStreetPick";
            public static readonly string SettingsEnableRecycleKey = $"{Constants.ModId}/UI/SettingsEnableRecycle";
            public static readonly string SettingsEnableTripleLotteryAnimationKey = $"{Constants.ModId}/UI/SettingsEnableTripleLotteryAnimation";
            public static readonly string SettingsEnablePatchUseTimeKey = $"{Constants.ModId}/UI/SettingsEnablePatchUseTime";
            public static readonly string SettingsPatchedUseTimeKey = $"{Constants.ModId}/UI/SettingsPatchedUseTime";
            public static readonly string SettingsEnablePatchUseTimeDescriptionKey = $"{Constants.ModId}/UI/SettingsEnablePatchUseTimeDescription";
            public static readonly string SettingsPatchedUseTimeDescriptionKey = $"{Constants.ModId}/UI/SettingsPatchedUseTimeDescription";
            public static readonly string RecyclingFailedKey = $"{Constants.ModId}/UI/RecyclingFailed";
            public static readonly string ItemIsNullKey = $"{Constants.ModId}/UI/ItemIsNull";
            public static readonly string ContractInventoryNotAvailableKey = $"{Constants.ModId}/UI/ContractInventoryNotAvailable";
            public static readonly string ItemNotValidForContractKey = $"{Constants.ModId}/UI/ItemNotValidForContract";
            public static readonly string ContractFullKey = $"{Constants.ModId}/UI/ContractFull";
            public static readonly string NoRewardAvailableKey = $"{Constants.ModId}/UI/NoRewardAvailable";
            public static readonly string ItemQualityMismatchKey = $"{Constants.ModId}/UI/ItemQualityMismatch";
            public static readonly string BulletMustBeStackableKey = $"{Constants.ModId}/UI/BulletMustBeStackable";
            public static readonly string BulletMustBeFullStackKey = $"{Constants.ModId}/UI/BulletMustBeFullStack";
            public static readonly string RecycleTooltipEmptyKey = $"{Constants.ModId}/UI/RecycleTooltipEmpty";
            public static readonly string RecycleTooltipNeedCountKey = $"{Constants.ModId}/UI/RecycleTooltipNeedCount";
            public static readonly string RecycleTooltipConfirmKey = $"{Constants.ModId}/UI/RecycleTooltipConfirm";
            public static readonly string RecycleTooltipClearKey = $"{Constants.ModId}/UI/RecycleTooltipClear";
            public static readonly string RecycleCannotMixKey = $"{Constants.ModId}/UI/RecycleCannotMix";
            public static readonly string RecycleNoHigherLevelKey = $"{Constants.ModId}/UI/RecycleNoHigherLevel";
            public static readonly string RecycleNoTargetUpgradeKey = $"{Constants.ModId}/UI/RecycleNoTargetUpgrade";
            public static readonly string DoubleClickToTripleLotteryKey = $"{Constants.ModId}/UI/DoubleClickToTripleLottery";

            // Item Operation Menu I18n keys
            public static readonly string ItemMenuDestroyKey = $"{Constants.ModId}/UI/ItemMenuDestroy";
            public static readonly string ItemMenuMeltKey = $"{Constants.ModId}/UI/ItemMenuLottery";
            public static readonly string LotteryResultFormatKey = $"{Constants.ModId}/Notification/LotteryResultFormat";

            // Melt Operation I18n keys
            public static readonly string MeltResultFormatKey = $"{Constants.ModId}/Notification/MeltResultFormat";
            public static readonly string MeltLevelUpNotificationKey = $"{Constants.ModId}/Notification/MeltLevelUp";
            public static readonly string MeltLevelDownNotificationKey = $"{Constants.ModId}/Notification/MeltLevelDown";
            public static readonly string MeltLevelSameNotificationKey = $"{Constants.ModId}/Notification/MeltLevelSame";
            public static readonly string MeltSameItemNotificationKey = $"{Constants.ModId}/Notification/MeltSameItem";
            public static readonly string MeltDestroyedNotificationKey = $"{Constants.ModId}/Notification/MeltDestroyed";
            public static readonly string MeltCostFormatKey = $"{Constants.ModId}/Notification/MeltCostFormat";
            public static readonly string MeltMutatedNotificationKey = $"{Constants.ModId}/Notification/MeltMutated";
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
            { I18n.SettingsEnableUseToCreateItemWeightedLotteryKey, "Use Game-default weight for item lottery" },
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
            { I18n.SettingsEnableUseToCreateItemWeightedLotteryKey, "道具抽奖采用原版权重" },
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
            { I18n.SettingsEnableUseToCreateItemWeightedLotteryKey, "使用遊戲預設權重進行道具抽獎" },
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
