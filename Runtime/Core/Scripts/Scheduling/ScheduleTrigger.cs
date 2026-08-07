namespace Celestia
{
    public enum ScheduleTrigger
    {
        TimeOfDay = 0,
        SkyEvent = 1,
        TimeRange = 2,
        Interval = 3
    }

    public enum SkyEvent
    {
        Sunrise = 0,
        Sunset = 1,
        SolarNoon = 2,
        Midnight = 3,
        GoldenHourStart = 4,
        GoldenHourEnd = 5,
        CivilDuskStart = 6,
        CivilDawnEnd = 7,
        Moonrise = 8,
        Moonset = 9
    }

    public enum ScheduleInterval
    {
        EveryMinute = 0,
        EveryHour = 1,
        EveryDay = 2
    }
}
