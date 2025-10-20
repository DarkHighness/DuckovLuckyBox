using System.Reflection;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using TMPro;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using DuckovLuckyBox.Core;

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
            harmony = new Harmony(Constants.ModId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Localizations.Instance.Initialize();
            // ItemAssetsCollection.AddDynamicEntry(LuckyBoxItem.Instance);
        }

        void OnDisable()
        {
            Localizations.Instance.Destroy();
            // ItemAssetsCollection.RemoveDynamicEntry(LuckyBoxItem.Instance);
            harmony?.UnpatchAll(Constants.ModId);
        }
    }
}
