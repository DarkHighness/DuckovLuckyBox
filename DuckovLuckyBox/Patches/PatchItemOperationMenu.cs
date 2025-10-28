using System.Collections.Generic;
using Duckov.UI;
using DuckovLuckyBox.Core;
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

namespace DuckovLuckyBox.Patches
{

  public class MenuState
  {
    public static Dictionary<ItemOperationMenu, MenuState> States = new Dictionary<ItemOperationMenu, MenuState>();

    public Button? DestroyButton { get; set; }
    public Button? MeltButton { get; set; }
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
      if (state.DestroyButton == null || state.MeltButton == null)
      {
        CreateCustomButtons(__instance, ___contentRectTransform, state);
      }
    }

    private static void RegisterSettingListeners(MenuState state)
    {
      // Listen to EnableDestroyButton setting changes
      SettingManager.Instance.EnableDestroyButton.OnValueChanged += (value) =>
      {
        bool enabled = (bool)value;
        if (state.DestroyButton != null && state.Menu != null)
        {
          // Re-evaluate button visibility based on new setting
          UpdateDestroyButtonVisibility(state);
        }
      };

      // Listen to EnableLotteryButton setting changes
      SettingManager.Instance.EnableLotteryButton.OnValueChanged += (value) =>
      {
        bool enabled = (bool)value;
        if (state.MeltButton != null && state.Menu != null)
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

      bool destroyButtonEnabled = (bool)SettingManager.Instance.EnableDestroyButton.Value;

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
      if (state.MeltButton == null || state.Menu == null) return;

      bool lotteryButtonEnabled = (bool)SettingManager.Instance.EnableLotteryButton.Value;

      // Get current target item
      var targetDisplayField = AccessTools.Field(typeof(ItemOperationMenu), "TargetDisplay");
      var targetDisplay = targetDisplayField?.GetValue(state.Menu) as ItemDisplay;
      var targetItem = targetDisplay?.Target;

      // Check if item is a bullet (should hide lottery button)
      bool isBullet = false;
      if (targetItem != null)
      {
        string category = ItemUtils.GameItemCache.GetItemCategory(targetItem.TypeID);
        isBullet = category == "Bullet";
      }

      // Show button only if enabled in settings, there's a target item, and it's not a bullet
      bool shouldShow = lotteryButtonEnabled && targetItem != null && !isBullet;

      state.MeltButton.gameObject.SetActive(shouldShow);
      state.MeltButton.interactable = shouldShow;

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
        Localizations.I18n.ItemMenuDestroyKey.ToPlainText(),
        new Color(0.8f, 0.2f, 0.2f, 1f), // BG color - dark red
        new Color(0.9f, 0.3f, 0.3f, 1f), // Main color - light red
        () => OnDestroyClicked(menu));

      // Create Lottery button with gold color
      state.MeltButton = CreateButton(
        existingButton,
        contentRect,
        Localizations.I18n.ItemMenuMeltKey.ToPlainText(),
        new Color(0.9f, 0.7f, 0.2f, 1f), // BG color - dark gold
        new Color(1f, 0.8f, 0.3f, 1f),    // Main color - light gold
        () => OnMeltClicked(menu).Forget());

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

    private static void OnDestroyClicked(ItemOperationMenu menu)
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

    private static async UniTask OnMeltClicked(ItemOperationMenu menu)
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
      }

      // Destroy the dropped item immediately
      Object.Destroy(targetItem.gameObject);
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
        state.DestroyButton?.gameObject.SetActive(false);
        state.MeltButton?.gameObject.SetActive(false);
        return;
      }

      // Check if buttons are enabled in settings
      bool destroyButtonEnabled = (bool)SettingManager.Instance.EnableDestroyButton.Value;
      bool lotteryButtonEnabled = (bool)SettingManager.Instance.EnableLotteryButton.Value;

      // Check if item is a bullet (should hide lottery button)
      bool isBullet = false;
      if (targetItem != null)
      {
        string category = ItemUtils.MeltableItemCache.GetItemCategory(targetItem.TypeID);
        isBullet = category == "Bullet";
      }

      // Show/hide Destroy button based on settings only
      if (state.DestroyButton != null)
      {
        state.DestroyButton.gameObject.SetActive(destroyButtonEnabled);
        state.DestroyButton.interactable = destroyButtonEnabled;
      }

      // Show/hide Lottery button based on settings and item type
      if (state.MeltButton != null)
      {
        bool shouldShowLottery = lotteryButtonEnabled && !isBullet;
        state.MeltButton.gameObject.SetActive(shouldShowLottery);
        state.MeltButton.interactable = shouldShowLottery;
      }
    }
  }
}
