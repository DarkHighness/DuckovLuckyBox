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
            // Destroy all UI elements created by the mod when the level is initialized (i.e., when returning to the main menu, returning to the base).
            ItemOperationMenuUI.Instance.Destroy();
            StockShopViewUI.Instance.Destroy();
            RecycleSessionUI.Instance.Destroy();

            LotteryAnimation.Instance.Destroy();
            RecycleAnimation.Instance.Destroy();
        }
    }
}