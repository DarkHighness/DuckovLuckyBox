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

  [HarmonyPatch(typeof(ItemOperationMenu), "Initialize")]
  public class PatchItemOperationMenu_Initialize
  {
    public static void Postfix(ItemOperationMenu __instance, RectTransform ___contentRectTransform)
    {
      if (___contentRectTransform == null) return;

      ItemOperationMenuUI.Instance.Setup(__instance);
    }
  }

  [HarmonyPatch(typeof(ItemOperationMenu), "Setup")]
  public class PatchItemOperationMenu_Setup
  {
    public static void Postfix(ItemOperationMenu __instance)
    {
      ItemOperationMenuUI.Instance.Setup(__instance);
      ItemOperationMenuUI.Instance.Open();
    }
  }

  [HarmonyPatch(typeof(ItemOperationMenu), "OnClose")]
  public class PatchItemOperationMenu_OnClose
  {
    public static void Postfix(ItemOperationMenu __instance)
    {
      ItemOperationMenuUI.Instance.Close();
    }
  }
}
