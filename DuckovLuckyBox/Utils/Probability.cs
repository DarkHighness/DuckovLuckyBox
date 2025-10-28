using System;
using System.Collections.Generic;
using System.Linq;

namespace DuckovLuckyBox
{
  public static class ProbabilityUtils
  {
    // Return the probability (thousandth) of getting an item of the specified ItemValueLevel
    public static int GetProbabilityForItemValueLevel(ItemValueLevel level)
    {
      return level switch
      {
        ItemValueLevel.White => 300,
        ItemValueLevel.Green => 250,
        ItemValueLevel.Blue => 200,
        ItemValueLevel.Purple => 130,
        ItemValueLevel.Orange => 70,
        ItemValueLevel.Red => 34,
        ItemValueLevel.LightRed => 16,
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

      // Get melt probabilities for a given ItemValueLevel
      public static MeltProbability GetMeltProbabilityForLevel(ItemValueLevel level)
      {
        // The higher the level, the higher the chance to go down, the lower the chance to go up, etc.
        return level switch
        {
          ItemValueLevel.White => new MeltProbability(level, 300, 200, 300, 200),
          ItemValueLevel.Green => new MeltProbability(level, 250, 300, 250, 200),
          ItemValueLevel.Blue => new MeltProbability(level, 200, 350, 350, 100),
          ItemValueLevel.Purple => new MeltProbability(level, 200, 350, 370, 80),
          ItemValueLevel.Orange => new MeltProbability(level, 100, 500, 300, 100),
          ItemValueLevel.LightRed => new MeltProbability(level, 50, 550, 200, 200),
          ItemValueLevel.Red => new MeltProbability(level, 0, 300, 500, 200),
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