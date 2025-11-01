using System;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using SodaCraft.Localizations;
using Duckov.UI;
using Duckov.Economy;
using DuckovLuckyBox.Core.Settings;
using FMODUnity;
using FMOD;
using Duckov;
using System.Linq;
using System.Collections.Generic;

namespace DuckovLuckyBox.Core
{
    /// <summary>
    /// Service for managing item melting operations
    /// Handles probabilistic level changes (upgrade, downgrade, or maintain) based on quality
    /// </summary>
    public static class MeltService
    {
        private const float MELT_OPERATION_DELAY = 0.8f; // Delay between melt operations
        private const string SFX_BUY = "UI/buy";

        // Negative result weights
        private const int SAME_LEVEL_WEIGHT = 1;
        private const int LEVEL_DOWN_WEIGHT = 3;
        private const int DESTROYED_WEIGHT = 6;

        // Probability adjustment threshold
        private const int NEGATIVE_WEIGHT_THRESHOLD = 10;

        // Probability adjustment multipliers
        private const int SAME_LEVEL_BONUS_MULTIPLIER = 5;
        private const int LEVEL_UP_BONUS_MULTIPLIER = 3;

        // Cumulative negative weight: SameLevel=1, LevelDown=3, Destroyed=6
        private static int cumulativeNegativeWeight = 0;

        /// <summary>
        /// Represents the result of a melt operation
        /// </summary>
        public struct MeltResult
        {
            public bool Success { get; set; }
            public int MeltCount { get; set; } // How many times melt was performed (based on StackCount)
            public int LevelUpCount { get; set; } // Number of times item was upgraded
            public int LevelDownCount { get; set; } // Number of times item was downgraded
            public int SameLevelCount { get; set; } // Number of times item level stayed the same
            public int DestroyCount { get; set; } // Number of times item was completely destroyed
            public int MutatedCount { get; set; } // Number of times item was mutated (if applicable)

            public override string ToString() =>
                $"Success={Success}, MeltCount={MeltCount}, LevelUp={LevelUpCount}, LevelDown={LevelDownCount}, SameLevel={SameLevelCount}, Destroy={DestroyCount}, Mutated={MutatedCount}";
        }

        enum MeltLevelOut
        {
            LevelUp,
            LevelDown,
            SameLevel,
            Destroyed
        }

        private static bool DetermineMutation(ItemValueLevel currentLevel)
        {
            int randomValue = UnityEngine.Random.Range(0, 1000); // 0-999 (thousandths)
            var probability = ProbabilityUtils.MeltProbability.GetMeltProbabilityForLevel(currentLevel);

            return randomValue < probability.ProbabilityMutation;
        }

        /// <summary>
        /// Determines the quality level change for a melt operation
        /// </summary>
        private static MeltLevelOut DetermineLevelChange(ItemValueLevel currentLevel, int cumulativeWeight)
        {
            var meltProb = ProbabilityUtils.MeltProbability.GetMeltProbabilityForLevel(currentLevel);

            // If cumulative weight <= 10, use original probabilities
            if (cumulativeWeight <= NEGATIVE_WEIGHT_THRESHOLD)
            {
                int originalRandomValue = UnityEngine.Random.Range(0, 1000); // 0-999 (thousandths)

                // Check level up
                if (originalRandomValue < meltProb.ProbabilityLevelUp)
                {
                    return MeltLevelOut.LevelUp;
                }

                // Check level down
                if (originalRandomValue < meltProb.ProbabilityLevelUp + meltProb.ProbabilityLevelDown)
                {
                    return MeltLevelOut.LevelDown;
                }

                // Check level to remain the same
                if (originalRandomValue < meltProb.ProbabilityLevelUp + meltProb.ProbabilityLevelDown + meltProb.ProbabilitySameLevel)
                {
                    return MeltLevelOut.SameLevel;
                }

                // Destroyed
                return MeltLevelOut.Destroyed;
            }

            // Complex probability adjustment when cumulative weight > 10
            int excessWeight = cumulativeWeight - NEGATIVE_WEIGHT_THRESHOLD;
            int sameLevelBonus = excessWeight * SAME_LEVEL_BONUS_MULTIPLIER; // Increase SameLevel by 5 per excess weight
            int levelUpBonus = excessWeight * LEVEL_UP_BONUS_MULTIPLIER; // Increase LevelUp by 3 per excess weight

            // Calculate adjusted probabilities with caps
            int adjustedLevelUp = Math.Min(meltProb.ProbabilityLevelUp + levelUpBonus, 1000);
            int adjustedSameLevel = Math.Min(meltProb.ProbabilitySameLevel + sameLevelBonus, 1000);
            int adjustedLevelDown = Math.Max(meltProb.ProbabilityLevelDown - (levelUpBonus + sameLevelBonus) / 4, 0);
            int adjustedDestroyed = Math.Max(1000 - (meltProb.ProbabilityLevelUp + meltProb.ProbabilityLevelDown + meltProb.ProbabilitySameLevel) - (levelUpBonus + sameLevelBonus) / 4, 0);

            // Ensure total probability doesn't exceed 1000
            int totalAdjusted = adjustedLevelUp + adjustedLevelDown + adjustedSameLevel + adjustedDestroyed;
            if (totalAdjusted > 1000)
            {
                int excess = totalAdjusted - 1000;
                // Reduce from Destroyed first, then LevelDown
                adjustedDestroyed = Math.Max(adjustedDestroyed - excess, 0);
                if (adjustedDestroyed == 0 && excess > 0)
                {
                    adjustedLevelDown = Math.Max(adjustedLevelDown - excess, 0);
                }
            }

            int randomValue = UnityEngine.Random.Range(0, 1000); // 0-999 (thousandths)

            // Check level up
            if (randomValue < adjustedLevelUp)
            {
                return MeltLevelOut.LevelUp;
            }

            // Check level down
            if (randomValue < adjustedLevelUp + adjustedLevelDown)
            {
                return MeltLevelOut.LevelDown;
            }

            // Check level to remain the same
            if (randomValue < adjustedLevelUp + adjustedLevelDown + adjustedSameLevel)
            {
                return MeltLevelOut.SameLevel;
            }

            // Destroyed
            return MeltLevelOut.Destroyed;
        }

        /// <summary>
        /// Attempts to get an item at a specific level in the same category
        /// Returns null if no such item exists
        /// </summary>
        private static async UniTask<Item?> GetItemAtLevelInCategoryAsync(IEnumerable<string> categories, ItemValueLevel targetLevel)
        {
            var item = await RecycleService.PickRandomItemByCategoriesAndQualityAsync(
                categories,
                targetLevel
            );
            return item;
        }

        /// <summary>
        /// Performs a single melt operation on an item
        /// Returns a tuple of (result item, level change direction: 1=up, -1=down, 0=same)
        /// </summary>
        private static async UniTask<(Item?, MeltLevelOut, bool)> MeltSingleItemAsync(Item item)
        {
            if (item == null)
            {
                return (null, MeltLevelOut.SameLevel, false);
            }

            ItemValueLevel currentLevel = QualityUtils.GetCachedItemValueLevel(item);
            string category = RecycleService.GetItemCategory(item.TypeID);

            bool isMutated = DetermineMutation(currentLevel);
            var mutatedCategories = ItemUtils.RecyclableCategories.Where(cat => cat != category).ToList();
            var targetCategories = isMutated ? mutatedCategories : new List<string> { category };

            MeltLevelOut meltOutcome = DetermineLevelChange(currentLevel, cumulativeNegativeWeight);

            if (meltOutcome == MeltLevelOut.SameLevel)
            {
                // Same level - return a random item from the same level
                Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> Same level");
                var sameItem = await GetItemAtLevelInCategoryAsync(targetCategories, currentLevel);
                return (sameItem, MeltLevelOut.SameLevel, isMutated);
            }

            if (meltOutcome == MeltLevelOut.Destroyed)
            {
                // Item destroyed
                Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> Destroyed");
                return (null, MeltLevelOut.Destroyed, false);
            }

            int levelChange = meltOutcome == MeltLevelOut.LevelUp ? 1 : -1;
            int nextLevelValue = (int)currentLevel + levelChange;

            // Check if target level is valid
            // If nextLevelValue is less than 0, return destroyed
            // If nextLevelValue is greater than max level, return same level
            if (nextLevelValue < 0)
            {
                Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> Out of range (below 0), destroyed");
                return (null, MeltLevelOut.Destroyed, false);
            }

            if (nextLevelValue > 6)
            {
                // Level is out of range - return same level
                Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> Out of range, keep same level");
                var sameItem = await GetItemAtLevelInCategoryAsync(targetCategories, currentLevel);
                return (sameItem, MeltLevelOut.SameLevel, isMutated);
            }

            ItemValueLevel targetLevel = (ItemValueLevel)nextLevelValue;

            // Check if target item exists in category
            if (!RecycleService.HasCategoryItemAtLevel(new[] { category }, targetLevel))
            {
                // Target item doesn't exist - return same level
                Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> No item at level {targetLevel}, keep same level");
                var sameItem = await GetItemAtLevelInCategoryAsync(targetCategories, currentLevel);
                return (sameItem, MeltLevelOut.SameLevel, isMutated);
            }

            // Get random item at target level
            var resultItem = await GetItemAtLevelInCategoryAsync(targetCategories, targetLevel);
            string direction = meltOutcome > 0 ? "Up" : "Down";
            Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> {direction} to {targetLevel}");

            return (resultItem, meltOutcome, isMutated);
        }

        /// <summary>
        /// Performs melt operation on an item with StackCount handling
        /// Each stack is melted individually, and the result is sent to player inventory immediately
        /// The item's StackCount is decremented after each melt
        /// Records statistics for level ups, level downs, same level, and destroy outcomes
        /// </summary>
        public static async UniTask<MeltResult> MeltItemAsync(Item item)
        {
            var result = new MeltResult
            {
                Success = false,
                MeltCount = 0,
                LevelUpCount = 0,
                LevelDownCount = 0,
                SameLevelCount = 0,
                DestroyCount = 0
            };

            if (item == null)
            {
                Log.Error("Melt: Cannot melt null item");
                return result;
            }

            if (item.StackCount <= 0)
            {
                Log.Warning($"Melt: Item has invalid stack count ({item.StackCount}) ");
                return result;
            }

            // Special handling for bullets - leave empty for now
            string category = RecycleService.GetItemCategory(item.TypeID);

            // Special handling for bullets - leave empty for now
            if (category == "Bullet")
            {
                Log.Debug($"Melt: Bullet items can not be melted - {item.DisplayName}");
                return result;
            }

            string displayName = item.DisplayName.ToPlainText();
            int stackCount = item.Stackable ? item.StackCount : 1;
            result.MeltCount = stackCount;
            Log.Debug($"Melt: Starting melt on {displayName} (Stack: {stackCount})");

            // Calculate melt cost per stack
            ItemValueLevel currentLevel = QualityUtils.GetCachedItemValueLevel(item);
            int levelValue = (int)currentLevel + 1; // White=1, Green=2, Blue=3, Purple=4, Orange=5, Red=6, LightRed=7
            long meltBasePrice = SettingManager.Instance.MeltBasePrice.GetAsLong();
            long costPerStack = meltBasePrice * levelValue;
            long totalCost = costPerStack * stackCount;

            // Check if player has enough money by attempting to pay
            if (totalCost > 0)
            {
                if (!EconomyManager.Pay(new Cost(totalCost), true, true))
                {
                    var notEnoughMoneyMessage = Localizations.I18n.NotEnoughMoneyFormatKey.ToPlainText().Replace("{price}", totalCost.ToString());
                    NotificationText.Push(notEnoughMoneyMessage);
                    Log.Error($"Melt: Failed to charge player for melt operation. Cost: {totalCost}");
                    return result;
                }
                Log.Debug($"Melt: Charged player {totalCost} for melt operation ({costPerStack} per stack x {stackCount} stacks)");

                // Show cost breakdown notification
                var costMessage = Localizations.I18n.MeltCostFormatKey.ToPlainText()
                  .Replace("{basePrice}", meltBasePrice.ToString())
                  .Replace("{level}", levelValue.ToString())
                  .Replace("{count}", stackCount.ToString())
                  .Replace("{totalCost}", totalCost.ToString());
                NotificationText.Push(costMessage);
            }

            // Melt each stack individually
            RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup sfxGroup);

            // Play the buy sound if totalCost > 0
            if (totalCost > 0)
            {
                AudioManager.Post(SFX_BUY);
            }

            void stopAndPlay(Sound? sound)
            {
                var fmodResult = sfxGroup.stop();
                if (fmodResult != RESULT.OK)
                {
                    Log.Warning($"Melt: Failed to stop SFX group before playing sound. FMOD Result: {fmodResult}");
                }

                if (sound == null)
                {
                    Log.Warning("Melt: stopAndPlay called with null sound");
                    return;
                }

                SoundUtils.PlaySound(sound, sfxGroup);
            }

            for (int i = 0; i < stackCount; i++)
            {
                var (meltedItem, meltOutcome, isMutated) = await MeltSingleItemAsync(item);

                // Decrement the original item's stack count
                if (item.Stackable)
                {
                    item.StackCount--;
                }

                string message = string.Empty;
                // Check if item was destroyed
                if (meltOutcome == MeltLevelOut.Destroyed)
                {
                    result.DestroyCount++;
                    stopAndPlay(Constants.Sound.MELT_DESTROY_SOUND);
                    message = Localizations.I18n.MeltDestroyedNotificationKey.ToPlainText()
                      .Replace("{originalItem}", item.DisplayName.ToPlainText());
                    NotificationText.Push(message);
                    Log.Debug($"Melt: Item destroyed (iteration {i + 1}/{stackCount})");
                    continue;
                }

                if (meltedItem == null)
                {
                    Log.Warning($"Melt: Failed to melt item at iteration {i + 1}/{stackCount}");
                    continue;
                }

                var meltedItemDisplayName = meltedItem.DisplayName.ToPlainText();
                if (meltedItem.Stackable)
                {
                    meltedItem.StackCount = 1; // Each melt produces one item
                }

                // Record level change statistics and play sounds
                if (meltOutcome == MeltLevelOut.LevelUp)
                {
                    result.LevelUpCount++;
                    stopAndPlay(Constants.Sound.MELT_LEVEL_UP_SOUND);
                    // Show notification for level up
                    message = Localizations.I18n.MeltLevelUpNotificationKey.ToPlainText()
                     .Replace("{originalItem}", item.DisplayName.ToPlainText())
                     .Replace("{newItem}", meltedItemDisplayName);
                }
                else if (meltOutcome == MeltLevelOut.LevelDown)
                {
                    result.LevelDownCount++;
                    stopAndPlay(Constants.Sound.MELT_LEVEL_DOWN_SOUND);
                    // Show notification for level down
                    message = Localizations.I18n.MeltLevelDownNotificationKey.ToPlainText()
                       .Replace("{originalItem}", item.DisplayName.ToPlainText())
                       .Replace("{newItem}", meltedItemDisplayName);
                }
                else if (meltOutcome == MeltLevelOut.SameLevel)
                {
                    result.SameLevelCount++;
                    stopAndPlay(Constants.Sound.MELT_LEVEL_SAME_SOUND);
                    // Show notification for same level
                    if (meltedItem.TypeID == item.TypeID)
                    {
                        // Same item - use special notification
                        message = Localizations.I18n.MeltSameItemNotificationKey.ToPlainText()
                          .Replace("{originalItem}", item.DisplayName.ToPlainText());

                    }
                    else
                    {
                        // Different item at same level
                        message = Localizations.I18n.MeltLevelSameNotificationKey.ToPlainText()
                         .Replace("{originalItem}", item.DisplayName.ToPlainText())
                         .Replace("{newItem}", meltedItemDisplayName);
                    }
                }

                if (isMutated)
                {
                    result.MutatedCount++;
                    message += " " + Localizations.I18n.MeltMutatedNotificationKey.ToPlainText()
                      .Replace("{newItem}", meltedItemDisplayName);
                }

                NotificationText.Push(message);

                // Send melted item to player inventory immediately
                var sentToStorage =
                !ItemUtilities.SendToPlayerCharacterInventory(meltedItem, dontMerge: false);
                if (sentToStorage)
                {
                    var storageMessage = Localizations.I18n.InventoryFullAndSendToStorageKey.ToPlainText();
                    NotificationText.Push(storageMessage);
                    ItemUtilities.SendToPlayerStorage(meltedItem, directToBuffer: false);
                }
                Log.Debug($"Melt: Sent melted item to player (iteration {i + 1}/{stackCount})");

                await UniTask.WaitForSeconds(MELT_OPERATION_DELAY); // Small delay to avoid overwhelming the inventory system

                // Update cumulative negative weight
                if (meltOutcome == MeltLevelOut.LevelDown || meltOutcome == MeltLevelOut.SameLevel || meltOutcome == MeltLevelOut.Destroyed)
                {
                    int weight = meltOutcome == MeltLevelOut.SameLevel ? SAME_LEVEL_WEIGHT : meltOutcome == MeltLevelOut.LevelDown ? LEVEL_DOWN_WEIGHT : DESTROYED_WEIGHT;
                    cumulativeNegativeWeight += weight;
                }
                else
                {
                    cumulativeNegativeWeight = 0;
                }
            }

            result.Success = true;

            if (result.MeltCount > 1)
            {
                // Show final summary notification
                string summaryMessage = Localizations.I18n.MeltResultFormatKey.ToPlainText()
                  .Replace("{meltCount}", result.MeltCount.ToString())
                  .Replace("{levelUpCount}", result.LevelUpCount.ToString())
                  .Replace("{levelDownCount}", result.LevelDownCount.ToString())
                  .Replace("{sameLevelCount}", result.SameLevelCount.ToString())
                  .Replace("{destroyedCount}", result.DestroyCount.ToString());
                NotificationText.Push(summaryMessage);
            }

            Log.Debug($"Melt: Completed - {displayName} (Total: {result.MeltCount}, Up: {result.LevelUpCount}, Down: {result.LevelDownCount}, Same: {result.SameLevelCount}, Destroy: {result.DestroyCount})");

            // Destroy the target item immediately
            // We periodically decrement the stack count of the original item, so we have no need to destroy it here
            // UnityEngine.Object.Destroy(item.gameObject);

            return result;
        }

        /// <summary>
        /// Check whether an item can be melted (can attempt an upgrade)
        /// Conditions:
        /// 1. Its upgrade probability (level up) is not zero
        /// 2. There exists at least one item of level + 1 in the same category
        /// 3. The item is not a Bullet and not Cash (TypeID 451)
        /// </summary>
        public static bool CanMeltItem(Item? item)
        {
            if (item == null) return false;

            // Exclude cash
            if (item.TypeID == 451) return false;

            string category = RecycleService.GetItemCategory(item.TypeID);
            if (category == "Bullet")
            {
                return false;
            }

            ItemValueLevel currentLevel = QualityUtils.GetCachedItemValueLevel(item);
            var meltProb = ProbabilityUtils.MeltProbability.GetMeltProbabilityForLevel(currentLevel);

            // Upgrade probability must be > 0
            if (meltProb.ProbabilityLevelUp <= 0) return false;

            int nextLevelValue = (int)currentLevel + 1;
            if (!Enum.IsDefined(typeof(ItemValueLevel), nextLevelValue)) return false;
            var targetLevel = (ItemValueLevel)nextLevelValue;

            // At least one item of next level
            bool hasNext = RecycleService.HasCategoryItemAtLevel(ItemUtils.RecyclableCategories, targetLevel);
            return hasNext;
        }
    }
}