using Duckov.Economy.UI;
using Cysharp.Threading.Tasks;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using Duckov.Economy;

namespace DuckovLuckyBox.Patches.StockShopActions
{
    /// <summary>
    /// Action to perform street lottery (random items from cache)
    /// </summary>
    public class StreetPickAction : IStockShopAction
    {
        public string GetLocalizationKey() => Localizations.I18n.StreetPickKey;

        public async UniTask ExecuteAsync(StockShopView stockShopView, bool isDoubleClick = false)
        {

            // Get price from settings
            long unitPrice = SettingManager.Instance.StreetPickPrice.Value as long? ?? DefaultSettings.StreetPickPrice;

            // Determine the lottery count that can be performed with the current balance
            int lotteryCount = 1;
            if (SettingManager.Instance.EnableTripleLotteryAnimation.GetAsBool() && isDoubleClick)
            {
                while (lotteryCount < 3 && EconomyManager.IsEnough(new Cost(unitPrice * lotteryCount), true, true))
                {
                    lotteryCount++;
                }
            }

            var context = new DefaultLotteryContext();
            await LotteryService.PerformLotteryWithContextAsync(
                itemTypeIds: ItemUtils.LotteryItemCache.GetAllItemTypeIds(),
                lotteryCount: lotteryCount,
                unitPrice: unitPrice,
                playAnimation: SettingManager.Instance.EnableAnimation.Value as bool? ?? DefaultSettings.EnableAnimation,
                context: context);
        }
    }
}
