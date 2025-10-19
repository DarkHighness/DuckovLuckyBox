using Duckov;
using Duckov.UI;
using Duckov.Economy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Duckov.Economy;
using SodaCraft.Localizations;
using HarmonyLib;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System;
using ItemStatsSystem;
using System.Linq;

namespace DuckovLuckyBox
{

  [HarmonyPatch(typeof(StockShopView), "Setup")]
  public class PatchStockShopView
  {
    private static TextMeshProUGUI? _refreshStockText;
    private static Button? _refreshStockButton;
    private static TextMeshProUGUI? _pickOneText;
    private static Button? _pickOneButton;
    private static TextMeshProUGUI? _buyLuckyBoxText;
    private static Button? _buyLuckyBoxButton;
    private static RectTransform? _actionsContainer;
    private static readonly string SFX_BUY = "UI/buy";
    private static List<int>? _itemTypeIdsCache = null;
    public static List<int> ItemTypeIdsCache
    {
      get
      {
        if (_itemTypeIdsCache == null)
        {
          _itemTypeIdsCache = ItemAssetsCollection.Instance.entries.Select(entry => entry.typeID).ToList();
        }
        return _itemTypeIdsCache;
      }
    }


    public static void Postfix(StockShopView __instance)
    {
      var merchantNameText = GetMerchantNameText(__instance);
      if (merchantNameText == null) return;

      EnsureTexts(merchantNameText);
      EnsureButtons(__instance);
    }

    private static TextMeshProUGUI? GetMerchantNameText(StockShopView instance)
    {
      var field = typeof(StockShopView).GetField("merchantNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      if (field == null)
      {
        Log.Error("Failed to find merchantNameText field in StockShopView");
        return null;
      }

      var merchantNameText = field.GetValue(instance) as TextMeshProUGUI;
      if (merchantNameText == null)
      {
        Log.Error("Failed to get merchantNameText from StockShopView");
      }

      return merchantNameText;
    }

    private static void EnsureTexts(TextMeshProUGUI merchantNameText)
    {
      if (_refreshStockText != null || _pickOneText != null) return;

      if (_actionsContainer == null)
      {
        var parent = merchantNameText.transform.parent as RectTransform;
        if (parent == null) return;

        _actionsContainer = new GameObject("ExtraActionsContainer", typeof(RectTransform)).GetComponent<RectTransform>();
        _actionsContainer.SetParent(parent, false);

        float anchorOffset = merchantNameText.rectTransform.rect.height;
        if (anchorOffset <= 0f) anchorOffset = merchantNameText.fontSize + 40f;
        _actionsContainer.anchorMin = new Vector2(0.5f, 1f);
        _actionsContainer.anchorMax = new Vector2(0.5f, 1f);
        _actionsContainer.pivot = new Vector2(0.5f, 1f);
        _actionsContainer.anchoredPosition = new Vector2(0f, -anchorOffset);

        float width = merchantNameText.rectTransform.rect.width;
        if (width <= 0f) width = 320f;
        _actionsContainer.sizeDelta = new Vector2(width, 48f);

        var layout = _actionsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = 16f;
        layout.padding = new RectOffset(12, 12, 8, 8);
      }

      _refreshStockText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
      ConfigureActionLabel(_refreshStockText, Constants.I18n.RefreshStockKey.ToPlainText());

      _pickOneText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
      ConfigureActionLabel(_pickOneText, Constants.I18n.PickOneKey.ToPlainText());

      _buyLuckyBoxText = UnityEngine.Object.Instantiate(merchantNameText, _actionsContainer);
      ConfigureActionLabel(_buyLuckyBoxText, Constants.I18n.BuyLuckyBoxText.ToPlainText());
    }

    private static void EnsureButtons(StockShopView view)
    {
      if (_refreshStockButton != null || _pickOneButton != null || _buyLuckyBoxButton != null) return;
      if (_refreshStockText == null || _pickOneText == null || _buyLuckyBoxText == null) return;

      _refreshStockButton = _refreshStockText.gameObject.AddComponent<Button>();
      ConfigureActionButton(_refreshStockButton, _refreshStockText);

      _pickOneButton = _pickOneText.gameObject.AddComponent<Button>();
      ConfigureActionButton(_pickOneButton, _pickOneText);

      _buyLuckyBoxButton = _buyLuckyBoxText.gameObject.AddComponent<Button>();
      ConfigureActionButton(_buyLuckyBoxButton, _buyLuckyBoxText);

      _refreshStockButton.onClick.AddListener(() => OnRefreshButtonClicked(view));
      _pickOneButton.onClick.AddListener(() => OnPickOneClicked(view).Forget());
      _buyLuckyBoxButton.onClick.AddListener(() => OnBuyLuckyBoxClicked().Forget());
    }

    private static async UniTask OnBuyLuckyBoxClicked()
    {
      var selectedIndex = UnityEngine.Random.Range(0, ItemTypeIdsCache.Count);
      var selectedItemTypeId = ItemTypeIdsCache[selectedIndex];
      Item obj = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
      if (!ItemUtilities.SendToPlayerCharacterInventory(obj))
      {
        Log.Error($"Failed to send item to player inventory: {selectedItemTypeId}. Send to the player storage.");
        ItemUtilities.SendToPlayerStorage(obj);
      }
      var messageTemplate = Constants.I18n.PickOneNotificationFormatKey.ToPlainText();
      var message = messageTemplate.Replace("{itemDisplayName}", obj.DisplayName);
      NotificationText.Push(message);
      AudioManager.Post(SFX_BUY);
    }

    private static void OnRefreshButtonClicked(StockShopView stockShopView)
    {
      if (!TryGetStockShop(stockShopView, out var stockShop)) return;
      if (!TryInvokeRefresh(stockShop)) return;
      AudioManager.Post(SFX_BUY);
      Log.Debug("Stock refreshed");
    }


    private static async UniTask OnPickOneClicked(StockShopView stockShopView)
    {
      if (!TryGetStockShop(stockShopView, out var stockShop)) return;
      if (stockShop == null) return;

      if (stockShop.Busy) return;

      var itemEntries = new List<StockShop.Entry>();
      foreach (var entry in stockShop.entries)
      {
        if (entry == null) continue;
        if (entry.CurrentStock <= 0) continue;
        if (entry.Possibility <= 0f) continue;
        if (!entry.Show) continue;

        itemEntries.Add(entry);
      }

      if (itemEntries.Count == 0)
      {
        Log.Warning("No available items to pick");
        return;
      }

      var randomIndex = UnityEngine.Random.Range(0, itemEntries.Count);
      var pickedItem = itemEntries[randomIndex];

      if (!SetBuyingState(stockShop, true)) return;
      Item item = stockShop.GetItemInstanceDirect(pickedItem.ItemTypeID);
      if (item == null)
      {
        Log.Error("Failed to get item instance for " + pickedItem.ItemTypeID);
        SetBuyingState(stockShop, false);
        return;
      }

      Item obj = await ItemAssetsCollection.InstantiateAsync(pickedItem.ItemTypeID);
      if (!ItemUtilities.SendToPlayerCharacterInventory(obj))
      {
        Log.Error($"Failed to send item to player inventory: {pickedItem.ItemTypeID}. Send to the player storage.");
        ItemUtilities.SendToPlayerStorage(obj);
      }

      pickedItem.CurrentStock = Math.Max(0, pickedItem.CurrentStock - 1);

      var onAfterItemSoldField = AccessTools.Field(typeof(StockShop), "OnAfterItemSold");
      if (onAfterItemSoldField?.GetValue(null) is Action<StockShop> onAfterItemSold)
        onAfterItemSold(stockShop);
      var onItemPurchasedField = AccessTools.Field(typeof(StockShop), "OnItemPurchased");
      if (onItemPurchasedField?.GetValue(null) is Action<StockShop, Item> onItemPurchased)
        onItemPurchased(stockShop, obj);

      var messageTemplate = Constants.I18n.PickOneNotificationFormatKey.ToPlainText();
      var message = messageTemplate.Replace("{itemDisplayName}", obj.DisplayName);
      NotificationText.Push(message);
      AudioManager.Post(SFX_BUY);

      if (!SetBuyingState(stockShop, false)) return;
      return;
    }

    private static bool SetBuyingState(StockShop? stockShop, bool isBuying)
    {
      if (stockShop == null) return false;

      AccessTools.Field(typeof(StockShop), "buying").SetValue(stockShop, isBuying);
      return true;
    }

    private static bool TryGetStockShop(StockShopView view, out StockShop? stockShop)
    {

      stockShop = AccessTools.Field(typeof(StockShopView), "target").GetValue(view) as StockShop;
      return stockShop != null;
    }

    private static bool TryInvokeRefresh(StockShop? stockShop)
    {
      if (stockShop == null) return false;

      AccessTools.Method(typeof(StockShop), "DoRefreshStock").Invoke(stockShop, null);
      return true;
    }

    private static void ConfigureActionLabel(TextMeshProUGUI label, string text)
    {
      label.text = text;
      label.margin = Vector4.zero;
      label.alignment = TextAlignmentOptions.Center;
      label.enableAutoSizing = false;
      label.fontSize = Mathf.Max(18f, label.fontSize * 0.9f);
      label.raycastTarget = true;

      var rectTransform = label.rectTransform;
      rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
      rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
      rectTransform.pivot = new Vector2(0.5f, 0.5f);
      rectTransform.sizeDelta = new Vector2(0f, 40f);

      var layoutElement = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
      layoutElement.preferredHeight = 40f;
      layoutElement.preferredWidth = Mathf.Max(140f, label.preferredWidth + 24f);
      layoutElement.flexibleWidth = 0f;
    }

    private static void ConfigureActionButton(Button button, TextMeshProUGUI label)
    {
      button.transition = Selectable.Transition.ColorTint;
      button.targetGraphic = label;

      var colors = button.colors;
      colors.normalColor = Color.white;
      colors.highlightedColor = new Color(0.9f, 0.95f, 1f, 1f);
      colors.pressedColor = new Color(0.8f, 0.85f, 0.95f, 1f);
      colors.selectedColor = colors.highlightedColor;
      colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
      button.colors = colors;
    }
  }
}