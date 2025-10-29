using System.IO;
using System.Reflection;
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
    }
}
