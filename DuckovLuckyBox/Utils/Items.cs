using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DuckovLuckyBox.Core;
using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;

namespace DuckovLuckyBox
{

    using ItemPredicate = Func<Entry, bool>;

    public class Entry
    {
        public Item Item;
        public ItemMetaData MetaData;
        public ItemValueLevel ValueLevel => QualityUtils.GetCachedItemValueLevel(Item);

        public Entry(Item item, ItemMetaData metaData)
        {
            Item = item;
            MetaData = metaData;
        }
    }


    public static class ItemUtils
    {

        public static readonly string[] RecyclableCategories =
        {
        "Drink", // Addon
        "Special",
        "JLab",
        "Key",
        "Daily",
        "Gem",
        "Food",
        "Headset",
        "Accessory",
        "Backpack",
        "Weapon",
        "MeleeWeapon",
        "Helmat",
        "Medic",
        "FaceMask",
        "Armor",
        "Luxury",
        "Injector",
        "Electric",
        "Totem",
        "Tool",
        "Bullet"
      };

        public static List<Entry> QueryGameItems(ItemPredicate predicate, bool includeDynamicItems = true)
        {
            var result = new List<Entry>();
            var itemDatabase = ItemAssetsCollection.Instance.entries;
            if (itemDatabase != null)
            {
                Log.Info($"[ItemUtils.QueryGameItems] Found item database with {itemDatabase.Count} entries");
                foreach (var item in itemDatabase)
                {
                    var entry = new Entry(item.prefab, item.metaData);
                    if (predicate(entry))
                    {
                        result.Add(entry);
                    }
                }
            }
            else
            {
                Log.Warning("[ItemUtils.QueryGameItems] Item database is null!");
            }

            if (includeDynamicItems)
            {
                var dynamicDicField = AccessTools.Field(typeof(ItemAssetsCollection), "dynamicDic");
                if (dynamicDicField != null)
                {
                    var dynamicDicValue = dynamicDicField.GetValue(ItemAssetsCollection.Instance);
                    if (dynamicDicValue is Dictionary<int, ItemAssetsCollection.DynamicEntry> dynamicItems)
                    {
                        Log.Info($"[ItemUtils.QueryGameItems] Found dynamic items dictionary with {dynamicItems.Count} entries");
                        foreach (var kvp in dynamicItems)
                        {
                            var dynamicEntry = kvp.Value;
                            if (dynamicEntry != null)
                            {
                                var dynamicItem = dynamicEntry.prefab;
                                var dynamicMetaData = dynamicEntry.MetaData;

                                if (dynamicItem != null)
                                {
                                    var entry = new Entry(dynamicItem, dynamicMetaData);
                                    if (predicate(entry))
                                    {
                                        result.Add(entry);
                                    }
                                }
                                else
                                {
                                    Log.Warning($"[ItemUtils.QueryGameItems] Dynamic item is null for entry {kvp.Key}");
                                }
                            }
                            else
                            {
                                Log.Warning($"[ItemUtils.QueryGameItems] Dynamic entry is null for key {kvp.Key}");
                            }
                        }
                    }
                    else
                    {
                        Log.Warning("[ItemUtils.QueryGameItems] Dynamic items dictionary is not of expected type or null");
                    }
                }
                else
                {
                    Log.Warning("[ItemUtils.QueryGameItems] dynamicDic field not found via reflection");
                }
            }

            Log.Info($"[ItemUtils.QueryGameItems] Query completed. Total entries: {result.Count}");
            return result;
        }

        public static ItemPredicate DefaultItemPredicate = (entry) => true;

        public static ItemPredicate LotteryItemPredicate = (entry) =>
        {
            // 1: Exclude items that the quality is below 0
            if (entry.Item.Quality < 0)
            {
                return false;
            }

            // 2: Exclude items that are Quest items
            if (entry.MetaData.Catagory == "Quest")
            {
                return false;
            }

            // 3. Exclude items that icon is the default "cross" icon
            if (entry.Item.Icon == null || entry.Item.Icon.name == "cross")
            {
                return false;
            }

            // 4. Exclude items that not translated
            if (entry.Item.DisplayName.StartsWith("*Item_"))
            {
                return false;
            }

            // 5. Exclude items that the description is not translated
            if (entry.Item.Description.StartsWith("*Item_"))
            {
                return false;
            }

            return true;
        };

        public static ItemPredicate RecycleItemPredicate = (entry) =>
        {
            // 1. If the item is not lottery item, exclude it
            if (!LotteryItemPredicate(entry))
            {
                return false;
            }

            // 2. If the item is the Cash, exclude it
            if (entry.Item.TypeID == 451) // Cash TypeID
            {
                return false;
            }

            return true;
        };

        public static ItemPredicate BulletItemPredicate = (entry) =>
        {
            // 1. If the item is not lottery item, exclude it
            if (!LotteryItemPredicate(entry))
            {
                return false;
            }

            // 2. If the item is not a bullet, exclude it
            if (entry.MetaData.Catagory != "Bullet")
            {
                return false;
            }

            return true;
        };

        public class ItemQueryCache
        {
            private List<Entry> _items;
            private Dictionary<int, Entry> _itemCache;
            private Dictionary<ItemValueLevel, List<int>> _itemValueLevelToTypeIdsCache = new Dictionary<ItemValueLevel, List<int>>();

            public IEnumerable<Entry> Entries => _items;
            public string Name;

            public ItemQueryCache(string Name, ItemPredicate predicate, bool includeDynamicItems = true)
            {
                this.Name = Name;
                _items = QueryGameItems(predicate, includeDynamicItems);
                _itemCache = new Dictionary<int, Entry>();
                _itemValueLevelToTypeIdsCache = new Dictionary<ItemValueLevel, List<int>>();

                foreach (var entry in _items)
                {
                    _itemCache[entry.Item.TypeID] = entry;
                    if (!_itemValueLevelToTypeIdsCache.ContainsKey(entry.ValueLevel))
                    {
                        _itemValueLevelToTypeIdsCache[entry.ValueLevel] = new List<int>();
                    }

                    _itemValueLevelToTypeIdsCache[entry.ValueLevel].Add(entry.Item.TypeID);
                }
            }

            public Item? GetItemById(int typeId)
            {
                if (_itemCache.TryGetValue(typeId, out var entry))
                {
                    return entry.Item;
                }

                Log.Warning($"ItemQueryCache[{Name}] - Item with typeId {typeId} not found in cache.");
                return null;
            }

            public ItemMetaData? GetItemMetaDataById(int typeId)
            {
                if (_itemCache.TryGetValue(typeId, out var entry))
                {
                    return entry.MetaData;
                }

                Log.Warning($"ItemQueryCache[{Name}] - ItemMetaData with typeId {typeId} not found in cache.");
                return null;
            }

            public ItemValueLevel? GetItemValueLevelById(int typeId)
            {
                if (_itemCache.TryGetValue(typeId, out var entry))
                {
                    return entry.ValueLevel;
                }

                Log.Warning($"ItemQueryCache[{Name}] - ItemValueLevel with typeId {typeId} not found in cache.");
                return null;
            }

            public List<int> GetItemTypeIdsByValueLevel(ItemValueLevel valueLevel)
            {
                if (_itemValueLevelToTypeIdsCache.TryGetValue(valueLevel, out var typeIdList))
                {
                    return typeIdList;
                }

                Log.Warning($"ItemQueryCache[{Name}] - No items found for ItemValueLevel {valueLevel}.");
                return new List<int>();
            }

            public List<int> GetAllItemTypeIds()
            {
                return new List<int>(_itemCache.Keys);
            }

            /// <summary>
            /// Gets item display information
            /// </summary>
            public Item? GetItem(int typeId)
            {
                return GetItemById(typeId);
            }

            /// <summary>
            /// Gets item display name
            /// </summary>
            public string GetDisplayName(int typeId)
            {
                var item = GetItem(typeId);
                return item?.DisplayName ?? $"#{typeId}";
            }

            /// <summary>
            /// Gets item quality level
            /// </summary>
            public ItemValueLevel GetItemQuality(int typeId)
            {
                var item = GetItem(typeId);
                if (item == null)
                {
                    return ItemValueLevel.White;
                }
                return QualityUtils.GetCachedItemValueLevel(item);
            }

            /// <summary>
            /// Gets item icon sprite
            /// </summary>
            public Sprite? GetItemIcon(int typeId)
            {
                var item = GetItem(typeId);
                return item?.Icon;
            }

            /// <summary>
            /// Gets color associated with item quality
            /// </summary>
            public Color GetItemQualityColor(int typeId)
            {
                var item = GetItem(typeId);
                if (item == null)
                {
                    return Color.white;
                }
                return QualityUtils.GetItemValueLevelColor(QualityUtils.GetCachedItemValueLevel(item));
            }

            /// <summary>
            /// Gets category associated with item
            /// </summary>
            public string GetItemCategory(int typeId)
            {
                var metaData = GetItemMetaDataById(typeId);
                if (metaData == null)
                {
                    return "Unknown";
                }
                return ((ItemMetaData)metaData).Catagory;
            }

            public async UniTask<Item?> CreateItemById(int typeId, int count = 1)
            {
                if (!_itemCache.ContainsKey(typeId))
                {
                    Log.Warning($"ItemQueryCache[{Name}] - Cannot create item with typeId {typeId} because it is not in the cache.");
                    return null;
                }

                Item? item = await ItemAssetsCollection.InstantiateAsync(typeId);
                if (item == null)
                {
                    return null;
                }

                item.StackCount = Math.Clamp(count, 1, item.MaxStackCount);
                return item;
            }

            public async UniTask SendItemToCharacterInventory(int typeId, int count = 1)
            {
                Item? item = await CreateItemById(typeId, count);
                if (item == null)
                {
                    Log.Warning($"ItemQueryCache[{Name}] - Failed to create item with typeId {typeId}");
                    return;
                }

                if (!ItemUtilities.SendToPlayerCharacterInventory(item, dontMerge: false))
                {
                    ItemUtilities.SendToPlayerStorage(item);
                }
            }
        }

        private static ItemQueryCache? _gameItemCache = null;
        public static ItemQueryCache GameItemCache
        {
            get
            {
                _gameItemCache ??= new ItemQueryCache("GameItems", DefaultItemPredicate, includeDynamicItems: true);
                return _gameItemCache;
            }
        }

        private static ItemQueryCache? _lotteryItemCache = null;

        public static ItemQueryCache LotteryItemCache
        {
            get
            {
                _lotteryItemCache ??= new ItemQueryCache("LotteryItems", LotteryItemPredicate, includeDynamicItems: true);
                return _lotteryItemCache;
            }
        }

        private static ItemQueryCache? _RecycleItemCache = null;

        public static ItemQueryCache RecycleItemCache
        {
            get
            {
                _RecycleItemCache ??= new ItemQueryCache("RecycleItemCache", RecycleItemPredicate, includeDynamicItems: true);
                return _RecycleItemCache;
            }
        }

        private static ItemQueryCache? _bulletItemCache = null;
        public static ItemQueryCache BulletItemCache
        {
            get
            {
                _bulletItemCache ??= new ItemQueryCache("BulletItems", BulletItemPredicate, includeDynamicItems: true);
                return _bulletItemCache;
            }
        }
    }
}