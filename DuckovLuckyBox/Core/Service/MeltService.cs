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

      public override string ToString() =>
          $"Success={Success}, MeltCount={MeltCount}, LevelUp={LevelUpCount}, LevelDown={LevelDownCount}, SameLevel={SameLevelCount}, Destroy={DestroyCount}";
    }

    enum MeltOutcome
    {
      LevelUp,
      LevelDown,
      SameLevel,
      Destroyed
    }

    /// <summary>
    /// Determines the quality level change for a melt operation
    /// </summary>
    private static MeltOutcome DetermineLevelChange(ItemValueLevel currentLevel)
    {
      var meltProb = ProbabilityUtils.MeltProbability.GetMeltProbabilityForLevel(currentLevel);

      int randomValue = UnityEngine.Random.Range(0, 1000); // 0-999 (thousandths)

      // Check level up
      if (randomValue < meltProb.ProbabilityLevelUp)
      {
        return MeltOutcome.LevelUp;
      }

      // Check level down
      if (randomValue < meltProb.ProbabilityLevelUp + meltProb.ProbabilityLevelDown)
      {
        return MeltOutcome.LevelDown;
      }

      // Check level to remain the same
      if (randomValue < meltProb.ProbabilityLevelUp + meltProb.ProbabilityLevelDown + meltProb.ProbabilitySameLevel)
      {
        return MeltOutcome.SameLevel;
      }

      // Destroyed
      return MeltOutcome.Destroyed;
    }

    /// <summary>
    /// Attempts to get an item at a specific level in the same category
    /// Returns null if no such item exists
    /// </summary>
    private static async UniTask<Item?> GetItemAtLevelInCategoryAsync(string category, ItemValueLevel targetLevel)
    {
      var item = await RecycleService.PickRandomItemByCategoriesAndQualityAsync(
          new[] { category },
          targetLevel
      );
      return item;
    }

    /// <summary>
    /// Performs a single melt operation on an item
    /// Returns a tuple of (result item, level change direction: 1=up, -1=down, 0=same)
    /// </summary>
    private static async UniTask<(Item?, MeltOutcome)> MeltSingleItemAsync(Item item)
    {
      if (item == null)
      {
        return (null, MeltOutcome.SameLevel);
      }

      ItemValueLevel currentLevel = QualityUtils.GetCachedItemValueLevel(item);
      string category = RecycleService.GetItemCategory(item.TypeID);

      MeltOutcome meltOutcome = DetermineLevelChange(currentLevel);

      if (meltOutcome == MeltOutcome.SameLevel)
      {
        // Same level - return a random item from the same level
        Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> Same level");
        var sameItem = await GetItemAtLevelInCategoryAsync(category, currentLevel);
        return (sameItem, MeltOutcome.SameLevel);
      }

      if (meltOutcome == MeltOutcome.Destroyed)
      {
        // Item destroyed
        Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> Destroyed");
        return (null, MeltOutcome.Destroyed);
      }


      int levelChange = meltOutcome == MeltOutcome.LevelUp ? 1 : -1;
      int nextLevelValue = (int)currentLevel + levelChange;

      // Check if target level is valid
      // If nextLevelValue is less than 0, return destroyed
      // If nextLevelValue is greater than max level, return same level
      if (nextLevelValue < 0)
      {
        Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> Out of range (below 0), destroyed");
        return (null, MeltOutcome.Destroyed);
      }

      if (nextLevelValue > 6)
      {
        // Level is out of range - return same level
        Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> Out of range, keep same level");
        var sameItem = await GetItemAtLevelInCategoryAsync(category, currentLevel);
        return (sameItem, MeltOutcome.SameLevel);
      }

      ItemValueLevel targetLevel = (ItemValueLevel)nextLevelValue;

      // Check if target item exists in category
      if (!RecycleService.HasCategoryItemAtLevel(new[] { category }, targetLevel))
      {
        // Target item doesn't exist - return same level
        Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> No item at level {targetLevel}, keep same level");
        var sameItem = await GetItemAtLevelInCategoryAsync(category, currentLevel);
        return (sameItem, MeltOutcome.SameLevel);
      }

      // Get random item at target level
      var resultItem = await GetItemAtLevelInCategoryAsync(category, targetLevel);
      string direction = meltOutcome > 0 ? "Up" : "Down";
      Log.Debug($"Melt: {item.DisplayName} ({currentLevel}) -> {direction} to {targetLevel}");

      return (resultItem, meltOutcome);
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
        var (meltedItem, meltOutcome) = await MeltSingleItemAsync(item);

        // Decrement the original item's stack count
        if (item.Stackable)
        {
          item.StackCount--;
        }

        // Check if item was destroyed
        if (meltOutcome == MeltOutcome.Destroyed)
        {
          result.DestroyCount++;
          stopAndPlay(Constants.Sound.MELT_DESTROY_SOUND);
          var message = Localizations.I18n.MeltDestroyedNotificationKey.ToPlainText()
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
        if (meltOutcome == MeltOutcome.LevelUp)
        {
          result.LevelUpCount++;
          stopAndPlay(Constants.Sound.MELT_LEVEL_UP_SOUND);
          // Show notification for level up
          string message = Localizations.I18n.MeltLevelUpNotificationKey.ToPlainText()
            .Replace("{originalItem}", item.DisplayName.ToPlainText())
            .Replace("{newItem}", meltedItemDisplayName);
          NotificationText.Push(message);
        }
        else if (meltOutcome == MeltOutcome.LevelDown)
        {
          result.LevelDownCount++;
          stopAndPlay(Constants.Sound.MELT_LEVEL_DOWN_SOUND);
          // Show notification for level down
          string message = Localizations.I18n.MeltLevelDownNotificationKey.ToPlainText()
            .Replace("{originalItem}", item.DisplayName.ToPlainText())
            .Replace("{newItem}", meltedItemDisplayName);
          NotificationText.Push(message);
        }
        else if (meltOutcome == MeltOutcome.SameLevel)
        {
          result.SameLevelCount++;
          stopAndPlay(Constants.Sound.MELT_LEVEL_SAME_SOUND);
          // Show notification for same level
          if (meltedItem.TypeID == item.TypeID)
          {
            // Same item - use special notification
            string message = Localizations.I18n.MeltSameItemNotificationKey.ToPlainText()
              .Replace("{originalItem}", item.DisplayName.ToPlainText());
            NotificationText.Push(message);
          }
          else
          {
            // Different item at same level
            string message = Localizations.I18n.MeltLevelSameNotificationKey.ToPlainText()
              .Replace("{originalItem}", item.DisplayName.ToPlainText())
              .Replace("{newItem}", meltedItemDisplayName);
            NotificationText.Push(message);
          }
        }

        // Send melted item to player inventory immediately
        var sentToStorage =
        !ItemUtilities.SendToPlayerCharacterInventory(meltedItem, dontMerge: false);
        if (sentToStorage)
        {
          var storageMessage = Localizations.I18n.InventoryFullAndSendToStorageKey.ToPlainText();
          NotificationText.Push(storageMessage);
        }
        Log.Debug($"Melt: Sent melted item to player (iteration {i + 1}/{stackCount})");

        await UniTask.WaitForSeconds(MELT_OPERATION_DELAY); // Small delay to avoid overwhelming the inventory system
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