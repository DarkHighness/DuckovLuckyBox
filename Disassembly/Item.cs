// Decompiled with JetBrains decompiler
// Type: ItemStatsSystem.Item
// Assembly: ItemStatsSystem, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 71A3D795-3727-4DE2-B084-D0FD207549D6
// Assembly location: D:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Managed\ItemStatsSystem.dll

using Cysharp.Threading.Tasks;
using Duckov.Utilities;
using ItemStatsSystem.Items;
using ItemStatsSystem.Stats;
using Sirenix.OdinInspector;
using SodaCraft.Localizations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable disable
namespace ItemStatsSystem;

public class Item : MonoBehaviour, ISelfValidator
{
  [SerializeField]
  private int typeID;
  [SerializeField]
  private int order;
  [LocalizationKey("Items")]
  [SerializeField]
  private string displayName;
  [SerializeField]
  private Sprite icon;
  [SerializeField]
  private int maxStackCount = 1;
  [SerializeField]
  private int value;
  [SerializeField]
  private int quality;
  [SerializeField]
  private DisplayQuality displayQuality;
  [SerializeField]
  private float weight;
  private float _cachedWeight;
  private float? _cachedTotalWeight;
  private int handheldHash = "Handheld".GetHashCode();
  [SerializeField]
  private TagCollection tags = new TagCollection();
  [SerializeField]
  private ItemAgentUtilities agentUtilities = new ItemAgentUtilities();
  [SerializeField]
  private ItemGraphicInfo itemGraphic;
  [SerializeField]
  private StatCollection stats;
  [SerializeField]
  private SlotCollection slots;
  [SerializeField]
  private ModifierDescriptionCollection modifiers;
  [SerializeField]
  private CustomDataCollection variables = new CustomDataCollection();
  [SerializeField]
  private CustomDataCollection constants = new CustomDataCollection();
  [SerializeField]
  private Inventory inventory;
  [SerializeField]
  private List<Effect> effects = new List<Effect>();
  [SerializeField]
  private UsageUtilities usageUtilities;
  private ItemStatsSystem.Items.Slot pluggedIntoSlot;
  private Inventory inInventory;
  private bool initialized;
  private const string StackCountVariableKey = "Count";
  private static readonly int StackCountVariableHash = "Count".GetHashCode();
  private const string InspectedVariableKey = "Inspected";
  private static readonly int InspectedVariableHash = nameof (Inspected).GetHashCode();
  private const string MaxDurabilityConstantKey = "MaxDurability";
  private const string DurabilityVariableKey = "Durability";
  private static readonly int MaxDurabilityConstantHash = nameof (MaxDurability).GetHashCode();
  private static readonly int DurabilityVariableHash = nameof (Durability).GetHashCode();
  private bool _inspecting;
  public string soundKey;
  private bool isBeingDestroyed;

  public int TypeID
  {
    get => this.typeID;
    internal set => this.typeID = value;
  }

  public int Order
  {
    get => this.order;
    set => this.order = value;
  }

  public string DisplayName => this.displayName.ToPlainText();

  public string DisplayNameRaw
  {
    get => this.displayName;
    set => this.displayName = value;
  }

  [LocalizationKey("Items")]
  private string description
  {
    get => this.displayName + "_Desc";
    set
    {
    }
  }

  public string DescriptionRaw => this.description;

  public string Description => this.description.ToPlainText();

  public Sprite Icon
  {
    get => this.icon;
    set => this.icon = value;
  }

  private string MaxStackCountSuffixLabel => this.MaxStackCount <= 1 ? "不可堆叠" : "可堆叠";

  public int MaxStackCount
  {
    get => this.maxStackCount;
    set => this.maxStackCount = value;
  }

  public bool Stackable => this.MaxStackCount > 1;

  public int Value
  {
    get => this.value;
    set => this.value = value;
  }

  public int Quality
  {
    get => this.quality;
    set => this.quality = value;
  }

  public DisplayQuality DisplayQuality
  {
    get => this.displayQuality;
    set => this.displayQuality = value;
  }

  public float UnitSelfWeight => this.weight;

  public float SelfWeight => this.weight * (float) this.StackCount;

  public bool Sticky => this.Tags.Contains(nameof (Sticky));

  public bool CanBeSold => !this.Sticky && !this.Tags.Contains("NotSellable");

  public bool CanDrop => !this.Sticky;

  public float TotalWeight
  {
    get
    {
      if (!this._cachedTotalWeight.HasValue || (double) this._cachedWeight != (double) this.SelfWeight)
      {
        this._cachedWeight = this.SelfWeight;
        this._cachedTotalWeight = new float?(this.RecalculateTotalWeight());
      }
      return this._cachedTotalWeight.Value;
    }
  }

  public bool HasHandHeldAgent
  {
    get => (UnityEngine.Object) this.AgentUtilities.GetPrefab(this.handheldHash) != (UnityEngine.Object) null;
  }

  private string TagsLabelText
  {
    get
    {
      string tagsLabelText = "Tags: ";
      bool flag = true;
      foreach (Tag tag in this.tags)
      {
        tagsLabelText = tagsLabelText + (flag ? "" : ", ") + tag.DisplayName;
        flag = false;
      }
      return tagsLabelText;
    }
  }

  public ItemAgentUtilities AgentUtilities => this.agentUtilities;

  public ItemAgent ActiveAgent => this.agentUtilities.ActiveAgent;

  public ItemGraphicInfo ItemGraphic => this.itemGraphic;

  private string StatsTabLabelText
  {
    get => !(bool) (UnityEngine.Object) this.stats ? "No Stats" : $"Stats({this.stats.Count})";
  }

  [SerializeField]
  private void CreateStatsComponent()
  {
    StatCollection statCollection = this.gameObject.AddComponent<StatCollection>();
    this.stats = statCollection;
    statCollection.Master = this;
  }

  private string SlotsTabLabelText
  {
    get => !(bool) (UnityEngine.Object) this.slots ? "No Slots" : $"Slots({this.slots.Count})";
  }

  [SerializeField]
  public void CreateSlotsComponent()
  {
    if ((UnityEngine.Object) this.slots != (UnityEngine.Object) null)
    {
      Debug.LogError((object) "Slot component已存在");
    }
    else
    {
      SlotCollection slotCollection = this.gameObject.AddComponent<SlotCollection>();
      this.slots = slotCollection;
      slotCollection.Master = this;
    }
  }

  private string ModifiersTabLabelText
  {
    get => !(bool) (UnityEngine.Object) this.modifiers ? "No Modifiers" : $"Modifiers({this.modifiers.Count})";
  }

  [SerializeField]
  public void CreateModifiersComponent()
  {
    if ((UnityEngine.Object) this.modifiers == (UnityEngine.Object) null)
      this.modifiers = this.gameObject.AddComponent<ModifierDescriptionCollection>();
    this.modifiers.Master = this;
  }

  [SerializeField]
  private void CreateInventoryComponent()
  {
    Inventory inventory = this.gameObject.AddComponent<Inventory>();
    this.inventory = inventory;
    inventory.AttachedToItem = this;
  }

  public UsageUtilities UsageUtilities => this.usageUtilities;

  public bool IsUsable(object user)
  {
    return (UnityEngine.Object) this.usageUtilities != (UnityEngine.Object) null && this.usageUtilities.IsUsable(this, user);
  }

  public float UseTime
  {
    get => (UnityEngine.Object) this.usageUtilities != (UnityEngine.Object) null ? this.usageUtilities.UseTime : 0.0f;
  }

  public void AddUsageUtilitiesComponent()
  {
  }

  public StatCollection Stats => this.stats;

  public ModifierDescriptionCollection Modifiers => this.modifiers;

  public SlotCollection Slots => this.slots;

  public Inventory Inventory
  {
    get => this.inventory;
    internal set => this.inventory = value;
  }

  public List<Effect> Effects => this.effects;

  public ItemStatsSystem.Items.Slot PluggedIntoSlot
  {
    get
    {
      if (this.pluggedIntoSlot == null)
        return (ItemStatsSystem.Items.Slot) null;
      return (UnityEngine.Object) this.pluggedIntoSlot.Master == (UnityEngine.Object) null ? (ItemStatsSystem.Items.Slot) null : this.pluggedIntoSlot;
    }
  }

  public Inventory InInventory => this.inInventory;

  public Item ParentItem
  {
    get
    {
      UnityEngine.Object parentObject1 = this.ParentObject;
      if (parentObject1 == (UnityEngine.Object) null)
        return (Item) null;
      Item parentObject2 = this.ParentObject as Item;
      if ((UnityEngine.Object) parentObject2 != (UnityEngine.Object) null)
        return parentObject2;
      Inventory inventory = parentObject1 as Inventory;
      if ((UnityEngine.Object) inventory == (UnityEngine.Object) null)
      {
        Debug.LogError((object) "侦测到不合法的Parent Object。需要检查ParentObject代码。");
        return (Item) null;
      }
      Item attachedToItem = inventory.AttachedToItem;
      return (UnityEngine.Object) attachedToItem != (UnityEngine.Object) null ? attachedToItem : (Item) null;
    }
  }

  public UnityEngine.Object ParentObject
  {
    get
    {
      if (this.PluggedIntoSlot != null && (UnityEngine.Object) this.InInventory != (UnityEngine.Object) null)
        Debug.LogError((object) $"物品 {this.DisplayName} ({this.GetInstanceID()})同时存在于Slot和Inventory中。");
      if (this.PluggedIntoSlot != null)
      {
        ItemStatsSystem.Items.Slot pluggedIntoSlot = this.PluggedIntoSlot;
        return pluggedIntoSlot == null ? (UnityEngine.Object) null : (UnityEngine.Object) pluggedIntoSlot.Master;
      }
      return (UnityEngine.Object) this.InInventory != (UnityEngine.Object) null ? (UnityEngine.Object) this.InInventory : (UnityEngine.Object) null;
    }
  }

  public TagCollection Tags => this.tags;

  public CustomDataCollection Variables => this.variables;

  public CustomDataCollection Constants => this.constants;

  public bool IsCharacter
  {
    get
    {
      return this.tags.Any<Tag>((Func<Tag, bool>) (e => (UnityEngine.Object) e != (UnityEngine.Object) null && e.name == "Character"));
    }
  }

  public event Action<Item> onItemTreeChanged;

  public event Action<Item> onDestroy;

  public event Action<Item> onSetStackCount;

  public event Action<Item> onDurabilityChanged;

  public event Action<Item> onInspectionStateChanged;

  public event Action<Item, object> onUse;

  public static event Action<Item, object> onUseStatic;

  public event Action<Item> onChildChanged;

  public event Action<Item> onParentChanged;

  public event Action<Item> onPluggedIntoSlot;

  public event Action<Item> onUnpluggedFromSlot;

  public event Action<Item, ItemStatsSystem.Items.Slot> onSlotContentChanged;

  public event Action<Item> onSlotTreeChanged;

  private void Awake()
  {
    if (!this.initialized)
      this.Initialize();
    if (!(bool) (UnityEngine.Object) this.inventory)
      return;
    this.inventory.onContentChanged += new Action<Inventory, int>(this.OnInventoryContentChanged);
  }

  private void OnInventoryContentChanged(Inventory inventory, int index)
  {
    this.NotifyChildChanged();
  }

  public void Initialize()
  {
    if (this.initialized)
      return;
    this.initialized = true;
    this.agentUtilities.Initialize(this);
    this.Stats?.Initialize();
    this.Slots?.Initialize();
    this.Modifiers?.Initialize();
    this.modifiers?.ReapplyModifiers();
    this.HandleEffectsActive();
  }

  public Item GetCharacterItem()
  {
    for (Item characterItem = this; (UnityEngine.Object) characterItem != (UnityEngine.Object) null; characterItem = characterItem.ParentItem)
    {
      if (characterItem.IsCharacter)
        return characterItem;
    }
    return (Item) null;
  }

  public bool IsInCharacterSlot()
  {
    Item obj1 = (Item) null;
    Item obj2 = this;
    if (obj2.IsCharacter)
      return false;
    for (; (UnityEngine.Object) obj2 != (UnityEngine.Object) null; obj2 = obj2.ParentItem)
    {
      if (obj2.IsCharacter)
        return obj1.PluggedIntoSlot != null;
      obj1 = obj2;
    }
    return false;
  }

  public Item CreateInstance()
  {
    Item instance = UnityEngine.Object.Instantiate<Item>(this);
    instance.Initialize();
    return instance;
  }

  public void Detach()
  {
    this.PluggedIntoSlot?.Unplug();
    this.InInventory?.RemoveItem(this);
  }

  internal void NotifyPluggedTo(ItemStatsSystem.Items.Slot slot)
  {
    this.pluggedIntoSlot = slot;
    Action<Item> onPluggedIntoSlot = this.onPluggedIntoSlot;
    if (onPluggedIntoSlot != null)
      onPluggedIntoSlot(this);
    Action<Item> onParentChanged = this.onParentChanged;
    if (onParentChanged == null)
      return;
    onParentChanged(this);
  }

  internal void NotifyUnpluggedFrom(ItemStatsSystem.Items.Slot slot)
  {
    if (this.pluggedIntoSlot == slot)
    {
      this.pluggedIntoSlot = (ItemStatsSystem.Items.Slot) null;
      Action<Item> unpluggedFromSlot = this.onUnpluggedFromSlot;
      if (unpluggedFromSlot != null)
        unpluggedFromSlot(this);
      Action<Item> onParentChanged = this.onParentChanged;
      if (onParentChanged == null)
        return;
      onParentChanged(this);
    }
    else
      Debug.LogError((object) $"物品 {this.DisplayName} 被通知从Slot移除，但当前Slot {(this.pluggedIntoSlot != null ? $"{this.pluggedIntoSlot.Master.DisplayName}/{this.pluggedIntoSlot.Key}" : "空")} 与通知Slot {(slot != null ? $"{slot.Master.DisplayName}/{slot.Key}" : "空")} 不匹配。");
  }

  internal void NotifySlotPlugged(ItemStatsSystem.Items.Slot slot)
  {
    this.NotifyChildChanged();
    this.NotifySlotTreeChanged();
    Action<Item, ItemStatsSystem.Items.Slot> slotContentChanged = this.onSlotContentChanged;
    if (slotContentChanged == null)
      return;
    slotContentChanged(this, slot);
  }

  internal void NotifySlotUnplugged(ItemStatsSystem.Items.Slot slot)
  {
    this.NotifyChildChanged();
    this.NotifySlotTreeChanged();
    Action<Item, ItemStatsSystem.Items.Slot> slotContentChanged = this.onSlotContentChanged;
    if (slotContentChanged == null)
      return;
    slotContentChanged(this, slot);
  }

  internal void NotifyRemovedFromInventory(Inventory inventory)
  {
    if ((UnityEngine.Object) inventory == (UnityEngine.Object) this.InInventory)
    {
      this.inInventory = (Inventory) null;
      Action<Item> onParentChanged = this.onParentChanged;
      if (onParentChanged == null)
        return;
      onParentChanged(this);
    }
    else
    {
      if (!((UnityEngine.Object) this.InInventory != (UnityEngine.Object) null))
        return;
      Debug.LogError((object) "尝试从不是当前的Inventory中移除，已取消。");
    }
  }

  internal void NotifyAddedToInventory(Inventory inventory)
  {
    this.inInventory = inventory;
    Action<Item> onParentChanged = this.onParentChanged;
    if (onParentChanged == null)
      return;
    onParentChanged(this);
  }

  internal void NotifyItemTreeChanged()
  {
    Action<Item> onItemTreeChanged = this.onItemTreeChanged;
    if (onItemTreeChanged != null)
      onItemTreeChanged(this);
    this.HandleEffectsActive();
  }

  private void HandleEffectsActive()
  {
    if (this.effects == null)
      return;
    bool flag = this.IsCharacter || this.PluggedIntoSlot != null;
    if (this.UseDurability && (double) this.Durability <= 0.0)
      flag = false;
    foreach (Effect effect in this.effects)
    {
      if (!((UnityEngine.Object) effect == (UnityEngine.Object) null))
        effect.gameObject.SetActive(flag);
    }
  }

  internal void InitiateNotifyItemTreeChanged()
  {
    List<Item> allConnected = this.GetAllConnected();
    if (allConnected == null)
      return;
    foreach (Item obj in allConnected)
      obj.NotifyItemTreeChanged();
  }

  internal void NotifyChildChanged()
  {
    double num = (double) this.RecalculateTotalWeight();
    Action<Item> onChildChanged = this.onChildChanged;
    if (onChildChanged != null)
      onChildChanged(this);
    Item parentItem = this.ParentItem;
    if (!((UnityEngine.Object) parentItem != (UnityEngine.Object) null))
      return;
    parentItem.NotifyChildChanged();
  }

  internal void NotifySlotTreeChanged()
  {
    Action<Item> onSlotTreeChanged = this.onSlotTreeChanged;
    if (onSlotTreeChanged != null)
      onSlotTreeChanged(this);
    Item parentItem = this.ParentItem;
    if (!((UnityEngine.Object) parentItem != (UnityEngine.Object) null))
      return;
    parentItem.NotifySlotTreeChanged();
  }

  public void Use(object user)
  {
    Action<Item, object> onUse = this.onUse;
    if (onUse != null)
      onUse(this, user);
    Action<Item, object> onUseStatic = Item.onUseStatic;
    if (onUseStatic != null)
      onUseStatic(this, user);
    this.usageUtilities.Use(this, user);
  }

  public int StackCount
  {
    get => this.Stackable ? this.GetInt(Item.StackCountVariableHash, 1) : 1;
    set
    {
      if (!this.Stackable)
      {
        if (value == 1)
          return;
        Debug.LogError((object) $"该物品 {this.DisplayName} 不可堆叠。无法设置数量。");
      }
      else
      {
        int num = value;
        if (value >= 1 && value > this.MaxStackCount)
        {
          Debug.LogWarning((object) $"尝试将数量设为{value},但该物品 {this.DisplayName} 的数量最多为{this.MaxStackCount}。将改为设为{this.MaxStackCount}。");
          num = this.MaxStackCount;
        }
        this.SetInt("Count", num);
        Action<Item> onSetStackCount = this.onSetStackCount;
        if (onSetStackCount != null)
          onSetStackCount(this);
        this.NotifyChildChanged();
        if ((bool) (UnityEngine.Object) this.InInventory)
          this.InInventory.NotifyContentChanged(this);
        if (this.StackCount >= 1)
          return;
        this.DestroyTree();
      }
    }
  }

  public bool UseDurability => (double) this.MaxDurability > 0.0;

  public float MaxDurability
  {
    get => this.Constants.GetFloat(Item.MaxDurabilityConstantHash);
    set
    {
      this.Constants.SetFloat(nameof (MaxDurability), value);
      Action<Item> durabilityChanged = this.onDurabilityChanged;
      if (durabilityChanged == null)
        return;
      durabilityChanged(this);
    }
  }

  public float MaxDurabilityWithLoss => this.MaxDurability * (1f - this.DurabilityLoss);

  public float DurabilityLoss
  {
    get => Mathf.Clamp01(this.Variables.GetFloat(nameof (DurabilityLoss)));
    set => this.Variables.SetFloat(nameof (DurabilityLoss), value);
  }

  public float Durability
  {
    get => this.GetFloat(Item.DurabilityVariableHash);
    set
    {
      float num = Mathf.Min(this.MaxDurability, value);
      if ((double) num < 0.0)
        num = 0.0f;
      this.SetFloat(nameof (Durability), num);
      Action<Item> durabilityChanged = this.onDurabilityChanged;
      if (durabilityChanged != null)
        durabilityChanged(this);
      this.HandleEffectsActive();
    }
  }

  public bool Inspected
  {
    get => this.Variables.GetBool(Item.InspectedVariableHash);
    set
    {
      this.Variables.SetBool(nameof (Inspected), value);
      if ((UnityEngine.Object) this.slots != (UnityEngine.Object) null)
      {
        foreach (ItemStatsSystem.Items.Slot slot in this.slots)
        {
          if (slot != null)
          {
            Item content = slot.Content;
            if (!((UnityEngine.Object) content == (UnityEngine.Object) null))
              content.Inspected = value;
          }
        }
      }
      Action<Item> inspectionStateChanged = this.onInspectionStateChanged;
      if (inspectionStateChanged == null)
        return;
      inspectionStateChanged(this);
    }
  }

  public bool Inspecting
  {
    get => this._inspecting;
    set
    {
      this._inspecting = value;
      Action<Item> inspectionStateChanged = this.onInspectionStateChanged;
      if (inspectionStateChanged == null)
        return;
      inspectionStateChanged(this);
    }
  }

  public bool NeedInspection
  {
    get
    {
      return !this.Inspected && !((UnityEngine.Object) this.InInventory == (UnityEngine.Object) null) && this.InInventory.NeedInspection;
    }
  }

  public CustomData GetVariableEntry(string variableKey) => this.Variables.GetEntry(variableKey);

  public CustomData GetVariableEntry(int hash) => this.Variables.GetEntry(hash);

  public float GetFloat(string key, float defaultResult = 0.0f)
  {
    return this.Variables.GetFloat(key, defaultResult);
  }

  public int GetInt(string key, int defaultResult = 0) => this.Variables.GetInt(key, defaultResult);

  public bool GetBool(string key, bool defaultResult = false)
  {
    return this.Variables.GetBool(key, defaultResult);
  }

  public string GetString(string key, string defaultResult = null)
  {
    return this.Variables.GetString(key, defaultResult);
  }

  public float GetFloat(int hash, float defaultResult = 0.0f)
  {
    return this.Variables.GetFloat(hash, defaultResult);
  }

  public int GetInt(int hash, int defaultResult = 0) => this.Variables.GetInt(hash, defaultResult);

  public bool GetBool(int hash, bool defaultResult = false)
  {
    return this.Variables.GetBool(hash, defaultResult);
  }

  public string GetString(int hash, string defaultResult = null)
  {
    return this.Variables.GetString(hash, defaultResult);
  }

  public void SetFloat(string key, float value, bool createNewIfNotExist = true)
  {
    this.Variables.Set(key, value, createNewIfNotExist);
  }

  public void SetInt(string key, int value, bool createNewIfNotExist = true)
  {
    this.Variables.Set(key, value, createNewIfNotExist);
  }

  public void SetBool(string key, bool value, bool createNewIfNotExist = true)
  {
    this.Variables.Set(key, value, createNewIfNotExist);
  }

  public void SetString(string key, string value, bool createNewIfNotExist = true)
  {
    this.Variables.Set(key, value, createNewIfNotExist);
  }

  public void SetFloat(int hash, float value) => this.Variables.Set(hash, value);

  public void SetInt(int hash, int value) => this.Variables.Set(hash, value);

  public void SetBool(int hash, bool value) => this.Variables.Set(hash, value);

  public void SetString(int hash, string value) => this.Variables.Set(hash, value);

  internal void ForceSetStackCount(int value)
  {
    Debug.LogWarning((object) $"正在强制将物品 {this.DisplayName} 的 Stack Count 设置为 {value}。");
    this.SetInt(Item.StackCountVariableHash, value);
    Action<Item> onSetStackCount = this.onSetStackCount;
    if (onSetStackCount == null)
      return;
    onSetStackCount(this);
  }

  public void Combine(Item incomingItem)
  {
    if ((UnityEngine.Object) incomingItem == (UnityEngine.Object) null || (UnityEngine.Object) incomingItem == (UnityEngine.Object) this)
      return;
    if (!this.Stackable)
      Debug.LogError((object) $"正在尝试组合物品，但物品 {this.DisplayName} 不能堆叠。");
    else if (this.TypeID != incomingItem.TypeID)
    {
      Debug.LogError((object) $"物品 {this.DisplayName} 与 {incomingItem.DisplayName} 类型不同，无法组合。");
    }
    else
    {
      int num1 = this.MaxStackCount - this.StackCount;
      if (num1 <= 0)
        return;
      int stackCount1 = this.StackCount;
      int stackCount2 = incomingItem.StackCount;
      int num2 = incomingItem.StackCount >= num1 ? num1 : incomingItem.StackCount;
      int num3 = incomingItem.StackCount - num2;
      this.StackCount += num2;
      incomingItem.StackCount = num3;
      if (num3 > 0)
        return;
      incomingItem.Detach();
      if (Application.isPlaying)
        incomingItem.DestroyTree();
      else
        UnityEngine.Object.DestroyImmediate((UnityEngine.Object) incomingItem);
    }
  }

  public void CombineInto(Item otherItem) => otherItem.Combine(this);

  public async UniTask<Item> Split(int count)
  {
    if (!this.Stackable)
      Debug.LogError((object) $"物品 {this.DisplayName} 无法被分割。");
    if (count <= 0)
      return (Item) null;
    if (count > this.StackCount)
    {
      Debug.LogError((object) $"物品 {this.DisplayName} 数量为{this.StackCount}，不足以分割出 {count} 。");
      return (Item) null;
    }
    if (count == this.StackCount)
    {
      Debug.LogError((object) $"正在尝试分割物品 {this.DisplayName} ，但目标数量 {count} 与该物品总数量相同。无法分割。");
      return (Item) null;
    }
    this.StackCount -= count;
    Item obj = await ItemAssetsCollection.InstantiateAsync(this.TypeID);
    if ((UnityEngine.Object) obj == (UnityEngine.Object) null)
    {
      Debug.LogWarning((object) $"物体 ID:{this.TypeID} ({this.DisplayName}) 创建失败。");
      return (Item) null;
    }
    obj.Initialize();
    obj.StackCount = count;
    obj.Inspected = true;
    return obj;
  }

  public override string ToString() => this.displayName + " (物品)";

  public bool IsBeingDestroyed => this.isBeingDestroyed;

  public bool Repairable => this.UseDurability && this.Tags.Contains(nameof (Repairable));

  public string SoundKey => string.IsNullOrWhiteSpace(this.soundKey) ? "default" : this.soundKey;

  public void MarkDestroyed() => this.isBeingDestroyed = true;

  private void OnDestroy()
  {
    this.isBeingDestroyed = true;
    this.Detach();
    this.agentUtilities.ReleaseActiveAgent();
    Action<Item> onDestroy = this.onDestroy;
    if (onDestroy == null)
      return;
    onDestroy(this);
  }

  public Stat GetStat(int hash)
  {
    if ((UnityEngine.Object) this.Stats == (UnityEngine.Object) null)
      return (Stat) null;
    return this.Stats?.GetStat(hash);
  }

  public Stat GetStat(string key) => this.Stats?.GetStat(key);

  public float GetStatValue(int hash)
  {
    Stat stat = this.GetStat(hash);
    return stat == null ? 0.0f : stat.Value;
  }

  public static Stat GetStat(Item item, int hash)
  {
    return (UnityEngine.Object) item == (UnityEngine.Object) null ? (Stat) null : item.GetStat(hash);
  }

  public static float GetStatValue(Item item, int hash)
  {
    if ((UnityEngine.Object) item == (UnityEngine.Object) null)
      return 0.0f;
    Stat stat = Item.GetStat(item, hash);
    return stat == null ? 0.0f : stat.Value;
  }

  private void OnValidate() => this.transform.hideFlags = HideFlags.HideInInspector;

  public void Validate(SelfValidationResult result)
  {
    if ((UnityEngine.Object) this.Stats != (UnityEngine.Object) null && (UnityEngine.Object) this.Stats.gameObject != (UnityEngine.Object) this.gameObject)
      result.AddError("引用了其他物体上的Stats组件。").WithFix("改为引用本物体的Stats组件", (Action) (() => this.stats = this.GetComponent<StatCollection>()));
    if ((UnityEngine.Object) this.Slots != (UnityEngine.Object) null && (UnityEngine.Object) this.Slots.gameObject != (UnityEngine.Object) this.gameObject)
      result.AddError("引用了其他物体上的Slots组件。").WithFix("改为引用本物体的Slots组件", (Action) (() => this.slots = this.GetComponent<SlotCollection>()));
    if ((UnityEngine.Object) this.Modifiers != (UnityEngine.Object) null && (UnityEngine.Object) this.Modifiers.gameObject != (UnityEngine.Object) this.gameObject)
      result.AddError("引用了其他物体上的Modifiers组件。").WithFix("改为引用本物体的Modifiers组件", (Action) (() => this.modifiers = this.GetComponent<ModifierDescriptionCollection>()));
    if ((UnityEngine.Object) this.Inventory != (UnityEngine.Object) null && (UnityEngine.Object) this.Inventory.gameObject != (UnityEngine.Object) this.gameObject)
      result.AddError("引用了其他物体上的Inventory组件。").WithFix("改为引用本物体的Inventory组件", (Action) (() => this.inventory = this.GetComponent<Inventory>()));
    if (this.Effects.Any<Effect>((Func<Effect, bool>) (e => (UnityEngine.Object) e == (UnityEngine.Object) null)))
      result.AddError("Effects列表中有空物体。").WithFix("移除空Effect项目", (Action) (() => this.Effects.RemoveAll((Predicate<Effect>) (e => (UnityEngine.Object) e == (UnityEngine.Object) null))));
    if (this.Effects.Any<Effect>((Func<Effect, bool>) (e => !e.transform.IsChildOf(this.transform))))
      result.AddError("引用了其他物体上的Effects。").WithFix("移除不正确的Effects", (Action) (() => this.Effects.RemoveAll((Predicate<Effect>) (e => !e.transform.IsChildOf(this.transform)))));
    if (this.Stackable)
    {
      if ((UnityEngine.Object) this.Slots != (UnityEngine.Object) null || (UnityEngine.Object) this.Inventory != (UnityEngine.Object) null)
        result.AddError("可堆叠物体不应包含Slot、Inventory等独特信息。").WithFix("变为不可堆叠物体", (Action) (() => this.maxStackCount = 1));
      if (this.Variables.Any<CustomData>((Func<CustomData, bool>) (e => e.Key != "Count")))
        result.AddError("可堆叠物体不应包含特殊变量。");
      if (this.Variables.Any<CustomData>((Func<CustomData, bool>) (e => e.Key == "Count")))
        return;
      result.AddWarning("可堆叠物体应包含Count变量，记录当前具体数量。(默认数量)").WithFix("添加Count变量。", (Action) (() => this.variables.Add(new CustomData("Count", this.MaxStackCount))));
    }
    else
    {
      if (!this.Variables.Any<CustomData>((Func<CustomData, bool>) (e => e.Key == "Count")))
        return;
      result.AddWarning("不可堆叠物体包含了Count变量。建议删除。").WithFix("删除Count变量。", (Action) (() => this.variables.Remove(this.variables.GetEntry("Count"))));
    }
  }

  public float RecalculateTotalWeight()
  {
    float num = 0.0f + this.SelfWeight;
    if ((UnityEngine.Object) this.inventory != (UnityEngine.Object) null)
    {
      this.inventory.RecalculateWeight();
      float cachedWeight = this.inventory.CachedWeight;
      num += cachedWeight;
    }
    if ((UnityEngine.Object) this.slots != (UnityEngine.Object) null)
    {
      foreach (ItemStatsSystem.Items.Slot slot in this.slots)
      {
        if (slot != null && (UnityEngine.Object) slot.Content != (UnityEngine.Object) null)
        {
          float totalWeight = slot.Content.TotalWeight;
          num += totalWeight;
        }
      }
    }
    this._cachedTotalWeight = new float?(num);
    return num;
  }

  public void AddEffect(Effect instance)
  {
    instance.SetItem(this);
    if (this.effects.Contains(instance))
      return;
    this.effects.Add(instance);
  }

  private void CreateNewEffect()
  {
    GameObject gameObject = new GameObject("New Effect");
    gameObject.transform.SetParent(this.transform, false);
    this.AddEffect(gameObject.AddComponent<Effect>());
  }

  public int GetTotalRawValue()
  {
    float f = (float) this.Value;
    if (this.UseDurability && (double) this.MaxDurability > 0.0)
    {
      if ((double) this.MaxDurability > 0.0)
        f *= this.Durability / this.MaxDurability;
      else
        f = 0.0f;
    }
    int totalRawValue = Mathf.FloorToInt(f) * (this.Stackable ? this.StackCount : 1);
    if ((UnityEngine.Object) this.Slots != (UnityEngine.Object) null)
    {
      foreach (ItemStatsSystem.Items.Slot slot in this.Slots)
      {
        if (slot != null)
        {
          Item content = slot.Content;
          if (!((UnityEngine.Object) content == (UnityEngine.Object) null))
            totalRawValue += content.GetTotalRawValue();
        }
      }
    }
    if ((UnityEngine.Object) this.Inventory != (UnityEngine.Object) null)
    {
      foreach (Item obj in this.Inventory)
      {
        if (!((UnityEngine.Object) obj == (UnityEngine.Object) null))
          totalRawValue += obj.GetTotalRawValue();
      }
    }
    return totalRawValue;
  }

  public int RemoveAllModifiersFrom(object endowmentEntry)
  {
    if ((UnityEngine.Object) this.stats == (UnityEngine.Object) null)
      return 0;
    int num = 0;
    foreach (Stat stat in this.stats)
    {
      if (stat != null)
        num += stat.RemoveAllModifiersFromSource(endowmentEntry);
    }
    return num;
  }

  public bool AddModifier(string statKey, Modifier modifier)
  {
    if ((UnityEngine.Object) this.stats == (UnityEngine.Object) null)
      return false;
    Stat stat = this.stats[statKey];
    if (stat == null)
      return false;
    stat.AddModifier(modifier);
    return true;
  }
}
