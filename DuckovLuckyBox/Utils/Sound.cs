using DuckovLuckyBox.Core;
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
      catch (System.Exception ex)
      {
        Log.Error($"Exception occurred while playing sound: {ex.Message}");
        return false;
      }
    }
  }
}