// Decompiled with JetBrains decompiler
// Type: ItemStatsSystem.Inventory
// Assembly: ItemStatsSystem, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 71A3D795-3727-4DE2-B084-D0FD207549D6
// Assembly location: D:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Managed\ItemStatsSystem.dll

using Duckov.Utilities;
using Sirenix.OdinInspector;
using SodaCraft.Localizations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable disable
namespace ItemStatsSystem;

public class Inventory : MonoBehaviour, ISelfValidator, IEnumerable<Item>, IEnumerable
{
  private bool loading;
  [LocalizationKey("Default")]
  [SerializeField]
  private string displayNameKey = "";
  private const string defaultDisplayNameKey = "UI_InventoryDefault";
  [SerializeField]
  private int defaultCapacity = 64 /*0x40*/;
  [SerializeField]
  private Item attachedToItem;
  [SerializeField]
  private List<Item> content = new List<Item>();
  [SerializeField]
  private bool needInspection;
  [SerializeField]
  private bool acceptSticky;
  private const bool TrimListWhenRemovingItem = true;
  public bool hasBeenInspectedInLootBox;
  [SerializeField]
  public List<int> lockedIndexes = new List<int>();
  private float? cachedWeight;

  public bool Loading
  {
    get => this.loading;
    set => this.loading = value;
  }

  public string DisplayNameKey
  {
    get
    {
      return string.IsNullOrWhiteSpace(this.displayNameKey) ? "UI_InventoryDefault" : this.displayNameKey;
    }
    set => this.displayNameKey = value;
  }

  public string DisplayName => this.DisplayNameKey.ToPlainText();

  public List<Item> Content => this.content;

  public bool AcceptSticky
  {
    get => this.acceptSticky;
    set => this.acceptSticky = value;
  }

  public bool NeedInspection
  {
    set => this.needInspection = value;
    get => this.needInspection;
  }

  public int Capacity => this.defaultCapacity;

  public Item AttachedToItem
  {
    get => this.attachedToItem;
    internal set => this.attachedToItem = value;
  }

  public Item this[int index] => this.GetItemAt(index);

  public event Action<Inventory, int> onContentChanged;

  public event Action<Inventory> onInventorySorted;

  public event Action<Inventory> onCapacityChanged;

  public event Action<Inventory, int> onSetIndexLock;

  public void LockIndex(int index)
  {
    if (this.lockedIndexes.Contains(index))
      return;
    this.lockedIndexes.Add(index);
    Action<Inventory, int> onSetIndexLock = this.onSetIndexLock;
    if (onSetIndexLock == null)
      return;
    onSetIndexLock(this, index);
  }

  public void UnlockIndex(int index)
  {
    this.lockedIndexes.RemoveAll((Predicate<int>) (e => e == index));
    Action<Inventory, int> onSetIndexLock = this.onSetIndexLock;
    if (onSetIndexLock == null)
      return;
    onSetIndexLock(this, index);
  }

  public bool IsIndexLocked(int index) => this.lockedIndexes.Contains(index);

  public void ToggleLockIndex(int index)
  {
    if (this.IsIndexLocked(index))
      this.UnlockIndex(index);
    else
      this.LockIndex(index);
  }

  public float CachedWeight
  {
    get
    {
      if (!this.cachedWeight.HasValue)
        this.RecalculateWeight();
      return this.cachedWeight.Value;
    }
  }

  private void Start()
  {
    foreach (Item obj in this)
    {
      if (!((UnityEngine.Object) obj == (UnityEngine.Object) null) && (UnityEngine.Object) obj.ParentItem != (UnityEngine.Object) this)
        obj.NotifyAddedToInventory(this);
    }
  }

  public bool IsEmpty()
  {
    foreach (UnityEngine.Object @object in this.content)
    {
      if (@object != (UnityEngine.Object) null)
        return false;
    }
    return true;
  }

  public void Sort(Comparison<Item> comparison) => this.content.Sort(comparison);

  [ContextMenu("Sort")]
  public void Sort()
  {
    if (this.Loading)
      return;
    this.Loading = true;
    List<Item> source1 = new List<Item>();
    for (int index = 0; index < this.content.Count; ++index)
    {
      if (!this.IsIndexLocked(index))
      {
        Item obj = this.content[index];
        if (!((UnityEngine.Object) obj == (UnityEngine.Object) null))
        {
          obj.Detach();
          source1.Add(obj);
        }
      }
    }
    List<IGrouping<Tag, Item>> list1 = source1.Where<Item>((Func<Item, bool>) (e => (UnityEngine.Object) e != (UnityEngine.Object) null)).GroupBy<Item, Tag>((Func<Item, Tag>) (item => GetFirstTag(item))).ToList<IGrouping<Tag, Item>>();
    list1.Sort((Comparison<IGrouping<Tag, Item>>) ((g1, g2) =>
    {
      Tag key1 = g1.Key;
      Tag key2 = g2.Key;
      int num1 = (UnityEngine.Object) key1 != (UnityEngine.Object) null ? key1.Priority : -1;
      int num2 = (UnityEngine.Object) key2 != (UnityEngine.Object) null ? key2.Priority : -1;
      return num1 != num2 ? num1 - num2 : string.Compare(key1.name, key2.name, StringComparison.OrdinalIgnoreCase);
    }));
    List<Item> objList = new List<Item>();
    foreach (IEnumerable<Item> source2 in list1)
    {
      List<IGrouping<int, Item>> list2 = source2.GroupBy<Item, int>((Func<Item, int>) (item => item.TypeID)).ToList<IGrouping<int, Item>>();
      list2.Sort((Comparison<IGrouping<int, Item>>) ((a, b) =>
      {
        Item obj1 = a.First<Item>();
        Item obj2 = b.First<Item>();
        return obj1.Order == obj2.Order ? a.Key - b.Key : obj1.Order - obj2.Order;
      }));
      foreach (IGrouping<int, Item> grouping in list2)
      {
        List<Item> result;
        if (grouping.First<Item>().Stackable && Inventory.TryMerge((IEnumerable<Item>) grouping, out result))
          objList.AddRange((IEnumerable<Item>) result);
        else
          objList.AddRange((IEnumerable<Item>) grouping);
      }
    }
    int count = this.content.Count;
    foreach (Item obj in objList)
      this.AddItem(obj);
    this.Loading = false;
    Action<Inventory> onInventorySorted = this.onInventorySorted;
    if (onInventorySorted == null)
      return;
    onInventorySorted(this);

    static Tag GetFirstTag(Item item)
    {
      if ((UnityEngine.Object) item == (UnityEngine.Object) null)
        return (Tag) null;
      return item.Tags == null || item.Tags.Count == 0 ? (Tag) null : item.Tags.Get(0);
    }
  }

  private static bool TryMerge(IEnumerable<Item> itemsOfSameTypeID, out List<Item> result)
  {
    result = (List<Item>) null;
    List<Item> list = itemsOfSameTypeID.ToList<Item>();
    list.RemoveAll((Predicate<Item>) (e => (UnityEngine.Object) e == (UnityEngine.Object) null));
    if (list.Count <= 0)
      return false;
    int typeId = list[0].TypeID;
    foreach (Item obj in list)
    {
      if (typeId != obj.TypeID)
      {
        Debug.LogError((object) "尝试融合的Item具有不同的TypeID,已取消");
        return false;
      }
    }
    if (!list[0].Stackable)
    {
      Debug.LogError((object) "此类物品不可堆叠，已取消");
      return false;
    }
    result = new List<Item>();
    Stack<Item> objStack = new Stack<Item>((IEnumerable<Item>) list);
    Item obj1 = (Item) null;
    while (objStack.Count > 0)
    {
      if ((UnityEngine.Object) obj1 == (UnityEngine.Object) null)
        obj1 = objStack.Pop();
      if (objStack.Count <= 0)
      {
        result.Add(obj1);
        break;
      }
      obj1.Detach();
      Item incomingItem = (Item) null;
      while (obj1.StackCount < obj1.MaxStackCount && objStack.Count > 0)
      {
        incomingItem = objStack.Pop();
        incomingItem.Detach();
        obj1.Combine(incomingItem);
      }
      result.Add(obj1);
      if ((UnityEngine.Object) incomingItem != (UnityEngine.Object) null && incomingItem.StackCount > 0)
      {
        if (objStack.Count <= 0)
        {
          result.Add(incomingItem);
          break;
        }
        obj1 = incomingItem;
      }
      else
        obj1 = (Item) null;
    }
    return true;
  }

  public int GetFirstEmptyPosition(int preferedFirstPosition = 0)
  {
    if (preferedFirstPosition < 0)
      preferedFirstPosition = 0;
    if (this.content.Count <= preferedFirstPosition)
      return preferedFirstPosition;
    for (int index = preferedFirstPosition; index < this.content.Count; ++index)
    {
      if ((UnityEngine.Object) this.content[index] == (UnityEngine.Object) null)
        return index;
    }
    if (this.content.Count < this.Capacity)
      return this.content.Count;
    for (int index = 0; index < preferedFirstPosition; ++index)
    {
      if ((UnityEngine.Object) this.content[index] == (UnityEngine.Object) null)
        return index;
    }
    return -1;
  }

  public int GetLastItemPosition()
  {
    for (int index = this.content.Count - 1; index >= 0; --index)
    {
      if ((UnityEngine.Object) this.content[index] != (UnityEngine.Object) null)
        return index;
    }
    return -1;
  }

  public bool AddAt(Item item, int atPosition)
  {
    if ((UnityEngine.Object) item == (UnityEngine.Object) null)
    {
      Debug.LogError((object) "尝试添加的物体为空");
      return false;
    }
    if (this.Capacity <= atPosition)
    {
      Debug.LogError((object) $"向 Inventory {this.name} 加入物品时位置 {atPosition} 超出最大容量 {this.Capacity}。");
      return false;
    }
    if (item.ParentObject != (UnityEngine.Object) null)
    {
      Debug.Log((object) $"{item.name} \nParent: {item.ParentItem} \nInventory: {item.InInventory?.name} \nPlug: {item.PluggedIntoSlot?.DisplayName}");
      Debug.LogError((object) $"正在尝试将一个有父物体的物品 {item.DisplayName} 放入Inventory。请先使其脱离其父物体 {item.ParentObject.name} 再进行此操作。");
      return false;
    }
    Item itemAt = this.GetItemAt(atPosition);
    if ((UnityEngine.Object) itemAt != (UnityEngine.Object) null)
      Debug.LogError((object) $"正在尝试将物品 {item.DisplayName} 放入 Inventory {this.name} 的 {atPosition} 位置。但此位置已经存在另一物体 {itemAt.DisplayName}。");
    while (this.content.Count <= atPosition)
      this.content.Add((Item) null);
    this.content[atPosition] = item;
    item.transform.SetParent(this.transform);
    item.NotifyAddedToInventory(this);
    item.InitiateNotifyItemTreeChanged();
    this.RecalculateWeight();
    Action<Inventory, int> onContentChanged = this.onContentChanged;
    if (onContentChanged != null)
      onContentChanged(this, atPosition);
    return true;
  }

  public bool RemoveAt(int position, out Item removedItem)
  {
    removedItem = (Item) null;
    if (this.Capacity <= position && position >= this.content.Count)
    {
      Debug.LogError((object) "位置超出Inventory容量。");
      return false;
    }
    Item itemAt = this.GetItemAt(position);
    if (!((UnityEngine.Object) itemAt != (UnityEngine.Object) null))
      return false;
    this.content[position] = (Item) null;
    removedItem = itemAt;
    removedItem.NotifyRemovedFromInventory(this);
    removedItem.InitiateNotifyItemTreeChanged();
    this.AttachedToItem?.InitiateNotifyItemTreeChanged();
    for (int index = this.content.Count - 1; index >= 0 && (UnityEngine.Object) this.content[index] == (UnityEngine.Object) null; --index)
      this.content.RemoveAt(index);
    this.RecalculateWeight();
    Action<Inventory, int> onContentChanged = this.onContentChanged;
    if (onContentChanged != null)
      onContentChanged(this, position);
    return true;
  }

  public bool AddItem(Item item)
  {
    int firstEmptyPosition = this.GetFirstEmptyPosition();
    if (firstEmptyPosition >= 0)
      return this.AddAt(item, firstEmptyPosition);
    Debug.Log((object) $"添加物品失败，Inventory {this.name} 已满。");
    return false;
  }

  public bool RemoveItem(Item item)
  {
    int position = this.content.IndexOf(item);
    if (position >= 0)
      return this.RemoveAt(position, out Item _);
    Debug.LogError((object) $"正在尝试从Inventory {this.name} 中删除 {item.DisplayName}，但它并不在这个Inventory中。");
    return false;
  }

  public Item GetItemAt(int position)
  {
    if (position >= this.Capacity && position >= this.content.Count)
    {
      Debug.LogError((object) "访问的位置超出Inventory容量。");
      return (Item) null;
    }
    return this.content.Count <= position ? (Item) null : this.content[position];
  }

  public void Validate(SelfValidationResult result)
  {
    if ((UnityEngine.Object) this.AttachedToItem != (UnityEngine.Object) null)
    {
      if ((UnityEngine.Object) this.AttachedToItem.gameObject != (UnityEngine.Object) this.gameObject)
        result.AddError("AttachedItem引用了另一个Game Object上的Item。").WithFix("引用本物体上的Item。", (Action) (() => this.attachedToItem = this.GetComponent<Item>()));
      if ((UnityEngine.Object) this.AttachedToItem.Inventory != (UnityEngine.Object) this)
      {
        if ((UnityEngine.Object) this.AttachedToItem.Inventory != (UnityEngine.Object) null)
          result.AddError("AttachedItem引用了其他的Inventory。请检查Item内的配置。");
        else
          result.AddError("AttachedItem没有引用此Inventory。").WithFix("使AttachedItem引用此Inventory。", (Action) (() => this.AttachedToItem.Inventory = this));
      }
    }
    if (!((UnityEngine.Object) this.AttachedToItem == (UnityEngine.Object) null))
      return;
    Item gotItem = this.GetComponent<Item>();
    if (!((UnityEngine.Object) gotItem != (UnityEngine.Object) null))
      return;
    result.AddError("同一GameObject上存在Item，但AttachedToItem变量留空。").WithFix("设为本物体上的Item。", (Action) (() => this.attachedToItem = gotItem));
  }

  public IEnumerator<Item> GetEnumerator()
  {
    foreach (Item obj in this.content)
    {
      if (!((UnityEngine.Object) obj == (UnityEngine.Object) null))
        yield return obj;
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.GetEnumerator();

  public void DestroyAllContent()
  {
    for (int index = 0; index < this.content.Count; ++index)
    {
      Item obj = this.content[index];
      if (!((UnityEngine.Object) obj == (UnityEngine.Object) null))
      {
        this.RemoveItem(obj);
        obj.DestroyTree();
      }
    }
  }

  public List<Item> FindAll(Predicate<Item> match) => this.content.FindAll(match);

  public void RecalculateWeight()
  {
    float num1 = 0.0f;
    foreach (Item obj in this.content)
    {
      if (!((UnityEngine.Object) obj == (UnityEngine.Object) null))
      {
        float num2 = obj.RecalculateTotalWeight();
        num1 += num2;
      }
    }
    this.cachedWeight = new float?(num1);
  }

  public void SetCapacity(int capacity)
  {
    this.defaultCapacity = capacity;
    Action<Inventory> onCapacityChanged = this.onCapacityChanged;
    if (onCapacityChanged == null)
      return;
    onCapacityChanged(this);
  }

  public int GetItemCount()
  {
    int itemCount = 0;
    foreach (UnityEngine.Object @object in this.content)
    {
      if (!(@object == (UnityEngine.Object) null))
        ++itemCount;
    }
    return itemCount;
  }

  internal void NotifyContentChanged(Item item)
  {
    if (!(bool) (UnityEngine.Object) item)
      return;
    Action<Inventory, int> onContentChanged = this.onContentChanged;
    if (onContentChanged == null)
      return;
    onContentChanged(this, this.content.IndexOf(item));
  }

  public int GetIndex(Item item)
  {
    return (UnityEngine.Object) item == (UnityEngine.Object) null ? -1 : this.content.IndexOf(item);
  }

  public Item Find(int typeID)
  {
    return this.content.Find((Predicate<Item>) (e => (UnityEngine.Object) e != (UnityEngine.Object) null && e.TypeID == typeID));
  }
}
