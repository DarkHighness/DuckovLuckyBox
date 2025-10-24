using System.Collections.Generic;
using DuckovLuckyBox.Core;
using ItemStatsSystem;

namespace DuckovLuckyBox
{
  public static class ItemUtils
  {
    public static List<ItemMetaData> GetAllItemMetadata()
    {
      var itemMetadata = new List<ItemMetaData>();
      var itemDatabase = ItemAssetsCollection.Instance.entries;
      if (itemDatabase != null)
      {
        foreach (var item in itemDatabase)
        {
          itemMetadata.Add(item.metaData);
        }
      }
      return itemMetadata;
    }

    public static void DumpAllItemMetadataCSV(string filePath = "ItemMetadataDump.csv")
    {
     var absFilePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), filePath);
      Log.Info($"Dumping all item metadata to: {absFilePath}");

      var items = GetAllItemMetadata();
      var lines = new List<string>
      {
        "ID,Name,DisplayName,Description,Quality,MaxStackCount,DefaultStackCount,PriceEach,Category"
      };

      foreach (var item in items)
      {
        var line = $"{item.id},\"{item.Name}\",\"{item.DisplayName}\",\"{item.Description}\",{item.quality},{item.maxStackCount},{item.defaultStackCount},{item.priceEach},\"{item.Catagory}\"";
        lines.Add(line);
      }

      System.IO.File.WriteAllLines(filePath, lines);
    }
  }
}