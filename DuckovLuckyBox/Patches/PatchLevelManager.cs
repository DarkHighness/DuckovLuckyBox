using DuckovLuckyBox.Core;
using DuckovLuckyBox.UI;
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

      LotteryAnimation.Instance.Destroy();
      RecycleAnimation.Instance.Initialize();
    }
  }
}