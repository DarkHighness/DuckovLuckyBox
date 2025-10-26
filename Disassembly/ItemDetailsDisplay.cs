// Decompiled with JetBrains decompiler
// Type: Duckov.UI.ItemDetailsDisplay
// Assembly: TeamSoda.Duckov.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FDA9642D-7C8C-43D7-BA39-BA2AFEF5C9C5
// Assembly location: D:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Managed\TeamSoda.Duckov.Core.dll

using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using SodaCraft.StringUtilities;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable disable
namespace Duckov.UI;

public class ItemDetailsDisplay : MonoBehaviour
{
  [SerializeField]
  private Image icon;
  [SerializeField]
  private LeTai.TrueShadow.TrueShadow iconShadow;
  [SerializeField]
  private TextMeshProUGUI displayName;
  [SerializeField]
  private TextMeshProUGUI itemID;
  [SerializeField]
  private TextMeshProUGUI description;
  [SerializeField]
  private GameObject countContainer;
  [SerializeField]
  private TextMeshProUGUI count;
  [SerializeField]
  private GameObject durabilityContainer;
  [SerializeField]
  private TextMeshProUGUI durabilityText;
  [SerializeField]
  private TooltipsProvider durabilityToolTips;
  [SerializeField]
  [LocalizationKey("Default")]
  private string durabilityToolTipsFormatKey = "UI_DurabilityToolTips";
  [SerializeField]
  private Image durabilityFill;
  [SerializeField]
  private Image durabilityLoss;
  [SerializeField]
  private Gradient durabilityColorOverT;
  [SerializeField]
  private TextMeshProUGUI weightText;
  [SerializeField]
  private ItemSlotCollectionDisplay slotCollectionDisplay;
  [SerializeField]
  private RectTransform propertiesParent;
  [SerializeField]
  private BulletTypeDisplay bulletTypeDisplay;
  [SerializeField]
  private TagsDisplay tagsDisplay;
  [SerializeField]
  private GameObject usableIndicator;
  [SerializeField]
  private UsageUtilitiesDisplay usageUtilitiesDisplay;
  [SerializeField]
  private GameObject registeredIndicator;
  [SerializeField]
  private ItemVariableEntry variableEntryPrefab;
  [SerializeField]
  private ItemStatEntry statEntryPrefab;
  [SerializeField]
  private ItemModifierEntry modifierEntryPrefab;
  [SerializeField]
  private ItemEffectEntry effectEntryPrefab;
  [SerializeField]
  private string weightFormat = "{0:0.#} kg";
  private Item target;
  private PrefabPool<ItemVariableEntry> _variablePool;
  private PrefabPool<ItemStatEntry> _statPool;
  private PrefabPool<ItemModifierEntry> _modifierPool;
  private PrefabPool<ItemEffectEntry> _effectPool;

  private string DurabilityToolTipsFormat => this.durabilityToolTipsFormatKey.ToPlainText();

  public ItemSlotCollectionDisplay SlotCollectionDisplay => this.slotCollectionDisplay;

  private PrefabPool<ItemVariableEntry> VariablePool
  {
    get
    {
      if (this._variablePool == null)
        this._variablePool = new PrefabPool<ItemVariableEntry>(this.variableEntryPrefab, (Transform) this.propertiesParent);
      return this._variablePool;
    }
  }

  private PrefabPool<ItemStatEntry> StatPool
  {
    get
    {
      if (this._statPool == null)
        this._statPool = new PrefabPool<ItemStatEntry>(this.statEntryPrefab, (Transform) this.propertiesParent);
      return this._statPool;
    }
  }

  private PrefabPool<ItemModifierEntry> ModifierPool
  {
    get
    {
      if (this._modifierPool == null)
        this._modifierPool = new PrefabPool<ItemModifierEntry>(this.modifierEntryPrefab, (Transform) this.propertiesParent);
      return this._modifierPool;
    }
  }

  private PrefabPool<ItemEffectEntry> EffectPool
  {
    get
    {
      if (this._effectPool == null)
        this._effectPool = new PrefabPool<ItemEffectEntry>(this.effectEntryPrefab, (Transform) this.propertiesParent);
      return this._effectPool;
    }
  }

  public Item Target => this.target;

  internal void Setup(Item target)
  {
    this.UnregisterEvents();
    this.Clear();
    if ((UnityEngine.Object) target == (UnityEngine.Object) null)
      return;
    this.target = target;
    this.icon.sprite = target.Icon;
    (float shadowOffset, Color color, bool innerGlow) andColorOfQuality = GameplayDataSettings.UIStyle.GetShadowOffsetAndColorOfQuality(target.DisplayQuality);
    this.iconShadow.IgnoreCasterColor = true;
    this.iconShadow.OffsetDistance = andColorOfQuality.shadowOffset;
    this.iconShadow.Color = andColorOfQuality.color;
    this.iconShadow.Inset = andColorOfQuality.innerGlow;
    this.displayName.text = target.DisplayName;
    this.itemID.text = $"#{target.TypeID}";
    this.description.text = target.Description;
    this.countContainer.SetActive(target.Stackable);
    this.count.text = target.StackCount.ToString();
    this.tagsDisplay.Setup(target);
    this.usageUtilitiesDisplay.Setup(target);
    this.usableIndicator.gameObject.SetActive((UnityEngine.Object) target.UsageUtilities != (UnityEngine.Object) null);
    this.RefreshDurability();
    this.slotCollectionDisplay.Setup(target);
    this.registeredIndicator.SetActive(target.IsRegistered());
    this.RefreshWeightText();
    this.SetupGunDisplays();
    this.SetupVariables();
    this.SetupConstants();
    this.SetupStats();
    this.SetupModifiers();
    this.SetupEffects();
    this.RegisterEvents();
  }

  private void Awake()
  {
    this.SlotCollectionDisplay.onElementDoubleClicked += new Action<ItemSlotCollectionDisplay, SlotDisplay>(this.OnElementDoubleClicked);
  }

  private void OnElementDoubleClicked(
    ItemSlotCollectionDisplay collectionDisplay,
    SlotDisplay slotDisplay)
  {
    if (!collectionDisplay.Editable)
      return;
    Item obj = slotDisplay.GetItem();
    if ((UnityEngine.Object) obj == (UnityEngine.Object) null)
      return;
    ItemUtilities.SendToPlayer(obj, sendToStorage: (UnityEngine.Object) PlayerStorage.Instance != (UnityEngine.Object) null);
  }

  private void OnDestroy() => this.UnregisterEvents();

  private void Clear()
  {
    this.tagsDisplay.Clear();
    this.VariablePool.ReleaseAll();
    this.StatPool.ReleaseAll();
    this.ModifierPool.ReleaseAll();
    this.EffectPool.ReleaseAll();
  }

  private void SetupGunDisplays()
  {
    ItemSetting_Gun component = this.Target?.GetComponent<ItemSetting_Gun>();
    if ((UnityEngine.Object) component == (UnityEngine.Object) null)
    {
      this.bulletTypeDisplay.gameObject.SetActive(false);
    }
    else
    {
      this.bulletTypeDisplay.gameObject.SetActive(true);
      this.bulletTypeDisplay.Setup(component.TargetBulletID);
    }
  }

  private void SetupVariables()
  {
    if (this.target.Variables == null)
      return;
    foreach (CustomData variable in this.target.Variables)
    {
      if (variable.Display)
      {
        ItemVariableEntry itemVariableEntry = this.VariablePool.Get((Transform) this.propertiesParent);
        itemVariableEntry.Setup(variable);
        itemVariableEntry.transform.SetAsLastSibling();
      }
    }
  }

  private void SetupConstants()
  {
    if (this.target.Constants == null)
      return;
    foreach (CustomData constant in this.target.Constants)
    {
      if (constant.Display)
      {
        ItemVariableEntry itemVariableEntry = this.VariablePool.Get((Transform) this.propertiesParent);
        itemVariableEntry.Setup(constant);
        itemVariableEntry.transform.SetAsLastSibling();
      }
    }
  }

  private void SetupStats()
  {
    if ((UnityEngine.Object) this.target.Stats == (UnityEngine.Object) null)
      return;
    foreach (Stat stat in this.target.Stats)
    {
      if (stat.Display)
      {
        ItemStatEntry itemStatEntry = this.StatPool.Get((Transform) this.propertiesParent);
        itemStatEntry.Setup(stat);
        itemStatEntry.transform.SetAsLastSibling();
      }
    }
  }

  private void SetupModifiers()
  {
    if ((UnityEngine.Object) this.target.Modifiers == (UnityEngine.Object) null)
      return;
    foreach (ModifierDescription modifier in this.target.Modifiers)
    {
      if (modifier.Display)
      {
        ItemModifierEntry itemModifierEntry = this.ModifierPool.Get((Transform) this.propertiesParent);
        itemModifierEntry.Setup(modifier);
        itemModifierEntry.transform.SetAsLastSibling();
      }
    }
  }

  private void SetupEffects()
  {
    foreach (Effect effect in this.target.Effects)
    {
      if (effect.Display)
      {
        ItemEffectEntry itemEffectEntry = this.EffectPool.Get((Transform) this.propertiesParent);
        itemEffectEntry.Setup(effect);
        itemEffectEntry.transform.SetAsLastSibling();
      }
    }
  }

  private void RegisterEvents()
  {
    if ((UnityEngine.Object) this.target == (UnityEngine.Object) null)
      return;
    this.target.onDestroy += new Action<Item>(this.OnTargetDestroy);
    this.target.onChildChanged += new Action<Item>(this.OnTargetChildChanged);
    this.target.onSetStackCount += new Action<Item>(this.OnTargetSetStackCount);
    this.target.onDurabilityChanged += new Action<Item>(this.OnTargetDurabilityChanged);
  }

  private void RefreshWeightText()
  {
    this.weightText.text = string.Format(this.weightFormat, (object) this.target.TotalWeight);
  }

  private void OnTargetSetStackCount(Item item) => this.RefreshWeightText();

  private void OnTargetChildChanged(Item obj) => this.RefreshWeightText();

  internal void UnregisterEvents()
  {
    if ((UnityEngine.Object) this.target == (UnityEngine.Object) null)
      return;
    this.target.onDestroy -= new Action<Item>(this.OnTargetDestroy);
    this.target.onChildChanged -= new Action<Item>(this.OnTargetChildChanged);
    this.target.onSetStackCount -= new Action<Item>(this.OnTargetSetStackCount);
    this.target.onDurabilityChanged -= new Action<Item>(this.OnTargetDurabilityChanged);
  }

  private void OnTargetDurabilityChanged(Item item) => this.RefreshDurability();

  private void RefreshDurability()
  {
    bool useDurability = this.target.UseDurability;
    this.durabilityContainer.SetActive(useDurability);
    if (!useDurability)
      return;
    float durability = this.target.Durability;
    float maxDurability = this.target.MaxDurability;
    float durabilityWithLoss = this.target.MaxDurabilityWithLoss;
    string str = $"{(ValueType) (float) ((double) this.target.DurabilityLoss * 100.0):0}%";
    float time = durability / maxDurability;
    this.durabilityText.text = $"{durability:0} / {durabilityWithLoss:0}";
    this.durabilityToolTips.text = this.DurabilityToolTipsFormat.Format((object) new
    {
      curDurability = durability,
      maxDurability = maxDurability,
      maxDurabilityWithLoss = durabilityWithLoss,
      lossPercentage = str
    });
    this.durabilityFill.fillAmount = time;
    this.durabilityFill.color = this.durabilityColorOverT.Evaluate(time);
    this.durabilityLoss.fillAmount = this.target.DurabilityLoss;
  }

  private void OnTargetDestroy(Item item)
  {
  }
}
