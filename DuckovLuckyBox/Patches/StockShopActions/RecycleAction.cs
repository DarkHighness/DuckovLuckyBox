using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using Duckov;
using Duckov.Economy.UI;
using Duckov.UI;
using Duckov.UI.Animations;
using Duckov.Utilities;
using DuckovLuckyBox.Core;
using DuckovLuckyBox.Core.Settings;
using DuckovLuckyBox.UI;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckovLuckyBox.Patches.StockShopActions
{
  /// <summary>
  /// Implements the stock shop "trash bin" action using native inventory widgets.
  /// </summary>
  public sealed class RecycleAction : IStockShopAction
  {
    public string GetLocalizationKey() => Localizations.I18n.RecycleKey;

    public async UniTask ExecuteAsync(StockShopView stockShopView)
    {
      if (stockShopView == null)
      {
        Log.Warning("RecycleAction executed without a StockShopView instance.");
        return;
      }

      var session = RecycleSessionUI.Instance;
      session.Setup(stockShopView);
      session.Toggle();
      await UniTask.CompletedTask;
    }
  }
}
