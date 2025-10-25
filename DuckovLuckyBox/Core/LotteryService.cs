using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using UnityEngine;
using SodaCraft.Localizations;
using Duckov.UI;
using Duckov.Economy;
using HarmonyLib;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;
using Duckov;

namespace DuckovLuckyBox.Core
{
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
                        // (5) whose catagory is "Quest"
                        // to avoid illegal items.
                        .Where(entry => !entry.prefab.DisplayName.StartsWith("*Item_") &&
                                       !entry.prefab.Description.StartsWith("*Item_") &&
                                       entry.prefab.Quality > 0 &&
                                       entry.prefab.Icon.name != "cross" &&
                                       entry.metaData.Catagory != "Quest")
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
            var pool = candidateTypeIds?.ToList() ?? new List<int>();

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

        /// <summary>
        /// Performs a complete lottery flow with context support
        /// </summary>
        /// <param name="candidateTypeIds">Pool of item type IDs to choose from. If null/empty, uses ItemTypeIdsCache.</param>
        /// <param name="price">Price to charge (0 for free)</param>
        /// <param name="playAnimation">Whether to play lottery animation</param>
        /// <param name="context">Context for handling payment, success/failure callbacks</param>
        /// <returns>Tuple of (success, item, sentToStorage)</returns>
        public static async UniTask<(bool success, Item? item, bool sentToStorage)> PerformLotteryWithContextAsync(
            IEnumerable<int>? candidateTypeIds = null,
            long price = 0,
            bool playAnimation = true,
            ILotteryContext? context = null)
        {
            var candidateList = candidateTypeIds?.ToList() ?? new List<int>();

            // Use cached items if no candidates provided
            if (candidateList.Count == 0)
            {
                candidateList = ItemTypeIdsCache;
                Log.Debug("Using cached item type IDs for lottery candidates");
            }

            if (candidateList.Count == 0)
            {
                Log.Warning("No candidate items for lottery");
                context?.OnLotteryFailed();
                return (false, null, false);
            }

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

            // Pick random item
            var selectedItemTypeId = PickRandomItem(candidateList);
            if (selectedItemTypeId < 0)
            {
                Log.Error("Failed to pick item for lottery");
                context?.OnLotteryFailed();
                return (false, null, false);
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
                await LotteryAnimation.PlayAsync(candidateList, selectedItemTypeId, item.DisplayName, item.Icon);
            }

            // Send to inventory or storage
            var sentToStorage = false;
            if (!ItemUtilities.SendToPlayerCharacterInventory(item))
            {
                Log.Warning($"Failed to send item to player inventory: {selectedItemTypeId}. Sending to storage.");
                ItemUtilities.SendToPlayerStorage(item);
                sentToStorage = true;
            }

            // Call context success hook
            context?.OnLotterySuccess(item, sentToStorage);

            return (true, item, sentToStorage);
        }
    }
}
