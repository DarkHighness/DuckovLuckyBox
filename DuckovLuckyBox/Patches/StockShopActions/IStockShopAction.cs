using Duckov.Economy.UI;
using Cysharp.Threading.Tasks;

namespace DuckovLuckyBox.Patches.StockShopActions
{
    /// <summary>
    /// Interface for stock shop actions
    /// </summary>
    public interface IStockShopAction
    {
        /// <summary>
        /// Get the localization key for this action
        /// </summary>
        string GetLocalizationKey();

        /// <summary>
        /// Execute this action asynchronously
        /// </summary>
        UniTask ExecuteAsync(StockShopView stockShopView);
    }
}
