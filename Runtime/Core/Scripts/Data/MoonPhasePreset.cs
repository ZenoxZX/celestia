namespace Celestia
{
    public enum MoonPhasePreset
    {
        NewMoon = 0,
        WaxingCrescent = 1,
        FirstQuarter = 2,
        WaxingGibbous = 3,
        FullMoon = 4,
        WaningGibbous = 5,
        LastQuarter = 6,
        WaningCrescent = 7
    }

    public static class MoonPhasePresetExtensions
    {
        public static float ToPhase(this MoonPhasePreset preset)
        {
            return (int)preset * 0.125f;
        }

        public static MoonPhasePreset FromPhase(float phase)
        {
            float wrapped = phase - UnityEngine.Mathf.Floor(phase);
            int index = UnityEngine.Mathf.RoundToInt(wrapped * 8f) % 8;
            return (MoonPhasePreset)index;
        }

        public static string ToDisplayName(this MoonPhasePreset preset)
        {
            switch (preset)
            {
                case MoonPhasePreset.WaxingCrescent: return "Waxing Crescent";
                case MoonPhasePreset.FirstQuarter: return "First Quarter";
                case MoonPhasePreset.WaxingGibbous: return "Waxing Gibbous";
                case MoonPhasePreset.FullMoon: return "Full Moon";
                case MoonPhasePreset.WaningGibbous: return "Waning Gibbous";
                case MoonPhasePreset.LastQuarter: return "Last Quarter";
                case MoonPhasePreset.WaningCrescent: return "Waning Crescent";
                default: return "New Moon";
            }
        }
    }
}
