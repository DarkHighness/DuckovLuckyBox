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
            public int ProbabilityMutation { get; private set; } // in thousandths (mutation to other types)

            private MeltProbability(ItemValueLevel level, int probUp, int probDown, int probSame, int probDestroy, int probMutation)
            {
                Level = level;
                ProbabilityLevelUp = probUp;
                ProbabilityLevelDown = probDown;
                ProbabilitySameLevel = probSame;
                ProbabilityDestroy = probDestroy;
                ProbabilityMutation = probMutation;
            }

            // ===========================================
            // 熔炼概率设置 (千分之几 = 百分比)
            // ===========================================
            // | 品质   | 升级概率 | 降级概率 | 保持概率 | 损毁概率 |
            // |--------|----------|----------|----------|----------|
            // | 白色   | 800(80%) | 0(0%)    | 150(15%) | 50(5%)   |
            // | 绿色   | 600(60%) | 50(5%)   | 300(30%) | 50(5%)   |
            // | 蓝色   | 550(55%) | 50(5%)   | 350(35%) | 50(5%)   |
            // | 紫色   | 400(40%) | 200(20%) | 350(35%) | 50(5%)   |
            // | 橙色   | 350(35%) | 250(25%) | 300(30%) | 100(10%) |
            // | 浅红色 | 300(30%) | 300(30%) | 300(25%) | 100(10%) |
            // | 红色   | 0(0%)    | 250(35%) | 650(60%) | 100(10%) |
            // ===========================================

            // Get melt probabilities for a given ItemValueLevel
            public static MeltProbability GetMeltProbabilityForLevel(ItemValueLevel level)
            {
                // The higher the level, the higher the chance to go down, the lower the chance to go up, etc.
                return level switch
                {
                    ItemValueLevel.White => new MeltProbability(level, 800, 0, 150, 50, 300),
                    ItemValueLevel.Green => new MeltProbability(level, 600, 50, 300, 50, 300),
                    ItemValueLevel.Blue => new MeltProbability(level, 550, 50, 350, 50, 300),
                    ItemValueLevel.Purple => new MeltProbability(level, 400, 200, 350, 50, 300),
                    ItemValueLevel.Orange => new MeltProbability(level, 350, 250, 300, 100, 300),
                    ItemValueLevel.LightRed => new MeltProbability(level, 300, 300, 300, 100, 300),
                    ItemValueLevel.Red => new MeltProbability(level, 0, 250, 650, 100, 300),
                    _ => new MeltProbability(level, 0, 0, 1000, 0, 300)
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