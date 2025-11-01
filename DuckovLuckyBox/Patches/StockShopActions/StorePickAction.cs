using Duckov.Economy.UI;
using Cysharp.Threading.Tasks;
using System.Linq;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using HarmonyLib;
using Duckov.Economy;

namespace DuckovLuckyBox.Patches.StockShopActions
{
    /// <summary>
    /// Action to perform lottery pick from store inventory
    /// </summary>
    public class StorePickAction : IStockShopAction
    {
        public string GetLocalizationKey() => Localizations.I18n.StorePickKey;

        public async UniTask ExecuteAsync(StockShopView stockShopView)
        {

            var target = AccessTools.Field(typeof(StockShopView), "target").GetValue(stockShopView) as Duckov.Economy.StockShop;
            if (target == null) return;
            if (target.Busy) return;

            var itemEntries = target.entries.Where(entry =>
              entry.CurrentStock > 0 &&
              entry.Possibility > 0f &&
              entry.Show).ToList();

            if (itemEntries.Count == 0)
            {
                Log.Warning("No available items to pick");
                return;
            }

            // Get price from settings
            long unitPrice = SettingManager.Instance.StorePickPrice.Value as long? ?? DefaultSettings.StorePickPrice;
            // Determine the lottery count that can be performed with the current balance
            int lotteryCount = 1;
            while (lotteryCount < 3 && EconomyManager.IsEnough(new Cost(unitPrice * lotteryCount), true, true))
            {
                lotteryCount++;
            }

            var candidateTypeIds = itemEntries.Select(entry => entry.ItemTypeID).ToList();
            var context = new StoreLotteryContext(target, itemEntries);
            await LotteryService.PerformLotteryWithContextAsync(
                    candidateTypeIds,
                    lotteryCount,
                    unitPrice,
                    SettingManager.Instance.EnableAnimation.Value as bool? ?? DefaultSettings.EnableAnimation,
                    context);
        }
    }
}
