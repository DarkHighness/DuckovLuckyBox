using DuckovLuckyBox.Core;
using HarmonyLib;
namespace DuckovLuckyBox.Patches
{
  [HarmonyPatch(typeof(LevelManager), "InitLevel")]
  public static class PatchLevelManager
  {
    public static void Postfix()
    {
      ItemOperationMenuUI.Instance.Destroy();
      StockShopViewUI.Instance.Destroy();
      RecycleSessionUI.Instance.Destroy();
    }
  }
}