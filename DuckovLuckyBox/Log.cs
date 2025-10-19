namespace DuckovLuckyBox
{
    public static class Log
    {
        public static void Debug(string message)
        {
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            UnityEngine.Debug.Log($"[{Constants.ModName}][DEBUG] {timestamp} {message}");
        }
        public static void Info(string message)
        {
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            UnityEngine.Debug.Log($"[{Constants.ModName}][INFO] {timestamp} {message}");
        }

        public static void Error(string message)
        {
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            UnityEngine.Debug.LogError($"[{Constants.ModName}][ERROR] {timestamp} {message}");
        }

        public static void Warning(string message)
        {
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            UnityEngine.Debug.LogWarning($"[{Constants.ModName}][WARNING] {timestamp} {message}");
        }
    }
}