using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DuckovLuckyBox.Core;
using ItemStatsSystem;

namespace DuckovLuckyBox
{
  public static class ItemUtils
  {
    public static async UniTask<Item?> CreateItemById(int typeId, int count = 1)
    {
      Item? item = await ItemAssetsCollection.InstantiateAsync(typeId);
      if (item == null) return null;

      item.StackCount = Math.Clamp(count, 1, item.MaxStackCount);
      return item;
    }

    public static async UniTask SendItemToCharacterInventory(int typeId,int count = 1)
    {
      Item? item = await CreateItemById(typeId, count);
      if (item == null)
      {
        Log.Warning($"Failed to create item with typeId {typeId}");
        return;
      }

      ItemUtilities.SendToPlayer(item, dontMerge: false, sendToStorage: true);
    }

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