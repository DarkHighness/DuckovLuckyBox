using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace DuckovLuckyBox.Core.Settings
{
    [Serializable]
    public class ConfigData
    {
        public bool EnableAnimation = DefaultSettings.EnableAnimation;
        public string SettingsHotkey = DefaultSettings.SettingsHotkey.ToString();
        public bool EnableDestroyButton = DefaultSettings.EnableDestroyButton;
        public bool EnableLotteryButton = DefaultSettings.EnableLotteryButton;
        public bool EnableDebug = DefaultSettings.EnableDebug;
        public bool EnableUseToCreateItemPatch = DefaultSettings.EnableUseToCreateItemPatch;
        public bool EnableWeightedLottery = DefaultSettings.EnableWeightedLottery;
        public bool EnableHighQualitySound = DefaultSettings.EnableHighQualitySound;
        public string HighQualitySoundFilePath = DefaultSettings.HighQualitySoundFilePath;
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

        public void Initialize(Action? callback = null)
        {
            // Load existing config or create default (async)
            coroutineHost?.StartCoroutine(LoadConfigAsync(callback));

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

        private IEnumerator LoadConfigAsync(Action? callback = null)
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
            callback?.Invoke();

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
            var settings = SettingManager.Instance;
            return new ConfigData
            {
                EnableAnimation = settings.EnableAnimation.GetAsBool(),
                SettingsHotkey = settings.SettingsHotkey.GetAsHotkey().ToString(),
                EnableDestroyButton = settings.EnableDestroyButton.GetAsBool(),
                EnableLotteryButton = settings.EnableLotteryButton.GetAsBool(),
                EnableDebug = settings.EnableDebug.GetAsBool(),
                EnableUseToCreateItemPatch = settings.EnableUseToCreateItemPatch.GetAsBool(),
                EnableWeightedLottery = settings.EnableWeightedLottery.GetAsBool(),
                EnableHighQualitySound = settings.EnableHighQualitySound.GetAsBool(),
                HighQualitySoundFilePath = settings.HighQualitySoundFilePath.GetAsString(),
                RefreshStockPrice = settings.RefreshStockPrice.GetAsLong(),
                StorePickPrice = settings.StorePickPrice.GetAsLong(),
                StreetPickPrice = settings.StreetPickPrice.GetAsLong(),
            };
        }

        private void ApplyConfigToSettings(ConfigData config)
        {
            var settings = SettingManager.Instance;

            // Temporarily unsubscribe to avoid triggering saves during load
            UnsubscribeFromSettingChanges();

            try
            {
                settings.EnableAnimation.Value = config.EnableAnimation;

                // Parse Hotkey from string (e.g., "Ctrl+F1")
                settings.SettingsHotkey.Value = Hotkey.Parse(config.SettingsHotkey);

                settings.EnableDestroyButton.Value = config.EnableDestroyButton;
                settings.EnableLotteryButton.Value = config.EnableLotteryButton;
                settings.EnableDebug.Value = config.EnableDebug;
                settings.EnableUseToCreateItemPatch.Value = config.EnableUseToCreateItemPatch;
                settings.EnableWeightedLottery.Value = config.EnableWeightedLottery;
                settings.EnableHighQualitySound.Value = config.EnableHighQualitySound;
                settings.HighQualitySoundFilePath.Value = config.HighQualitySoundFilePath ?? DefaultSettings.HighQualitySoundFilePath;
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
            var settings = SettingManager.Instance;
            settings.EnableAnimation.OnValueChanged += OnSettingChanged;
            settings.SettingsHotkey.OnValueChanged += OnSettingChanged;
            settings.EnableDestroyButton.OnValueChanged += OnSettingChanged;
            settings.EnableLotteryButton.OnValueChanged += OnSettingChanged;
            settings.EnableDebug.OnValueChanged += OnSettingChanged;
            settings.EnableUseToCreateItemPatch.OnValueChanged += OnSettingChanged;
            settings.EnableWeightedLottery.OnValueChanged += OnSettingChanged;
            settings.EnableHighQualitySound.OnValueChanged += OnSettingChanged;
            settings.HighQualitySoundFilePath.OnValueChanged += OnSettingChanged;
            settings.RefreshStockPrice.OnValueChanged += OnSettingChanged;
            settings.StorePickPrice.OnValueChanged += OnSettingChanged;
            settings.StreetPickPrice.OnValueChanged += OnSettingChanged;
        }

        private void UnsubscribeFromSettingChanges()
        {
            var settings = SettingManager.Instance;
#pragma warning disable CS8601
            settings.EnableAnimation.OnValueChanged -= OnSettingChanged!;
            settings.SettingsHotkey.OnValueChanged -= OnSettingChanged!;
            settings.EnableDestroyButton.OnValueChanged -= OnSettingChanged!;
            settings.EnableLotteryButton.OnValueChanged -= OnSettingChanged!;
            settings.EnableDebug.OnValueChanged -= OnSettingChanged!;
            settings.EnableUseToCreateItemPatch.OnValueChanged -= OnSettingChanged!;
            settings.EnableWeightedLottery.OnValueChanged -= OnSettingChanged!;
            settings.EnableHighQualitySound.OnValueChanged -= OnSettingChanged!;
            settings.HighQualitySoundFilePath.OnValueChanged -= OnSettingChanged!;
            settings.RefreshStockPrice.OnValueChanged -= OnSettingChanged!;
            settings.StorePickPrice.OnValueChanged -= OnSettingChanged!;
            settings.StreetPickPrice.OnValueChanged -= OnSettingChanged!;
#pragma warning restore CS8601
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
