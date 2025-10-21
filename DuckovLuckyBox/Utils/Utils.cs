using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using DuckovLuckyBox.Core;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace DuckovLuckyBox
{
    public static class Utils
    {
        public static Texture2D LoadTexture(string textureName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("DuckovLuckyBox." + textureName) ?? throw new FileNotFoundException("Resource not found: " + textureName);
            using MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(memoryStream.ToArray());
            return texture;
        }

        public static Sound? CreateSound(string soundFileName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("DuckovLuckyBox." + soundFileName) ?? throw new FileNotFoundException("Resource not found: " + soundFileName);
            using MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            byte[] soundData = memoryStream.ToArray();

            Sound sound;
            CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO
            {
                cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
                length = (uint)soundData.Length
            };
            var soundResult = RuntimeManager.CoreSystem.createSound(soundData, MODE.DEFAULT | MODE.LOOP_OFF | MODE.OPENMEMORY, ref exinfo, out sound);
            if (soundResult != RESULT.OK)
            {
                Log.Error($"Failed to create sound from resource {soundFileName}: {soundResult}");
                return null;
            }

            return sound;
        }
    }
}