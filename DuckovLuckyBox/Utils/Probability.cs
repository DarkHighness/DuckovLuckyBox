using System;
using System.Collections.Generic;
using System.Linq;

namespace DuckovLuckyBox
{
  public static class ProbabilityUtils
  {
    // ===========================================
    // 物品爆率设置 (千分之几 = 百分比)
    // ===========================================
    // | 品质   | 概率 | 百分比 |
    // |--------|------|--------|
    // | 白色   | 200  | 20.0%  |
    // | 绿色   | 268  | 26.8%  |
    // | 蓝色   | 252  | 25.2%  |
    // | 紫色   | 120  | 12.0%  |
    // | 橙色   | 80   | 8.0%   |
    // | 红色   | 50   | 5.0%   |
    // | 浅红色 | 30   | 3.0%   |
    // ===========================================

    // Return the probability (thousandth) of getting an item of the specified ItemValueLevel
    public static int GetProbabilityForItemValueLevel(ItemValueLevel level)
    {
      return level switch
      {
        ItemValueLevel.White => 200,
        ItemValueLevel.Green => 268,
        ItemValueLevel.Blue => 252,
        ItemValueLevel.Purple => 120,
        ItemValueLevel.Orange => 80,
        ItemValueLevel.Red => 50,
        ItemValueLevel.LightRed => 30,
        _ => 0
      };
    }

    public class MeltProbability
    {
      public ItemValueLevel Level { get; private set; }
      public int ProbabilityLevelUp { get; private set; } // in thousandths
      public int ProbabilityLevelDown { get; private set; } // in thousandths
      public int ProbabilitySameLevel { get; private set; } // in thousandths
      public int ProbabilityDestroy { get; private set; } // in thousandths (item completely destroyed)

      private MeltProbability(ItemValueLevel level, int probUp, int probDown, int probSame, int probDestroy)
      {
        Level = level;
        ProbabilityLevelUp = probUp;
        ProbabilityLevelDown = probDown;
        ProbabilitySameLevel = probSame;
        ProbabilityDestroy = probDestroy;

        if (probUp + probDown + probSame + probDestroy != 1000)
        {
          throw new ArgumentException("Probabilities must sum to 1000 (thousandths)");
        }
      }

      // ===========================================
      // 熔炼概率设置 (千分之几 = 百分比)
      // ===========================================
      // | 品质   | 升级概率 | 降级概率 | 保持概率 | 损毁概率 |
      // |--------|----------|----------|----------|----------|
      // | 白色   | 300(30%) | 200(20%) | 400(40%) | 100(10%) |
      // | 绿色   | 250(25%) | 250(25%) | 400(40%) | 100(10%) |
      // | 蓝色   | 200(20%) | 300(30%) | 400(40%) | 100(10%) |
      // | 紫色   | 150(15%) | 350(35%) | 400(40%) | 100(10%) |
      // | 橙色   | 100(10%) | 400(40%) | 350(35%) | 150(15%) |
      // | 浅红色 | 50(5%)   | 450(45%) | 300(30%) | 200(20%) |
      // | 红色   | 0(0%)    | 500(50%) | 300(30%) | 200(20%) |
      // ===========================================

      // Get melt probabilities for a given ItemValueLevel
      public static MeltProbability GetMeltProbabilityForLevel(ItemValueLevel level)
      {
        // The higher the level, the higher the chance to go down, the lower the chance to go up, etc.
        return level switch
        {
          ItemValueLevel.White => new MeltProbability(level, 300, 200, 400, 100),
          ItemValueLevel.Green => new MeltProbability(level, 250, 250, 400, 100),
          ItemValueLevel.Blue => new MeltProbability(level, 200, 300, 400, 100),
          ItemValueLevel.Purple => new MeltProbability(level, 150, 350, 400, 100),
          ItemValueLevel.Orange => new MeltProbability(level, 100, 400, 350, 150),
          ItemValueLevel.LightRed => new MeltProbability(level, 50, 450, 300, 200),
          ItemValueLevel.Red => new MeltProbability(level, 0, 500, 300, 200),
          _ => new MeltProbability(level, 0, 0, 1000, 0)
        };
      }
    }

    public static List<T> ReservoirSample<T>(IEnumerable<T> source, int k, bool allowDuplicates = false)
    {
      var sourceList = source.ToList();
      int n = sourceList.Count;
      var reservoir = new List<T>(Math.Min(k, n));
      for (int i = 0; i < n; i++)
      {
        if (i < k)
        {
          reservoir.Add(sourceList[i]);
        }
        else
        {
          int j = UnityEngine.Random.Range(0, i + 1);
          if (j < k)
          {
            reservoir[j] = sourceList[i];
          }
        }
      }

      // If we allow duplicates and haven't filled the reservoir yet, fill it randomly
      while (reservoir.Count < k && allowDuplicates)
      {
        int index = UnityEngine.Random.Range(0, n);
        reservoir.Add(sourceList[index]);
      }
      return reservoir;
    }
  }
}