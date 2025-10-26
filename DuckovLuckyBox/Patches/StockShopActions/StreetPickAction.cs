using Duckov.Economy.UI;
using Cysharp.Threading.Tasks;
using System;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;
using SodaCraft.Localizations;

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

            try
            {
                var context = new DefaultLotteryContext();
                await LotteryService.PerformLotteryWithContextAsync(
                    candidateTypeIds: null, // Use default cache
                    price: price,
                    playAnimation: SettingManager.Instance.EnableAnimation.Value as bool? ?? DefaultSettings.EnableAnimation,
                    context: context);
            }
            catch (Exception ex)
            {
                Log.Error($"Street lottery failed: {ex.Message}");
            }
        }
    }
}
