using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using UnityEngine;

namespace DuckovLuckyBox.Core
{
    /// <summary>
    /// Service for managing item recycling and sorting operations
    /// Handles category lookups and item quality mappings for Recycle and similar features
    /// </summary>
    public static class RecycleService
    {
        // Number of bullets per logical group for recycling
        public const int BulletGroupSize = 30;

        private static Dictionary<string, Dictionary<ItemValueLevel, List<Item>>>? _itemLookupByCategoryAndQuality = null;

        /// <summary>
        /// Gets a lookup dictionary mapping categories and quality levels to items
        /// Used for efficient category and quality-based item queries
        /// </summary>
        public static Dictionary<string, Dictionary<ItemValueLevel, List<Item>>> ItemLookupByCategoryAndQuality
        {
            get
            {
                if (_itemLookupByCategoryAndQuality == null)
                {
                    _itemLookupByCategoryAndQuality = new Dictionary<string, Dictionary<ItemValueLevel, List<Item>>>();
                    foreach (var entry in ItemUtils.RecycleItemCache.Entries)
                    {
                        var item = entry.Item;
                        var category = entry.MetaData.Catagory;
                        var valueLevel = entry.ValueLevel;

                        if (!_itemLookupByCategoryAndQuality.ContainsKey(category))
                        {
                            _itemLookupByCategoryAndQuality[category] = new Dictionary<ItemValueLevel, List<Item>>();
                        }

                        if (!_itemLookupByCategoryAndQuality[category].ContainsKey(valueLevel))
                        {
                            _itemLookupByCategoryAndQuality[category][valueLevel] = new List<Item>();
                        }

                        // Add item to the list for this category & quality
                        _itemLookupByCategoryAndQuality[category][valueLevel].Add(item);
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
            return qualityMap.TryGetValue(nextLevel, out var nextItems) && nextItems != null && nextItems.Count > 0;
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
                if (qualityMap != null && qualityMap.TryGetValue(level, out var items) && items != null && items.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a random item from the specified categories that matches the desired value level.
        /// </summary>
        /// <summary>
        /// Pick a random item from the specified categories at the given quality level.
        /// If materialSources is provided, selection is biased toward the most frequent
        /// material categories found in materialSources (tries most-common first, then next).
        /// If no biased candidate is found, falls back to uniform random among all candidates.
        /// </summary>
        public static async UniTask<Item?> PickRandomItemByCategoriesAndQualityAsync(IEnumerable<string> categories, ItemValueLevel level, IEnumerable<Item>? materialSources = null)
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

            var candidateEntries = ItemUtils.RecycleItemCache.Entries
                .Where(entry => entry.MetaData.Catagory != "Bullet" && categorySet.Contains(entry.MetaData.Catagory))
                .Where(entry => entry.ValueLevel == level)
                .ToList();

            var candidateTypeIds = candidateEntries.Select(e => e.Item.TypeID).ToList();

            if (candidateTypeIds.Count == 0)
            {
                return null;
            }

            // If no material bias requested, pick uniform random
            if (materialSources == null)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidateTypeIds.Count);
                int selectedItemTypeId = candidateTypeIds[randomIndex];
                Item? obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
                return obj;
            }

            // Build frequency map of source item categories (material types)
            var materialCategories = materialSources
                .Where(i => i != null)
                .Select(i => GetItemCategory(i.TypeID))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Try each material category in order of frequency to bias selection
            foreach (var mat in materialCategories)
            {
                var biased = candidateEntries
                    .Where(e => e.MetaData.Catagory == mat.Category)
                    .Select(e => e.Item.TypeID)
                    .ToList();

                if (biased.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, biased.Count);
                    int selectedItemTypeId = biased[idx];
                    Item? obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
                    return obj;
                }
            }

            // No biased candidate found; fallback to uniform random across all candidates
            int fallbackIndex = UnityEngine.Random.Range(0, candidateTypeIds.Count);
            int fallbackTypeId = candidateTypeIds[fallbackIndex];
            Item? fallbackObj = await ItemAssetsCollection.InstantiateAsync(fallbackTypeId);
            return fallbackObj;
        }

        /// <summary>
        /// Pick a random bullet item (full stack) at the specified quality level.
        /// Returns an item whose StackCount is set to its MaxStackCount.
        /// </summary>
        public static async UniTask<Item?> PickRandomBulletStackByQualityAsync(ItemValueLevel level)
        {
            var candidateEntries = ItemUtils.BulletItemCache.Entries
                .Where(entry => entry.ValueLevel == level)
                .ToList();

            if (candidateEntries.Count == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, candidateEntries.Count);
            int selectedItemTypeId = candidateEntries[randomIndex].Item.TypeID;

            Item? obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
            if (obj != null)
            {
                // set to configured bullet group size (but never exceed the item's MaxStackCount)
                obj.StackCount = Math.Min(BulletGroupSize, obj.MaxStackCount);
            }

            return obj;
        }

        /// <summary>
        /// Check whether an item can be recycled given a set of supported reward categories.
        /// Conditions:
        /// 1. Item's category must be one of supportedCategories.
        /// 2. There exists at least one item at item level + 1 in supportedCategories.
        /// 3. If item is Bullet, it must be a full stack (submit whole stack) and the next-level bullet must exist.
        /// </summary>
        public static bool CanRecycleItem(Item? item, IEnumerable<string> supportedCategories)
        {
            if (item == null) return false;
            if (supportedCategories == null) return false;

            var category = GetItemCategory(item.TypeID);
            var supportedSet = new HashSet<string>(supportedCategories.Where(c => !string.IsNullOrWhiteSpace(c)), StringComparer.OrdinalIgnoreCase);
            if (!supportedSet.Contains(category)) return false;

            ItemValueLevel currentLevel = QualityUtils.GetCachedItemValueLevel(item);
            int nextLevelValue = (int)currentLevel + 1;
            if (!Enum.IsDefined(typeof(ItemValueLevel), nextLevelValue)) return false;
            var targetLevel = (ItemValueLevel)nextLevelValue;

            // For bullets, require submission of a full logical bullet group.
            // Some bullet types may have MaxStackCount < BulletGroupSize, so use the smaller of the two.
            if (string.Equals(category, "Bullet", StringComparison.OrdinalIgnoreCase))
            {
                // If bullet, require stackable and at least one full group
                if (!item.Stackable) return false;
                int requiredGroupSize = Math.Min(BulletGroupSize, item.MaxStackCount);
                if (item.StackCount < requiredGroupSize) return false;

                // Ensure there is a bullet at next level in supported categories
                return HasCategoryItemAtLevel(new[] { category }, targetLevel);
            }

            // Generic case: ensure at least one item at next level exists among supported categories
            return HasCategoryItemAtLevel(supportedSet, targetLevel);
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
        /// Gets item category
        /// </summary>
        public static string GetItemCategory(int typeId)
        {
            return ItemUtils.GameItemCache.GetItemCategory(typeId);
        }

        /// <summary>
        /// Gets item display information
        /// </summary>
        public static Item? GetItem(int typeId)
        {
            return ItemUtils.GameItemCache.GetItem(typeId);
        }

        /// <summary>
        /// Gets item display name
        /// </summary>
        public static string GetDisplayName(int typeId)
        {
            return ItemUtils.GameItemCache.GetDisplayName(typeId);
        }

        /// <summary>
        /// Gets item quality level
        /// </summary>
        public static ItemValueLevel GetItemQuality(int typeId)
        {
            return ItemUtils.GameItemCache.GetItemQuality(typeId);
        }

        /// <summary>
        /// Gets item icon sprite
        /// </summary>
        public static Sprite? GetItemIcon(int typeId)
        {
            return ItemUtils.GameItemCache.GetItemIcon(typeId);
        }

        /// <summary>
        /// Gets color associated with item quality
        /// </summary>
        public static Color GetItemQualityColor(int typeId)
        {
            return ItemUtils.GameItemCache.GetItemQualityColor(typeId);
        }
    }
}
