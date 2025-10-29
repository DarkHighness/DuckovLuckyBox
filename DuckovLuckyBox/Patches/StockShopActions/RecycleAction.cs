using Cysharp.Threading.Tasks;
using Duckov.Economy.UI;
using DuckovLuckyBox.Core;

namespace DuckovLuckyBox.Patches.StockShopActions
{
    /// <summary>
    /// Implements the stock shop "trash bin" action using native inventory widgets.
    /// </summary>
    public sealed class RecycleAction : IStockShopAction
    {
        public string GetLocalizationKey() => Localizations.I18n.RecycleKey;

        public async UniTask ExecuteAsync(StockShopView stockShopView)
        {
            if (stockShopView == null)
            {
                Log.Warning("RecycleAction executed without a StockShopView instance.");
                return;
            }

            var session = RecycleSessionUI.Instance;
            session.Setup(stockShopView);
            session.Toggle();
            await UniTask.CompletedTask;
        }
    }
}
