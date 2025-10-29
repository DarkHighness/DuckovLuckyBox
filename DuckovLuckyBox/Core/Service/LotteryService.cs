using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using SodaCraft.Localizations;
using Duckov.UI;
using Duckov.Economy;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;
using Duckov;

namespace DuckovLuckyBox.Core
{
    /// <summary>
    /// Represents an item with its weight for weighted lottery
    /// </summary>
    public struct WeightedItem
    {
        public int ItemTypeId { get; set; }
        public float Weight { get; set; }

        public WeightedItem(int itemTypeId, float weight = 1f)
        {
            ItemTypeId = itemTypeId;
            Weight = weight > 0 ? weight : 1f;
        }

        public override string ToString() => $"ItemTypeId={ItemTypeId}, Weight={Weight}";
    }

    /// <summary>
    /// Interface for lottery context that defines custom behavior during lottery flow
    /// </summary>
    public interface ILotteryContext
    {
        /// <summary>
        /// Called before payment to validate lottery can proceed
        /// </summary>
        UniTask<bool> OnBeforeLotteryAsync();

        /// <summary>
        /// Called after successful lottery to perform domain-specific actions (e.g., stock decrement, event firing)
        /// </summary>
        void OnLotterySuccess(Item resultItem, bool sentToStorage);

        /// <summary>
        /// Called when lottery fails
        /// </summary>
        void OnLotteryFailed();
    }

    /// <summary>
    /// Lottery context for street pick operations
    /// </summary>
    public class DefaultLotteryContext : ILotteryContext
    {
        private const string SFX_BUY = "UI/buy";

        public async UniTask<bool> OnBeforeLotteryAsync()
        {
            // No special validation needed for street lottery
            return await UniTask.FromResult(true);
        }

        public void OnLotterySuccess(Item resultItem, bool sentToStorage)
        {
            if (resultItem == null) return;

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

        public void OnLotteryFailed()
        {
            // No special action needed on failure
        }
    }

    /// <summary>
    /// Provides lottery functionality for randomly selecting items using reservoir sampling algorithm
    /// Groups items by quality level, performs proportional sampling with optional quality-based weighting
    /// </summary>
    public static class LotteryService
    {
        // High level item threshold (>= Orange)
        private const ItemValueLevel HIGH_LEVEL_THRESHOLD = ItemValueLevel.Orange; // ItemValueLevel.Orange = 4

        // Consecutive failure threshold for probability adjustment
        private const int CONSECUTIVE_FAILURE_THRESHOLD = 5;

        // Probability bonus per excess failure for high level items (in thousandths)
        private const int HIGH_LEVEL_BONUS_PER_FAILURE = 50; // 50 thousandths = 5%

        // Counter for consecutive high level item failures
        private static int consecutiveHighLevelFailures = 0;
        /// <summary>
        /// Performs weighted random selection on a collection of items
        /// Used by animation and other UI components for weighted sampling
        /// </summary>
        /// <param name="weightedItems">Items with their weights. Weight must be > 0.</param>
        /// <returns>Selected item type ID, or -1 if failed</returns>
        public static int SampleWeightedItems(IEnumerable<WeightedItem> weightedItems)
        {
            var items = weightedItems?.ToList() ?? new List<WeightedItem>();

            if (items.Count == 0)
            {
                return -1;
            }

            // Calculate total weight, skipping invalid entries
            float totalWeight = 0f;
            var validItems = new List<WeightedItem>();

            foreach (var item in items)
            {
                if (item.Weight > 0)
                {
                    validItems.Add(item);
                    totalWeight += item.Weight;
                }
            }

            if (validItems.Count == 0 || totalWeight <= 0)
            {
                return -1;
            }

            // Weighted random selection using cumulative probability
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float accumulatedWeight = 0f;

            foreach (var item in validItems)
            {
                accumulatedWeight += item.Weight;
                if (randomValue <= accumulatedWeight)
                {
                    return item.ItemTypeId;
                }
            }

            // Fallback (should never reach here due to previous checks)
            return validItems[validItems.Count - 1].ItemTypeId;
        }

        /// <summary>
        /// Converts a list of item IDs to weighted items based on their quality
        /// Used by animation and UI components
        /// </summary>
        public static List<WeightedItem> ConvertToQualityWeightedItems(IEnumerable<int> itemTypeIds)
        {
            var result = new List<WeightedItem>();
            foreach (var id in itemTypeIds ?? Enumerable.Empty<int>())
            {
                var item = ItemUtils.LotteryItemCache.GetItem(id);
                if (item != null)
                {
                    var quality = QualityUtils.GetCachedItemValueLevel(item);
                    float probability = ProbabilityUtils.GetProbabilityForItemValueLevel(quality);
                    result.Add(new WeightedItem(id, probability));
                }
            }
            return result;
        }

        /// <summary>
        /// Performs a lottery using quality-level based reservoir sampling
        /// Groups items by quality level into a unified pool, then samples using reservoir sampling
        /// If a quality level has fewer items than needed, allows duplicate sampling
        /// </summary>
        /// <param name="count">Number of items to sample</param>
        /// <param name="useWeightedSampling">If true, uses quality-based probability weights; if false, uses uniform distribution across all items</param>
        /// <returns>List of selected item type IDs</returns>
        private static List<int> ReservoirSampleByQuality(IEnumerable<int> itemTypeIds, int count, bool useWeightedSampling)
        {
            var result = new List<int>();
            if (useWeightedSampling)
            {
                // Get all lottery items grouped by quality level
                var itemsByQuality = new Dictionary<ItemValueLevel, List<int>>();
                foreach (ItemValueLevel level in Enum.GetValues(typeof(ItemValueLevel)))
                {
                    itemsByQuality[level] = new List<int>();
                }

                foreach (var id in itemTypeIds)
                {
                    var quality = ItemUtils.GameItemCache.GetItemQuality(id);
                    itemsByQuality[quality].Add(id);
                }

                if (itemsByQuality.Count == 0)
                {
                    Log.Error("No items available for lottery");
                    return result;
                }

                // Remove empty quality levels
                var nonEmptyItemsByQuality = itemsByQuality
                    .Where(kvp => kvp.Value.Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // Use quality-based probability weights
                for (int i = 0; i < count; i++)
                {
                    // Create weighted list of quality levels
                    var qualityWeights = new List<WeightedItem>();
                    foreach (var level in nonEmptyItemsByQuality.Keys)
                    {
                        float probability = ProbabilityUtils.GetProbabilityForItemValueLevel(level);
                        // Apply bonus for high level items if consecutive failures exceed threshold
                        if (consecutiveHighLevelFailures >= CONSECUTIVE_FAILURE_THRESHOLD && (int)level >= (int)HIGH_LEVEL_THRESHOLD)
                        {
                            int excessFailures = consecutiveHighLevelFailures - CONSECUTIVE_FAILURE_THRESHOLD + 1;
                            probability += excessFailures * HIGH_LEVEL_BONUS_PER_FAILURE;
                        }
                        qualityWeights.Add(new WeightedItem((int)level, probability));
                    }

                    // Select a quality level based on probability
                    int qualityValue = SampleWeightedItems(qualityWeights);
                    if (qualityValue < 0) break;

                    var selectedQuality = (ItemValueLevel)qualityValue;
                    if (!nonEmptyItemsByQuality.TryGetValue(selectedQuality, out var itemsAtLevel))
                        continue;

                    // Sample an item from the selected quality level with repetition allowed
                    int itemIndex = UnityEngine.Random.Range(0, itemsAtLevel.Count);
                    result.Add(itemsAtLevel[itemIndex]);
                }
            }
            else
            {
                // Uniform distribution: direct uniform sampling from all item IDs
                var allItems = itemTypeIds.ToList();
                if (allItems.Count == 0)
                {
                    Log.Error("No items available for lottery");
                    return result;
                }
                result = ProbabilityUtils.ReservoirSample(allItems, count, allowDuplicates: true);
            }

            return result;
        }

        /// <summary>
        /// Performs a complete lottery flow with context support
        /// Uses reservoir sampling for even quality distribution when sampling
        /// </summary>
        /// <param name="price">Price to charge (0 for free)</param>
        /// <param name="playAnimation">Whether to play lottery animation</param>
        /// <param name="context">Context for handling payment, success/failure callbacks</param>
        /// <returns>Tuple of (success, item, sentToStorage)</returns>
        public static async UniTask<(bool success, Item? item, bool sentToStorage)> PerformLotteryWithContextAsync(
            IEnumerable<int> itemTypeIds,
            long price = 0,
            bool playAnimation = true,
            ILotteryContext? context = null)
        {
            bool useWeightedSampling = SettingManager.Instance.EnableWeightedLottery.GetAsBool();

            // Use reservoir sampling to select one item
            var sampledIds = ReservoirSampleByQuality(itemTypeIds, 1, useWeightedSampling);
            if (sampledIds.Count == 0)
            {
                Log.Error("Failed to sample item for lottery");
                context?.OnLotteryFailed();
                return (false, null, false);
            }

            int selectedItemTypeId = sampledIds[0];

            // Call context pre-lottery hook
            if (context != null && !await context.OnBeforeLotteryAsync())
            {
                context.OnLotteryFailed();
                return (false, null, false);
            }

            // Charge player
            if (price > 0)
            {
                if (!EconomyManager.Pay(new Cost(price), true, true))
                {
                    var notEnoughMoneyMessage = Localizations.I18n.NotEnoughMoneyFormatKey.ToPlainText().Replace("{price}", price.ToString());
                    NotificationText.Push(notEnoughMoneyMessage);
                    context?.OnLotteryFailed();
                    return (false, null, false);
                }
            }

            // Instantiate item
            Item? item = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
            if (item == null)
            {
                Log.Error($"Failed to instantiate lottery item: {selectedItemTypeId}");
                context?.OnLotteryFailed();
                return (false, null, false);
            }

            // Play animation if requested
            if (playAnimation)
            {
                await LotteryAnimation.Instance.PlayAsync(itemTypeIds, selectedItemTypeId, item.DisplayName, item.Icon);
            }

            // Send to inventory or storage
            var sentToStorage = !ItemUtilities.SendToPlayerCharacterInventory(item);
            if (sentToStorage)
            {
                Log.Warning($"Failed to send item to player inventory: {selectedItemTypeId}. Sending to storage.");
                ItemUtilities.SendToPlayerStorage(item);
            }

            // Call context success hook
            context?.OnLotterySuccess(item, sentToStorage);

            // Update consecutive high level failure counter
            var itemLevel = QualityUtils.GetCachedItemValueLevel(item);
            if ((int)itemLevel >= (int)HIGH_LEVEL_THRESHOLD)
            {
                consecutiveHighLevelFailures = 0; // Reset on high level item
            }
            else
            {
                consecutiveHighLevelFailures++; // Increment on non-high level item
            }

            return (true, item, sentToStorage);
        }

        public static async UniTask<List<Item>> PickRandomItemsByQualityAsync(ItemValueLevel level, int count)
        {
            var result = new List<Item>();
            var qualityItems = ItemUtils.LotteryItemCache.GetItemTypeIdsByValueLevel(level);

            if (qualityItems.Count == 0)
            {
                Log.Warning($"No items found with value level {level}");
                return result;
            }

            Log.Debug($"Found {qualityItems.Count} items with value level {level}");

            // Pick random items with repetition allowed
            for (int i = 0; i < count; i++)
            {
                var selectedIndex = UnityEngine.Random.Range(0, qualityItems.Count);
                var selectedItemTypeId = qualityItems[selectedIndex];

                Item? obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
                if (obj == null)
                {
                    Log.Error($"Failed to instantiate item: {selectedItemTypeId}");
                    continue;
                }

                result.Add(obj);
                Log.Debug($"Picked item {i + 1}/{count}: {obj.DisplayName} (value level {level})");
            }

            return result;
        }
    }
}
