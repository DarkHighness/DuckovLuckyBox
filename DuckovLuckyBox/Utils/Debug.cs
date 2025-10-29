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
      public string Name;
      public string DisplayName;
      public string Description;
      public int GameQuality;
      public ItemValueLevel Quality;
      public int MaxStackCount;
      public int DefaultStackCount;
      public int PriceEach;
      public string Category;
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
  }
}