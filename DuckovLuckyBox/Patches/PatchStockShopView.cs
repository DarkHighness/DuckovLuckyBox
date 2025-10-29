using Duckov;
using Duckov.UI;
using Duckov.Economy.UI;
using TMPro;
using Duckov.Economy;
using SodaCraft.Localizations;
using HarmonyLib;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using ItemStatsSystem;
using System.Linq;
using DuckovLuckyBox.Core;

namespace DuckovLuckyBox.Patches
{
    /// <summary>
    /// Lottery context for store pick operations
    /// </summary>
    public class StoreLotteryContext : ILotteryContext
    {
        private readonly StockShop _stockShop;
        private readonly IList<StockShop.Entry> _stockEntries;
        private const string SFX_BUY = "UI/buy";

        public StoreLotteryContext(StockShop stockShop, IList<StockShop.Entry> stockEntries)
        {
            _stockShop = stockShop;
            _stockEntries = stockEntries;
        }

        public async UniTask<bool> OnBeforeLotteryAsync()
        {
            if (_stockShop == null || _stockShop.Busy) return false;

            // Mark shop as buying
            try { AccessTools.Field(typeof(StockShop), "buying").SetValue(_stockShop, true); } catch { }

            return await UniTask.FromResult(true);
        }

        public void OnLotterySuccess(Item resultItem, bool sentToStorage)
        {
            if (_stockShop == null || resultItem == null) return;

            try
            {
                // Decrement stock of the entry matching the result item's TypeID
                if (_stockEntries != null && _stockEntries.Count > 0)
                {
                    var matchingEntry = _stockEntries.FirstOrDefault(entry => entry?.ItemTypeID == resultItem.TypeID);
                    if (matchingEntry != null)
                    {
                        matchingEntry.CurrentStock = Math.Max(0, matchingEntry.CurrentStock - 1);
                    }
                    else
                    {
                        Log.Error($"No matching stock entry found for item TypeID {resultItem.TypeID}");
                    }
                }

                // Fire shop events
                var onAfterItemSoldField = AccessTools.Field(typeof(StockShop), "OnAfterItemSold");
                if (onAfterItemSoldField?.GetValue(null) is Action<StockShop> onAfterItemSold)
                    onAfterItemSold(_stockShop);
                var onItemPurchasedField = AccessTools.Field(typeof(StockShop), "OnItemPurchased");
                if (onItemPurchasedField?.GetValue(null) is Action<StockShop, Item> onItemPurchased)
                    onItemPurchased(_stockShop, resultItem);

                // Show notification
                var messageTemplate = Localizations.I18n.PickNotificationFormatKey.ToPlainText();
                var message = messageTemplate.Replace("{itemDisplayName}", resultItem.DisplayName);
                if (sentToStorage)
                {
                    message += " " + Localizations.I18n.InventoryFullAndSendToStorageKey.ToPlainText();
                }
                NotificationText.Push(message);
                AudioManager.Post(SFX_BUY);
            }
            finally
            {
                try { AccessTools.Field(typeof(StockShop), "buying").SetValue(_stockShop, false); } catch { }
            }
        }

        public void OnLotteryFailed()
        {
            try { AccessTools.Field(typeof(StockShop), "buying").SetValue(_stockShop, false); } catch { }
        }
    }

    [HarmonyPatch(typeof(StockShopView), "Setup")]
    public class PatchStockShopView_Setup
    {
        public static void Postfix(StockShopView __instance, TextMeshProUGUI ___merchantNameText, StockShop ___target)
        {
            StockShopViewUI.Instance.Setup(__instance, ___merchantNameText, ___target);
        }
    }

    [HarmonyPatch(typeof(StockShopView), "OnOpen")]
    public static class PatchStockShopView_OnOpen
    {
        public static void Postfix(StockShopView __instance)
        {

            StockShopViewUI.Instance.Open();
            RecycleSessionUI.Instance.Close();
        }
    }

    [HarmonyPatch(typeof(StockShopView), "OnClose")]
    public static class PatchStockShopView_OnClose
    {
        public static void Postfix(StockShopView __instance)
        {
            StockShopViewUI.Instance.Close();
            RecycleSessionUI.Instance.Close();
        }
    }

    [HarmonyPatch(typeof(StockShopView), "OnSelectionChanged")]
    public static class PatchStockShopView_OnSelectionChanged
    {
        public static bool Prefix(StockShopView __instance)
        {
            // Prevent selection changes when Cycle Bin view is open
            if (RecycleSessionUI.Instance.IsOpen)
            {
                return false; // Skip original method
            }
            return true; // Proceed with original method
        }
    }
}
