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
using Cysharp.Threading.Tasks;

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
            SettingManager.InitializeConfig(this);
            Log.Debug("Settings config initialized.");

            settingsUI = gameObject.AddComponent<SettingsUI>();
            Log.Debug("Settings UI component created.");

            Constants.Sound.LoadSounds();

            harmony = new Harmony(Constants.ModId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Debug("Harmony patches applied.");

            Localizations.Instance.Initialize();
            Log.Debug("Localizations initialized.");

            // In this moment, the SettingsManager may not have loaded settings yet.
            // Thus, we start a coroutine to wait for one frame before checking the debug setting.
            StartCoroutine(PostEnableSetup().ToCoroutine());
        }

        async UniTask PostEnableSetup()
        {
            while (!SettingManager.Instance.IsInitialized)
            {
                await UniTask.Yield(); // Wait until settings are initialized
            }

            if (SettingManager.Instance.EnableDebug.GetAsBool())
            {
                Log.Warning("Debug mode is enabled.");
                ItemUtils.DumpAllItemMetadataCSV();
            }
        }

        void OnDisable()
        {
            Localizations.Instance.Destroy();
            Log.Debug("Localizations destroyed.");

            harmony?.UnpatchAll(Constants.ModId);
            Log.Debug("Harmony patches removed.");

            SettingManager.CleanupConfig();
            Log.Debug("Settings config cleaned up.");
        }

        void Update()
        {
            var hotkey = SettingManager.Instance.SettingsHotkey.Value as Hotkey ?? DefaultSettings.SettingsHotkey;

            if (hotkey.IsPressed())
            {
                settingsUI!.ToggleSettingsUI();
            }
        }
    }
}
