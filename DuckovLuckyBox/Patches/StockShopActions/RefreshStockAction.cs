using Duckov;
using Duckov.UI;
using Duckov.Economy.UI;
using Duckov.Economy;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using SodaCraft.Localizations;

namespace DuckovLuckyBox.Patches.StockShopActions
{
    /// <summary>
    /// Action to refresh stock in the shop
    /// </summary>
    public class RefreshStockAction : IStockShopAction
    {
        private const string SFX_BUY = "UI/buy";

        public string GetLocalizationKey() => Localizations.I18n.RefreshStockKey;

        public async UniTask ExecuteAsync(StockShopView stockShopView)
        {
            var target = AccessTools.Field(typeof(StockShopView), "target").GetValue(stockShopView) as Duckov.Economy.StockShop;
            if (target == null) return;

            // Get price from settings and try to pay
            long price = SettingManager.Instance.RefreshStockPrice.Value as long? ?? DefaultSettings.RefreshStockPrice;

            // Skip payment if price is zero
            if (price > 0 && !EconomyManager.Pay(new Cost(price), true, true))
            {
                Log.Warning($"Failed to pay {price} for refresh stock");
                var notEnoughMoneyMessage = Localizations.I18n.NotEnoughMoneyFormatKey.ToPlainText().Replace("{price}", price.ToString());
                NotificationText.Push(notEnoughMoneyMessage);
                return;
            }

            if (!TryInvokeRefresh(target)) return;
            AudioManager.Post(SFX_BUY);

            await UniTask.CompletedTask;
        }

        private static bool TryInvokeRefresh(Duckov.Economy.StockShop stockShop)
        {
            if (stockShop == null) return false;

            AccessTools.Method(typeof(Duckov.Economy.StockShop), "DoRefreshStock").Invoke(stockShop, null);
            return true;
        }
    }
}
