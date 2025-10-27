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