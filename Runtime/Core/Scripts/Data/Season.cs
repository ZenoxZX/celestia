namespace Celestia
{
    public enum Season
    {
        SpringEquinox = 0,
        SummerSolstice = 1,
        AutumnEquinox = 2,
        WinterSolstice = 3
    }

    public static class SeasonExtensions
    {
        public static float ToYearProgress(this Season season)
        {
            switch (season)
            {
                case Season.SummerSolstice: return 0.25f;
                case Season.AutumnEquinox: return 0.5f;
                case Season.WinterSolstice: return 0.75f;
                default: return 0f;
            }
        }
    }
}
