namespace DuckovLuckyBox.Core
{
    public static class Constants
    {
        public const string ModId = "Duckov.LuckyBox";
        public const string ModName = "Duckov Lucky Box";
        public const string AnimationOnlyModId = "Duckov.LuckyAnimation";
        public const string AnimationOnlyModName = "Duckov Lucky Animation";

        public static class Sound
        {
            public static readonly string RollingSoundResourceName = "Resources.rolling.mp3";
            public static readonly string DestroySoundResourceName = "Resources.destroy.mp3";
            public static readonly string LotterySoundResourceName = "Resources.lottery.mp3";
            public static readonly string HighQualityLotterySoundResourceName = "Resources.lottery_high_quality.mp3";

            public static FMOD.Sound? ROLLING_SOUND { get; private set; }
            public static FMOD.Sound? DESTROY_SOUND { get; private set; }
            public static FMOD.Sound? LOTTERY_SOUND { get; private set; }
            public static FMOD.Sound? HIGH_QUALITY_LOTTERY_SOUND { get; private set; }

            public static void LoadSounds()
            {
                ROLLING_SOUND = SoundUtils.CreateSound(RollingSoundResourceName);
                DESTROY_SOUND = SoundUtils.CreateSound(DestroySoundResourceName);
                LOTTERY_SOUND = SoundUtils.CreateSound(LotterySoundResourceName);
                HIGH_QUALITY_LOTTERY_SOUND = SoundUtils.CreateSound(HighQualityLotterySoundResourceName);
            }
        }
    }
}
