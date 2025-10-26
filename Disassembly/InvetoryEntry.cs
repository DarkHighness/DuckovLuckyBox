// Decompiled with JetBrains decompiler
// Type: Duckov.UI.InventoryEntry
// Assembly: TeamSoda.Duckov.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FDA9642D-7C8C-43D7-BA39-BA2AFEF5C9C5
// Assembly location: D:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Managed\TeamSoda.Duckov.Core.dll

using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

#nullable disable
namespace Duckov.UI;

public class InventoryEntry :
  MonoBehaviour,
  IPoolable,
  IPointerClickHandler,
  IEventSystemHandler,
  IDropHandler,
  IItemDragSource,
  IBeginDragHandler,
  IEndDragHandler,
  IDragHandler,
  IPointerEnterHandler,
  IPointerExitHandler
{
  [SerializeField]
  private ItemDisplay itemDisplay;
  [SerializeField]
  private GameObject shortcutIndicator;
  [SerializeField]
  private GameObject disabledIndicator;
  [SerializeField]
  private GameObject hoveringIndicator;
  [SerializeField]
  private GameObject highlightIndicator;
  [SerializeField]
  private GameObject lockIndicator;
  [SerializeField]
  private int index;
  [SerializeField]
  private bool disabled;
  private bool cacheContentIsGun;
  private ItemMetaData cachedMeta;
  public const float doubleClickTimeThreshold = 0.3f;
  private float lastClickTime;
  private bool hovering;

  public InventoryDisplay Master { get; private set; }

  public int Index => this.index;

  public bool Disabled
  {
    get => this.disabled;
    set
    {
      this.disabled = value;
      this.Refresh();
    }
  }

  public Item Content
  {
    get
    {
      Inventory target = this.Master?.Target;
      if ((UnityEngine.Object) target == (UnityEngine.Object) null)
        return (Item) null;
      if (this.index >= target.Capacity)
        return (Item) null;
      return this.Master?.Target?.GetItemAt(this.index);
    }
  }

  public bool ShouldHighlight
  {
    get
    {
      if ((UnityEngine.Object) this.Master == (UnityEngine.Object) null || (UnityEngine.Object) this.Content == (UnityEngine.Object) null)
        return false;
      if (this.Master.EvaluateShouldHighlight(this.Content))
        return true;
      return this.Editable && ItemUIUtilities.IsGunSelected && !this.cacheContentIsGun && this.IsCaliberMatchItemSelected();
    }
  }

  private bool IsCaliberMatchItemSelected()
  {
    return !((UnityEngine.Object) this.Content == (UnityEngine.Object) null) && ItemUIUtilities.SelectedItemCaliber == this.cachedMeta.caliber;
  }

  public bool CanOperate
  {
    get => !((UnityEngine.Object) this.Master == (UnityEngine.Object) null) && this.Master.Func_CanOperate(this.Content);
  }

  public bool Editable
  {
    get => !((UnityEngine.Object) this.Master == (UnityEngine.Object) null) && this.Master.Editable && this.CanOperate;
  }

  public bool Movable => !((UnityEngine.Object) this.Master == (UnityEngine.Object) null) && this.Master.Movable;

  public event Action<InventoryEntry> onRefresh;

  private void Awake()
  {
    this.itemDisplay.onPointerClick += new Action<ItemDisplay, PointerEventData>(this.OnItemDisplayPointerClicked);
    this.itemDisplay.onDoubleClicked += new Action<ItemDisplay, PointerEventData>(this.OnDisplayDoubleClicked);
    this.itemDisplay.onReceiveDrop += new Action<PointerEventData>(this.OnDrop);
    this.hoveringIndicator?.SetActive(false);
    UIInputManager.OnFastPick += new Action<UIInputEventData>(this.OnFastPick);
    UIInputManager.OnDropItem += new Action<UIInputEventData>(this.OnDropItemButton);
    UIInputManager.OnUseItem += new Action<UIInputEventData>(this.OnUseItemButton);
  }

  private void OnEnable()
  {
    ItemUIUtilities.OnSelectionChanged += new Action(this.OnSelectionChanged);
    UIInputManager.OnLockInventoryIndex += new Action<UIInputEventData>(this.OnInputLockInventoryIndex);
    UIInputManager.OnShortcutInput += new Action<UIInputEventData, int>(this.OnShortcutInput);
  }

  private void OnDisable()
  {
    this.hovering = false;
    this.hoveringIndicator?.SetActive(false);
    ItemUIUtilities.OnSelectionChanged -= new Action(this.OnSelectionChanged);
    UIInputManager.OnLockInventoryIndex -= new Action<UIInputEventData>(this.OnInputLockInventoryIndex);
    UIInputManager.OnShortcutInput -= new Action<UIInputEventData, int>(this.OnShortcutInput);
  }

  private void OnShortcutInput(UIInputEventData data, int shortcutIndex)
  {
    if (!this.hovering || (UnityEngine.Object) this.Item == (UnityEngine.Object) null)
      return;
    ItemShortcut.Set(shortcutIndex, this.Item);
    ItemUIUtilities.NotifyPutItem(this.Item);
  }

  private void OnInputLockInventoryIndex(UIInputEventData data)
  {
    if (!this.hovering)
      return;
    this.ToggleLock();
  }

  private void OnSelectionChanged()
  {
    this.highlightIndicator.SetActive(this.ShouldHighlight);
    if (!((UnityEngine.Object) ItemUIUtilities.SelectedItemDisplay == (UnityEngine.Object) this.itemDisplay))
      return;
    this.Refresh();
  }

  private void OnDestroy()
  {
    UIInputManager.OnFastPick -= new Action<UIInputEventData>(this.OnFastPick);
    UIInputManager.OnDropItem -= new Action<UIInputEventData>(this.OnDropItemButton);
    UIInputManager.OnUseItem -= new Action<UIInputEventData>(this.OnUseItemButton);
    if (!((UnityEngine.Object) this.itemDisplay != (UnityEngine.Object) null))
      return;
    this.itemDisplay.onPointerClick -= new Action<ItemDisplay, PointerEventData>(this.OnItemDisplayPointerClicked);
    this.itemDisplay.onDoubleClicked -= new Action<ItemDisplay, PointerEventData>(this.OnDisplayDoubleClicked);
    this.itemDisplay.onReceiveDrop -= new Action<PointerEventData>(this.OnDrop);
  }

  private void OnFastPick(UIInputEventData data)
  {
    if (data.Used || !this.isActiveAndEnabled || !this.hovering)
      return;
    this.Master.NotifyItemDoubleClicked(this, new PointerEventData(EventSystem.current));
    data.Use();
  }

  private void OnDropItemButton(UIInputEventData data)
  {
    if (!this.isActiveAndEnabled || !this.hovering || (UnityEngine.Object) this.Item == (UnityEngine.Object) null || !this.Item.CanDrop || !this.CanOperate)
      return;
    this.Item.Drop(CharacterMainControl.Main, true);
  }

  private void OnUseItemButton(UIInputEventData data)
  {
    if (!this.isActiveAndEnabled || !this.hovering || (UnityEngine.Object) this.Item == (UnityEngine.Object) null || !this.Item.IsUsable((object) CharacterMainControl.Main) || !this.CanOperate)
      return;
    CharacterMainControl.Main.UseItem(this.Item);
  }

  private void OnItemDisplayPointerClicked(ItemDisplay display, PointerEventData data)
  {
    if (!this.isActiveAndEnabled)
      return;
    if (this.disabled || !this.CanOperate)
    {
      data.Use();
    }
    else
    {
      if (!this.Editable)
        return;
      if (data.button == PointerEventData.InputButton.Left)
      {
        if ((UnityEngine.Object) this.Content == (UnityEngine.Object) null)
          return;
        if (Keyboard.current != null && Keyboard.current.altKey.isPressed)
        {
          data.Use();
          if ((UnityEngine.Object) ItemUIUtilities.SelectedItem != (UnityEngine.Object) null)
            ItemUIUtilities.SelectedItem.TryPlug(this.Content);
          CharacterMainControl.Main.CharacterItem.TryPlug(this.Content);
        }
        else
        {
          if ((UnityEngine.Object) ItemUIUtilities.SelectedItem == (UnityEngine.Object) null || !this.Content.Stackable || !((UnityEngine.Object) ItemUIUtilities.SelectedItem != (UnityEngine.Object) this.Content) || ItemUIUtilities.SelectedItem.TypeID != this.Content.TypeID)
            return;
          ItemUIUtilities.SelectedItem.CombineInto(this.Content);
        }
      }
      else
      {
        if (data.button != PointerEventData.InputButton.Right || !this.Editable || !((UnityEngine.Object) this.Content != (UnityEngine.Object) null))
          return;
        ItemOperationMenu.Show(this.itemDisplay);
      }
    }
  }

  private void OnDisplayDoubleClicked(ItemDisplay display, PointerEventData data)
  {
    this.Master.NotifyItemDoubleClicked(this, data);
  }

  public void Setup(InventoryDisplay master, int index, bool disabled = false)
  {
    this.Master = master;
    this.index = index;
    this.disabled = disabled;
    this.Refresh();
  }

  internal void Refresh()
  {
    Item content = this.Content;
    if ((UnityEngine.Object) content != (UnityEngine.Object) null)
    {
      this.cachedMeta = ItemAssetsCollection.GetMetaData(content.TypeID);
      this.cacheContentIsGun = content.Tags.Contains("Gun");
    }
    else
    {
      this.cacheContentIsGun = false;
      this.cachedMeta = new ItemMetaData();
    }
    this.itemDisplay.Setup(content);
    this.itemDisplay.CanDrop = this.CanOperate;
    this.itemDisplay.Movable = this.Movable;
    this.itemDisplay.Editable = this.Editable && this.CanOperate;
    this.itemDisplay.CanLockSort = true;
    if (!this.Master.Target.NeedInspection && (UnityEngine.Object) content != (UnityEngine.Object) null)
      content.Inspected = true;
    this.itemDisplay.ShowOperationButtons = this.Master.ShowOperationButtons;
    this.shortcutIndicator.gameObject.SetActive(this.Master.IsShortcut(this.index));
    this.disabledIndicator.SetActive(this.disabled || !this.CanOperate);
    this.highlightIndicator.SetActive(this.ShouldHighlight);
    this.lockIndicator.SetActive(this.Master.Target.IsIndexLocked(this.Index));
    Action<InventoryEntry> onRefresh = this.onRefresh;
    if (onRefresh == null)
      return;
    onRefresh(this);
  }

  public static PrefabPool<InventoryEntry> Pool => GameplayUIManager.Instance.InventoryEntryPool;

  public Item Item
  {
    get
    {
      return (UnityEngine.Object) this.itemDisplay != (UnityEngine.Object) null && this.itemDisplay.isActiveAndEnabled ? this.itemDisplay.Target : (Item) null;
    }
  }

  public static InventoryEntry Get() => InventoryEntry.Pool.Get();

  public static void Release(InventoryEntry item) => InventoryEntry.Pool.Release(item);

  public void NotifyPooled()
  {
  }

  public void NotifyReleased() => this.Master = (InventoryDisplay) null;

  public void OnPointerClick(PointerEventData eventData)
  {
    this.Punch();
    if (eventData.button != PointerEventData.InputButton.Left)
      return;
    this.lastClickTime = eventData.clickTime;
    if (this.Editable)
    {
      Item selectedItem = ItemUIUtilities.SelectedItem;
      if (!((UnityEngine.Object) selectedItem == (UnityEngine.Object) null))
      {
        if ((UnityEngine.Object) this.Content != (UnityEngine.Object) null)
        {
          Debug.Log((object) $"{this.Master.Target.name}(Inventory) 的 {this.index} 已经有物品。操作已取消。");
        }
        else
        {
          eventData.Use();
          selectedItem.Detach();
          this.Master.Target.AddAt(selectedItem, this.index);
          ItemUIUtilities.NotifyPutItem(selectedItem);
        }
      }
    }
    this.lastClickTime = eventData.clickTime;
  }

  internal void Punch() => this.itemDisplay.Punch();

  public void OnDrag(PointerEventData eventData)
  {
  }

  public void OnDrop(PointerEventData eventData)
  {
    if (eventData.used || !this.Editable || eventData.button != PointerEventData.InputButton.Left)
      return;
    IItemDragSource component = eventData.pointerDrag.gameObject.GetComponent<IItemDragSource>();
    if (component == null || !component.IsEditable())
      return;
    Item incomingItem = component.GetItem();
    if ((UnityEngine.Object) incomingItem == (UnityEngine.Object) null || incomingItem.Sticky && !this.Master.Target.AcceptSticky)
      return;
    if ((Keyboard.current == null ? 0 : (Keyboard.current.ctrlKey.isPressed ? 1 : 0)) != 0)
    {
      if ((UnityEngine.Object) this.Content != (UnityEngine.Object) null)
      {
        NotificationText.Push("UI_Inventory_TargetOccupiedCannotSplit".ToPlainText());
      }
      else
      {
        Debug.Log((object) "SPLIT");
        SplitDialogue.SetupAndShow(incomingItem, this.Master.Target, this.index);
      }
    }
    else
    {
      ItemUIUtilities.NotifyPutItem(incomingItem);
      if ((UnityEngine.Object) this.Content == (UnityEngine.Object) null)
      {
        incomingItem.Detach();
        this.Master.Target.AddAt(incomingItem, this.index);
      }
      else if (this.Content.TypeID == incomingItem.TypeID && this.Content.Stackable)
      {
        this.Content.Combine(incomingItem);
      }
      else
      {
        Inventory inInventory = incomingItem.InInventory;
        Inventory target = this.Master.Target;
        if (!((UnityEngine.Object) inInventory != (UnityEngine.Object) null))
          return;
        int index1 = inInventory.GetIndex(incomingItem);
        int index2 = this.index;
        Item content = this.Content;
        if (!((UnityEngine.Object) content != (UnityEngine.Object) incomingItem))
          return;
        incomingItem.Detach();
        content.Detach();
        inInventory.AddAt(content, index1);
        target.AddAt(incomingItem, index2);
      }
    }
  }

  public bool IsEditable()
  {
    return !((UnityEngine.Object) this.Content == (UnityEngine.Object) null) && !this.Content.NeedInspection && this.Editable;
  }

  public Item GetItem() => this.Content;

  public void OnPointerEnter(PointerEventData eventData)
  {
    this.hovering = true;
    this.hoveringIndicator?.SetActive(this.Editable);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    this.hovering = false;
    this.hoveringIndicator?.SetActive(false);
  }

  public void ToggleLock() => this.Master.Target.ToggleLockIndex(this.Index);
}
