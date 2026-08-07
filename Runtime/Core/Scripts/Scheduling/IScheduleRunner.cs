using System;
using System.Collections.Generic;

namespace Celestia
{
    public interface IScheduleRunner
    {
        IReadOnlyList<CelestialSchedule> Schedules { get; }

        CelestialSchedule Add(CelestialSchedule schedule);

        CelestialSchedule At(TimeOfDay time, Action action);

        CelestialSchedule At(int hour, int minute, Action action);

        CelestialSchedule On(SkyEvent skyEvent, Action action);

        CelestialSchedule Between(TimeOfDay start, TimeOfDay end,
                                  Action entered, Action exited = null);

        CelestialSchedule Every(ScheduleInterval interval, Action action);

        bool Remove(CelestialSchedule schedule);

        void Clear();

        CelestialSchedule Find(string label);
    }
}
