using System;

namespace UI
{
    [Serializable]
    public class PlayerSettings
    {
        public float backgroundMusicVolume;
        public float effectVolume;
        public bool backgroundMusicBoost;
        public bool effectVolumeBoost;

        public PlayerSettings(float backgroundMusicVolume, float effectVolume, bool backgroundMusicBoost, bool effectVolumeBoost)
        {
            this.backgroundMusicVolume = backgroundMusicVolume;
            this.effectVolume = effectVolume;
            this.backgroundMusicBoost = backgroundMusicBoost;
            this.effectVolumeBoost = effectVolumeBoost;
        }
    }
}
