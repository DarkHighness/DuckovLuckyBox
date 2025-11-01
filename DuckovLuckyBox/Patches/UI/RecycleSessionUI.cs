using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Duckov;
using Duckov.Economy.UI;
using Duckov.UI;
using Duckov.UI.Animations;
using DuckovLuckyBox.UI;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckovLuckyBox.Core
{
    public class RecycleSessionUI : IComponent
    {
        private const int ContractSize = 5;
        private const float ContractVerticalOffset = -120f;
        private static RecycleSessionUI? _instance;
        public static RecycleSessionUI Instance => _instance ??= new RecycleSessionUI();

        private StockShopView? _view;
        private TextMeshProUGUI? _textTemplate;
        private readonly Dictionary<Item, OriginItemContext> _itemOrigins = new Dictionary<Item, OriginItemContext>();
        private readonly List<InventoryDisplay> _monitoredDisplays = new List<InventoryDisplay>();
        private readonly Dictionary<InventoryDisplay, Delegate?> _suppressedDoubleClickHandlers = new Dictionary<InventoryDisplay, Delegate?>();

        private static readonly FieldInfo? DoubleClickEventField = AccessTools.Field(typeof(InventoryDisplay), "onDisplayDoubleClicked");

        private FadeGroup? _detailsFadeGroup;
        private RectTransform? _contractRoot;
        private InventoryDisplay? _contractDisplay;
        private Inventory? _contractInventory;
        private const float ButtonGroupHeight = 72f;
        private const float ButtonGroupSpacing = 16f;
        private const float ActionLabelPreferredHeight = 40f;
        private const float ActionLabelMinWidth = 140f;
        private const float ActionLabelExtraWidth = 24f;
        private const float ActionLabelMinFontSize = 18f;
        private const float ActionLabelFontScale = 0.9f;
        private static readonly Color ActionButtonNormalColor = new Color(1f, 1f, 1f, 0.8f);
        private static readonly Color ActionButtonHighlightedColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color ActionButtonPressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color ActionButtonDisabledColor = new Color(1f, 1f, 1f, 0.35f);
        private RectTransform? _actionButtonContainer;
        private Button? _confirmButton;
        private TextMeshProUGUI? _confirmButtonLabel;
        private Button? _clearButton;
        private TextMeshProUGUI? _clearButtonLabel;
        private GameObject? _tooltipPanel;
        private TextMeshProUGUI? _tooltipText;

        private bool _visible;
        private bool _busy;
        private bool _transferring;
        private ItemValueLevel? _targetQuality;
        private ItemValueLevel? _rewardQuality;
        private ItemDisplay? _savedSelectedDisplay;

        private bool isInitialized = false;
        private bool isOpen = false;

        public bool IsOpen => isOpen;

        public void Open()
        {
            if (!isInitialized) return;
            Log.Debug("Opening RecycleSessionUI");
            InitializeUI();
            if (_contractRoot == null || _contractDisplay == null || _contractInventory == null)
            {
                Log.Warning($"Cycle bin UI failed to initialize correctly. Since _contractRoot is {(_contractRoot == null ? "null" : "valid")}, _contractDisplay is {(_contractDisplay == null ? "null" : "valid")}, _contractInventory is {(_contractInventory == null ? "null" : "valid")}. Aborting show.");
                return;
            }

            // Save current selection state
            var currentSelected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            _savedSelectedDisplay = currentSelected?.GetComponent<ItemDisplay>();

            _visible = true;
            isOpen = true;
            _contractRoot?.SetAsLastSibling();
            _contractRoot?.gameObject.SetActive(true);
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

            UpdateActionButtonsState();
        }

        public void Close()
        {
            if (!isInitialized) return;
            Log.Debug("Closing RecycleSessionUI");
            if (_contractRoot == null || _contractDisplay == null || _contractInventory == null)
            {
                return;
            }

            _visible = false;
            isOpen = false;
            if (_contractDisplay != null)
            {
                _contractDisplay.onDisplayDoubleClicked -= OnContractDoubleClicked;
            }

            UnsubscribeMonitoredDisplays();

            ReturnAllItems();

            // Verify that contract inventory is empty after returning all items
            if (_contractInventory != null && _contractInventory.GetItemCount() > 0)
            {
                Log.Error($"[Recycle] Contract inventory still contains {_contractInventory.GetItemCount()} items after ReturnAllItems. Forcing cleanup.");
                ForceClearContractInventory();
            }

            _contractRoot?.gameObject.SetActive(false);

            // Hide tooltip if visible
            HideTooltip();

            // Restore selection state
            if (_savedSelectedDisplay != null)
            {
                ItemUIUtilities.Select(_savedSelectedDisplay);
            }
        }


        private sealed class OriginItemContext
        {
            public OriginItemContext(Inventory inventory, Item? stackSource)
            {
                Inventory = inventory;
                StackSource = stackSource;
            }

            public Inventory Inventory { get; }
            public Item? StackSource { get; }
        }

        public void Setup(StockShopView view)
        {
            if (!isInitialized)
            {
                Log.Debug("Initializing RecycleSessionUI");
                _view = view;
                _textTemplate = GetTextTemplate(view);
                InitializeUI();
            }
            else
            {
                _view = view;
            }
        }

        private static TextMeshProUGUI? GetTextTemplate(StockShopView view)
        {
            if (view == null)
            {
                return null;
            }

            var merchantNameTextField = AccessTools.Field(typeof(StockShopView), "merchantNameText");
            return merchantNameTextField?.GetValue(view) as TextMeshProUGUI;
        }

        public void Toggle()
        {
            if (!isInitialized) return;
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }


        private void CleanupTooltip()
        {
            if (_tooltipPanel != null)
            {
                UnityEngine.Object.Destroy(_tooltipPanel);
                _tooltipPanel = null;
                _tooltipText = null;
            }
        }

        public void Destroy()
        {
            // Ensure contract inventory is completely empty
            if (_contractInventory != null && _contractInventory.GetItemCount() > 0)
            {
                ForceClearContractInventory();
            }

            // Clear all references
            _itemOrigins.Clear();
            _monitoredDisplays.Clear();
            _suppressedDoubleClickHandlers.Clear();
            CleanupTooltip();

            // Destroy contract inventory object
            if (_contractInventory != null)
            {
                UnityEngine.Object.Destroy(_contractInventory.gameObject);
                _contractInventory = null;
            }

            // Destroy contract display
            if (_contractDisplay != null)
            {
                UnityEngine.Object.Destroy(_contractDisplay.gameObject);
                _contractDisplay = null;
            }

            // Destroy contract root
            if (_contractRoot != null)
            {
                UnityEngine.Object.Destroy(_contractRoot.gameObject);
                _contractRoot = null;
            }

            // Destroy action button container
            if (_actionButtonContainer != null)
            {
                UnityEngine.Object.Destroy(_actionButtonContainer.gameObject);
                _actionButtonContainer = null;
            }

            // Reset other UI components
            _confirmButton = null;
            _confirmButtonLabel = null;
            _clearButton = null;
            _clearButtonLabel = null;

            // Reset other references
            _view = null;
            _textTemplate = null;
            _detailsFadeGroup = null;
            _savedSelectedDisplay = null;
            _targetQuality = null;
            _rewardQuality = null;

            // Reset state flags
            _visible = false;
            _busy = false;
            _transferring = false;
            isOpen = false;

            _instance = null;
            isInitialized = false;
        }

        private void InitializeUI()
        {
            if (isInitialized) return;
            Log.Debug("[Recycle:EnsureInitialized] Starting initialization...");

            // Do not destroy existing objects before re-initializing

            CaptureViewReferences();
            Log.Debug("[Recycle:EnsureInitialized] Captured view references.");

            if (_detailsFadeGroup == null)
            {
                Log.Warning("Cycle bin could not locate details fade group in StockShopView.");
                return;
            }
            Log.Debug("[Recycle:EnsureInitialized] Details fade group located.");

            var templateDisplay = GetInventoryDisplay("playerInventoryDisplay");
            if (templateDisplay == null)
            {
                Log.Warning("Cycle bin could not locate a template InventoryDisplay.");
                return;
            }
            Log.Debug("[Recycle:EnsureInitialized] Template InventoryDisplay located.");

            BuildContractRoot();
            if (_contractRoot == null)
            {
                return;
            }
            Log.Debug("[Recycle:EnsureInitialized] Contract root built.");

            CreateContractInventory();
            if (_contractInventory == null)
            {
                return;
            }
            Log.Debug("[Recycle:EnsureInitialized] Contract inventory created.");

            CreateContractDisplay(templateDisplay);
            Log.Debug("[Recycle:EnsureInitialized] Contract display created.");

            CreateActionButtons();
            Log.Debug("[Recycle:EnsureInitialized] Action buttons created.");

            Log.Debug("[Recycle:EnsureInitialized] Initialization completed.");
            isInitialized = true;
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

            if (_view == null)
            {
                return;
            }

            // Use similar hierarchy logic as PatchStockShopView
            var merchantNameText = GetTextTemplate(_view);
            RectTransform? targetParent = null;

            if (merchantNameText != null)
            {
                // Follow PatchStockShopView's hierarchy logic
                var parent = merchantNameText.transform.parent as RectTransform;
                var grandParent = parent?.parent as RectTransform;
                var greatGrandParent = grandParent?.parent as RectTransform;
                targetParent = greatGrandParent ?? grandParent ?? parent ?? detailsRect.parent as RectTransform;
            }
            else
            {
                // Fallback to original logic
                targetParent = detailsRect.parent as RectTransform;
            }

            if (targetParent == null)
            {
                var fallbackCanvas = _view.GetComponentInParent<Canvas>();
                targetParent = fallbackCanvas != null ? fallbackCanvas.transform as RectTransform : _view.transform as RectTransform;
            }

            _contractRoot = new GameObject("RecycleContractRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            if (_contractRoot == null)
            {
                return;
            }

            int viewLayer = _view.gameObject.layer;
            _contractRoot.gameObject.layer = viewLayer;

            Vector2 targetSize = detailsRect.rect.size;
            if (targetSize == Vector2.zero)
            {
                targetSize = detailsRect.sizeDelta;
            }

            _contractRoot.SetParent(targetParent, false);
            _contractRoot.anchorMin = new Vector2(0.5f, 1f);
            _contractRoot.anchorMax = new Vector2(0.5f, 1f);
            _contractRoot.pivot = new Vector2(0.5f, 1f);
            _contractRoot.sizeDelta = new Vector2(targetSize.x, 0f);

            var anchoredPosition = detailsRect.anchoredPosition;
            anchoredPosition.y += ContractVerticalOffset;
            _contractRoot.anchoredPosition = anchoredPosition;

            _contractRoot.SetAsLastSibling();

            // Add VerticalLayoutGroup to _contractRoot
            var verticalLayout = _contractRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.childForceExpandWidth = false;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.spacing = ButtonGroupSpacing;
            verticalLayout.padding = new RectOffset(0, 0, 0, 0);

            // Add ContentSizeFitter
            var fitter = _contractRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var parentCanvas = targetParent?.GetComponentInParent<Canvas>();
            ConfigureContractRootCanvas(parentCanvas);

            _contractRoot.gameObject.SetActive(false);
        }

        private void CreateContractInventory()
        {
            if (_contractRoot == null)
            {
                return;
            }

            var inventoryObject = new GameObject("RecycleContractInventory");
            if (_view == null)
            {
                return;
            }
            inventoryObject.transform.SetParent(_view.transform, false);

            _contractInventory = inventoryObject.AddComponent<Inventory>();
            if (_contractInventory == null)
            {
                return;
            }
            _contractInventory.DisplayNameKey = Localizations.I18n.RecycleKey;
            _contractInventory.SetCapacity(ContractSize);
        }

        private void CreateContractDisplay(InventoryDisplay template)
        {
            Log.Debug("[Recycle:CreateContractDisplay] Creating contract display...");

            if (_contractRoot == null || _contractInventory == null)
            {
                Log.Debug("[Recycle:CreateContractDisplay] Contract root or inventory is null, skipping display creation.");
                return;
            }

            Log.Debug("[Recycle:CreateContractDisplay] Instantiating template display.");
            var clone = UnityEngine.Object.Instantiate(template.gameObject, _contractRoot);
            clone.name = "RecycleContractDisplay";

            Log.Debug("[Recycle:CreateContractDisplay] Getting InventoryDisplay component.");
            _contractDisplay = clone.GetComponent<InventoryDisplay>();
            if (_contractDisplay == null)
            {
                UnityEngine.Object.Destroy(clone);
                Log.Warning("Cycle bin could not clone InventoryDisplay properly.");
                return;
            }

            Log.Debug("[Recycle:CreateContractDisplay] Preparing cloned inventory display.");
            PrepareClonedInventoryDisplay(_contractDisplay);

            Log.Debug("[Recycle:CreateContractDisplay] Configuring rect transform.");
            var rect = clone.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.zero; // Let layout system determine size
                rect.anchoredPosition = Vector2.zero;
            }

            Log.Debug("[Recycle:CreateContractDisplay] Setting up layout element.");
            var displayLayout = clone.GetComponent<LayoutElement>() ?? clone.AddComponent<LayoutElement>();
            displayLayout.preferredWidth = -1f; // Use preferred size
            displayLayout.preferredHeight = -1f; // Use preferred size
            displayLayout.flexibleWidth = 1f; // Allow flexible sizing
            displayLayout.flexibleHeight = 1f; // Allow flexible sizing

            Log.Debug("[Recycle:CreateContractDisplay] Disabling inventory display extras.");
            DisableInventoryDisplayExtras(_contractDisplay);

            Log.Debug("[Recycle:CreateContractDisplay] Setting up inventory display.");
            if (_contractInventory == null)
            {
                Log.Warning("Cycle bin contract inventory is null during display setup.");
                return;
            }
            _contractDisplay.Setup(_contractInventory, funcCanOperate: _ => true, movable: false);

            Log.Debug("[Recycle:CreateContractDisplay] Adding double click event handler.");
            _contractDisplay.onDisplayDoubleClicked += OnContractDoubleClicked;

            Log.Debug("[Recycle:CreateContractDisplay] Activating display game object.");
            _contractDisplay.gameObject.SetActive(true);

            if (_contractRoot != null)
            {
                Log.Debug("[Recycle:CreateContractDisplay] Rebuilding layout.");
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contractRoot);
            }

            Log.Debug("[Recycle:CreateContractDisplay] Contract display creation completed.");
        }

        private static void PrepareClonedInventoryDisplay(InventoryDisplay display)
        {
            if (display == null)
            {
                Log.Warning("[Recycle:PrepareClonedInventoryDisplay] Display is null.");
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

                    child.gameObject.SetActive(false);
                    // UnityEngine.Object.Destroy(child.gameObject);
                }
            }

            // Clear existing pools and entries
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
            placeholder?.SetActive(false);
        }

        private static void DisableInventoryDisplayExtras(InventoryDisplay display)
        {
            try
            {
                var sortButtonField = AccessTools.Field(typeof(InventoryDisplay), "sortButton");
                var sortButton = sortButtonField?.GetValue(display) as Button;
                sortButton?.gameObject.SetActive(false);

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

        private void ConfigureContractRootCanvas(Canvas? parentCanvas)
        {
            if (_contractRoot == null)
            {
                return;
            }

            // Remove any existing Canvas and GraphicRaycaster to use parent's
            var existingCanvas = _contractRoot.GetComponent<Canvas>();
            if (existingCanvas != null)
            {
                UnityEngine.Object.Destroy(existingCanvas);
            }

            var existingRaycaster = _contractRoot.GetComponent<GraphicRaycaster>();
            if (existingRaycaster != null)
            {
                UnityEngine.Object.Destroy(existingRaycaster);
            }

            var group = _contractRoot.GetComponent<CanvasGroup>() ?? _contractRoot.gameObject.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;
            group.ignoreParentGroups = false; // Use parent's groups
            group.alpha = 1f;
        }

        private void CreateActionButtons()
        {
            if (_contractRoot == null)
            {
                return;
            }

            _actionButtonContainer = new GameObject("RecycleActionButtons", typeof(RectTransform)).GetComponent<RectTransform>();
            if (_actionButtonContainer == null)
            {
                return;
            }

            int uiLayer = _contractDisplay != null ? _contractDisplay.gameObject.layer : _contractRoot.gameObject.layer;
            _actionButtonContainer.gameObject.layer = uiLayer;

            var displayRect = _contractDisplay != null ? _contractDisplay.transform as RectTransform : null;

            _actionButtonContainer.SetParent(_contractRoot, false);
            int siblingIndex = displayRect != null ? displayRect.transform.GetSiblingIndex() + 1 : _contractRoot.childCount;
            _actionButtonContainer.SetSiblingIndex(siblingIndex);

            var actionsRect = _actionButtonContainer;
            actionsRect.anchorMin = new Vector2(0.5f, 0.5f);
            actionsRect.anchorMax = new Vector2(0.5f, 0.5f);
            actionsRect.pivot = new Vector2(0.5f, 0.5f);
            actionsRect.sizeDelta = Vector2.zero; // Let layout system determine size
            actionsRect.anchoredPosition = Vector2.zero;

            var actionsLayoutElement = _actionButtonContainer.GetComponent<LayoutElement>() ?? _actionButtonContainer.gameObject.AddComponent<LayoutElement>();
            actionsLayoutElement.preferredWidth = -1f; // Use preferred size
            actionsLayoutElement.preferredHeight = ButtonGroupHeight;
            actionsLayoutElement.flexibleWidth = 1f; // Allow flexible sizing
            actionsLayoutElement.flexibleHeight = 0f;

            var layout = _actionButtonContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 20f;
            layout.padding = new RectOffset(24, 24, 8, 8);

            _confirmButtonLabel = CreateActionButtonLabel(_actionButtonContainer, "RecycleConfirmButton", Localizations.I18n.ConfirmKey.ToPlainText(), out _confirmButton);
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(() =>
                {
                    CompleteContractAsync().Forget();
                });
                AddTooltipToButton(_confirmButton);
            }

            _clearButtonLabel = CreateActionButtonLabel(_actionButtonContainer, "RecycleClearButton", Localizations.I18n.ClearKey.ToPlainText(), out _clearButton);
            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveAllListeners();
                _clearButton.onClick.AddListener(() =>
                {
                    ReturnAllItems();
                });
                AddTooltipToButton(_clearButton);
            }

            UpdateActionButtonsState();

            CreateTooltipPanel();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contractRoot);
        }

        private TextMeshProUGUI CreateActionButtonLabel(RectTransform parent, string name, string labelText, out Button button)
        {
            int uiLayer = parent.gameObject.layer;

            TextMeshProUGUI label;
            if (_textTemplate != null)
            {
                // Use template instantiation like PatchStockShopView
                var labelObject = UnityEngine.Object.Instantiate(_textTemplate.gameObject, parent);
                labelObject.name = name;
                labelObject.layer = uiLayer;
                label = labelObject.GetComponent<TextMeshProUGUI>();
                if (label == null)
                {
                    Log.Warning("[RecycleDebug] Failed to get TextMeshProUGUI from template instantiation");
                    UnityEngine.Object.Destroy(labelObject);
                    // Fallback to direct creation
                    return CreateActionButtonLabelFallback(parent, name, labelText, out button);
                }
            }
            else
            {
                Log.Warning("[RecycleDebug] No text template available, using fallback creation");
                return CreateActionButtonLabelFallback(parent, name, labelText, out button);
            }

            ConfigureActionLabel(label, labelText);

            var labelRect = label.rectTransform;
            // Parent is already set by Instantiate

            button = label.gameObject.AddComponent<Button>();
            ConfigureActionButton(button, label);

            return label;
        }

        private static TextMeshProUGUI CreateActionButtonLabelFallback(RectTransform parent, string name, string labelText, out Button button)
        {
            int uiLayer = parent.gameObject.layer;

            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI))
            {
                layer = uiLayer
            };
            var label = labelObject.GetComponent<TextMeshProUGUI>();

            ConfigureActionLabel(label, labelText);

            var labelRect = label.rectTransform;
            labelRect.SetParent(parent, false);

            button = labelObject.AddComponent<Button>();
            ConfigureActionButton(button, label);

            return label;
        }

        private static void ConfigureActionLabel(TextMeshProUGUI label, string text)
        {
            label.text = text;
            label.margin = Vector4.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.fontSize = Mathf.Max(ActionLabelMinFontSize, label.fontSize * ActionLabelFontScale);
            label.color = Color.white;
            label.raycastTarget = true;

            var rectTransform = label.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(0f, ActionLabelPreferredHeight);

            var layoutElement = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = ActionLabelPreferredHeight;
            layoutElement.preferredWidth = Mathf.Max(ActionLabelMinWidth, label.preferredWidth + ActionLabelExtraWidth);
            layoutElement.flexibleWidth = 0f;
        }

        private static void ConfigureActionButton(Button button, TextMeshProUGUI label)
        {
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = label;
            button.interactable = true;

            // Configure navigation to prevent unwanted focus changes
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            var colors = button.colors;
            colors.normalColor = ActionButtonNormalColor;
            colors.highlightedColor = ActionButtonHighlightedColor;
            colors.pressedColor = ActionButtonPressedColor;
            colors.selectedColor = ActionButtonHighlightedColor;
            colors.disabledColor = ActionButtonDisabledColor;
            button.colors = colors;
        }

        private void AddTooltipToButton(Button button)
        {
            // Add tooltip trigger component
            var tooltipTrigger = button.gameObject.AddComponent<TooltipTrigger>();
            tooltipTrigger.session = this;
        }

        private class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public RecycleSessionUI? session;

            public void OnPointerEnter(PointerEventData eventData)
            {
                session?.ShowTooltip(GetComponent<Button>());
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                session?.HideTooltip();
            }
        }

        private void CreateTooltipPanel()
        {
            if (_contractRoot == null || _textTemplate == null)
            {
                return;
            }

            // Create tooltip panel as a child of the main canvas to avoid UI hierarchy issues
            var mainCanvas = _contractRoot.GetComponentInParent<Canvas>();
            if (mainCanvas == null)
            {
                return;
            }

            _tooltipPanel = new GameObject("RecycleTooltipPanel", typeof(RectTransform));
            _tooltipPanel.transform.SetParent(mainCanvas.transform, false);
            _tooltipPanel.layer = mainCanvas.gameObject.layer;

            var tooltipRect = _tooltipPanel.transform as RectTransform;
            if (tooltipRect != null)
            {
                tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
                tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
                tooltipRect.pivot = new Vector2(0.5f, 0.5f);
                tooltipRect.sizeDelta = new Vector2(300f, 60f);
                // Position will be set dynamically when shown
            }

            // Create background with proper CanvasGroup
            var background = _tooltipPanel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.9f);
            background.raycastTarget = false;

            // Add CanvasGroup for better control
            var canvasGroup = _tooltipPanel.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Create text
            var textObject = UnityEngine.Object.Instantiate(_textTemplate.gameObject, _tooltipPanel.transform);
            textObject.name = "TooltipText";
            _tooltipText = textObject.GetComponent<TextMeshProUGUI>();
            if (_tooltipText != null)
            {
                _tooltipText.text = "";
                _tooltipText.alignment = TextAlignmentOptions.Center;
                _tooltipText.fontSize = 16f;
                _tooltipText.color = Color.white;
                _tooltipText.raycastTarget = false;

                var textRect = _tooltipText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10f, 5f);
                textRect.offsetMax = new Vector2(-10f, -5f);
            }

            _tooltipPanel.SetActive(false);
        }

        private void ShowTooltip(Button button)
        {
            if (_tooltipPanel == null || _tooltipText == null || _contractInventory == null || button == null)
            {
                return;
            }

            // Safety check: don't show tooltip if Recycle is not visible
            if (!_visible)
            {
                return;
            }

            string tooltipText = "";
            if (button == _confirmButton)
            {
                int itemCount = _contractInventory.GetItemCount();
                if (itemCount == 0)
                {
                    tooltipText = Localizations.I18n.RecycleTooltipEmptyKey.ToPlainText();
                }
                else if (itemCount < ContractSize)
                {
                    // Replace placeholders: {current} and {needed} are defined in localization strings
                    tooltipText = Localizations.I18n.RecycleTooltipNeedCountKey.ToPlainText()
                      .Replace("{current}", itemCount.ToString())
                      .Replace("{needed}", ContractSize.ToString());
                }
                else
                {
                    // {count} placeholder for confirmed count
                    tooltipText = Localizations.I18n.RecycleTooltipConfirmKey.ToPlainText()
                      .Replace("{count}", itemCount.ToString());
                }
            }
            else if (button == _clearButton)
            {
                int itemCount = _contractInventory.GetItemCount();
                if (itemCount > 0)
                {
                    tooltipText = Localizations.I18n.RecycleTooltipClearKey.ToPlainText()
                      .Replace("{count}", itemCount.ToString());
                }
                else
                {
                    tooltipText = Localizations.I18n.RecycleTooltipEmptyKey.ToPlainText();
                }
            }

            _tooltipText.text = tooltipText;

            // Position tooltip near the button with bounds checking
            if (button.transform is RectTransform buttonRect && _tooltipPanel.transform is RectTransform tooltipRect)
            {
                var buttonWorldPos = buttonRect.position;
                var buttonSize = buttonRect.rect.size;

                // Get canvas for bounds checking
                var canvas = _tooltipPanel.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    var canvasRect = canvas.GetComponent<RectTransform>();
                    if (canvasRect != null)
                    {
                        var canvasSize = canvasRect.rect.size;
                        var tooltipSize = tooltipRect.rect.size;

                        // Calculate position with bounds checking
                        float xPos = Mathf.Clamp(buttonWorldPos.x, tooltipSize.x / 2, canvasSize.x - tooltipSize.x / 2);
                        float yPos = Mathf.Clamp(buttonWorldPos.y + buttonSize.y / 2 + 40f, tooltipSize.y / 2, canvasSize.y - tooltipSize.y / 2);

                        tooltipRect.position = new Vector3(xPos, yPos, buttonWorldPos.z);
                    }
                    else
                    {
                        // Fallback positioning
                        tooltipRect.position = new Vector3(
                          buttonWorldPos.x,
                          buttonWorldPos.y + buttonSize.y / 2 + 40f,
                          buttonWorldPos.z
                        );
                    }
                }
            }

            _tooltipPanel.SetActive(true);
        }

        private void HideTooltip()
        {
            _tooltipPanel?.SetActive(false);
        }

        private void ForceClearContractInventory()
        {
            if (_contractInventory == null)
            {
                return;
            }

            var remainingItems = _contractInventory.Content.Where(i => i != null).ToList();
            foreach (var item in remainingItems)
            {
                _contractInventory.RemoveItem(item);
                _itemOrigins.Remove(item);
                GiveToPlayer(item);
                Log.Warning($"[Recycle] Force returned item {item.DisplayName} to player inventory during cleanup.");
            }

            ResetContractRequirements();
        }

        private void SubscribeMonitoredDisplays()
        {
            foreach (var display in _monitoredDisplays)
            {
                if (display == null)
                {
                    continue;
                }

                SuppressOriginalDoubleClick(display);
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
                RestoreOriginalDoubleClick(display);
            }
        }

        private void SuppressOriginalDoubleClick(InventoryDisplay display)
        {
            if (DoubleClickEventField == null || _suppressedDoubleClickHandlers.ContainsKey(display))
            {
                return;
            }

            var existing = DoubleClickEventField.GetValue(display) as Delegate;
            if (existing == null)
            {
                return;
            }

            _suppressedDoubleClickHandlers[display] = existing;
            DoubleClickEventField.SetValue(display, null);
        }

        private void RestoreOriginalDoubleClick(InventoryDisplay display)
        {
            if (DoubleClickEventField == null)
            {
                return;
            }

            if (_suppressedDoubleClickHandlers.TryGetValue(display, out var original) && original != null)
            {
                DoubleClickEventField.SetValue(display, original);
            }

            _suppressedDoubleClickHandlers.Remove(display);
        }

        private readonly struct ContractValidationResult
        {
            public ContractValidationResult(bool isValid, string category, ItemValueLevel quality, bool isFirstItem, string? errorMessage, ItemValueLevel? rewardLevel)
            {
                IsValid = isValid;
                Category = category;
                Quality = quality;
                IsFirstItem = isFirstItem;
                ErrorMessage = errorMessage;
                RewardLevel = rewardLevel;
            }

            public bool IsValid { get; }
            public string Category { get; }
            public ItemValueLevel Quality { get; }
            public bool IsFirstItem { get; }
            public string? ErrorMessage { get; }
            public ItemValueLevel? RewardLevel { get; }

            public static ContractValidationResult Invalid(string category, ItemValueLevel quality, bool isFirstItem, string? errorMessage)
            {
                return new ContractValidationResult(false, category, quality, isFirstItem, errorMessage, null);
            }
        }

        private ContractValidationResult EvaluateItemForContract(Item item)
        {
            if (item == null)
            {
                return ContractValidationResult.Invalid(string.Empty, ItemValueLevel.White, true, Localizations.I18n.ItemIsNullKey.ToPlainText());
            }

            if (_contractInventory == null)
            {
                return ContractValidationResult.Invalid(string.Empty, ItemValueLevel.White, true, Localizations.I18n.ContractInventoryNotAvailableKey.ToPlainText());
            }

            string category = RecycleService.GetItemCategory(item.TypeID) ?? string.Empty;
            ItemValueLevel quality = QualityUtils.GetCachedItemValueLevel(item);
            int currentCount = _contractInventory.GetItemCount();
            bool isFirstItem = currentCount == 0;
            ItemValueLevel? rewardLevel = null;

            if (string.IsNullOrEmpty(category) || !ItemUtils.RecyclableCategories.Contains(category))
            {
                return ContractValidationResult.Invalid(category, quality, isFirstItem, Localizations.I18n.ItemNotValidForContractKey.ToPlainText());
            }

            // Special handling for bullets: only allow if a full bullet group (configured size) is submitted
            if (category == "Bullet")
            {
                if (!item.Stackable)
                {
                    return ContractValidationResult.Invalid(category, quality, isFirstItem, Localizations.I18n.BulletMustBeStackableKey.ToPlainText());
                }

                int requiredGroupSize = Math.Min(RecycleService.BulletGroupSize, item.MaxStackCount);
                if (item.StackCount < requiredGroupSize)
                {
                    var template = Localizations.I18n.BulletMustBeFullStackKey.ToPlainText();
                    var message = template?.Replace("{groupSize}", requiredGroupSize.ToString());
                    return ContractValidationResult.Invalid(category, quality, isFirstItem, message);
                }
            }

            // Disallow mixing bullets with non-bullets: if contract already has items and first item is bullet,
            // new items must also be bullets; similarly if first is non-bullet, bullets cannot be added.
            if (!isFirstItem && _contractInventory != null)
            {
                var firstExisting = _contractInventory.Content.FirstOrDefault();
                if (firstExisting != null)
                {
                    var firstCategory = RecycleService.GetItemCategory(firstExisting.TypeID);
                    bool firstIsBullet = firstCategory == "Bullet";
                    bool thisIsBullet = category == "Bullet";
                    if (firstIsBullet != thisIsBullet)
                    {
                        return ContractValidationResult.Invalid(category, quality, isFirstItem, Localizations.I18n.RecycleCannotMixKey.ToPlainText());
                    }
                }
            }

            if (currentCount >= ContractSize)
            {
                return ContractValidationResult.Invalid(category, quality, isFirstItem, Localizations.I18n.ContractFullKey.ToPlainText());
            }

            if (isFirstItem)
            {
                int nextLevelValue = (int)quality + 1;
                if (!Enum.IsDefined(typeof(ItemValueLevel), nextLevelValue))
                {
                    return ContractValidationResult.Invalid(category, quality, true, Localizations.I18n.RecycleNoHigherLevelKey.ToPlainText());
                }

                rewardLevel = (ItemValueLevel)nextLevelValue;

                if (!RecycleService.HasCategoryItemAtLevel(ItemUtils.RecyclableCategories, rewardLevel.Value))
                {
                    return ContractValidationResult.Invalid(category, quality, true, Localizations.I18n.RecycleNoTargetUpgradeKey.ToPlainText());
                }
            }
            else if (!MatchesContractRequirements(quality))
            {
                return ContractValidationResult.Invalid(category, quality, false, Localizations.I18n.ItemQualityMismatchKey.ToPlainText());
            }
            else
            {
                rewardLevel = _rewardQuality;
            }

            return new ContractValidationResult(true, category, quality, isFirstItem, null, rewardLevel);
        }

        private bool MatchesContractRequirements(ItemValueLevel quality)
        {
            if (!_targetQuality.HasValue)
            {
                return false;
            }

            return _targetQuality.Value == quality;
        }

        private void ResetContractRequirements()
        {
            _targetQuality = null;
            _rewardQuality = null;
        }

        private void OnSourceDisplayDoubleClicked(InventoryDisplay display, InventoryEntry entry, PointerEventData data)
        {
            if (!_visible || _busy || _transferring)
            {
                return;
            }

            data?.Use();

            var item = entry?.GetItem();
            if (item == null)
            {
                return;
            }

            AddItemToContractAsync(item).Forget();
        }

        private void OnContractDoubleClicked(InventoryDisplay display, InventoryEntry entry, PointerEventData data)
        {
            if (_busy || _transferring)
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
            UpdateActionButtonsState();
        }

        private async UniTask AddItemToContractAsync(Item item)
        {
            if (item == null)
            {
                return;
            }

            if (_transferring)
            {
                return;
            }

            var validation = EvaluateItemForContract(item);
            if (!validation.IsValid)
            {
                if (!string.IsNullOrEmpty(validation.ErrorMessage))
                {
                    NotificationText.Push(validation.ErrorMessage);
                }
                return;
            }

            var origin = item.InInventory;
            if (origin == null)
            {
                Log.Warning("Cycle bin could not resolve the source inventory for the item.");
                return;
            }

            _transferring = true;

            try
            {
                Item itemToMove = item;
                Item? stackSource = null;

                var itemCategory = RecycleService.GetItemCategory(item.TypeID);

                // If bullet, move the whole stack (requirement: full stack checked in validation)
                if (string.Equals(itemCategory, "Bullet", StringComparison.OrdinalIgnoreCase))
                {
                    // If the player holds more than one logical bullet group, split out a group of BulletGroupSize
                    if (item.StackCount > RecycleService.BulletGroupSize)
                    {
                        Item? splitItem = null;
                        try
                        {
                            splitItem = await item.Split(RecycleService.BulletGroupSize);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Cycle bin failed to split bullet stack: {ex.Message}");
                        }

                        if (splitItem == null)
                        {
                            return;
                        }

                        itemToMove = splitItem;
                        stackSource = item;
                    }
                    else
                    {
                        // StackCount is exactly BulletGroupSize (validation ensures >=), remove the whole stack
                        if (!origin.RemoveItem(item))
                        {
                            Log.Warning("Cycle bin failed to remove bullet stack from source inventory.");
                            return;
                        }

                        item.Detach();
                        itemToMove = item;
                    }
                }
                else
                {
                    if (item.StackCount > 1)
                    {
                        Item? splitItem = null;

                        try
                        {
                            splitItem = await item.Split(1);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Cycle bin failed to split item stack: {ex.Message}");
                        }

                        if (splitItem == null)
                        {
                            return;
                        }

                        itemToMove = splitItem;
                        stackSource = item;
                    }
                    else
                    {
                        if (!origin.RemoveItem(item))
                        {
                            Log.Warning("Cycle bin failed to remove item from source inventory.");
                            return;
                        }

                        item.Detach();
                    }

                    // non-bullet: each moved item is a single count
                    itemToMove.StackCount = 1;
                }

                if (_contractInventory == null)
                {
                    return;
                }

                if (!_contractInventory.AddItem(itemToMove))
                {
                    Log.Warning("Cycle bin failed to add item into contract inventory.");

                    if (stackSource != null)
                    {
                        stackSource.Combine(itemToMove);
                    }
                    else if (itemToMove != null && !origin.AddItem(itemToMove))
                    {
                        GiveToPlayer(itemToMove);
                    }

                    return;
                }

                if (itemToMove != null)
                {
                    _itemOrigins[itemToMove] = new OriginItemContext(origin, stackSource);
                }

                if (validation.IsFirstItem)
                {
                    _targetQuality = validation.Quality;
                    _rewardQuality = validation.RewardLevel;
                    if (!_rewardQuality.HasValue && _targetQuality.HasValue)
                    {
                        int nextLevelValue = (int)_targetQuality.Value + 1;
                        if (Enum.IsDefined(typeof(ItemValueLevel), nextLevelValue))
                        {
                            _rewardQuality = (ItemValueLevel)nextLevelValue;
                        }
                    }
                }

                ItemUIUtilities.Select((ItemDisplay)null!);
            }
            catch (Exception ex)
            {
                Log.Error($"[RecycleDebug] Exception during transfer: {ex.Message}");
                throw;
            }
            finally
            {
                _transferring = false;
                UpdateActionButtonsState();
            }
        }

        private void ReturnItem(Item item)
        {
            if (_contractInventory == null || item == null)
            {
                return;
            }

            try
            {
                if (!_contractInventory.RemoveItem(item))
                {
                    Log.Warning($"[Recycle] Failed to remove item {item.DisplayName} from contract inventory.");
                    return;
                }

                if (!_itemOrigins.TryGetValue(item, out var originContext) || originContext.Inventory == null)
                {
                    GiveToPlayer(item);
                    _itemOrigins.Remove(item);
                    return;
                }

                bool restored = false;

                if (originContext.StackSource != null)
                {
                    try
                    {
                        originContext.StackSource.Combine(item);
                        restored = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Cycle bin failed to combine returned item: {ex.Message}");
                        restored = false;
                    }
                }

                if (!restored)
                {
                    if (!originContext.Inventory.AddItem(item))
                    {
                        GiveToPlayer(item);
                    }
                }

                _itemOrigins.Remove(item);

                if (_contractInventory.GetItemCount() == 0)
                {
                    ResetContractRequirements();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Recycle] Exception during ReturnItem for {item?.DisplayName ?? "null"}: {ex.Message}");
                // Ensure item is removed from origins even on error
                if (item != null)
                {
                    _itemOrigins.Remove(item);
                    // Give to player as fallback
                    GiveToPlayer(item);
                }
            }
        }

        private void ReturnAllItems()
        {
            if (_contractInventory == null)
            {
                return;
            }

            try
            {
                var items = _contractInventory.Content.Where(i => i != null).ToList();
                foreach (var item in items)
                {
                    try
                    {
                        ReturnItem(item);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[Recycle] Exception during ReturnItem for {item?.DisplayName ?? "null"} in ReturnAllItems: {ex.Message}");
                    }
                }

                ResetContractRequirements();
                UpdateActionButtonsState();
            }
            catch (Exception ex)
            {
                Log.Error($"[Recycle] Exception during ReturnAllItems: {ex.Message}");
                // Attempt force cleanup
                ForceClearContractInventory();
            }
        }

        private static void GiveToPlayer(Item item)
        {
            if (!ItemUtilities.SendToPlayerCharacterInventory(item))
            {
                ItemUtilities.SendToPlayerStorage(item);
            }
        }

        private void UpdateActionButtonsState()
        {
            if (_contractInventory == null)
            {
                if (_confirmButton != null)
                {
                    _confirmButton.interactable = false;
                }

                if (_clearButton != null)
                {
                    _clearButton.interactable = false;
                }

                if (_confirmButtonLabel != null)
                {
                    _confirmButtonLabel.text = Localizations.I18n.ConfirmKey.ToPlainText();
                }

                if (_clearButtonLabel != null)
                {
                    _clearButtonLabel.text = Localizations.I18n.ClearKey.ToPlainText();
                }

                return;
            }

            int itemCount = _contractInventory.GetItemCount();
            bool hasItems = itemCount > 0;
            bool contractFull = itemCount >= ContractSize;
            bool canConfirm = !_busy && !_transferring && contractFull && _rewardQuality.HasValue;
            bool canClear = !_busy && !_transferring && hasItems;

            if (_confirmButton != null)
            {
                _confirmButton.interactable = canConfirm;
            }

            if (_confirmButtonLabel != null)
            {
                var confirmText = Localizations.I18n.ConfirmKey.ToPlainText();
                confirmText = $"{confirmText} ({Mathf.Min(itemCount, ContractSize)}/{ContractSize})";
                _confirmButtonLabel.text = confirmText;
            }

            if (_clearButton != null)
            {
                _clearButton.interactable = canClear;
            }

            if (_clearButtonLabel != null)
            {
                var clearText = Localizations.I18n.ClearKey.ToPlainText();
                _clearButtonLabel.text = clearText;
            }
        }

        private async UniTask CompleteContractAsync()
        {
            Log.Debug("[Recycle:CompleteContract] Starting contract completion");

            if (_busy || _contractInventory == null)
            {
                Log.Debug($"[Recycle:CompleteContract] Aborting - busy: {_busy}, contractInventory null: {_contractInventory == null}");
                return;
            }

            var items = _contractInventory.Content.Where(i => i != null).ToList();
            Log.Debug($"[Recycle:CompleteContract] Found {items.Count} items in contract (required: {ContractSize})");

            if (items.Count < ContractSize)
            {
                Log.Debug("[Recycle:CompleteContract] Not enough items, aborting");
                return;
            }

            _busy = true;
            UpdateActionButtonsState();

            try
            {
                var rewardLevel = _rewardQuality;
                Log.Debug($"[Recycle:CompleteContract] Target reward level: {rewardLevel}");

                Log.Debug("[Recycle:CompleteContract] Destroying submitted items");
                foreach (var item in items)
                {
                    _contractInventory.RemoveItem(item);
                    _itemOrigins.Remove(item);
                    item.Detach();
                    UnityEngine.Object.Destroy(item.gameObject);
                }

                Item? reward = null;

                // If all submitted items are bullets, use bullet-only logic and return a full stack bullet
                bool allBullets = items.All(i => RecycleService.GetItemCategory(i.TypeID) == "Bullet");
                Log.Debug($"[Recycle:CompleteContract] All items are bullets: {allBullets}");

                if (allBullets)
                {
                    if (rewardLevel.HasValue)
                    {
                        Log.Debug($"[Recycle:CompleteContract] Picking bullet stack at level {rewardLevel.Value}");
                        reward = await RecycleService.PickRandomBulletStackByQualityAsync(rewardLevel.Value);
                    }

                    if (reward == null)
                    {
                        var fallbackLevel = DetermineTargetValueLevel(items);
                        Log.Debug($"[Recycle:CompleteContract] Bullet reward failed, using fallback level {fallbackLevel}");
                        reward = await RecycleService.PickRandomBulletStackByQualityAsync(fallbackLevel);
                    }
                }
                else
                {
                    if (rewardLevel.HasValue)
                    {
                        Log.Debug($"[Recycle:CompleteContract] Picking item at level {rewardLevel.Value}");
                        reward = await RecycleService.PickRandomItemByCategoriesAndQualityAsync(ItemUtils.RecyclableCategories, rewardLevel.Value);
                    }

                    if (reward == null)
                    {
                        var fallbackLevel = DetermineTargetValueLevel(items);
                        Log.Debug($"[Recycle:CompleteContract] Item reward failed, trying fallback level {fallbackLevel}");
                        reward = await TryCreateRewardItem(fallbackLevel);
                    }
                }

                ResetContractRequirements();

                if (reward == null)
                {
                    Log.Debug("[Recycle:CompleteContract] No reward generated, showing failure notification");
                    NotificationText.Push(Localizations.I18n.RecyclingFailedKey.ToPlainText());
                    return;
                }

                Log.Debug($"[Recycle:CompleteContract] Generated reward: {reward.DisplayName} (quality: {QualityUtils.GetCachedItemValueLevel(reward)})");

                // Play reward animation
                Log.Debug("[Recycle:CompleteContract] Playing reward animation");
                await RecycleAnimation.Instance.PlayAsync(reward);

                Log.Debug("[Recycle:CompleteContract] Adding reward to player inventory");
                if (!ItemUtilities.SendToPlayerCharacterInventory(reward))
                {
                    ItemUtilities.SendToPlayerStorage(reward);
                }

                string message = Localizations.I18n.PickNotificationFormatKey.ToPlainText()
                    .Replace("{itemDisplayName}", reward.DisplayName);
                NotificationText.Push(message);
                AudioManager.Post("UI/buy");

                Log.Debug("[Recycle:CompleteContract] Contract completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"Cycle bin contract failed: {ex.Message}");
            }
            finally
            {
                _busy = false;
                UpdateActionButtonsState();
                Log.Debug("[Recycle:CompleteContract] Contract completion finished");
            }
        }

        private static ItemValueLevel DetermineTargetValueLevel(IEnumerable<Item> items)
        {
            var levels = items
              .Where(i => i != null)
              .Select(QualityUtils.GetCachedItemValueLevel)
              .Select(level => (int)level)
              .ToList();

            if (levels.Count == 0)
            {
                return ItemValueLevel.Green;
            }

            float average = (float)levels.Average();
            int targetValue = Mathf.Clamp(Mathf.RoundToInt(average) + 1, (int)ItemValueLevel.White, (int)ItemValueLevel.Red);
            return (ItemValueLevel)targetValue;
        }

        private static async UniTask<Item?> TryCreateRewardItem(ItemValueLevel targetLevel)
        {
            var rewardList = await RecycleService.PickRandomItemsByQualityAsync(targetLevel, 1);
            if (rewardList != null && rewardList.Count > 0)
            {
                return rewardList[0];
            }

            Log.Warning("Cycle bin value-level-based reward failed – falling back to default lottery pool.");
            var fallback = await RecycleService.PickRandomLotteryItemAsync();
            if (fallback != null)
            {
                fallback.StackCount = 1;
            }

            return fallback;
        }
    }
}