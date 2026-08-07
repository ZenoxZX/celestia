using System;
using System.Collections.Generic;
using UnityEngine;

namespace Celestia
{
    public sealed class ScheduleRunner : IScheduleRunner, IDisposable
    {
        private const double k_GoldenHourAltitude = 6.0;
        private const double k_CivilTwilightAltitude = -6.0;
        private const double k_HorizonAltitude = 0.0;

        private readonly List<CelestialSchedule> m_Schedules = new List<CelestialSchedule>();
        private readonly List<CelestialSchedule> m_Buffer = new List<CelestialSchedule>();
        private readonly Func<SkyEvent, float> m_ResolveSkyEvent;

        private readonly IWorldClock m_Clock;
        private readonly ICelestialSource m_Source;

        private bool m_Bound;

        public ScheduleRunner(IWorldClock clock, ICelestialSource source)
        {
            m_Clock = clock;
            m_Source = source;
            m_ResolveSkyEvent = ResolveSkyEvent;
        }

        public IReadOnlyList<CelestialSchedule> Schedules => m_Schedules;

        public void Bind()
        {
            if (m_Bound || m_Clock == null)
                return;

            for (int i = 0; i < m_Schedules.Count; i++)
            {
                m_Schedules[i].ResetState();
                m_Schedules[i].Prime(m_Clock.DayProgress, m_ResolveSkyEvent);
            }

            m_Clock.Advanced += OnAdvanced;
            m_Clock.Resynced += OnResynced;
            m_Bound = true;
        }

        public void Unbind()
        {
            if (!m_Bound)
                return;

            m_Clock.Advanced -= OnAdvanced;
            m_Clock.Resynced -= OnResynced;
            m_Bound = false;
        }

        public CelestialSchedule Add(CelestialSchedule schedule)
        {
            if (schedule == null)
                return null;

            m_Schedules.Add(schedule);

            if (m_Clock == null)
                return schedule;

            schedule.ResetState();
            schedule.Prime(m_Clock.DayProgress, m_ResolveSkyEvent);

            return schedule;
        }

        public CelestialSchedule At(TimeOfDay time, Action action) => Add(CelestialSchedule.At(time, action));
        public CelestialSchedule At(int hour, int minute, Action action) => Add(CelestialSchedule.At(hour, minute, action));
        public CelestialSchedule On(SkyEvent skyEvent, Action action) => Add(CelestialSchedule.On(skyEvent, action));
        public CelestialSchedule Between(TimeOfDay start, TimeOfDay end, Action entered, Action exited = null) => Add(CelestialSchedule.Between(start, end, entered, exited));
        public CelestialSchedule Every(ScheduleInterval interval, Action action) => Add(CelestialSchedule.Every(interval, action));
        public bool Remove(CelestialSchedule schedule) => m_Schedules.Remove(schedule);
        public void Clear() => m_Schedules.Clear();

        public CelestialSchedule Find(string label)
        {
            for (int i = 0; i < m_Schedules.Count; i++)
            {
                if (m_Schedules[i].Label == label)
                    return m_Schedules[i];
            }

            return null;
        }

        public float ResolveSkyEvent(SkyEvent skyEvent)
        {
            CelestialPreset preset = m_Source?.Preset;
            if (preset == null)
            {
                Debug.LogError(
                    $"{nameof(ScheduleRunner)} needs a celestial source with a preset " +
                    "to resolve sky events.");
                return -1f;
            }

            double year = preset.YearProgress;
            double phase = preset.MoonPhase;
            double latitude = preset.Latitude;

            switch (skyEvent)
            {
                case SkyEvent.SolarNoon:
                    return 0.5f;

                case SkyEvent.Midnight:
                    return 0f;

                case SkyEvent.Sunrise:
                    return SunCrossing(k_HorizonAltitude, year, latitude, true);

                case SkyEvent.Sunset:
                    return SunCrossing(k_HorizonAltitude, year, latitude, false);

                case SkyEvent.GoldenHourStart:
                    return SunCrossing(k_GoldenHourAltitude, year, latitude, false);

                case SkyEvent.GoldenHourEnd:
                    return SunCrossing(k_GoldenHourAltitude, year, latitude, true);

                case SkyEvent.CivilDuskStart:
                    return SunCrossing(k_CivilTwilightAltitude, year, latitude, false);

                case SkyEvent.CivilDawnEnd:
                    return SunCrossing(k_CivilTwilightAltitude, year, latitude, true);

                case SkyEvent.Moonrise:
                    return MoonCrossing(year, phase, latitude, true);

                case SkyEvent.Moonset:
                    return MoonCrossing(year, phase, latitude, false);

                default:
                    return -1f;
            }
        }

        void IDisposable.Dispose()
        {
            Unbind();
        }

        private static float SunCrossing(double altitude, double year, double latitude, bool rising)
        {
            bool crosses = CelestialSolver.SunCrossing(altitude, year, latitude,
                out double rise, out double set);

            if (!crosses) return -1f;
            return (float)(rising ? rise : set);
        }

        private static float MoonCrossing(double year, double phase, double latitude, bool rising)
        {
            bool crosses = CelestialSolver.MoonCrossing(k_HorizonAltitude, year, phase, latitude,
                out double rise, out double set);

            if (!crosses) return -1f;
            return (float)(rising ? rise : set);
        }

        private void OnAdvanced(ClockAdvance advance)
        {
            m_Buffer.Clear();
            m_Buffer.AddRange(m_Schedules);

            for (int i = 0; i < m_Buffer.Count; i++)
            {
                m_Buffer[i].Evaluate(advance, m_ResolveSkyEvent);
            }

            m_Buffer.Clear();
        }

        private void OnResynced(float progress)
        {
            m_Buffer.Clear();
            m_Buffer.AddRange(m_Schedules);

            for (int i = 0; i < m_Buffer.Count; i++)
            {
                m_Buffer[i].Resync(progress);
            }

            m_Buffer.Clear();
        }
    }
}
