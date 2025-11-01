using Duckov.Options.UI;
using DuckovLuckyBox.UI;
using HarmonyLib;

namespace DuckovLuckyBox.Patches
{
    [HarmonyPatch(typeof(OptionsPanel), "Setup")]
    public static class PatchOptionsPanel
    {
        public static void Postfix(OptionsPanel __instance)
        {
            GameSettingUI.Instance.Initialize(__instance);
        }
    }
}