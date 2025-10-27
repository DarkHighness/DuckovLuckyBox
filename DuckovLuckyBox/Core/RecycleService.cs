using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using UnityEngine;
using SodaCraft.Localizations;
using Duckov.UI;
using DuckovLuckyBox.Core.Settings;

namespace DuckovLuckyBox.Core
{
    /// <summary>
    /// Service for managing item recycling and sorting operations
    /// Handles category lookups and item quality mappings for CycleBin and similar features
    /// </summary>
    public static class RecycleService
    {
        private static Dictionary<string, Dictionary<ItemValueLevel, Item>>? _itemLookupByCategoryAndQuality = null;

        /// <summary>
        /// Gets a lookup dictionary mapping categories and quality levels to items
        /// Used for efficient category and quality-based item queries
        /// </summary>
        public static Dictionary<string, Dictionary<ItemValueLevel, Item>> ItemLookupByCategoryAndQuality
        {
            get
            {
                if (_itemLookupByCategoryAndQuality == null)
                {
                    _itemLookupByCategoryAndQuality = new Dictionary<string, Dictionary<ItemValueLevel, Item>>();
                    foreach (var entry in ItemAssetsCollection.Instance.entries)
                    {
                        var item = entry.prefab;
                        var category = entry.metaData.Catagory;
                        var quality = QualityUtils.GetCachedItemValueLevel(item);

                        if (!_itemLookupByCategoryAndQuality.ContainsKey(category))
                        {
                            _itemLookupByCategoryAndQuality[category] = new Dictionary<ItemValueLevel, Item>();
                        }

                        if (!_itemLookupByCategoryAndQuality[category].ContainsKey(quality))
                        {
                            _itemLookupByCategoryAndQuality[category][quality] = item;
                        }
                    }
                }
                return _itemLookupByCategoryAndQuality;
            }
        }

        /// <summary>
        /// Determines whether a given category contains an item whose value level is exactly one tier higher than the provided baseline.
        /// </summary>
        public static bool HasHigherQualityItemInCategory(string category, ItemValueLevel baseQuality)
        {
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }

            if (!ItemLookupByCategoryAndQuality.TryGetValue(category, out var qualityMap) || qualityMap == null)
            {
                return false;
            }

            int nextLevelValue = (int)baseQuality + 1;
            if (!Enum.IsDefined(typeof(ItemValueLevel), nextLevelValue))
            {
                return false;
            }

            var nextLevel = (ItemValueLevel)nextLevelValue;
            return qualityMap.TryGetValue(nextLevel, out var nextItem) && nextItem != null;
        }

        /// <summary>
        /// Determines whether any of the specified categories contains an item at the provided value level.
        /// </summary>
        public static bool HasCategoryItemAtLevel(IEnumerable<string> categories, ItemValueLevel level)
        {
            if (categories == null)
            {
                return false;
            }

            var categorySet = new HashSet<string>(categories.Where(c => !string.IsNullOrWhiteSpace(c)), StringComparer.OrdinalIgnoreCase);
            if (categorySet.Count == 0)
            {
                return false;
            }

            foreach (var kvp in ItemLookupByCategoryAndQuality)
            {
                if (!categorySet.Contains(kvp.Key))
                {
                    continue;
                }

                var qualityMap = kvp.Value;
                if (qualityMap != null && qualityMap.TryGetValue(level, out var item) && item != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a random item from the specified categories that matches the desired value level.
        /// </summary>
        public static async UniTask<Item?> PickRandomItemByCategoriesAndQualityAsync(IEnumerable<string> categories, ItemValueLevel level)
        {
            if (categories == null)
            {
                return null;
            }

            var categorySet = new HashSet<string>(categories.Where(c => !string.IsNullOrWhiteSpace(c)), StringComparer.OrdinalIgnoreCase);
            if (categorySet.Count == 0)
            {
                return null;
            }

            var candidateTypeIds = ItemAssetsCollection.Instance.entries
                .Where(entry => entry != null && entry.prefab != null && categorySet.Contains(entry.metaData.Catagory))
                .Where(entry => QualityUtils.GetCachedItemValueLevel(entry.prefab) == level)
                .Select(entry => entry.typeID)
                .ToList();

            if (candidateTypeIds.Count == 0)
            {
                return null;
            }

            // Use uniform random selection for recycling
            int randomIndex = UnityEngine.Random.Range(0, candidateTypeIds.Count);
            int selectedItemTypeId = candidateTypeIds[randomIndex];

            Item? obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
            return obj;
        }

        /// <summary>
        /// Gets item category
        /// </summary>
        public static string GetItemCategory(int typeId)
        {
            var entry = ItemAssetsCollection.Instance.entries.FirstOrDefault(e => e != null && e.typeID == typeId);
            return entry?.metaData.Catagory ?? "Unknown";
        }

        /// <summary>
        /// Gets a random item from the lottery pool
        /// </summary>
        public static async UniTask<Item?> PickRandomLotteryItemAsync()
        {
            var allItemIds = ItemUtils.LotteryItemCache.GetAllItemTypeIds();
            if (allItemIds.Count == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, allItemIds.Count);
            int selectedItemTypeId = allItemIds[randomIndex];

            Item? obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
            return obj;
        }

        /// <summary>
        /// Picks multiple random items of the specified quality level
        /// </summary>
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

        /// <summary>
        /// Gets item display information
        /// </summary>
        public static Item? GetItem(int typeId)
        {
            return ItemUtils.LotteryItemCache.GetItem(typeId);
        }

        /// <summary>
        /// Gets item display name
        /// </summary>
        public static string GetDisplayName(int typeId)
        {
            return ItemUtils.LotteryItemCache.GetDisplayName(typeId);
        }

        /// <summary>
        /// Gets item quality level
        /// </summary>
        public static ItemValueLevel GetItemQuality(int typeId)
        {
            return ItemUtils.LotteryItemCache.GetItemQuality(typeId);
        }

        /// <summary>
        /// Gets item icon sprite
        /// </summary>
        public static Sprite? GetItemIcon(int typeId)
        {
            return ItemUtils.LotteryItemCache.GetItemIcon(typeId);
        }

        /// <summary>
        /// Gets color associated with item quality
        /// </summary>
        public static Color GetItemQualityColor(int typeId)
        {
            return ItemUtils.LotteryItemCache.GetItemQualityColor(typeId);
        }
    }
}
