using Duckov.Economy.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DuckovLuckyBox.Core;

namespace DuckovLuckyBox.Patches.StockShopActions
{
    /// <summary>
    /// Manager for stock shop actions
    /// </summary>
    public class StockShopActionManager
    {
        private readonly Dictionary<string, IStockShopAction> _actions = new Dictionary<string, IStockShopAction>();

        public StockShopActionManager()
        {
            RegisterDefaultActions();
        }

        /// <summary>
        /// Register default actions
        /// </summary>
        private void RegisterDefaultActions()
        {
            Register(nameof(RefreshStockAction), new RefreshStockAction());
            Register(nameof(StorePickAction), new StorePickAction());
            Register(nameof(StreetPickAction), new StreetPickAction());
            Register(nameof(CycleBinAction), new CycleBinAction());
        }

        /// <summary>
        /// Register a new action
        /// </summary>
        public void Register(string actionName, IStockShopAction action)
        {
            if (action == null)
            {
                Log.Warning($"Cannot register null action: {actionName}");
                return;
            }
            _actions[actionName] = action;
        }

        /// <summary>
        /// Get all registered actions
        /// </summary>
        public IEnumerable<IStockShopAction> GetAllActions() => _actions.Values;

        /// <summary>
        /// Execute an action by name
        /// </summary>
        public async UniTask ExecuteAsync(string actionName, StockShopView stockShopView)
        {
            if (!_actions.TryGetValue(actionName, out var action))
            {
                Log.Warning($"Action not found: {actionName}");
                return;
            }

            try
            {
                await action.ExecuteAsync(stockShopView);
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error executing action {actionName}: {ex.Message}");
            }
        }
    }
}
