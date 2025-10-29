using Duckov.UI;
using DuckovLuckyBox.Core;
using HarmonyLib;
using UnityEngine;

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
