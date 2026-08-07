using System;
using System.Collections.Generic;
using UnityEngine;

namespace Celestia
{
    [AddComponentMenu("Celestia/Celestial Scheduler")]
    [DisallowMultipleComponent]
    public class CelestialScheduler : MonoBehaviour
    {
        private const double k_GoldenHourAltitude = 6.0;
        private const double k_CivilTwilightAltitude = -6.0;
        private const double k_HorizonAltitude = 0.0;

        [Tooltip("Leave empty to use the first active WorldClock in the scene.")]
        [SerializeField] private WorldClock m_Clock;

        [Tooltip("Needed only by Sky Event schedules, which read latitude and season from it.")]
        [SerializeField] private CelestialHandler m_Handler;

        [SerializeField] private List<CelestialSchedule> m_Schedules = new List<CelestialSchedule>();

        private readonly List<CelestialSchedule> m_Buffer = new List<CelestialSchedule>();

        private WorldClock m_BoundClock;
        private Func<SkyEvent, float> m_ResolveSkyEvent;

        public IReadOnlyList<CelestialSchedule> Schedules => m_Schedules;

        public WorldClock BoundClock => m_BoundClock;

        private void OnEnable()
        {
            m_ResolveSkyEvent ??= ResolveSkyEvent;
            m_BoundClock = m_Clock != null ? m_Clock : WorldClock.Active;

            if (m_BoundClock == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialScheduler)} on '{name}' found no {nameof(WorldClock)}.", this);
                return;
            }

            for (int i = 0; i < m_Schedules.Count; i++)
            {
                m_Schedules[i].ResetState();
                m_Schedules[i].Prime(m_BoundClock.DayProgress, m_ResolveSkyEvent);
            }

            m_BoundClock.Advanced += OnAdvanced;
        }

        private void OnDisable()
        {
            if (m_BoundClock == null) return;

            m_BoundClock.Advanced -= OnAdvanced;
            m_BoundClock = null;
        }

        public CelestialSchedule Add(CelestialSchedule schedule)
        {
            if (schedule == null) return null;

            m_Schedules.Add(schedule);

            if (m_BoundClock == null) return schedule;

            m_ResolveSkyEvent ??= ResolveSkyEvent;
            schedule.ResetState();
            schedule.Prime(m_BoundClock.DayProgress, m_ResolveSkyEvent);

            return schedule;
        }

        public CelestialSchedule At(TimeOfDay time, Action action)
        {
            return Add(CelestialSchedule.At(time, action));
        }

        public CelestialSchedule At(int hour, int minute, Action action)
        {
            return Add(CelestialSchedule.At(hour, minute, action));
        }

        public CelestialSchedule On(SkyEvent skyEvent, Action action)
        {
            return Add(CelestialSchedule.On(skyEvent, action));
        }

        public CelestialSchedule Between(TimeOfDay start, TimeOfDay end,
                                         Action entered, Action exited = null)
        {
            return Add(CelestialSchedule.Between(start, end, entered, exited));
        }

        public CelestialSchedule Every(ScheduleInterval interval, Action action)
        {
            return Add(CelestialSchedule.Every(interval, action));
        }

        public bool Remove(CelestialSchedule schedule)
        {
            return m_Schedules.Remove(schedule);
        }

        public void Clear()
        {
            m_Schedules.Clear();
        }

        public CelestialSchedule Find(string label)
        {
            for (int i = 0; i < m_Schedules.Count; i++)
            {
                if (m_Schedules[i].Label == label) return m_Schedules[i];
            }

            return null;
        }

        public float ResolveSkyEvent(SkyEvent skyEvent)
        {
            CelestialPreset preset = m_Handler != null ? m_Handler.Preset : null;
            if (preset == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialScheduler)} on '{name}' needs a handler with a preset " +
                    "to resolve sky events.", this);
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
            // Callbacks may add or remove schedules, so iterate a snapshot.
            m_Buffer.Clear();
            m_Buffer.AddRange(m_Schedules);

            for (int i = 0; i < m_Buffer.Count; i++)
            {
                m_Buffer[i].Evaluate(advance, m_ResolveSkyEvent);
            }

            m_Buffer.Clear();
        }
    }
}
