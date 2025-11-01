using System.Reflection;
using HarmonyLib;
using UnityEngine;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;
using Cysharp.Threading.Tasks;

namespace DuckovLuckyAnimation
{
    public class ModBehaviour : DuckovLuckyBox.ModBehaviour { }

}

namespace DuckovLuckyBox
{

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
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

            harmony = new Harmony(Constants.ModId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Debug("Harmony patches applied.");

            Constants.Sound.LoadSounds();

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
                DebugUtils.DumpItemsToCSV();
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
            if (SettingManager.Instance.EnableDebug.GetAsBool())
            {
                if (Input.GetKeyDown(KeyCode.F9) || Input.GetKeyDown(KeyCode.F8))
                {
                    int[] items;
                    if (Input.GetKeyDown(KeyCode.F9))
                    {
                        items = new int[] { 1172, 1173, 1177, 95, 31 };
                    }
                    else
                    {
                        items = new int[] { 1178, 444 };
                    }

                    foreach (var itemId in items)
                    {
                        // send 5 of each item for testing
                        for (int i = 0; i < 5; i++)
                        {
                            ItemUtils.GameItemCache.SendItemToCharacterInventory(itemId, 1).Forget();
                        }
                    }
                }
            }
        }
    }
}
