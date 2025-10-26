using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Duckov;
using Duckov.Economy.UI;
using Duckov.UI;
using Duckov.UI.Animations;
using DuckovLuckyBox.Core;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckovLuckyBox.Patches.StockShopActions
{
    /// <summary>
    /// Implements the stock shop "trash bin" action using native inventory widgets.
    /// </summary>
    public sealed class CycleBinAction : IStockShopAction
    {
    private const int ContractSize = 5;
    private const float ContractVerticalOffset = -120f;

        private static readonly Dictionary<StockShopView, ContractSession> Sessions = new Dictionary<StockShopView, ContractSession>();

        public string GetLocalizationKey() => Localizations.I18n.TrashBinKey;

        public async UniTask ExecuteAsync(StockShopView stockShopView)
        {
            if (stockShopView == null)
            {
                Log.Warning("CycleBinAction executed without a StockShopView instance.");
                return;
            }

            var session = GetOrCreateSession(stockShopView);
            session.Toggle();
            await UniTask.CompletedTask;
        }

        internal static void OnViewOpened(StockShopView view)
        {
            if (view == null)
            {
                return;
            }

            if (Sessions.TryGetValue(view, out var session))
            {
                session.OnViewOpened();
            }
        }

        internal static void OnViewClosed(StockShopView view)
        {
            if (view == null)
            {
                return;
            }

            if (Sessions.TryGetValue(view, out var session))
            {
                session.OnViewClosed();
            }
        }

        private static ContractSession GetOrCreateSession(StockShopView view)
        {
            if (!Sessions.TryGetValue(view, out var session))
            {
                session = new ContractSession(view);
                Sessions[view] = session;
            }

            return session;
        }

        private sealed class ContractSession
        {
            private readonly StockShopView _view;
            private readonly Dictionary<Item, Inventory> _originalInventories = new Dictionary<Item, Inventory>();
            private readonly List<InventoryDisplay> _monitoredDisplays = new List<InventoryDisplay>();

            private FadeGroup? _detailsFadeGroup;
            private RectTransform? _contractRoot;
            private InventoryDisplay? _contractDisplay;
            private Inventory? _contractInventory;
            private Button? _clearButton;
            private TextMeshProUGUI? _clearButtonLabel;

            private bool _initialized;
            private bool _visible;
            private bool _busy;

            public ContractSession(StockShopView view)
            {
                _view = view;
            }

            public void Toggle()
            {
                if (_visible)
                {
                    Hide(false);
                }
                else
                {
                    Show();
                }
            }

            public void OnViewOpened()
            {
                Hide(true);
            }

            public void OnViewClosed() => Hide(true);

            private void Show()
            {
                EnsureInitialized();
                if (_contractRoot == null || _contractDisplay == null || _contractInventory == null)
                {
                    Log.Warning("Cycle bin UI failed to initialize correctly.");
                    return;
                }

                _visible = true;
                _contractRoot.gameObject.SetActive(true);
                _contractDisplay.gameObject.SetActive(true);
                _contractDisplay.Setup(_contractInventory, funcCanOperate: _ => true, movable: false);
                _contractDisplay.onDisplayDoubleClicked -= OnContractDoubleClicked;
                _contractDisplay.onDisplayDoubleClicked += OnContractDoubleClicked;

                SubscribeMonitoredDisplays();

                try
                {
                    _detailsFadeGroup?.Hide();
                }
                catch (Exception ex)
                {
                    Log.Debug($"Failed to hide details fade group: {ex.Message}");
                }

                UpdateClearButtonState();
            }

            private void Hide(bool force)
            {
                if (!_initialized)
                {
                    return;
                }

                if (!_visible && !force)
                {
                    return;
                }

                _visible = false;

                if (_contractDisplay != null)
                {
                    _contractDisplay.onDisplayDoubleClicked -= OnContractDoubleClicked;
                }

                UnsubscribeMonitoredDisplays();

                ReturnAllItems();
                UpdateClearButtonState();

                if (_contractRoot != null)
                {
                    _contractRoot.gameObject.SetActive(false);
                }
            }

            private void EnsureInitialized()
            {
                if (_initialized)
                {
                    return;
                }

                CaptureViewReferences();

                if (_detailsFadeGroup == null)
                {
                    Log.Warning("Cycle bin could not locate details fade group in StockShopView.");
                    return;
                }

                var templateDisplay = GetInventoryDisplay("playerInventoryDisplay");
                if (templateDisplay == null)
                {
                    Log.Warning("Cycle bin could not locate a template InventoryDisplay.");
                    return;
                }

                BuildContractRoot();
                if (_contractRoot == null)
                {
                    return;
                }

                CreateContractInventory();
                if (_contractInventory == null)
                {
                    return;
                }

                CreateContractDisplay(templateDisplay);
                CreateClearButton();

                _initialized = _contractRoot != null && _contractDisplay != null && _contractInventory != null;
            }

            private void CaptureViewReferences()
            {
                var detailsField = AccessTools.Field(typeof(StockShopView), "detailsFadeGroup");
                _detailsFadeGroup = detailsField?.GetValue(_view) as FadeGroup;

                _monitoredDisplays.Clear();
                AddDisplay("playerInventoryDisplay");
                AddDisplay("petInventoryDisplay");
                AddDisplay("playerStorageDisplay");
            }

            private void AddDisplay(string fieldName)
            {
                var display = GetInventoryDisplay(fieldName);
                if (display != null && !_monitoredDisplays.Contains(display))
                {
                    _monitoredDisplays.Add(display);
                }
            }

            private InventoryDisplay? GetInventoryDisplay(string fieldName)
            {
                var field = AccessTools.Field(typeof(StockShopView), fieldName);
                return field?.GetValue(_view) as InventoryDisplay;
            }

            private void BuildContractRoot()
            {
                if (_detailsFadeGroup == null)
                {
                    return;
                }

                var detailsRect = _detailsFadeGroup.transform as RectTransform;
                if (detailsRect == null)
                {
                    return;
                }

                _contractRoot = new GameObject("CycleBinContractRoot", typeof(RectTransform)).GetComponent<RectTransform>();
                if (_contractRoot == null)
                {
                    return;
                }

                Vector2 targetSize = detailsRect.rect.size;
                if (targetSize == Vector2.zero)
                {
                    targetSize = detailsRect.sizeDelta;
                }

                _contractRoot.SetParent(detailsRect.parent, false);
                _contractRoot.anchorMin = new Vector2(0.5f, 0.5f);
                _contractRoot.anchorMax = new Vector2(0.5f, 0.5f);
                _contractRoot.pivot = new Vector2(0.5f, 0.5f);
                _contractRoot.sizeDelta = targetSize;
                _contractRoot.anchoredPosition = new Vector2(0f, ContractVerticalOffset);
                _contractRoot.SetSiblingIndex(detailsRect.GetSiblingIndex());
                _contractRoot.gameObject.SetActive(false);
            }

            private void CreateContractInventory()
            {
                if (_contractRoot == null)
                {
                    return;
                }

                var inventoryObject = new GameObject("CycleBinContractInventory");
                inventoryObject.transform.SetParent(_view.transform, false);

                _contractInventory = inventoryObject.AddComponent<Inventory>();
                if (_contractInventory == null)
                {
                    return;
                }
                _contractInventory.DisplayNameKey = Localizations.I18n.TrashBinKey;
                _contractInventory.SetCapacity(ContractSize);
            }

            private void CreateContractDisplay(InventoryDisplay template)
            {
                if (_contractRoot == null || _contractInventory == null)
                {
                    return;
                }

                var clone = UnityEngine.Object.Instantiate(template.gameObject, _contractRoot);
                clone.name = "CycleBinContractDisplay";

                _contractDisplay = clone.GetComponent<InventoryDisplay>();
                if (_contractDisplay == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    Log.Warning("Cycle bin could not clone InventoryDisplay properly.");
                    return;
                }

                PrepareClonedInventoryDisplay(_contractDisplay);

                var rect = clone.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = new Vector2(0f, 64f);
                    rect.offsetMax = Vector2.zero;
                }

                DisableInventoryDisplayExtras(_contractDisplay);
                _contractDisplay.Setup(_contractInventory, funcCanOperate: _ => true, movable: false);
                _contractDisplay.onDisplayDoubleClicked += OnContractDoubleClicked;
                _contractDisplay.gameObject.SetActive(true);
            }

            private static void PrepareClonedInventoryDisplay(InventoryDisplay display)
            {
                if (display == null)
                {
                    return;
                }

                var displayType = typeof(InventoryDisplay);

                var entryPrefabField = AccessTools.Field(displayType, "entryPrefab");
                var entriesParentField = AccessTools.Field(displayType, "entriesParent");
                var entryPoolField = AccessTools.Field(displayType, "_entryPool");
                var entriesField = AccessTools.Field(displayType, "entries");
                var cachedIndexesField = AccessTools.Field(displayType, "cachedIndexesToDisplay");

                var entryPrefab = entryPrefabField?.GetValue(display) as InventoryEntry;
                Transform? entriesParent = entriesParentField?.GetValue(display) as Transform;
                if (entriesParent != null)
                {
                    for (int i = entriesParent.childCount - 1; i >= 0; i--)
                    {
                        var child = entriesParent.GetChild(i);
                        if (entryPrefab != null && child == entryPrefab.transform)
                        {
                            continue;
                        }

                        UnityEngine.Object.Destroy(child.gameObject);
                    }

                    if (entryPrefab != null)
                    {
                        entryPrefab.gameObject.SetActive(false);
                        entryPrefab.transform.SetParent(entriesParent, false);
                        entryPrefab.transform.SetAsFirstSibling();
                    }
                }

                entryPoolField?.SetValue(display, null);
                entriesField?.SetValue(display, new List<InventoryEntry>());
                cachedIndexesField?.SetValue(display, new List<int>());

                AccessTools.Field(displayType, "cachedSelectedPage")?.SetValue(display, 0);
                AccessTools.Field(displayType, "cachedMaxPage")?.SetValue(display, 1);
                AccessTools.Field(displayType, "cachedCapacity")?.SetValue(display, -1);
                AccessTools.Field(displayType, "filter")?.SetValue(display, null);

                AccessTools.Field(displayType, "usePages")?.SetValue(display, false);
                AccessTools.Field(displayType, "itemsEachPage")?.SetValue(display, ContractSize);
                AccessTools.Field(displayType, "editable")?.SetValue(display, false);
                AccessTools.Field(displayType, "showOperationButtons")?.SetValue(display, false);
                AccessTools.Field(displayType, "showSortButton")?.SetValue(display, false);

                var grid = AccessTools.Field(displayType, "contentLayout")?.GetValue(display) as GridLayoutGroup;
                if (grid != null)
                {
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = ContractSize;
                }

                var placeholder = AccessTools.Field(displayType, "placeHolder")?.GetValue(display) as GameObject;
                if (placeholder != null)
                {
                    placeholder.SetActive(false);
                }
            }

            private static void DisableInventoryDisplayExtras(InventoryDisplay display)
            {
                try
                {
                    var sortButtonField = AccessTools.Field(typeof(InventoryDisplay), "sortButton");
                    var sortButton = sortButtonField?.GetValue(display) as Button;
                    if (sortButton != null)
                    {
                        sortButton.gameObject.SetActive(false);
                    }

                    var showButtonsField = AccessTools.Field(typeof(InventoryDisplay), "showOperationButtons");
                    showButtonsField?.SetValue(display, false);

                    var showSortField = AccessTools.Field(typeof(InventoryDisplay), "showSortButton");
                    showSortField?.SetValue(display, false);
                }
                catch (Exception ex)
                {
                    Log.Debug($"Failed to adjust InventoryDisplay extras: {ex.Message}");
                }
            }

            private void CreateClearButton()
            {
                if (_contractRoot == null)
                {
                    return;
                }

                var interactionButtonField = AccessTools.Field(typeof(StockShopView), "interactionButton");
                var interactionButton = interactionButtonField?.GetValue(_view) as Button;
                if (interactionButton == null)
                {
                    return;
                }

                var buttonObject = UnityEngine.Object.Instantiate(interactionButton.gameObject, _contractRoot);
                buttonObject.name = "CycleBinClearButton";

                var rect = buttonObject.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(240f, rect.sizeDelta.y);
                }

                _clearButton = buttonObject.GetComponent<Button>();
                if (_clearButton != null)
                {
                    _clearButton.onClick.RemoveAllListeners();
                    _clearButton.onClick.AddListener(ReturnAllItems);

                    _clearButtonLabel = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (_clearButtonLabel != null)
                    {
                        _clearButtonLabel.text = Localizations.I18n.FreeKey.ToPlainText();
                    }
                }
            }

            private void SubscribeMonitoredDisplays()
            {
                foreach (var display in _monitoredDisplays)
                {
                    if (display == null)
                    {
                        continue;
                    }

                    display.onDisplayDoubleClicked -= OnSourceDisplayDoubleClicked;
                    display.onDisplayDoubleClicked += OnSourceDisplayDoubleClicked;
                }
            }

            private void UnsubscribeMonitoredDisplays()
            {
                foreach (var display in _monitoredDisplays)
                {
                    if (display == null)
                    {
                        continue;
                    }

                    display.onDisplayDoubleClicked -= OnSourceDisplayDoubleClicked;
                }
            }

            private void OnSourceDisplayDoubleClicked(InventoryDisplay display, InventoryEntry entry, PointerEventData data)
            {
                if (!_visible || _busy)
                {
                    return;
                }

                data?.Use();

                var item = entry?.GetItem();
                if (item == null)
                {
                    return;
                }

                AddItemToContract(item);
            }

            private void OnContractDoubleClicked(InventoryDisplay display, InventoryEntry entry, PointerEventData data)
            {
                if (_busy)
                {
                    return;
                }

                data?.Use();

                var item = entry?.GetItem();
                if (item == null)
                {
                    return;
                }

                ReturnItem(item);
                UpdateClearButtonState();
            }

            private void AddItemToContract(Item item)
            {
                if (_contractInventory == null)
                {
                    return;
                }

                if (_contractInventory.GetItemCount() >= ContractSize)
                {
                    NotificationText.Push(Localizations.I18n.TrashBinKey.ToPlainText());
                    return;
                }

                var origin = item.InInventory;
                if (origin == null)
                {
                    Log.Warning("Cycle bin could not resolve the source inventory for the item.");
                    return;
                }

                if (!origin.RemoveItem(item))
                {
                    Log.Warning("Cycle bin failed to remove item from source inventory.");
                    return;
                }

                item.Detach();
                _contractInventory.AddItem(item);
                _originalInventories[item] = origin;

                ItemUIUtilities.Select((ItemDisplay)null!);
                UpdateClearButtonState();

                if (_contractInventory.GetItemCount() >= ContractSize)
                {
                    CompleteContractAsync().Forget();
                }
            }

            private void ReturnItem(Item item)
            {
                if (_contractInventory == null)
                {
                    return;
                }

                if (!_contractInventory.RemoveItem(item))
                {
                    return;
                }

                if (!_originalInventories.TryGetValue(item, out var original) || original == null || !original.AddItem(item))
                {
                    GiveToPlayer(item);
                }

                _originalInventories.Remove(item);
            }

            private void ReturnAllItems()
            {
                if (_contractInventory == null)
                {
                    return;
                }

                var items = _contractInventory.Content.Where(i => i != null).ToList();
                foreach (var item in items)
                {
                    ReturnItem(item);
                }

                UpdateClearButtonState();
            }

            private static void GiveToPlayer(Item item)
            {
                if (!ItemUtilities.SendToPlayerCharacterInventory(item))
                {
                    ItemUtilities.SendToPlayerStorage(item);
                }
            }

            private void UpdateClearButtonState()
            {
                if (_clearButton == null)
                {
                    return;
                }

                bool hasItems = _contractInventory != null && _contractInventory.GetItemCount() > 0;
                _clearButton.interactable = !_busy && hasItems;

                if (_clearButtonLabel != null)
                {
                    _clearButtonLabel.text = hasItems
                        ? Localizations.I18n.TrashBinKey.ToPlainText()
                        : Localizations.I18n.FreeKey.ToPlainText();
                }
            }

            private async UniTask CompleteContractAsync()
            {
                if (_busy || _contractInventory == null)
                {
                    return;
                }

                var items = _contractInventory.Content.Where(i => i != null).ToList();
                if (items.Count < ContractSize)
                {
                    return;
                }

                _busy = true;
                UpdateClearButtonState();

                try
                {
                    foreach (var item in items)
                    {
                        _contractInventory.RemoveItem(item);
                        _originalInventories.Remove(item);
                        item.Detach();
                        UnityEngine.Object.Destroy(item.gameObject);
                    }

                    int targetQuality = DetermineTargetQuality(items);
                    Item? reward = await TryCreateRewardItem(targetQuality);

                    if (reward == null)
                    {
                        NotificationText.Push("Recycling failed.");
                        return;
                    }

                    if (!ItemUtilities.SendToPlayerCharacterInventory(reward))
                    {
                        ItemUtilities.SendToPlayerStorage(reward);
                    }

                    string message = Localizations.I18n.PickNotificationFormatKey.ToPlainText()
                        .Replace("{itemDisplayName}", reward.DisplayName);
                    NotificationText.Push(message);
                    AudioManager.Post("UI/buy");
                }
                catch (Exception ex)
                {
                    Log.Error($"Cycle bin contract failed: {ex.Message}");
                }
                finally
                {
                    _busy = false;
                    UpdateClearButtonState();
                }
            }

            private static int DetermineTargetQuality(IEnumerable<Item> items)
            {
                var qualities = items.Select(i => i != null ? i.Quality : 0).ToList();
                if (qualities.Count == 0)
                {
                    return 1;
                }

                float average = (float)qualities.Average();
                return Mathf.Clamp(Mathf.RoundToInt(average) + 1, 1, 6);
            }

            private static async UniTask<Item?> TryCreateRewardItem(int targetQuality)
            {
                var rewardList = await LotteryService.PickRandomItemsByQualityAsync(targetQuality, 1);
                if (rewardList != null && rewardList.Count > 0)
                {
                    return rewardList[0];
                }

                Log.Warning("Cycle bin quality-based reward failed – falling back to default lottery pool.");
                var fallback = await LotteryService.PickRandomItemAsync(LotteryService.ItemTypeIdsCache);
                if (fallback != null)
                {
                    fallback.StackCount = 1;
                }

                return fallback;
            }
        }
    }
}
