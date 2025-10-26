// Decompiled with JetBrains decompiler
// Type: Duckov.UI.InventoryDisplay
// Assembly: TeamSoda.Duckov.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FDA9642D-7C8C-43D7-BA39-BA2AFEF5C9C5
// Assembly location: D:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Managed\TeamSoda.Duckov.Core.dll

using Cysharp.Threading.Tasks;
using Duckov.UI.Animations;
using Duckov.Utilities;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#nullable disable
namespace Duckov.UI;

public class InventoryDisplay : MonoBehaviour, IPoolable
{
  [SerializeField]
  private InventoryEntry entryPrefab;
  [SerializeField]
  private TextMeshProUGUI displayNameText;
  [SerializeField]
  private TextMeshProUGUI capacityText;
  [SerializeField]
  private string capacityTextFormat = "({1}/{0})";
  [SerializeField]
  private FadeGroup loadingIndcator;
  [SerializeField]
  private FadeGroup contentFadeGroup;
  [SerializeField]
  private GridLayoutGroup contentLayout;
  [SerializeField]
  private LayoutElement gridLayoutElement;
  [SerializeField]
  private GameObject placeHolder;
  [SerializeField]
  private Transform entriesParent;
  [SerializeField]
  private Button sortButton;
  [SerializeField]
  private Vector2Int shortcutsRange = new Vector2Int(0, 3);
  [SerializeField]
  private bool editable = true;
  [SerializeField]
  private bool showOperationButtons = true;
  [SerializeField]
  private bool showSortButton;
  [SerializeField]
  private bool usePages;
  [SerializeField]
  private int itemsEachPage = 30;
  public Func<Item, bool> filter;
  [SerializeField]
  private List<InventoryEntry> entries = new List<InventoryEntry>();
  private PrefabPool<InventoryEntry> _entryPool;
  private Func<Item, bool> _func_ShouldHighlight;
  private Func<Item, bool> _func_CanOperate;
  private int cachedCapacity = -1;
  private int activeTaskToken;
  private int cachedMaxPage = 1;
  private int cachedSelectedPage;
  private List<int> cachedIndexesToDisplay = new List<int>();

  private bool shortcuts => false;

  public bool UsePages => this.usePages;

  public bool Editable
  {
    get => this.editable;
    internal set => this.editable = value;
  }

  public bool ShowOperationButtons
  {
    get => this.showOperationButtons;
    internal set => this.showOperationButtons = value;
  }

  public bool Movable { get; private set; }

  public Inventory Target { get; private set; }

  private PrefabPool<InventoryEntry> EntryPool
  {
    get
    {
      if (this._entryPool == null && (UnityEngine.Object) this.entryPrefab != (UnityEngine.Object) null)
        this._entryPool = new PrefabPool<InventoryEntry>(this.entryPrefab, this.contentLayout.transform);
      return this._entryPool;
    }
  }

  public event Action<InventoryDisplay, InventoryEntry, PointerEventData> onDisplayDoubleClicked;

  public event Action onPageInfoRefreshed;

  public Func<Item, bool> Func_ShouldHighlight => this._func_ShouldHighlight;

  public Func<Item, bool> Func_CanOperate => this._func_CanOperate;

  public bool ShowSortButton
  {
    get => this.showSortButton;
    internal set => this.showSortButton = value;
  }

  private void RegisterEvents()
  {
    if ((UnityEngine.Object) this.Target == (UnityEngine.Object) null)
      return;
    this.UnregisterEvents();
    this.Target.onContentChanged += new Action<Inventory, int>(this.OnTargetContentChanged);
    this.Target.onInventorySorted += new Action<Inventory>(this.OnTargetSorted);
    this.Target.onSetIndexLock += new Action<Inventory, int>(this.OnTargetSetIndexLock);
  }

  private void UnregisterEvents()
  {
    if ((UnityEngine.Object) this.Target == (UnityEngine.Object) null)
      return;
    this.Target.onContentChanged -= new Action<Inventory, int>(this.OnTargetContentChanged);
    this.Target.onInventorySorted -= new Action<Inventory>(this.OnTargetSorted);
    this.Target.onSetIndexLock -= new Action<Inventory, int>(this.OnTargetSetIndexLock);
  }

  private void OnTargetSetIndexLock(Inventory inventory, int index)
  {
    foreach (InventoryEntry entry in this.entries)
    {
      if (!((UnityEngine.Object) entry == (UnityEngine.Object) null) && entry.isActiveAndEnabled && entry.Index == index)
        entry.Refresh();
    }
  }

  private void OnTargetSorted(Inventory inventory)
  {
    if (this.filter == null)
    {
      foreach (InventoryEntry entry in this.entries)
        entry.Refresh();
    }
    else
      this.LoadEntriesTask().Forget();
  }

  private void OnTargetContentChanged(Inventory inventory, int position)
  {
    if (this.Target.Loading)
      return;
    if (this.filter != null)
    {
      this.RefreshCapacityText();
      this.LoadEntriesTask().Forget();
    }
    else
    {
      this.RefreshCapacityText();
      InventoryEntry inventoryEntry1 = this.entries.Find((Predicate<InventoryEntry>) (e => (UnityEngine.Object) e != (UnityEngine.Object) null && e.Index == position));
      if (!(bool) (UnityEngine.Object) inventoryEntry1)
        return;
      InventoryEntry inventoryEntry2 = inventoryEntry1;
      inventoryEntry2.Refresh();
      inventoryEntry2.Punch();
    }
  }

  private void RefreshCapacityText()
  {
    if ((UnityEngine.Object) this.Target == (UnityEngine.Object) null || !(bool) (UnityEngine.Object) this.capacityText)
      return;
    this.capacityText.text = string.Format(this.capacityTextFormat, (object) this.Target.Capacity, (object) this.Target.GetItemCount());
  }

  public void Setup(
    Inventory target,
    Func<Item, bool> funcShouldHighLight = null,
    Func<Item, bool> funcCanOperate = null,
    bool movable = false,
    Func<Item, bool> filter = null)
  {
    this.UnregisterEvents();
    this.Target = target;
    this.Clear();
    if ((UnityEngine.Object) this.Target == (UnityEngine.Object) null || this.Target.Loading)
      return;
    this._func_ShouldHighlight = funcShouldHighLight != null ? funcShouldHighLight : (Func<Item, bool>) (e => false);
    this._func_CanOperate = funcCanOperate != null ? funcCanOperate : (Func<Item, bool>) (e => true);
    this.displayNameText.text = target.DisplayName;
    this.Movable = movable;
    this.cachedCapacity = target.Capacity;
    this.filter = filter;
    this.RefreshCapacityText();
    this.RegisterEvents();
    this.sortButton.gameObject.SetActive(this.editable && this.showSortButton);
    this.LoadEntriesTask().Forget();
  }

  private void RefreshGridLayoutPreferredHeight()
  {
    if ((UnityEngine.Object) this.Target == (UnityEngine.Object) null)
    {
      this.placeHolder.gameObject.SetActive(true);
    }
    else
    {
      int num1 = this.cachedIndexesToDisplay.Count;
      if (this.usePages && num1 > 0)
      {
        int num2 = this.cachedSelectedPage * this.itemsEachPage;
        num1 = Mathf.Max(0, Mathf.Min(num2 + this.itemsEachPage, this.cachedIndexesToDisplay.Count) - num2);
      }
      this.gridLayoutElement.preferredHeight = (float) Mathf.CeilToInt((float) num1 / (float) this.contentLayout.constraintCount) * this.contentLayout.cellSize.y + (float) this.contentLayout.padding.top + (float) this.contentLayout.padding.bottom;
      this.placeHolder.gameObject.SetActive(num1 <= 0);
    }
  }

  public int MaxPage => this.cachedMaxPage;

  public int SelectedPage => this.cachedSelectedPage;

  public void SetPage(int page)
  {
    this.cachedSelectedPage = page;
    Action pageInfoRefreshed = this.onPageInfoRefreshed;
    if (pageInfoRefreshed != null)
      pageInfoRefreshed();
    this.LoadEntriesTask().Forget();
  }

  public void NextPage()
  {
    int page = this.cachedSelectedPage + 1;
    if (page >= this.cachedMaxPage)
      page = 0;
    this.SetPage(page);
  }

  public void PreviousPage()
  {
    int page = this.cachedSelectedPage - 1;
    if (page < 0)
      page = this.cachedMaxPage - 1;
    this.SetPage(page);
  }

  private void CacheIndexesToDisplay()
  {
    this.cachedIndexesToDisplay.Clear();
    for (int position = 0; position < this.Target.Capacity; ++position)
    {
      if (this.filter == null || this.filter(this.Target.GetItemAt(position)))
        this.cachedIndexesToDisplay.Add(position);
    }
    int count = this.cachedIndexesToDisplay.Count;
    this.cachedMaxPage = count / this.itemsEachPage + (count % this.itemsEachPage > 0 ? 1 : 0);
    if (this.cachedSelectedPage >= this.cachedMaxPage)
      this.cachedSelectedPage = Mathf.Max(0, this.cachedMaxPage - 1);
    Action pageInfoRefreshed = this.onPageInfoRefreshed;
    if (pageInfoRefreshed == null)
      return;
    pageInfoRefreshed();
  }

  private async UniTask LoadEntriesTask()
  {
    InventoryDisplay master = this;
    // ISSUE: variable of a compiler-generated type
    InventoryDisplay.\u003C\u003Ec__DisplayClass76_0 cDisplayClass760;
    // ISSUE: reference to a compiler-generated field
    cDisplayClass760.\u003C\u003E4__this = this;
    master.placeHolder.gameObject.SetActive(false);
    ref InventoryDisplay.\u003C\u003Ec__DisplayClass76_0 local = ref cDisplayClass760;
    InventoryDisplay inventoryDisplay = master;
    int num1 = master.activeTaskToken + 1;
    int num2 = num1;
    inventoryDisplay.activeTaskToken = num2;
    int num3 = num1;
    // ISSUE: reference to a compiler-generated field
    local.token = num3;
    master.EntryPool.ReleaseAll();
    master.entries.Clear();
    int batchCount = 5;
    int num4 = 0;
    master.CacheIndexesToDisplay();
    master.RefreshGridLayoutPreferredHeight();
    master.contentFadeGroup.SkipHide();
    master.loadingIndcator.Show();
    if (master.usePages)
    {
      int begin = master.cachedSelectedPage * master.itemsEachPage;
      int end_exclusive = Mathf.Min(begin + master.itemsEachPage, master.cachedIndexesToDisplay.Count);
      // ISSUE: reference to a compiler-generated field
      // ISSUE: reference to a compiler-generated method
      cDisplayClass760.indexes = begin >= master.cachedIndexesToDisplay.Count || begin >= end_exclusive ? new List<int>() : master.\u003CLoadEntriesTask\u003Eg__GetRange\u007C76_1(begin, end_exclusive, master.cachedIndexesToDisplay, ref cDisplayClass760);
    }
    else
    {
      // ISSUE: reference to a compiler-generated field
      cDisplayClass760.indexes = master.cachedIndexesToDisplay;
    }
    // ISSUE: reference to a compiler-generated field
    foreach (int index in cDisplayClass760.indexes)
    {
      if (num4 >= batchCount)
      {
        await UniTask.Yield();
        // ISSUE: reference to a compiler-generated method
        if (!master.\u003CLoadEntriesTask\u003Eg__TaskValid\u007C76_0(ref cDisplayClass760))
        {
          // ISSUE: object of a compiler-generated type is created
          cDisplayClass760 = new InventoryDisplay.\u003C\u003Ec__DisplayClass76_0();
          return;
        }
        num4 = 0;
      }
      InventoryEntry newInventoryEntry = master.GetNewInventoryEntry();
      newInventoryEntry.gameObject.SetActive(true);
      newInventoryEntry.Setup(master, index);
      master.entries.Add(newInventoryEntry);
      newInventoryEntry.transform.SetParent(master.entriesParent, false);
      ++num4;
    }
    master.loadingIndcator.Hide();
    master.contentFadeGroup.Show();
    // ISSUE: object of a compiler-generated type is created
    cDisplayClass760 = new InventoryDisplay.\u003C\u003Ec__DisplayClass76_0();
  }

  public void SetFilter(Func<Item, bool> filter)
  {
    this.filter = filter;
    this.cachedSelectedPage = 0;
    this.LoadEntriesTask().Forget();
  }

  private void Clear()
  {
    this.EntryPool.ReleaseAll();
    this.entries.Clear();
  }

  private void Awake()
  {
    this.sortButton.onClick.AddListener(new UnityAction(this.OnSortButtonClicked));
  }

  private void OnSortButtonClicked()
  {
    if (!this.Editable || !(bool) (UnityEngine.Object) this.Target || this.Target.Loading)
      return;
    this.Target.Sort();
  }

  private void OnEnable() => this.RegisterEvents();

  private void OnDisable()
  {
    this.UnregisterEvents();
    ++this.activeTaskToken;
  }

  private void Update()
  {
    if (!(bool) (UnityEngine.Object) this.Target || this.cachedCapacity == this.Target.Capacity)
      return;
    this.OnCapacityChanged();
  }

  private void OnCapacityChanged()
  {
    if ((UnityEngine.Object) this.Target == (UnityEngine.Object) null)
      return;
    this.cachedCapacity = this.Target.Capacity;
    this.RefreshCapacityText();
    this.LoadEntriesTask().Forget();
  }

  public bool IsShortcut(int index)
  {
    return this.shortcuts && index >= this.shortcutsRange.x && index <= this.shortcutsRange.y;
  }

  private InventoryEntry GetNewInventoryEntry() => this.EntryPool.Get();

  internal void NotifyItemDoubleClicked(InventoryEntry inventoryEntry, PointerEventData data)
  {
    Action<InventoryDisplay, InventoryEntry, PointerEventData> displayDoubleClicked = this.onDisplayDoubleClicked;
    if (displayDoubleClicked == null)
      return;
    displayDoubleClicked(this, inventoryEntry, data);
  }

  public void NotifyPooled()
  {
  }

  public void NotifyReleased()
  {
  }

  public void DisableItem(Item item)
  {
    foreach (InventoryEntry inventoryEntry in this.entries.Where<InventoryEntry>((Func<InventoryEntry, bool>) (e => (UnityEngine.Object) e.Content == (UnityEngine.Object) item)))
      inventoryEntry.Disabled = true;
  }

  internal bool EvaluateShouldHighlight(Item content)
  {
    if (this.Func_ShouldHighlight != null && this.Func_ShouldHighlight(content))
      return true;
    int num = (UnityEngine.Object) content == (UnityEngine.Object) null ? 1 : 0;
    return false;
  }
}
