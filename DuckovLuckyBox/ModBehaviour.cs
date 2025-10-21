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
using DuckovLuckyBox.Core.Settings.UI;

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
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                settingsUI!.ToggleSettingsUI();
            }
        }
    }
}
