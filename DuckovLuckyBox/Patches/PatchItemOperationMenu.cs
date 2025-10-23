using System.Collections.Generic;
using Duckov.UI;
using DuckovLuckyBox.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemStatsSystem;
using System.Linq;
using Cysharp.Threading.Tasks;
using SodaCraft.Localizations;
using FMODUnity;
using FMOD;

namespace DuckovLuckyBox.Patches
{

  public class MenuState
  {
    public static Dictionary<ItemOperationMenu, MenuState> States = new Dictionary<ItemOperationMenu, MenuState>();

    public Button? DestroyButton { get; set; }
    public Button? LotteryButton { get; set; }
    public ItemOperationMenu? Menu { get; set; }
  }

  [HarmonyPatch(typeof(ItemOperationMenu), "Initialize")]
  public class PatchItemOperationMenu_Initialize
  {
    public static void Postfix(ItemOperationMenu __instance, RectTransform ___contentRectTransform)
    {
      if (___contentRectTransform == null) return;

      // Get or create menu state
      if (!MenuState.States.TryGetValue(__instance, out var state))
      {
        state = new MenuState();
        state.Menu = __instance;
        MenuState.States[__instance] = state;

        // Register setting change listeners
        RegisterSettingListeners(state);
      }

      // Only create buttons once
      if (state.DestroyButton == null || state.LotteryButton == null)
      {
        CreateCustomButtons(__instance, ___contentRectTransform, state);
      }
    }

    private static void RegisterSettingListeners(MenuState state)
    {
      // Listen to EnableDestroyButton setting changes
      Core.Settings.Settings.Instance.EnableDestroyButton.OnValueChanged += (value) =>
      {
        bool enabled = (bool)value;
        if (state.DestroyButton != null && state.Menu != null)
        {
          // Re-evaluate button visibility based on new setting
          UpdateDestroyButtonVisibility(state);
        }
      };

      // Listen to EnableLotteryButton setting changes
      Core.Settings.Settings.Instance.EnableLotteryButton.OnValueChanged += (value) =>
      {
        bool enabled = (bool)value;
        if (state.LotteryButton != null && state.Menu != null)
        {
          // Re-evaluate button visibility based on new setting
          UpdateLotteryButtonVisibility(state);
        }
      };

      Log.Debug("Setting listeners registered for custom buttons");
    }

    private static void UpdateDestroyButtonVisibility(MenuState state)
    {
      if (state.DestroyButton == null || state.Menu == null) return;

      bool destroyButtonEnabled = (bool)Core.Settings.Settings.Instance.EnableDestroyButton.Value;

      // Get current target item
      var targetDisplayField = AccessTools.Field(typeof(ItemOperationMenu), "TargetDisplay");
      var targetDisplay = targetDisplayField?.GetValue(state.Menu) as ItemDisplay;
      var targetItem = targetDisplay?.Target;

      // Show button only if enabled in settings and there's a target item
      bool shouldShow = destroyButtonEnabled && targetItem != null;

      state.DestroyButton.gameObject.SetActive(shouldShow);
      state.DestroyButton.interactable = shouldShow;

      Log.Debug($"Destroy button visibility updated: {shouldShow}");
    }

    private static void UpdateLotteryButtonVisibility(MenuState state)
    {
      if (state.LotteryButton == null || state.Menu == null) return;

      bool lotteryButtonEnabled = (bool)Core.Settings.Settings.Instance.EnableLotteryButton.Value;

      // Get current target item
      var targetDisplayField = AccessTools.Field(typeof(ItemOperationMenu), "TargetDisplay");
      var targetDisplay = targetDisplayField?.GetValue(state.Menu) as ItemDisplay;
      var targetItem = targetDisplay?.Target;

      // Show button only if enabled in settings and there's a target item
      bool shouldShow = lotteryButtonEnabled && targetItem != null;

      state.LotteryButton.gameObject.SetActive(shouldShow);
      state.LotteryButton.interactable = shouldShow;

      Log.Debug($"Lottery button visibility updated: {shouldShow}");
    }

    private static void CreateCustomButtons(ItemOperationMenu menu, RectTransform contentRect, MenuState state)
    {
      // Find an existing button to clone
      var existingButton = contentRect.GetComponentInChildren<Button>();
      if (existingButton == null)
      {
        Log.Warning("Cannot find existing button to clone in ItemOperationMenu");
        return;
      }

      // Create Destroy button with red color
      state.DestroyButton = CreateButton(
        existingButton,
        contentRect,
        Constants.I18n.ItemMenuDestroyKey.ToPlainText(),
        new Color(0.8f, 0.2f, 0.2f, 1f), // BG color - dark red
        new Color(0.9f, 0.3f, 0.3f, 1f), // Main color - light red
        () => OnDestroyClicked(menu));

      // Create Lottery button with gold color
      state.LotteryButton = CreateButton(
        existingButton,
        contentRect,
        Constants.I18n.ItemMenuLotteryKey.ToPlainText(),
        new Color(0.9f, 0.7f, 0.2f, 1f), // BG color - dark gold
        new Color(1f, 0.8f, 0.3f, 1f),    // Main color - light gold
        () => OnLotteryClicked(menu).Forget());

      Log.Debug("Custom buttons created for ItemOperationMenu");
    }

    private static Button CreateButton(Button template, RectTransform parent, string text,
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
          Log.Debug($"Set BG color: {bgColor}");
        }
      }

      // Update main button color
      var mainGraphic = buttonObj.GetComponent<Graphic>();
      if (mainGraphic != null)
      {
        mainGraphic.color = mainColor;
        Log.Debug($"Set main color: {mainColor}");
      }

      // Hide or modify the Icon if needed
      var iconTransform = buttonObj.transform.Find("Icon");
      if (iconTransform != null)
      {
        // Hide the icon for custom buttons
        iconTransform.gameObject.SetActive(false);
        Log.Debug("Hidden icon for custom button");
      }

      // Initially hide the button
      buttonObj.SetActive(false);

      return button;
    }

    private static void PlaySound(FMOD.Sound? sound)
    {
      if (sound == null)
      {
        Log.Warning("Sound is null, cannot play");
        return;
      }

      try
      {
        RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup sfxGroup);
        RESULT result = RuntimeManager.CoreSystem.playSound(sound.Value, sfxGroup, false, out FMOD.Channel channel);
        if (result != RESULT.OK)
        {
          Log.Warning($"Failed to play sound: {result}");
        }
        else
        {
          Log.Debug("Sound played successfully");
        }
      }
      catch (System.Exception ex)
      {
        Log.Error($"Exception while playing sound: {ex.Message}");
      }
    }

    private static void OnDestroyClicked(ItemOperationMenu menu)
    {
      Log.Debug("Destroy button clicked");

      // Play destroy sound
      PlaySound(Constants.Sound.DESTROY_SOUND);

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
      if (mainCharacter != null && (UnityEngine.Object)mainCharacter != null)
      {
        targetItem.Detach();
      }

      // Destroy the dropped item immediately
      Object.Destroy(targetItem.gameObject);

      // Close the menu
      menu.Close();
    }

    private static async UniTask OnLotteryClicked(ItemOperationMenu menu)
    {
      Log.Debug("Lottery button clicked");

      // Play lottery sound
      PlaySound(Constants.Sound.LOTTERY_SOUND);

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
      if (mainCharacter != null && (UnityEngine.Object)mainCharacter != null)
      {
        targetItem.Detach();
      }

      // Destroy the dropped item immediately
      Object.Destroy(targetItem.gameObject);

      // Get a random item from the pool
      var itemTypeIds = ItemAssetsCollection.Instance.entries
        .Where(entry => !entry.prefab.DisplayName.StartsWith("*Item_") &&
                       entry.prefab.Quality > 0 &&
                       entry.prefab.Icon.name != "cross")
        .Select(entry => entry.typeID)
        .ToList();

      if (itemTypeIds.Count == 0)
      {
        Log.Error("No valid items available for lottery");
        return;
      }

      var selectedIndex = Random.Range(0, itemTypeIds.Count);
      var selectedItemTypeId = itemTypeIds[selectedIndex];

      Item newItem = await ItemAssetsCollection.InstantiateAsync(selectedItemTypeId);
      if (newItem == null)
      {
        Log.Error($"Failed to instantiate lottery item: {selectedItemTypeId}");
        return;
      }

      // Add to player inventory
      if (!ItemUtilities.SendToPlayerCharacterInventory(newItem))
      {
        Log.Warning($"Failed to send item to player inventory: {selectedItemTypeId}. Sending to storage.");
        ItemUtilities.SendToPlayerStorage(newItem);
      }

      // Show notification
      var message = Constants.I18n.LotteryResultFormatKey.ToPlainText()
        .Replace("{itemDisplayName}", newItem.DisplayName);
      NotificationText.Push(message);

      Log.Info($"Lottery result: {newItem.DisplayName}");
    }
  }

  [HarmonyPatch(typeof(ItemOperationMenu), "Setup")]
  public class PatchItemOperationMenu_Setup
  {
    public static void Postfix(ItemOperationMenu __instance)
    {
      if (!MenuState.States.TryGetValue(__instance, out var state))
      {
        return;
      }

      // Get the target item
      var targetDisplayField = AccessTools.Field(typeof(ItemOperationMenu), "TargetDisplay");
      var targetDisplay = targetDisplayField?.GetValue(__instance) as ItemDisplay;
      var targetItem = targetDisplay?.Target;

      if (targetItem == null)
      {
        // Hide buttons if no target item
        if (state.DestroyButton != null)
          state.DestroyButton.gameObject.SetActive(false);
        if (state.LotteryButton != null)
          state.LotteryButton.gameObject.SetActive(false);
        return;
      }

      // Check if buttons are enabled in settings
      bool destroyButtonEnabled = (bool)Core.Settings.Settings.Instance.EnableDestroyButton.Value;
      bool lotteryButtonEnabled = (bool)Core.Settings.Settings.Instance.EnableLotteryButton.Value;

      // Show/hide Destroy button based on settings only
      if (state.DestroyButton != null)
      {
        state.DestroyButton.gameObject.SetActive(destroyButtonEnabled);
        state.DestroyButton.interactable = destroyButtonEnabled;
      }

      // Show/hide Lottery button based on settings only
      if (state.LotteryButton != null)
      {
        state.LotteryButton.gameObject.SetActive(lotteryButtonEnabled);
        state.LotteryButton.interactable = lotteryButtonEnabled;
      }
    }
  }
}