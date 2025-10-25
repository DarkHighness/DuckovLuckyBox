using DuckovLuckyBox.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace DuckovLuckyBox
{
  public static class SoundUtils
  {
    // Cache for loaded sounds from files to avoid recreating them
    private static readonly Dictionary<string, Sound> _soundCache = new Dictionary<string, Sound>();

    // File watchers for each cached sound file
    private static readonly Dictionary<string, FileSystemWatcher> _fileWatchers = new Dictionary<string, FileSystemWatcher>();

    // Track last write time to debounce file change events
    private static readonly Dictionary<string, DateTime> _fileLastWriteTime = new Dictionary<string, DateTime>();

    public static Sound? CreateSound(string soundFileName)
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      using Stream stream = assembly.GetManifestResourceStream("DuckovLuckyBox." + soundFileName) ?? throw new FileNotFoundException("Resource not found: " + soundFileName);
      using MemoryStream memoryStream = new MemoryStream();
      stream.CopyTo(memoryStream);
      byte[] soundData = memoryStream.ToArray();

      CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO
      {
        cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
        length = (uint)soundData.Length
      };
      var soundResult = RuntimeManager.CoreSystem.createSound(soundData, MODE.DEFAULT | MODE.LOOP_OFF | MODE.OPENMEMORY, ref exinfo, out Sound sound);
      if (soundResult != RESULT.OK)
      {
        Log.Error($"Failed to create sound from resource {soundFileName}: {soundResult}");
        return null;
      }

      return sound;
    }

    public static bool PlaySound(Sound? sound, ChannelGroup channelGroup)
    {
      if (sound == null)
      {
        Log.Error("Cannot play null sound");
        return false;
      }

      try
      {
        var playResult = RuntimeManager.CoreSystem.playSound((Sound)sound, channelGroup, false, out Channel channel);
        if (playResult != RESULT.OK)
        {
          Log.Error($"Failed to play sound: {playResult}");
          return false;
        }
        return true;
      }
      catch (Exception ex)
      {
        Log.Error($"Exception occurred while playing sound: {ex.Message}");
        return false;
      }
    }

    public static bool PlaySoundFromFile(string filePath, ChannelGroup channelGroup)
    {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
      {
        Log.Error($"Sound file not found: {filePath}");
        return false;
      }

      try
      {
        // Check cache first
        Sound sound;
        if (_soundCache.ContainsKey(filePath))
        {
          Log.Debug($"Using cached sound from: {filePath}");
          sound = _soundCache[filePath];
        }
        else
        {
          // Create new sound and cache it
          CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO
          {
            cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>()
          };

          var soundResult = RuntimeManager.CoreSystem.createSound(filePath, MODE.DEFAULT | MODE.LOOP_OFF, ref exinfo, out sound);
          if (soundResult != RESULT.OK)
          {
            Log.Error($"Failed to create sound from file {filePath}: {soundResult}");
            return false;
          }

          _soundCache[filePath] = sound;
          Log.Debug($"Created and cached new sound from: {filePath}");

          // Start watching for file changes
          StartWatchingFile(filePath);
        }

        // Play the sound
        var playResult = RuntimeManager.CoreSystem.playSound(sound, channelGroup, false, out Channel channel);
        if (playResult != RESULT.OK)
        {
          Log.Error($"Failed to play sound from file: {playResult}");
          return false;
        }

        return true;
      }
      catch (Exception ex)
      {
        Log.Error($"Exception occurred while playing sound from file {filePath}: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Clear all cached sounds from memory
    /// </summary>
    public static void ClearSoundCache()
    {
      foreach (var sound in _soundCache.Values)
      {
        try
        {
          sound.release();
        }
        catch (Exception ex)
        {
          Log.Warning($"Failed to release cached sound: {ex.Message}");
        }
      }
      _soundCache.Clear();
      Log.Debug("Sound cache cleared");
    }

    /// <summary>
    /// Remove a specific sound from cache
    /// </summary>
    public static void RemoveSoundFromCache(string filePath)
    {
      if (_soundCache.ContainsKey(filePath))
      {
        try
        {
          _soundCache[filePath].release();
          _soundCache.Remove(filePath);
          Log.Debug($"Removed sound from cache: {filePath}");
        }
        catch (Exception ex)
        {
          Log.Warning($"Failed to remove cached sound {filePath}: {ex.Message}");
        }
      }

      // Stop watching this file
      StopWatchingFile(filePath);
    }

    /// <summary>
    /// Start watching a sound file for changes
    /// </summary>
    private static void StartWatchingFile(string filePath)
    {
      if (string.IsNullOrEmpty(filePath) || _fileWatchers.ContainsKey(filePath))
      {
        return;
      }

      try
      {
        string directory = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileName(filePath);

        if (string.IsNullOrEmpty(directory))
        {
          return;
        }

        var watcher = new FileSystemWatcher(directory)
        {
          Filter = fileName,
          NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
          EnableRaisingEvents = true
        };

        watcher.Changed += (sender, e) => OnSoundFileChanged(filePath, e);
        _fileWatchers[filePath] = watcher;
        _fileLastWriteTime[filePath] = File.GetLastWriteTime(filePath);

        Log.Debug($"Started watching sound file: {filePath}");
      }
      catch (Exception ex)
      {
        Log.Warning($"Failed to start watching sound file {filePath}: {ex.Message}");
      }
    }

    /// <summary>
    /// Stop watching a sound file for changes
    /// </summary>
    private static void StopWatchingFile(string filePath)
    {
      if (_fileWatchers.ContainsKey(filePath))
      {
        try
        {
          _fileWatchers[filePath].Changed -= OnSoundFileChanged;
          _fileWatchers[filePath].EnableRaisingEvents = false;
          _fileWatchers[filePath].Dispose();
          _fileWatchers.Remove(filePath);
          _fileLastWriteTime.Remove(filePath);

          Log.Debug($"Stopped watching sound file: {filePath}");
        }
        catch (Exception ex)
        {
          Log.Warning($"Failed to stop watching sound file {filePath}: {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Handle file change events with debouncing
    /// </summary>
    private static void OnSoundFileChanged(object sender, FileSystemEventArgs e)
    {
      if (e == null || e.FullPath == null)
      {
        return;
      }

      string filePath = e.FullPath;

      try
      {
        if (!File.Exists(filePath))
        {
          return;
        }

        // Debounce: check if file was actually modified (not just watched)
        DateTime currentWriteTime = File.GetLastWriteTime(filePath);
        if (_fileLastWriteTime.ContainsKey(filePath) && currentWriteTime <= _fileLastWriteTime[filePath].AddSeconds(0.5))
        {
          return;
        }

        // Update write time and refresh cache
        _fileLastWriteTime[filePath] = currentWriteTime;
        Log.Debug($"Sound file changed, refreshing cache: {filePath}");
        RefreshSoundCache(filePath);
      }
      catch (Exception ex)
      {
        Log.Warning($"Error handling sound file change for {filePath}: {ex.Message}");
      }
    }

    /// <summary>
    /// Refresh a specific sound in cache by reloading it from file
    /// </summary>
    private static void RefreshSoundCache(string filePath)
    {
      try
      {
        // Release old sound
        if (_soundCache.ContainsKey(filePath))
        {
          _soundCache[filePath].release();
          _soundCache.Remove(filePath);
        }

        // Reload sound
        CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO
        {
          cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>()
        };

        var soundResult = RuntimeManager.CoreSystem.createSound(filePath, MODE.DEFAULT | MODE.LOOP_OFF, ref exinfo, out Sound sound);
        if (soundResult != RESULT.OK)
        {
          Log.Error($"Failed to reload sound from file {filePath}: {soundResult}");
          return;
        }

        _soundCache[filePath] = sound;
        Log.Debug($"Successfully refreshed cached sound: {filePath}");
      }
      catch (Exception ex)
      {
        Log.Error($"Exception occurred while refreshing sound cache for {filePath}: {ex.Message}");
      }
    }

    /// <summary>
    /// Stop watching all files and clear watchers
    /// </summary>
    public static void StopAllFileWatchers()
    {
      foreach (var watcher in _fileWatchers.Values)
      {
        try
        {
          watcher.Changed -= OnSoundFileChanged;
          watcher.EnableRaisingEvents = false;
          watcher.Dispose();
        }
        catch (Exception ex)
        {
          Log.Warning($"Failed to dispose file watcher: {ex.Message}");
        }
      }
      _fileWatchers.Clear();
      _fileLastWriteTime.Clear();
      Log.Debug("All file watchers stopped");
    }
  }
}