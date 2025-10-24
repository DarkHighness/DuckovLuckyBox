using System.Reflection;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using TMPro;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;

namespace DuckovLuckyBox
{

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private SettingsUI? settingsUI = null;
        private Harmony? harmony = null;

        void Awake()
        {
            Log.Debug($"{Constants.ModName} has been loaded.");
        }

        void OnDestroy()
        {

        }

        void OnEnable()
        {
            // Initialize settings config first, passing this MonoBehaviour as host
            Settings.InitializeConfig(this);
            Log.Debug("Settings config initialized.");

            settingsUI = gameObject.AddComponent<SettingsUI>();
            Log.Debug("Settings UI component created.");

            Constants.Sound.LoadSounds();

            harmony = new Harmony(Constants.ModId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Debug("Harmony patches applied.");

            Localizations.Instance.Initialize();
            Log.Debug("Localizations initialized.");
        }

        void OnDisable()
        {
            Localizations.Instance.Destroy();
            Log.Debug("Localizations destroyed.");

            harmony?.UnpatchAll(Constants.ModId);
            Log.Debug("Harmony patches removed.");

            Settings.CleanupConfig();
            Log.Debug("Settings config cleaned up.");
        }

        void Update()
        {
            var hotkey = Settings.Instance.SettingsHotkey.Value as Hotkey ?? DefaultSettings.SettingsHotkey;

            if (hotkey.IsPressed())
            {
                settingsUI!.ToggleSettingsUI();
            }
        }
    }
}
