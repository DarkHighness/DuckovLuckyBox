using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace DuckovLuckyBox.Core.Settings
{
    [Serializable]
    public class ConfigData
    {
        // Config file version for future migration support
        // Default 0 means 'unspecified/old format' and will trigger migration
        public int Version = 0;
        public bool EnableAnimation = DefaultSettings.EnableAnimation;
        public bool EnableTripleLotteryAnimation = DefaultSettings.EnableTripleLotteryAnimation;
        public bool EnableDestroyButton = DefaultSettings.EnableDestroyButton;
        public bool EnableMeltButton = DefaultSettings.EnableMeltButton;
        public bool EnableDebug = DefaultSettings.EnableDebug;
        public bool EnableUseToCreateItemPatch = DefaultSettings.EnableUseToCreateItemPatch;
        public bool EnableWeightedLottery = DefaultSettings.EnableWeightedLottery;
        public bool EnableHighQualitySound = DefaultSettings.EnableHighQualitySound;
        public bool EnableStockShopActions = DefaultSettings.EnableStockShopActions;
        public string HighQualitySoundFilePath = DefaultSettings.HighQualitySoundFilePath;
        public long RefreshStockPrice = DefaultSettings.RefreshStockPrice;
        public long StorePickPrice = DefaultSettings.StorePickPrice;
        public long StreetPickPrice = DefaultSettings.StreetPickPrice;
        public long MeltBasePrice = DefaultSettings.MeltBasePrice;
    }


    public class ConfigManager
    {
        private const int CurrentConfigVersion = 3;

        // Create a timestamped backup of the current config file (if it exists).
        private void BackupConfigFile()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string backupPath = Path.Combine(configDirectory, $"config.json.{timestamp}.bak");
                    File.Copy(configFilePath, backupPath);
                    Log.Info($"Backed up old config to {backupPath}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to backup config file: {ex.Message}");
            }
        }

        // Migrate a loaded ConfigData up to CurrentConfigVersion. Returns migrated config.
        private ConfigData MigrateConfig(ConfigData config)
        {
            int ver = config.Version;
            Log.Info($"Starting migration: config version {ver} -> {CurrentConfigVersion}");

            try
            {
                // Sequential migrations: apply each version upgrade step
                while (ver < CurrentConfigVersion)
                {
                    switch (ver)
                    {
                        case 0:
                        case 1:
                        case 2:
                            // For v0 -> v1 migration we intentionally reset all settings to defaults.
                            Log.Info("Migrating config v0 -> v1/v2/v3: resetting all settings to defaults.");

                            var defaultConfig = new ConfigData
                            {
                                Version = CurrentConfigVersion
                            };

                            // Return early with default config to indicate migration result
                            return defaultConfig;
                        default:
                            // Unknown older version: set to current and stop
                            ver = CurrentConfigVersion;
                            config.Version = ver;
                            Log.Error($"Unknown config version encountered during migration; setting to {ver}.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error migrating config: {ex.Message}");
                // On failure, fall back to defaults
                var defaultConfig = new ConfigData
                {
                    Version = CurrentConfigVersion
                };
                return defaultConfig;
            }

            return config;
        }
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
                bool didMigrate = false;
                ConfigData? migratedConfig = null;

                if (success && json != null)
                {
                    try
                    {
                        config = JsonUtility.FromJson<ConfigData>(json);

                        if (config != null)
                        {
                            // If the loaded config is an older version, prepare migration
                            if (config.Version < CurrentConfigVersion)
                            {
                                Log.Info($"Config version {config.Version} detected; migrating to {CurrentConfigVersion}...");

                                // Backup original file before modifying
                                BackupConfigFile();

                                migratedConfig = MigrateConfig(config);

                                // Apply migrated values to settings so we can persist them later
                                ApplyConfigToSettings(migratedConfig);
                                didMigrate = true;
                            }
                            else
                            {
                                // Up-to-date config; apply to settings
                                ApplyConfigToSettings(config);
                                Log.Debug("Config loaded successfully (async).");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to parse config: {ex.Message}");
                    }
                }

                // If a migration was prepared, persist the migrated config outside the try/catch block
                if (didMigrate)
                {
                    if (coroutineHost != null)
                        yield return coroutineHost.StartCoroutine(SaveConfigAsync());
                    else
                        yield return null;

                    isLoading = false;
                    callback?.Invoke();
                    SubscribeToSettingChanges();
                    yield break;
                }
            }
            else
            {
                // Create default config
                if (coroutineHost != null)
                    yield return coroutineHost.StartCoroutine(SaveConfigAsync());
                else
                    yield return null;

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
                Version = CurrentConfigVersion,
                EnableAnimation = settings.EnableAnimation.GetAsBool(),
                EnableTripleLotteryAnimation = settings.EnableTripleLotteryAnimation.GetAsBool(),
                EnableDestroyButton = settings.EnableDestroyButton.GetAsBool(),
                EnableMeltButton = settings.EnableMeltButton.GetAsBool(),
                EnableDebug = settings.EnableDebug.GetAsBool(),
                EnableUseToCreateItemPatch = settings.EnableUseToCreateItemPatch.GetAsBool(),
                EnableWeightedLottery = settings.EnableWeightedLottery.GetAsBool(),
                EnableHighQualitySound = settings.EnableHighQualitySound.GetAsBool(),
                EnableStockShopActions = settings.EnableStockShopActions.GetAsBool(),
                HighQualitySoundFilePath = settings.HighQualitySoundFilePath.GetAsString(),
                RefreshStockPrice = settings.RefreshStockPrice.GetAsLong(),
                StorePickPrice = settings.StorePickPrice.GetAsLong(),
                StreetPickPrice = settings.StreetPickPrice.GetAsLong(),
                MeltBasePrice = settings.MeltBasePrice.GetAsLong(),
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
                settings.EnableTripleLotteryAnimation.Value = config.EnableTripleLotteryAnimation;
                settings.EnableDestroyButton.Value = config.EnableDestroyButton;
                settings.EnableMeltButton.Value = config.EnableMeltButton;
                settings.EnableDebug.Value = config.EnableDebug;
                settings.EnableUseToCreateItemPatch.Value = config.EnableUseToCreateItemPatch;
                settings.EnableWeightedLottery.Value = config.EnableWeightedLottery;
                settings.EnableHighQualitySound.Value = config.EnableHighQualitySound;
                settings.EnableStockShopActions.Value = config.EnableStockShopActions;
                settings.HighQualitySoundFilePath.Value = config.HighQualitySoundFilePath ?? DefaultSettings.HighQualitySoundFilePath;
                settings.RefreshStockPrice.Value = config.RefreshStockPrice;
                settings.StorePickPrice.Value = config.StorePickPrice;
                settings.StreetPickPrice.Value = config.StreetPickPrice;
                settings.MeltBasePrice.Value = config.MeltBasePrice;
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
            settings.EnableTripleLotteryAnimation.OnValueChanged += OnSettingChanged;
            settings.EnableDestroyButton.OnValueChanged += OnSettingChanged;
            settings.EnableMeltButton.OnValueChanged += OnSettingChanged;
            settings.EnableDebug.OnValueChanged += OnSettingChanged;
            settings.EnableUseToCreateItemPatch.OnValueChanged += OnSettingChanged;
            settings.EnableWeightedLottery.OnValueChanged += OnSettingChanged;
            settings.EnableHighQualitySound.OnValueChanged += OnSettingChanged;
            settings.EnableStockShopActions.OnValueChanged += OnSettingChanged;
            settings.HighQualitySoundFilePath.OnValueChanged += OnSettingChanged;
            settings.RefreshStockPrice.OnValueChanged += OnSettingChanged;
            settings.StorePickPrice.OnValueChanged += OnSettingChanged;
            settings.StreetPickPrice.OnValueChanged += OnSettingChanged;
            settings.MeltBasePrice.OnValueChanged += OnSettingChanged;
        }

        private void UnsubscribeFromSettingChanges()
        {
            var settings = SettingManager.Instance;
            settings.EnableAnimation.OnValueChanged -= OnSettingChanged!;
            settings.EnableTripleLotteryAnimation.OnValueChanged -= OnSettingChanged!;
            settings.EnableDestroyButton.OnValueChanged -= OnSettingChanged!;
            settings.EnableMeltButton.OnValueChanged -= OnSettingChanged!;
            settings.EnableDebug.OnValueChanged -= OnSettingChanged!;
            settings.EnableUseToCreateItemPatch.OnValueChanged -= OnSettingChanged!;
            settings.EnableWeightedLottery.OnValueChanged -= OnSettingChanged!;
            settings.EnableHighQualitySound.OnValueChanged -= OnSettingChanged!;
            settings.EnableStockShopActions.OnValueChanged -= OnSettingChanged!;
            settings.HighQualitySoundFilePath.OnValueChanged -= OnSettingChanged!;
            settings.RefreshStockPrice.OnValueChanged -= OnSettingChanged!;
            settings.StorePickPrice.OnValueChanged -= OnSettingChanged!;
            settings.StreetPickPrice.OnValueChanged -= OnSettingChanged!;
            settings.MeltBasePrice.OnValueChanged -= OnSettingChanged!;
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
