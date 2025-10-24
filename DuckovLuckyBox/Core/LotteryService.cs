using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using UnityEngine;

namespace DuckovLuckyBox.Core
{
    /// <summary>
    /// Provides lottery functionality for randomly selecting items from a pool
    /// </summary>
    public static class LotteryService
    {
        private static List<int>? _itemTypeIdsCache = null;

        /// <summary>
        /// Gets cached list of valid item type IDs that can be used in lottery
        /// </summary>
        public static List<int> ItemTypeIdsCache
        {
            get
            {
                if (_itemTypeIdsCache == null)
                {
                    _itemTypeIdsCache = ItemAssetsCollection.Instance.entries
                        // We predictably exclude items
                        // (1) whose display name is in the form of "*Item_*"
                        // (2) whose description is in the form of "*Item_*"
                        // (3) whose quality is 0 (junk items)
                        // (4) whose icon is the default "cross" icon
                        // to avoid illegal items.
                        .Where(entry => !entry.prefab.DisplayName.StartsWith("*Item_") &&
                                       !entry.prefab.Description.StartsWith("*Item_") &&
                                       entry.prefab.Quality > 0 &&
                                       entry.prefab.Icon.name != "cross")
                        .Select(entry => entry.typeID)
                        .ToList();
                }
                return _itemTypeIdsCache;
            }
        }

        /// <summary>
        /// Picks a random item from the specified pool
        /// </summary>
        /// <param name="candidateTypeIds">Pool of item type IDs to choose from</param>
        /// <returns>Selected item type ID</returns>
        public static int PickRandomItem(IEnumerable<int> candidateTypeIds)
        {
            var pool = candidateTypeIds?.Distinct().Where(id => id > 0).ToList() ?? new List<int>();

            if (pool.Count == 0)
            {
                Log.Warning("Empty item pool for lottery, using default cache");
                pool = ItemTypeIdsCache;
            }

            if (pool.Count == 0)
            {
                Log.Error("No valid items available for lottery");
                return -1;
            }

            var selectedIndex = UnityEngine.Random.Range(0, pool.Count);
            return pool[selectedIndex];
        }

        /// <summary>
        /// Picks a random item and instantiates it
        /// </summary>
        /// <param name="candidateTypeIds">Pool of item type IDs to choose from</param>
        /// <returns>Instantiated item, or null if failed</returns>
        public static async UniTask<Item?> PickRandomItemAsync(IEnumerable<int> candidateTypeIds)
        {
            var selectedItemTypeId = PickRandomItem(candidateTypeIds);

            if (selectedItemTypeId < 0)
            {
                return null;
            }

            Item obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
            if (obj == null)
            {
                Log.Error($"Failed to instantiate lottery item: {selectedItemTypeId}");
                return null;
            }

            return obj;
        }

        /// <summary>
        /// Picks multiple random items of the specified quality and instantiates them
        /// </summary>
        /// <param name="quality">Quality level to filter items by</param>
        /// <param name="count">Number of items to pick</param>
        /// <returns>List of instantiated items</returns>
        public static async UniTask<List<Item>> PickRandomItemsByQualityAsync(int quality, int count)
        {
            var result = new List<Item>();

            // Get all valid items of the specified quality from cache
            var qualityItems = ItemTypeIdsCache
                .Select(typeId => new { TypeId = typeId, Item = GetItem(typeId) })
                .Where(x => x.Item != null && x.Item.Quality == quality)
                .Select(x => x.TypeId)
                .ToList();

            if (qualityItems.Count == 0)
            {
                Log.Warning($"No items found with quality {quality}");
                return result;
            }

            Log.Debug($"Found {qualityItems.Count} items with quality {quality}");

            // Pick random items
            for (int i = 0; i < count; i++)
            {
                var selectedIndex = UnityEngine.Random.Range(0, qualityItems.Count);
                var selectedItemTypeId = qualityItems[selectedIndex];

                Item? obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
                if (obj == null)
                {
                    Log.Error($"Failed to instantiate lottery item: {selectedItemTypeId}");
                    continue;
                }

                result.Add(obj);
                Log.Debug($"Picked item {i + 1}/{count}: {obj.DisplayName} (quality {quality})");
            }

            return result;
        }

        /// <summary>
        /// Gets item display information
        /// </summary>
        public static Item? GetItem(int typeId)
        {
            try
            {
                var entry = ItemAssetsCollection.Instance.entries.FirstOrDefault(e => e != null && e.typeID == typeId);
                return entry?.prefab;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get item for typeId {typeId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets item icon sprite
        /// </summary>
        public static Sprite? GetItemIcon(int typeId)
        {
            var item = GetItem(typeId);
            return item?.Icon;
        }

        /// <summary>
        /// Gets item quality level
        /// </summary>
        public static int GetItemQuality(int typeId)
        {
            var item = GetItem(typeId);
            return item?.Quality ?? 0;
        }

        /// <summary>
        /// Gets item display name
        /// </summary>
        public static string GetDisplayName(int typeId)
        {
            var item = GetItem(typeId);
            return item?.DisplayName ?? $"#{typeId}";
        }

        /// <summary>
        /// Item quality color palette
        /// </summary>
        private static readonly Color[] ItemQualityColors = new Color[]
        {
            // Reference: https://github.com/shiquda/duckov-fancy-items/blob/7d3094cf40f1b01bbd08f0c8e05d341672b4fb33/fancy-items/Constants/FancyItemsConstants.cs#L56
            new Color(0.5f, 0.5f, 0.5f, 0.5f),      // Quality 0: 灰色
            new Color(0.9f, 0.9f, 0.9f, 0.24f),     // Quality 1: 浅白色
            new Color(0.6f, 0.9f, 0.6f, 0.24f),     // Quality 2: 柔和浅绿
            new Color(0.6f, 0.8f, 1.0f, 0.30f),     // Quality 3: 天蓝浅色
            new Color(1.0f, 0.50f, 1.0f, 0.40f),   // Quality 4: 亮浅紫（提亮,略粉）
            new Color(1.0f, 0.75f, 0.2f, 0.60f),   // Quality 5: 柔亮橙（更偏橙、更暖）
            new Color(1.0f, 0.3f, 0.3f, 0.4f),     // Quality 6+: 明亮红（亮度提升、透明度降低）
        };

        /// <summary>
        /// Gets color associated with item quality
        /// </summary>
        public static Color GetItemQualityColor(int typeId)
        {
            var quality = GetItemQuality(typeId);
            if (quality < 0 || quality >= ItemQualityColors.Length)
            {
                quality = 0;
            }
            return ItemQualityColors[quality];
        }
    }
}
