using Duckov.Economy.UI;
using Cysharp.Threading.Tasks;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;

namespace DuckovLuckyBox.Patches.StockShopActions
{
    /// <summary>
    /// Action to perform street lottery (random items from cache)
    /// </summary>
    public class StreetPickAction : IStockShopAction
    {
        public string GetLocalizationKey() => Localizations.I18n.StreetPickKey;

        public async UniTask ExecuteAsync(StockShopView stockShopView)
        {

            // Get price from settings
            long price = SettingManager.Instance.StreetPickPrice.Value as long? ?? DefaultSettings.StreetPickPrice;

            var context = new DefaultLotteryContext();
            await LotteryService.PerformLotteryWithContextAsync(
                itemTypeIds: ItemUtils.LotteryItemCache.GetAllItemTypeIds(),
                unitPrice: price,
                playAnimation: SettingManager.Instance.EnableAnimation.Value as bool? ?? DefaultSettings.EnableAnimation,
                context: context);
        }
    }
}
