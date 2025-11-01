using Duckov.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Cysharp.Threading.Tasks;
using SodaCraft.Localizations;
using FMODUnity;
using FMOD;
using DuckovLuckyBox.Core.Settings;

namespace DuckovLuckyBox.Core
{
    public class ItemOperationMenuUI : IComponent
    {
        private static ItemOperationMenuUI? _instance;
        public static ItemOperationMenuUI Instance
        {
            get
            {
                _instance ??= new ItemOperationMenuUI();
                return _instance;
            }
        }

        private bool isInitialized = false;
        private bool isOpen = false;
        private Button? destroyButton;
        private Button? meltButton;
        private ItemOperationMenu? target;

        public void Setup(ItemOperationMenu menu)
        {
            if (!isInitialized)
            {
                Log.Debug("Initializing ItemOperationMenuUI");
                var contentRectField = AccessTools.Field(typeof(ItemOperationMenu), "contentRectTransform");
                var contentRect = contentRectField?.GetValue(menu) as RectTransform;
                if (contentRect != null)
                {
                    isInitialized = true;
                    this.target = menu;
                    // Register setting change listeners
                    RegisterSettingListeners();
                    // Only create buttons once
                    if (destroyButton == null || meltButton == null)
                    {
                        CreateCustomButtons(menu, contentRect);
                    }
                }
                else
                {
                    Log.Warning("Cannot get contentRectTransform for initialization");
                    return;
                }
            }

            this.target = menu;
            isOpen = true; // Setting up the menu means it's open

            // Update Destroy button visibility
            UpdateDestroyButtonVisibility();

            // Update Melt button visibility
            UpdateMeltButtonVisibility();
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

        public void Open()
        {
            if (!isInitialized) return;

            Log.Debug("Opening ItemOperationMenuUI");
            isOpen = true;
            // Show buttons
            UpdateDestroyButtonVisibility();
            UpdateMeltButtonVisibility();
        }

        public void Close()
        {
            if (!isInitialized) return;

            Log.Debug("Closing ItemOperationMenuUI");
            isOpen = false;
            // Hide buttons
            UpdateDestroyButtonVisibility();
            UpdateMeltButtonVisibility();
        }

        public void Destroy()
        {
            if (!isInitialized) return;
            Log.Debug("Destroying ItemOperationMenuUI");
            // Destroy logic, clean up
            if (destroyButton != null)
            {
                Object.Destroy(destroyButton.gameObject);
                destroyButton = null;
            }
            if (meltButton != null)
            {
                Object.Destroy(meltButton.gameObject);
                meltButton = null;
            }
            _instance = null;
            isInitialized = false;
        }

        private void RegisterSettingListeners()
        {
            // Listen to EnableDestroyButton setting changes
            SettingManager.Instance.EnableDestroyButton.OnValueChanged += (value) =>
            {
                bool enabled = (bool)value;
                if (destroyButton != null && target != null)
                {
                    // Re-evaluate button visibility based on new setting
                    UpdateDestroyButtonVisibility();
                }
            };

            // Listen to EnableLotteryButton setting changes
            SettingManager.Instance.EnableMeltButton.OnValueChanged += (value) =>
            {
                bool enabled = (bool)value;
                if (meltButton != null && target != null)
                {
                    // Re-evaluate button visibility based on new setting
                    UpdateMeltButtonVisibility();
                }
            };

            Log.Debug("Setting listeners registered for custom buttons");
        }

        private void UpdateDestroyButtonVisibility()
        {
            if (destroyButton == null) return;

            if (!isOpen)
            {
                destroyButton.gameObject.SetActive(false);
                return;
            }

            bool destroyButtonEnabled = (bool)SettingManager.Instance.EnableDestroyButton.Value;

            // Get current target item
            var targetDisplayField = AccessTools.Field(typeof(ItemOperationMenu), "TargetDisplay");
            var targetDisplay = targetDisplayField?.GetValue(target) as ItemDisplay;
            var targetItem = targetDisplay?.Target;

            // Show button only if enabled in settings and there's a target item
            bool shouldShow = destroyButtonEnabled && targetItem != null;

            destroyButton.gameObject.SetActive(shouldShow);
            destroyButton.interactable = shouldShow;

            Log.Debug($"Destroy button visibility updated: {shouldShow}");
        }

        private void UpdateMeltButtonVisibility()
        {
            if (meltButton == null) return;

            if (!isOpen)
            {
                meltButton.gameObject.SetActive(false);
                return;
            }

            bool meltButtonEnabled = (bool)SettingManager.Instance.EnableMeltButton.Value;

            // Get current target item
            var targetDisplayField = AccessTools.Field(typeof(ItemOperationMenu), "TargetDisplay");
            var targetDisplay = targetDisplayField?.GetValue(target) as ItemDisplay;
            var targetItem = targetDisplay?.Target;

            // Check if item is a bullet (should hide melt button)
            bool isBullet = false;
            if (targetItem != null)
            {
                string category = ItemUtils.GameItemCache.GetItemCategory(targetItem.TypeID);
                isBullet = category == "Bullet";
            }

            // Show button only if enabled in settings, there's a target item, and it's not a bullet
            bool shouldShow = meltButtonEnabled && targetItem != null && !isBullet;

            meltButton.gameObject.SetActive(shouldShow);
            meltButton.interactable = shouldShow;

            Log.Debug($"Melt button visibility updated: {shouldShow}");
        }

        private void CreateCustomButtons(ItemOperationMenu menu, RectTransform contentRect)
        {
            // Find an existing button to clone
            var existingButton = contentRect.GetComponentInChildren<Button>();
            if (existingButton == null)
            {
                Log.Warning("Cannot find existing button to clone in ItemOperationMenu");
                return;
            }

            // Create Destroy button with red color
            destroyButton = CreateButton(
                existingButton,
                contentRect,
                Localizations.I18n.ItemMenuDestroyKey.ToPlainText(),
                new Color(0.8f, 0.2f, 0.2f, 1f), // BG color - dark red
                new Color(0.9f, 0.3f, 0.3f, 1f), // Main color - light red
                () => OnDestroyClicked(menu));

            // Create Lottery button with gold color
            meltButton = CreateButton(
                existingButton,
                contentRect,
                Localizations.I18n.ItemMenuMeltKey.ToPlainText(),
                new Color(0.9f, 0.7f, 0.2f, 1f), // BG color - dark gold
                new Color(1f, 0.8f, 0.3f, 1f),    // Main color - light gold
                () => OnMeltClicked(menu).Forget());

            Log.Debug("Custom buttons created for ItemOperationMenu");
        }

        private Button CreateButton(Button template, RectTransform parent, string text,
            Color bgColor, Color mainColor, System.Action onClick)
        {
            // Clone the button
            var buttonObj = Object.Instantiate(template.gameObject, parent);
            buttonObj.name = $"btn_{text}";

            var button = buttonObj.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());

            // Update button text - find "Text (TMP)" child
            var textTransform = buttonObj.transform.Find("Text (TMP)");
            if (textTransform != null)
            {
                // Disable TextLocalizor component to prevent text from being overridden
                var textLocalizor = textTransform.GetComponent<Component>();
                var localizorType = textTransform.GetComponents<Component>()
                    .FirstOrDefault(c => c.GetType().Name == "TextLocalizor");
                if (localizorType != null)
                {
                    Object.Destroy(localizorType);
                    Log.Debug("Removed TextLocalizor component");
                }

                var textComponent = textTransform.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                    Log.Debug($"Updated button text to: {text}");
                }
            }

            // Update button background colors - find "BG" child
            var bgTransform = buttonObj.transform.Find("BG");
            if (bgTransform != null)
            {
                var bgGraphic = bgTransform.GetComponent<Graphic>();
                if (bgGraphic != null)
                {
                    bgGraphic.color = bgColor;
                }
            }

            // Update main button color
            var mainGraphic = buttonObj.GetComponent<Graphic>();
            if (mainGraphic != null)
            {
                mainGraphic.color = mainColor;
            }

            // Hide or modify the Icon if needed
            var iconTransform = buttonObj.transform.Find("Icon");
            // Hide the icon for custom buttons
            iconTransform?.gameObject.SetActive(false);

            // Initially hide the button
            buttonObj.SetActive(false);

            return button;
        }

        private void OnDestroyClicked(ItemOperationMenu menu)
        {
            Log.Debug("Destroy button clicked");

            // Play destroy sound
            RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup sfxGroup);
            SoundUtils.PlaySound(Constants.Sound.DESTROY_SOUND, sfxGroup);

            // Get the target item using reflection
            var targetDisplayField = AccessTools.Field(typeof(ItemOperationMenu), "TargetDisplay");
            var targetDisplay = targetDisplayField?.GetValue(menu) as ItemDisplay;
            if (targetDisplay == null)
            {
                Log.Warning("Cannot get target item display");
                return;
            }

            var targetItem = targetDisplay.Target;
            if (targetItem == null)
            {
                Log.Warning("Target item is null");
                return;
            }

            Log.Info($"Destroying item: {targetItem.DisplayName}");

            var mainCharacter = LevelManager.Instance?.MainCharacter;
            if (mainCharacter != null && mainCharacter != null)
            {
                targetItem.Detach();
            }

            // Destroy the dropped item immediately
            Object.Destroy(targetItem.gameObject);

            // Close the menu
            menu.Close();
        }

        private async UniTask OnMeltClicked(ItemOperationMenu menu)
        {
            Log.Debug("Melt button clicked");

            // Play lottery sound
            // RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup sfxGroup);
            // SoundUtils.PlaySound(Constants.Sound.LOTTERY_SOUND, sfxGroup);

            // Get the target item using reflection
            var targetDisplayField = AccessTools.Field(typeof(ItemOperationMenu), "TargetDisplay");
            var targetDisplay = targetDisplayField?.GetValue(menu) as ItemDisplay;
            if (targetDisplay == null)
            {
                Log.Warning("Cannot get target item display");
                return;
            }

            var targetItem = targetDisplay.Target;
            if (targetItem == null)
            {
                Log.Warning("Target item is null");
                return;
            }

            Log.Info($"Using item for lottery: {targetItem.DisplayName}");

            // Close the menu first
            menu.Close();

            // Drop the original item (properly handle inventory removal) then destroy it
            var mainCharacter = LevelManager.Instance?.MainCharacter;
            if (mainCharacter != null && mainCharacter != null)
            {
                targetItem.Detach();
            }

            var meltResult = await MeltService.MeltItemAsync(targetItem);
            if (!meltResult.Success)
            {
                Log.Error("Melt action failed");
                // Restore the item to inventory if melt failed
                mainCharacter?.CharacterItem?.Inventory?.AddItem(targetItem);
            }

        }
    }
}