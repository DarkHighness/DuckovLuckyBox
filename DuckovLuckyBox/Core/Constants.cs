namespace DuckovLuckyBox.Core
{
    public static class Constants
    {
        public const string ModId = "Duckov.LuckyAnimation";
        public const string ModName = "Duckov Lucky Animation";
        public const string AnimationOnlyModId = "Duckov.LuckyAnimation";
        public static readonly string AnimationOnlyModName = "Duckov Lucky Animation";

        public static class Sound
        {
            public static readonly string RollingSoundResourceName = "Resources.rolling.mp3";
            public static readonly string DestroySoundResourceName = "Resources.destroy.mp3";
            public static readonly string LotterySoundResourceName = "Resources.lottery.mp3";
            public static readonly string HighQualityLotterySoundResourceName = "Resources.lottery_high_quality.mp3";
            public static readonly string MeltLevelSameResourceName = "Resources.melt_level_same.mp3";
            public static readonly string MeltLevelUpResourceName = "Resources.melt_level_up.mp3";
            public static readonly string MeltLevelDownResourceName = "Resources.melt_level_down.mp3";
            public static readonly string MeltDestroyResourceName = "Resources.melt_destroy.mp3";

            public static FMOD.Sound? ROLLING_SOUND { get; private set; }
            public static FMOD.Sound? DESTROY_SOUND { get; private set; }
            public static FMOD.Sound? LOTTERY_SOUND { get; private set; }
            public static FMOD.Sound? HIGH_QUALITY_LOTTERY_SOUND { get; private set; }
            public static FMOD.Sound? MELT_LEVEL_SAME_SOUND { get; private set; }
            public static FMOD.Sound? MELT_LEVEL_UP_SOUND { get; private set; }
            public static FMOD.Sound? MELT_LEVEL_DOWN_SOUND { get; private set; }
            public static FMOD.Sound? MELT_DESTROY_SOUND { get; private set; }

            public static void LoadSounds()
            {
                ROLLING_SOUND = SoundUtils.CreateSound(RollingSoundResourceName);
                DESTROY_SOUND = SoundUtils.CreateSound(DestroySoundResourceName);
                LOTTERY_SOUND = SoundUtils.CreateSound(LotterySoundResourceName);
                HIGH_QUALITY_LOTTERY_SOUND = SoundUtils.CreateSound(HighQualityLotterySoundResourceName);
                MELT_LEVEL_SAME_SOUND = SoundUtils.CreateSound(MeltLevelSameResourceName);
                MELT_LEVEL_UP_SOUND = SoundUtils.CreateSound(MeltLevelUpResourceName);
                MELT_LEVEL_DOWN_SOUND = SoundUtils.CreateSound(MeltLevelDownResourceName);
                MELT_DESTROY_SOUND = SoundUtils.CreateSound(MeltDestroyResourceName);
            }
        }
    }
}
