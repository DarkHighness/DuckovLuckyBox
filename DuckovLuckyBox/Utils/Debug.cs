using System.Collections.Generic;
using DuckovLuckyBox.Core;
using HarmonyLib;
using ItemStatsSystem;

namespace DuckovLuckyBox
{
    public static class DebugUtils
    {
        public class Entry
        {
            public int ID;
            public string Name = string.Empty;
            public string DisplayName = string.Empty;
            public string Description = string.Empty;
            public int GameQuality;
            public ItemValueLevel Quality;
            public int MaxStackCount;
            public int DefaultStackCount;
            public int PriceEach;
            public string Category = string.Empty;
            public bool IsAddon;

        }

        private static Entry CreateEntryFromMetaData(ItemMetaData metaData, Item prefab, bool isAddon)
        {
            return new Entry
            {
                ID = metaData.id,
                Name = metaData.Name,
                DisplayName = metaData.DisplayName,
                Description = metaData.Description,
                GameQuality = metaData.quality,
                Quality = QualityUtils.GetCachedItemValueLevel(prefab),
                MaxStackCount = metaData.maxStackCount,
                DefaultStackCount = metaData.defaultStackCount,
                PriceEach = metaData.priceEach,
                Category = metaData.Catagory,
                IsAddon = isAddon
            };
        }

        public static List<Entry> GetGameItems()
        {
            var entries = new List<Entry>();
            var itemDatabase = ItemAssetsCollection.Instance.entries;
            if (itemDatabase != null)
            {
                foreach (var item in itemDatabase)
                {
                    entries.Add(CreateEntryFromMetaData(item.metaData, item.prefab, false));
                }
            }
            return entries;
        }

        public static List<Entry> GetAddonItems()
        {
            var entries = new List<Entry>();
            var dynamicDicField = AccessTools.Field(typeof(ItemAssetsCollection), "dynamicDic");
            if (dynamicDicField != null)
            {
                var dynamicDicValue = dynamicDicField.GetValue(ItemAssetsCollection.Instance);
                if (dynamicDicValue is Dictionary<int, ItemAssetsCollection.DynamicEntry> dynamicItems)
                {
                    foreach (var kvp in dynamicItems)
                    {
                        var dynamicEntry = kvp.Value;
                        if (dynamicEntry != null)
                        {
                            var dynamicItem = dynamicEntry.prefab;
                            var metaData = dynamicEntry.MetaData;

                            if (dynamicItem != null)
                            {
                                entries.Add(CreateEntryFromMetaData(metaData, dynamicItem, true));
                            }
                        }
                    }
                }
            }

            return entries;
        }
        public static void DumpItemsToCSV(string filePath = "Items.csv")
        {
            var absFilePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), filePath);
            Log.Info($"Dumping all items to: {absFilePath}");

            var items = GetGameItems();
            var addonItems = GetAddonItems();
            items.AddRange(addonItems);

            var lines = new List<string>
      {
        "ID,Name,DisplayName,Description,GameQuality,Quality,MaxStackCount,DefaultStackCount,PriceEach,Category,IsAddon"
      };

            foreach (var item in items)
            {
                var line = $"{item.ID},\"{item.Name}\",\"{item.DisplayName}\",\"{item.Description}\",{item.GameQuality},{item.Quality},{item.MaxStackCount},{item.DefaultStackCount},{item.PriceEach},\"{item.Category}\",{item.IsAddon}";
                lines.Add(line);
            }

            System.IO.File.WriteAllLines(filePath, lines);
        }

        public static void DumpGameObjectHierarchy(UnityEngine.GameObject obj, int maxDepth = 10, bool includeComponents = false, bool toFile = false, string? filePath = null)
        {
            if (obj == null)
            {
                Log.Info("GameObject is null.");
                return;
            }

            var output = new System.Text.StringBuilder();
            BuildHierarchyString(obj, 0, maxDepth, includeComponents, output, new List<bool>());

            string result = output.ToString();
            if (toFile && !string.IsNullOrEmpty(filePath))
            {
                try
                {
                    System.IO.File.WriteAllText(filePath, result);
                    Log.Info($"GameObject hierarchy dumped to file: {filePath}");
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Failed to write hierarchy to file: {ex.Message}");
                }
            }
            else
            {
                Log.Info(result);
            }
        }

        private static void BuildHierarchyString(UnityEngine.GameObject obj, int depth, int maxDepth, bool includeComponents, System.Text.StringBuilder output, List<bool> isLast)
        {
            if (depth > maxDepth)
            {
                return;
            }

            // Build prefix
            for (int i = 0; i < depth; i++)
            {
                output.Append(isLast[i] ? "    " : "│   ");
            }
            if (depth > 0)
            {
                output.Append(isLast[depth - 1] ? "└── " : "├── ");
            }

            output.AppendLine(obj.name);

            if (includeComponents)
            {
                var components = obj.GetComponents<UnityEngine.Component>();
                if (components.Length > 0)
                {
                    // Same prefix as object, then ├──
                    for (int i = 0; i < depth; i++)
                    {
                        output.Append(isLast[i] ? "    " : "│   ");
                    }
                    if (depth > 0)
                    {
                        output.Append(isLast[depth - 1] ? "    " : "│   ");
                    }
                    output.AppendLine("├── [Components: " + components.Length + "]");
                    foreach (var comp in components)
                    {
                        for (int i = 0; i < depth; i++)
                        {
                            output.Append(isLast[i] ? "    " : "│   ");
                        }
                        if (depth > 0)
                        {
                            output.Append(isLast[depth - 1] ? "    " : "│   ");
                        }
                        output.Append("│   ├── ");
                        output.AppendLine(comp.GetType().Name);
                    }
                }
            }

            var children = new List<UnityEngine.Transform>();
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                children.Add(obj.transform.GetChild(i));
            }

            for (int i = 0; i < children.Count; i++)
            {
                isLast.Add(i == children.Count - 1);
                BuildHierarchyString(children[i].gameObject, depth + 1, maxDepth, includeComponents, output, isLast);
                isLast.RemoveAt(isLast.Count - 1);
            }
        }
    }


}