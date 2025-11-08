using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using FMODUnity;
using HarmonyLib;
using ItemStatsSystem;

namespace DuckovLuckyBox.Patches
{
    [HarmonyPatch(typeof(UseToCreateItem), "OnUse")]
    public class PatchUseToCreateItem_OnUse
    {
        private static List<int> extractItemIds(UseToCreateItem instance)
        {
            var itemIds = new List<int>();

            // Get the "entries" field which is of type RandomContainer<UseToCreateItem.Entry>
            var entriesField = AccessTools.Field(typeof(UseToCreateItem), "entries");
            if (entriesField == null)
            {
                Log.Warning("Could not find 'entries' field in UseToCreateItem");
                return itemIds;
            }

            var randomContainerObj = entriesField.GetValue(instance);
            if (randomContainerObj == null)
            {
                Log.Warning("entries field is null");
                return itemIds;
            }

            // RandomContainer<T> has a public 'entries' field of type List<RandomContainer<T>.Entry>
            var entriesListField = AccessTools.Field(randomContainerObj.GetType(), "entries");
            if (entriesListField == null)
            {
                Log.Warning("Could not find 'entries' list in RandomContainer");
                return itemIds;
            }

            var entriesList = entriesListField.GetValue(randomContainerObj) as System.Collections.IList;
            if (entriesList == null)
            {
                Log.Warning("entries list is null or not IList");
                return itemIds;
            }

            // Each entry in the list is RandomContainer<T>.Entry struct which has a public 'value' field
            // The 'value' field contains UseToCreateItem.Entry (private), which has an 'itemTypeID' field
            foreach (var entry in entriesList)
            {
                if (entry == null)
                    continue;

                // Get the 'value' field from RandomContainer<T>.Entry
                var valueField = AccessTools.Field(entry.GetType(), "value");
                if (valueField == null)
                    continue;

                var useToCreateItemEntry = valueField.GetValue(entry);
                if (useToCreateItemEntry == null)
                    continue;

                // Get the 'itemTypeID' field from UseToCreateItem.Entry (private struct)
                var itemTypeIdField = AccessTools.Field(useToCreateItemEntry.GetType(), "itemTypeID");
                if (itemTypeIdField == null)
                    continue;

                var itemId = itemTypeIdField.GetValue(useToCreateItemEntry);
                if (itemId is int id)
                {
                    itemIds.Add(id);
                }
            }

            return itemIds;
        }

        private static List<WeightedItem> extractWeightedItems(UseToCreateItem instance)
        {
            var weightedItems = new List<WeightedItem>();

            // Get the "entries" field which is of type RandomContainer<UseToCreateItem.Entry>
            var entriesField = AccessTools.Field(typeof(UseToCreateItem), "entries");
            if (entriesField == null)
            {
                Log.Warning("Could not find 'entries' field in UseToCreateItem");
                return weightedItems;
            }

            var randomContainerObj = entriesField.GetValue(instance);
            if (randomContainerObj == null)
            {
                Log.Warning("entries field is null");
                return weightedItems;
            }

            // RandomContainer<T> has a public 'entries' field of type List<RandomContainer<T>.Entry>
            var entriesListField = AccessTools.Field(randomContainerObj.GetType(), "entries");
            if (entriesListField == null)
            {
                Log.Warning("Could not find 'entries' list in RandomContainer");
                return weightedItems;
            }

            var entriesList = entriesListField.GetValue(randomContainerObj) as System.Collections.IList;
            if (entriesList == null)
            {
                Log.Warning("entries list is null or not IList");
                return weightedItems;
            }

            // Each entry in the list is RandomContainer<T>.Entry struct which has 'value' and 'weight' fields
            // The 'value' field contains UseToCreateItem.Entry (private), which has an 'itemTypeID' field
            foreach (var entry in entriesList)
            {
                if (entry == null)
                    continue;

                // Get the 'value' field from RandomContainer<T>.Entry
                var valueField = AccessTools.Field(entry.GetType(), "value");
                if (valueField == null)
                    continue;

                var useToCreateItemEntry = valueField.GetValue(entry);
                if (useToCreateItemEntry == null)
                    continue;

                // Get the 'itemTypeID' field from UseToCreateItem.Entry (private struct)
                var itemTypeIdField = AccessTools.Field(useToCreateItemEntry.GetType(), "itemTypeID");
                if (itemTypeIdField == null)
                    continue;

                var itemId = itemTypeIdField.GetValue(useToCreateItemEntry);
                if (itemId is int id)
                {
                    // Get the 'weight' field from RandomContainer<T>.Entry
                    var weightField = AccessTools.Field(entry.GetType(), "weight");
                    if (weightField == null)
                        continue;

                    var weight = weightField.GetValue(entry);
                    if (weight is float w)
                    {
                        weightedItems.Add(new WeightedItem(id, w));
                    }
                }
            }

            if (SettingManager.Instance.EnableDebug.GetAsBool())
            {
                Log.Debug($"Extracted {weightedItems.Count} weighted items from UseToCreateItem:");
                foreach (var wi in weightedItems)
                {
                    Log.Debug($" - Item: {ItemUtils.GameItemCache.GetDisplayName(wi.ItemTypeId)}, Weight: {wi.Weight}");
                }
            }

            return weightedItems;
        }

        public static bool Prefix(UseToCreateItem __instance, Item item, object? user)
        {
            // Check if the patch is enabled in settings
            if (!SettingManager.Instance.EnableUseToCreateItemPatch.GetAsBool())
            {
                Log.Debug("UseToCreateItem patch is disabled in settings, skipping patch.");
                return true; // Allow the original method to execute
            }

            // Prevent the original OnUse method from executing
            // This disables the default behavior of UseToCreateItem
            var character = user as CharacterMainControl;
            if (character == null)
            {
                Log.Warning("UseToCreateItem_OnUse: user is not a CharacterMainControl.");
                return true;
            }

            var lotteryCount = 1;
            var requiredCount = 3;
            if (SettingManager.Instance.EnableTripleLotteryAnimation.GetAsBool())
            {

                var requiredToConsume = requiredCount - lotteryCount;
                var consumedCount = ItemUtils.ConsumeItem(item, requiredToConsume, true, true);
                if (consumedCount < requiredToConsume)
                {
                    Log.Debug($"Not enough items to perform triple lottery animation. Required: {requiredToConsume}, Consumed: {consumedCount}");
                }
                lotteryCount += consumedCount;
            }

            // Play animation with optional weighted lottery
            var context = new DefaultLotteryContext();

            // Check if we should use weighted lottery from the container
            if (SettingManager.Instance.EnableUseToCreateItemWeightedLottery.GetAsBool())
            {
                var weightedItems = extractWeightedItems(__instance);
                if (weightedItems.Count > 0)
                {
                    Log.Debug($"Using weighted lottery with {weightedItems.Count} items from UseToCreateItem container");
                    LotteryService.PerformWeightedLotteryWithContextAsync(weightedItems, lotteryCount, 0, true, context).Forget();
                }
                else
                {
                    Log.Debug("No weighted items found, falling back to simple lottery");
                    var itemIds = extractItemIds(__instance);
                    if (itemIds.Count == 0)
                    {
                        Log.Warning("UseToCreateItem_OnUse: No item IDs found in entries.");
                        return true;
                    }
                    LotteryService.PerformLotteryWithContextAsync(itemIds, lotteryCount, 0, true, context).Forget();
                }
            }
            else
            {
                // Use simple item ID extraction without weights
                var itemIds = extractItemIds(__instance);
                if (itemIds.Count == 0)
                {
                    Log.Warning("UseToCreateItem_OnUse: No item IDs found in entries.");
                    return true;
                }
                LotteryService.PerformLotteryWithContextAsync(itemIds, lotteryCount, 0, true, context).Forget();
            }

            return false; // skip the original method
        }
    }
}