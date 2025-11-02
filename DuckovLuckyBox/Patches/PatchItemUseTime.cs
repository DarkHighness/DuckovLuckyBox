using System.Collections.Generic;
using DuckovLuckyBox.Core;
using HarmonyLib;
using ItemStatsSystem;

namespace DuckovLuckyBox.Patches
{
  public class PatchItemUseTime
  {
    public static PatchItemUseTime? _instance;
    public static PatchItemUseTime Instance
    {
      get
      {
        _instance ??= new PatchItemUseTime();
        return _instance;
      }
    }

    private bool _patched = false;
    private bool _initialized = false;
    private Dictionary<int, float> _originalUseTimes = new Dictionary<int, float>();

    private void Initialize()
    {
      _initialized = true;
      Core.Settings.SettingManager.Instance.EnablePatchUseTime.OnValueChanged += OnEnablePatchUseTimeChanged;
      Core.Settings.SettingManager.Instance.PatchedUseTime.OnValueChanged += OnPatchedUseTimeChanged;
    }

    private void OnEnablePatchUseTimeChanged(object value)
    {
      if ((bool)value)
      {
        Patch();
      }
      else
      {
        Unpatch();
      }
    }

    private void OnPatchedUseTimeChanged(object value)
    {
      if (Core.Settings.SettingManager.Instance.EnablePatchUseTime.GetAsBool())
      {
        var newUseTime = Core.Settings.SettingManager.Instance.PatchedUseTime.GetAsFloat();
        SetUseTimeForAllItems(newUseTime);
      }
    }

    private void SetUseTimeForAllItems(float useTime)
    {
      Log.Debug($"Setting item use time to: {useTime} for all applicable items.");

      var items = ItemUtils.FindAllUseToCreateItems();
      foreach (var item in items)
      {
        var usageUtilities = item.UsageUtilities;
        if (usageUtilities == null) continue;

        var useTimeField = AccessTools.Field(typeof(UsageUtilities), "useTime");
        if (useTimeField != null)
        {
          Log.Debug($"Setting item use time: {item.name} (original useTime: {usageUtilities.UseTime}, new useTime: {useTime})");
          useTimeField.SetValue(usageUtilities, useTime);
        }
        else
        {
          Log.Warning($"Failed to set item use time: {item.name} - useTime field not found.");
        }
      }
    }

    public bool Patch()
    {
      if (!_initialized)
      {
        Initialize();
      }

      if (_patched) return false;

      var items = ItemUtils.FindAllUseToCreateItems();
      foreach (var item in items)
      {
        var usageUtilities = item.UsageUtilities;
        if (usageUtilities == null) continue;

        var itemId = item.TypeID;
        var originalUseTime = usageUtilities.UseTime;
        _originalUseTimes[itemId] = originalUseTime;
      }

      var newUseTime = Core.Settings.SettingManager.Instance.PatchedUseTime.GetAsFloat();
      SetUseTimeForAllItems(newUseTime);

      _patched = true;
      return true;
    }

    public bool Unpatch()
    {
      if (!_initialized)
      {
        Initialize();
      }

      if (!_patched) return false;

      var items = ItemUtils.FindAllUseToCreateItems();
      foreach (var item in items)
      {
        var usageUtilities = item.UsageUtilities;
        if (usageUtilities == null) continue;

        var itemId = item.TypeID;
        if (_originalUseTimes.TryGetValue(itemId, out var originalUseTime))
        {
          var useTimeField = AccessTools.Field(typeof(UsageUtilities), "useTime");
          if (useTimeField != null)
          {
            Log.Debug($"Restoring item use time: {item.name} (restored useTime: {originalUseTime})");
            useTimeField.SetValue(usageUtilities, originalUseTime);
          }
          else
          {
            Log.Warning($"Failed to restore item use time: {item.name} - useTime field not found.");
          }
        }
      }

      _originalUseTimes.Clear();
      _patched = false;
      return true;
    }
  }
}