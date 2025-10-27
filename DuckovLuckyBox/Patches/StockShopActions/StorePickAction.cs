using Duckov.Economy.UI;
using Duckov.Economy;
using Cysharp.Threading.Tasks;
using System.Linq;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;
using SodaCraft.Localizations;
using HarmonyLib;
using System;

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
            long price = SettingManager.Instance.StorePickPrice.Value as long? ?? DefaultSettings.StorePickPrice;

            var candidateTypeIds = itemEntries.Select(entry => entry.ItemTypeID).ToList();
            var context = new StoreLotteryContext(target, itemEntries);
            await LotteryService.PerformLotteryWithContextAsync(
                    candidateTypeIds,
                    price,
                    playAnimation: SettingManager.Instance.EnableAnimation.Value as bool? ?? DefaultSettings.EnableAnimation,
                    context);
        }
    }
}
