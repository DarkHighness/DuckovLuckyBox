// Decompiled with JetBrains decompiler
// Type: Duckov.Economy.UI.StockShopView
// Assembly: TeamSoda.Duckov.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FDA9642D-7C8C-43D7-BA39-BA2AFEF5C9C5
// Assembly location: D:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Managed\TeamSoda.Duckov.Core.dll

using Cysharp.Threading.Tasks;
using Duckov.UI;
using Duckov.UI.Animations;
using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using SodaCraft.StringUtilities;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#nullable disable
namespace Duckov.Economy.UI;

public class StockShopView : View, ISingleSelectionMenu<StockShopItemEntry>
{
  [SerializeField]
  private FadeGroup fadeGroup;
  [SerializeField]
  private FadeGroup detailsFadeGroup;
  [SerializeField]
  private ItemDetailsDisplay details;
  [SerializeField]
  private InventoryDisplay playerInventoryDisplay;
  [SerializeField]
  private InventoryDisplay petInventoryDisplay;
  [SerializeField]
  private InventoryDisplay playerStorageDisplay;
  [SerializeField]
  private StockShopItemEntry entryTemplate;
  [SerializeField]
  private TextMeshProUGUI stockText;
  [SerializeField]
  [LocalizationKey("Default")]
  private string stockTextKey = "UI_Stock";
  [SerializeField]
  private string stockTextFormat = "{text} {current}/{max}";
  [SerializeField]
  private TextMeshProUGUI merchantNameText;
  [SerializeField]
  private Button interactionButton;
  [SerializeField]
  private Image interactionButtonImage;
  [SerializeField]
  private Color buttonColor_Interactable;
  [SerializeField]
  private Color buttonColor_NotInteractable;
  [SerializeField]
  private TextMeshProUGUI interactionText;
  [SerializeField]
  private GameObject cashOnlyIndicator;
  [SerializeField]
  private GameObject cannotSellIndicator;
  [LocalizationKey("Default")]
  [SerializeField]
  private string textBuy = "购买";
  [LocalizationKey("Default")]
  [SerializeField]
  private string textSoldOut = "已售罄";
  [LocalizationKey("Default")]
  [SerializeField]
  private string textSell = "出售";
  [LocalizationKey("Default")]
  [SerializeField]
  private string textUnlock = "解锁";
  [LocalizationKey("Default")]
  [SerializeField]
  private string textLocked = "已锁定";
  [SerializeField]
  private GameObject priceDisplay;
  [SerializeField]
  private TextMeshProUGUI priceText;
  [SerializeField]
  private GameObject lockDisplay;
  [SerializeField]
  private FadeGroup clickBlockerFadeGroup;
  [SerializeField]
  private TextMeshProUGUI refreshCountDown;
  private string sfx_Buy = "UI/buy";
  private string sfx_Sell = "UI/sell";
  private PrefabPool<StockShopItemEntry> _entryPool;
  private StockShop target;
  private StockShopItemEntry selectedItem;
  public Action onSelectionChanged;

  public static StockShopView Instance => View.GetViewInstance<StockShopView>();

  private string TextBuy => this.textBuy.ToPlainText();

  private string TextSoldOut => this.textSoldOut.ToPlainText();

  private string TextSell => this.textSell.ToPlainText();

  private string TextUnlock => this.textUnlock.ToPlainText();

  private string TextLocked => this.textLocked.ToPlainText();

  private PrefabPool<StockShopItemEntry> EntryPool
  {
    get
    {
      if (this._entryPool == null)
      {
        this._entryPool = new PrefabPool<StockShopItemEntry>(this.entryTemplate, this.entryTemplate.transform.parent);
        this.entryTemplate.gameObject.SetActive(false);
      }
      return this._entryPool;
    }
  }

  private UnityEngine.Object Selection
  {
    get
    {
      if ((UnityEngine.Object) ItemUIUtilities.SelectedItemDisplay != (UnityEngine.Object) null)
        return (UnityEngine.Object) ItemUIUtilities.SelectedItemDisplay;
      return (UnityEngine.Object) this.selectedItem != (UnityEngine.Object) null ? (UnityEngine.Object) this.selectedItem : (UnityEngine.Object) null;
    }
  }

  public StockShop Target => this.target;

  protected override void Awake()
  {
    base.Awake();
    this.interactionButton.onClick.AddListener(new UnityAction(this.OnInteractionButtonClicked));
    UIInputManager.OnFastPick += new Action<UIInputEventData>(this.OnFastPick);
  }

  protected override void OnDestroy()
  {
    base.OnDestroy();
    UIInputManager.OnFastPick -= new Action<UIInputEventData>(this.OnFastPick);
  }

  private void OnFastPick(UIInputEventData data)
  {
    if (!this.isActiveAndEnabled)
      return;
    this.OnInteractionButtonClicked();
  }

  private void FixedUpdate() => this.RefreshCountDown();

  private void RefreshCountDown()
  {
    if ((UnityEngine.Object) this.target == (UnityEngine.Object) null)
      this.refreshCountDown.text = "-";
    TimeSpan nextRefreshEta = this.target.NextRefreshETA;
    int days = nextRefreshEta.Days;
    int hours = nextRefreshEta.Hours;
    int minutes = nextRefreshEta.Minutes;
    int seconds = nextRefreshEta.Seconds;
    this.refreshCountDown.text = $"{(days > 0 ? (object) (days.ToString() + " - ") : (object) "")}{hours:00}:{minutes:00}:{seconds:00}";
  }

  private void OnInteractionButtonClicked()
  {
    if (this.Selection == (UnityEngine.Object) null)
      return;
    if (this.Selection is ItemDisplay selection1)
    {
      this.Target.Sell(selection1.Target).Forget();
      AudioManager.Post(this.sfx_Sell);
      ItemUIUtilities.Select((ItemDisplay) null);
      this.OnSelectionChanged();
    }
    else
    {
      if (!(this.Selection is StockShopItemEntry selection))
        return;
      int itemTypeId = selection.Target.ItemTypeID;
      if (selection.IsUnlocked())
      {
        this.BuyTask(itemTypeId).Forget();
      }
      else
      {
        if (!EconomyManager.IsWaitingForUnlockConfirm(itemTypeId))
          return;
        EconomyManager.ConfirmUnlock(itemTypeId);
      }
    }
  }

  private async UniTask BuyTask(int itemTypeID)
  {
    if (!await this.Target.Buy(itemTypeID))
      return;
    AudioManager.Post(this.sfx_Buy);
    this.clickBlockerFadeGroup.SkipShow();
    await UniTask.NextFrame();
    await this.clickBlockerFadeGroup.HideAndReturnTask();
  }

  private void OnEnable()
  {
    ItemUIUtilities.OnSelectionChanged += new Action(this.OnItemUIUtilitiesSelectionChanged);
    EconomyManager.OnItemUnlockStateChanged += new Action<int>(this.OnItemUnlockStateChanged);
    StockShop.OnAfterItemSold += new Action<StockShop>(this.OnAfterItemSold);
    UIInputManager.OnNextPage += new Action<UIInputEventData>(this.OnNextPage);
    UIInputManager.OnPreviousPage += new Action<UIInputEventData>(this.OnPreviousPage);
  }

  private void OnDisable()
  {
    ItemUIUtilities.OnSelectionChanged -= new Action(this.OnItemUIUtilitiesSelectionChanged);
    EconomyManager.OnItemUnlockStateChanged -= new Action<int>(this.OnItemUnlockStateChanged);
    StockShop.OnAfterItemSold -= new Action<StockShop>(this.OnAfterItemSold);
    UIInputManager.OnNextPage -= new Action<UIInputEventData>(this.OnNextPage);
    UIInputManager.OnPreviousPage -= new Action<UIInputEventData>(this.OnPreviousPage);
  }

  private void OnNextPage(UIInputEventData data) => this.playerStorageDisplay.NextPage();

  private void OnPreviousPage(UIInputEventData data) => this.playerStorageDisplay.PreviousPage();

  private void OnAfterItemSold(StockShop shop)
  {
    this.RefreshInteractionButton();
    this.RefreshStockText();
  }

  private void OnItemUnlockStateChanged(int itemTypeID)
  {
    if ((UnityEngine.Object) this.details.Target == (UnityEngine.Object) null || itemTypeID != this.details.Target.TypeID)
      return;
    this.RefreshInteractionButton();
    this.RefreshStockText();
  }

  private void OnItemUIUtilitiesSelectionChanged()
  {
    if ((UnityEngine.Object) this.selectedItem != (UnityEngine.Object) null && (UnityEngine.Object) ItemUIUtilities.SelectedItemDisplay != (UnityEngine.Object) null)
      this.selectedItem = (StockShopItemEntry) null;
    this.OnSelectionChanged();
  }

  private void OnSelectionChanged()
  {
    Action selectionChanged = this.onSelectionChanged;
    if (selectionChanged != null)
      selectionChanged();
    if (this.Selection == (UnityEngine.Object) null)
    {
      this.detailsFadeGroup.Hide();
    }
    else
    {
      Item target = (Item) null;
      if (this.Selection is StockShopItemEntry selection2)
        target = selection2.GetItem();
      else if (this.Selection is ItemDisplay selection1)
        target = selection1.Target;
      if ((UnityEngine.Object) target == (UnityEngine.Object) null)
      {
        this.detailsFadeGroup.Hide();
      }
      else
      {
        this.details.Setup(target);
        this.RefreshStockText();
        this.RefreshInteractionButton();
        this.RefreshCountDown();
        this.detailsFadeGroup.Show();
      }
    }
  }

  private void RefreshStockText()
  {
    if (this.Selection is StockShopItemEntry selection)
    {
      this.stockText.gameObject.SetActive(true);
      this.stockText.text = this.stockTextFormat.Format((object) new
      {
        text = this.stockTextKey.ToPlainText(),
        current = selection.Target.CurrentStock,
        max = selection.Target.MaxStock
      });
    }
    else
    {
      if (!(this.Selection is ItemDisplay))
        return;
      this.stockText.gameObject.SetActive(false);
    }
  }

  public StockShopItemEntry GetSelection() => this.Selection as StockShopItemEntry;

  public bool SetSelection(StockShopItemEntry selection)
  {
    if ((UnityEngine.Object) ItemUIUtilities.SelectedItem != (UnityEngine.Object) null)
      ItemUIUtilities.Select((ItemDisplay) null);
    this.selectedItem = selection;
    this.OnSelectionChanged();
    return true;
  }

  internal void Setup(StockShop target)
  {
    this.target = target;
    this.detailsFadeGroup.SkipHide();
    this.merchantNameText.text = target.DisplayName;
    this.playerInventoryDisplay.Setup(LevelManager.Instance?.MainCharacter?.CharacterItem?.Inventory, funcCanOperate: (Func<Item, bool>) (e => (UnityEngine.Object) e == (UnityEngine.Object) null || e.CanBeSold));
    if ((UnityEngine.Object) PetProxy.PetInventory != (UnityEngine.Object) null)
    {
      this.petInventoryDisplay.Setup(PetProxy.PetInventory, funcCanOperate: (Func<Item, bool>) (e => (UnityEngine.Object) e == (UnityEngine.Object) null || e.CanBeSold));
      this.petInventoryDisplay.gameObject.SetActive(true);
    }
    else
      this.petInventoryDisplay.gameObject.SetActive(false);
    Inventory inventory = PlayerStorage.Inventory;
    if ((UnityEngine.Object) inventory != (UnityEngine.Object) null)
    {
      this.playerStorageDisplay.gameObject.SetActive(true);
      this.playerStorageDisplay.Setup(inventory, funcCanOperate: (Func<Item, bool>) (e => (UnityEngine.Object) e == (UnityEngine.Object) null || e.CanBeSold));
    }
    else
      this.playerStorageDisplay.gameObject.SetActive(false);
    this.EntryPool.ReleaseAll();
    Transform parent = this.entryTemplate.transform.parent;
    foreach (StockShop.Entry entry in target.entries)
    {
      if (entry.Show)
      {
        StockShopItemEntry stockShopItemEntry = this.EntryPool.Get(parent);
        stockShopItemEntry.Setup(this, entry);
        stockShopItemEntry.transform.SetAsLastSibling();
      }
    }
    TradingUIUtilities.ActiveMerchant = (IMerchant) target;
  }

  private void RefreshInteractionButton()
  {
    this.cannotSellIndicator.SetActive(false);
    this.cashOnlyIndicator.SetActive(!this.Target.AccountAvaliable);
    if (this.Selection is ItemDisplay selection1)
    {
      this.interactionButton.interactable = selection1.Target.CanBeSold;
      this.priceDisplay.gameObject.SetActive(true);
      this.lockDisplay.gameObject.SetActive(false);
      this.interactionText.text = this.TextSell;
      this.interactionButtonImage.color = this.buttonColor_Interactable;
      this.priceText.text = GetPriceText(selection1.Target, true);
      this.cannotSellIndicator.SetActive(!selection1.Target.CanBeSold);
    }
    else
    {
      if (!(this.Selection is StockShopItemEntry selection))
        return;
      bool flag1 = selection.IsUnlocked();
      bool flag2 = EconomyManager.IsWaitingForUnlockConfirm(selection.Target.ItemTypeID);
      this.interactionButton.interactable = flag1 | flag2;
      this.priceDisplay.gameObject.SetActive(flag1);
      this.lockDisplay.gameObject.SetActive(!flag1);
      this.cannotSellIndicator.SetActive(false);
      if (flag1)
      {
        int price = GetPrice(selection.GetItem(), false);
        bool enough = new Cost((long) price).Enough;
        this.priceText.text = price.ToString("n0");
        if (selection.Target.CurrentStock > 0)
        {
          this.interactionText.text = this.TextBuy;
          this.interactionButtonImage.color = enough ? this.buttonColor_Interactable : this.buttonColor_NotInteractable;
        }
        else
        {
          this.interactionButton.interactable = false;
          this.interactionText.text = this.TextSoldOut;
          this.interactionButtonImage.color = this.buttonColor_NotInteractable;
        }
      }
      else if (flag2)
      {
        this.interactionText.text = this.TextUnlock;
        this.interactionButtonImage.color = this.buttonColor_Interactable;
      }
      else
      {
        this.interactionText.text = this.TextLocked;
        this.interactionButtonImage.color = this.buttonColor_NotInteractable;
      }
    }

    int GetPrice(Item item, bool selling) => this.Target.ConvertPrice(item, selling);

    string GetPriceText(Item item, bool selling) => GetPrice(item, selling).ToString("n0");
  }

  protected override void OnOpen()
  {
    base.OnOpen();
    this.fadeGroup.Show();
  }

  protected override void OnClose()
  {
    base.OnClose();
    this.fadeGroup.Hide();
  }

  internal void SetupAndShow(StockShop stockShop)
  {
    ItemUIUtilities.Select((ItemDisplay) null);
    this.SetSelection((StockShopItemEntry) null);
    this.Setup(stockShop);
    this.Open();
  }
}
