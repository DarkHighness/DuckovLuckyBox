using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
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

            var itemIds = extractItemIds(__instance);
            if (itemIds.Count == 0)
            {
                Log.Warning("UseToCreateItem_OnUse: No item IDs found in entries.");
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

            // Play animation
            var context = new DefaultLotteryContext();
            LotteryService.PerformLotteryWithContextAsync(itemIds, lotteryCount, 0, true, context).Forget();
            return false; // skip the original method
        }
    }
}