namespace DuckovLuckyBox.Core
{
    public static class Constants
    {
        public static readonly string ModId = "Duckov.LuckyBox";
        public static readonly string ModName = "Duckov Lucky Box";

        public static class Sound {
            public static readonly string RollingSoundResourceName = "Resources.rolling.mp3";
            public static readonly string DestroySoundResourceName = "Resources.destroy.mp3";
            public static readonly string LotterySoundResourceName = "Resources.lottery.mp3";

            public static FMOD.Sound? ROLLING_SOUND { get; private set; }
            public static FMOD.Sound? DESTROY_SOUND { get; private set; }
            public static FMOD.Sound? LOTTERY_SOUND { get; private set; }

            public static void LoadSounds()
            {
                ROLLING_SOUND = Utils.CreateSound(RollingSoundResourceName);
                DESTROY_SOUND = Utils.CreateSound(DestroySoundResourceName);
                LOTTERY_SOUND = Utils.CreateSound(LotterySoundResourceName);
            }
        }
    }
}
