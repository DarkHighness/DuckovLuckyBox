using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DuckovLuckyBox.Core.Settings
{
  [System.Serializable]
  public class ConfigData
  {
    public bool EnableAnimation = DefaultSettings.EnableAnimation;
    public string SettingsHotkey = DefaultSettings.SettingsHotkey.ToString();
    public long RefreshStockPrice = DefaultSettings.RefreshStockPrice;
    public long StorePickPrice = DefaultSettings.StorePickPrice;
    public long StreetPickPrice = DefaultSettings.StreetPickPrice;
  }

  public class ConfigManager
  {
    private readonly string configDirectory;
    private readonly string configFilePath;
    private FileSystemWatcher? fileWatcher;
    private DateTime lastWriteTime = DateTime.MinValue;
    private bool isLoading = false;
    private bool isSaving = false;
    private MonoBehaviour? coroutineHost;

    public ConfigManager(MonoBehaviour host)
    {
      coroutineHost = host;
      // Use Application.persistentDataPath for cross-platform support
      configDirectory = Path.Combine(Application.persistentDataPath, Constants.ModId);
      configFilePath = Path.Combine(configDirectory, "config.json");

      // Ensure directory exists
      if (!Directory.Exists(configDirectory))
      {
        Directory.CreateDirectory(configDirectory);
        Log.Debug($"Created config directory: {configDirectory}");
      }
    }

    public void Initialize()
    {
      // Load existing config or create default (async)
      coroutineHost?.StartCoroutine(LoadConfigAsync());

      // Start file watcher
      StartFileWatcher();

      Log.Debug($"ConfigManager initialized. Config path: {configFilePath}");
    }

    public void Cleanup()
    {
      StopFileWatcher();
      UnsubscribeFromSettingChanges();
      coroutineHost = null;
      Log.Debug("ConfigManager cleaned up.");
    }

    private IEnumerator LoadConfigAsync()
    {
      if (isLoading) yield break;
      isLoading = true;

      Log.Debug("Starting async config load...");

      // Yield to avoid blocking the main thread
      yield return null;

      bool success = false;
      string? json = null;
      ConfigData? config = null;

      // File reading
      if (File.Exists(configFilePath))
      {
        try
        {
          json = File.ReadAllText(configFilePath);
          success = true;
        }
        catch (Exception ex)
        {
          Log.Error($"Failed to read config file: {ex.Message}");
        }

        // Yield after file read
        yield return null;

        // JSON parsing
        if (success && json != null)
        {
          try
          {
            config = JsonUtility.FromJson<ConfigData>(json);
            if (config != null)
            {
              ApplyConfigToSettings(config);
              Log.Debug("Config loaded successfully (async).");
            }
          }
          catch (Exception ex)
          {
            Log.Error($"Failed to parse config: {ex.Message}");
          }
        }
      }
      else
      {
        // Create default config
        yield return coroutineHost?.StartCoroutine(SaveConfigAsync());
        Log.Debug("Created default config file (async).");
      }

      isLoading = false;

      // Subscribe to setting changes after initial load
      SubscribeToSettingChanges();
    }

    private IEnumerator SaveConfigAsync()
    {
      if (isSaving) yield break;
      isSaving = true;

      Log.Debug("Starting async config save...");

      // Yield to avoid blocking the main thread
      yield return null;

      ConfigData? config = null;
      string? json = null;
      bool configCreated = false;
      bool jsonCreated = false;

      // Create config object
      try
      {
        config = CreateConfigFromSettings();
        configCreated = true;
      }
      catch (Exception ex)
      {
        Log.Error($"Failed to create config object: {ex.Message}");
      }

      // Yield after creating config object
      yield return null;

      // JSON serialization
      if (configCreated && config != null)
      {
        try
        {
          json = JsonUtility.ToJson(config, true);
          jsonCreated = true;
        }
        catch (Exception ex)
        {
          Log.Error($"Failed to serialize config: {ex.Message}");
        }

        // Yield after JSON serialization
        yield return null;

        // File writing
        if (jsonCreated && json != null)
        {
          try
          {
            File.WriteAllText(configFilePath, json);
            lastWriteTime = File.GetLastWriteTime(configFilePath);
            Log.Debug("Config saved successfully (async).");
          }
          catch (Exception ex)
          {
            Log.Error($"Failed to write config file: {ex.Message}");
          }
        }
      }

      isSaving = false;
    }

    private ConfigData CreateConfigFromSettings()
    {
      var settings = Settings.Instance;
      return new ConfigData
      {
        EnableAnimation = settings.EnableAnimation.Value is bool b ? b : DefaultSettings.EnableAnimation,
        SettingsHotkey = (settings.SettingsHotkey.Value is KeyCode keyCode ? keyCode : DefaultSettings.SettingsHotkey).ToString(),
        RefreshStockPrice = settings.RefreshStockPrice.Value is long l1 ? l1 : DefaultSettings.RefreshStockPrice,
        StorePickPrice = settings.StorePickPrice.Value is long l2 ? l2 : DefaultSettings.StorePickPrice,
        StreetPickPrice = settings.StreetPickPrice.Value is long l3 ? l3 : DefaultSettings.StreetPickPrice,
      };
    }

    private void ApplyConfigToSettings(ConfigData config)
    {
      var settings = Settings.Instance;

      // Temporarily unsubscribe to avoid triggering saves during load
      UnsubscribeFromSettingChanges();

      try
      {
        settings.EnableAnimation.Value = config.EnableAnimation;

        // Parse KeyCode from string
        if (Enum.TryParse<KeyCode>(config.SettingsHotkey, out KeyCode keyCode))
        {
          settings.SettingsHotkey.Value = keyCode;
        }

        settings.RefreshStockPrice.Value = config.RefreshStockPrice;
        settings.StorePickPrice.Value = config.StorePickPrice;
        settings.StreetPickPrice.Value = config.StreetPickPrice;
      }
      finally
      {
        // Resubscribe after loading
        SubscribeToSettingChanges();
      }
    }

    private void SubscribeToSettingChanges()
    {
      var settings = Settings.Instance;
      settings.EnableAnimation.OnValueChanged += OnSettingChanged;
      settings.SettingsHotkey.OnValueChanged += OnSettingChanged;
      settings.RefreshStockPrice.OnValueChanged += OnSettingChanged;
      settings.StorePickPrice.OnValueChanged += OnSettingChanged;
      settings.StreetPickPrice.OnValueChanged += OnSettingChanged;
    }

    private void UnsubscribeFromSettingChanges()
    {
      var settings = Settings.Instance;
      settings.EnableAnimation.OnValueChanged -= OnSettingChanged;
      settings.SettingsHotkey.OnValueChanged -= OnSettingChanged;
      settings.RefreshStockPrice.OnValueChanged -= OnSettingChanged;
      settings.StorePickPrice.OnValueChanged -= OnSettingChanged;
      settings.StreetPickPrice.OnValueChanged -= OnSettingChanged;
    }

    private void OnSettingChanged(object value)
    {
      // Don't save during loading to avoid recursion
      if (!isLoading && coroutineHost != null)
      {
        coroutineHost.StartCoroutine(SaveConfigAsync());
      }
    }

    private void StartFileWatcher()
    {
      try
      {
        fileWatcher = new FileSystemWatcher(configDirectory)
        {
          Filter = "config.json",
          NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
          EnableRaisingEvents = true
        };

        fileWatcher.Changed += OnConfigFileChanged;
        Log.Debug("File watcher started.");
      }
      catch (Exception ex)
      {
        Log.Error($"Failed to start file watcher: {ex.Message}");
      }
    }

    private void StopFileWatcher()
    {
      if (fileWatcher != null)
      {
        fileWatcher.Changed -= OnConfigFileChanged;
        fileWatcher.EnableRaisingEvents = false;
        fileWatcher.Dispose();
        fileWatcher = null;
        Log.Debug("File watcher stopped.");
      }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
      try
      {
        // Debounce: only reload if file was actually modified (not just our own save)
        DateTime currentWriteTime = File.GetLastWriteTime(configFilePath);
        if (currentWriteTime > lastWriteTime.AddSeconds(1) && !isSaving && coroutineHost != null)
        {
          lastWriteTime = currentWriteTime;
          Log.Debug("Config file changed externally, reloading...");
          coroutineHost.StartCoroutine(LoadConfigAsync());
        }
      }
      catch (Exception ex)
      {
        Log.Error($"Error handling config file change: {ex.Message}");
      }
    }
  }
}
