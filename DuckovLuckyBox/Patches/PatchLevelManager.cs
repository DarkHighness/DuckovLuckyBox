using DuckovLuckyBox.Core;
using HarmonyLib;
namespace DuckovLuckyBox.Patches
{
  [HarmonyPatch(typeof(LevelManager), "InitLevel")]
  public static class PatchLevelManager
  {
    public static void Postfix()
    {
      ModBehaviour.SettingsUI?.Destroy();
      ItemOperationMenuUI.Instance.Destroy();
      StockShopViewUI.Instance.Destroy();
      RecycleSessionUI.Instance.Destroy();
    }
  }
}