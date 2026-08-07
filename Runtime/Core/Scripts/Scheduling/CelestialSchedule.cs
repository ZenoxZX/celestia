using System;
using UnityEngine;
using UnityEngine.Events;

namespace Celestia
{
    [Serializable]
    public class CelestialSchedule
    {
        [SerializeField] private string m_Label = "Schedule";
        [SerializeField] private bool m_Enabled = true;
        [SerializeField] private ScheduleTrigger m_Trigger = ScheduleTrigger.TimeOfDay;

        [SerializeField] private TimeOfDay m_Time = new(19, 42);
        [SerializeField] private SkyEvent m_SkyEvent = SkyEvent.Sunset;

        [SerializeField] private TimeOfDay m_RangeStart = new(19, 0);
        [SerializeField] private TimeOfDay m_RangeEnd = new(6, 0);

        [SerializeField] private ScheduleInterval m_Interval = ScheduleInterval.EveryHour;

        [Tooltip("Fires once and then stops until the scheduler is re-enabled.")]
        [SerializeField] private bool m_Once;

        [Tooltip("For ranges: fire Entered on enable if the clock already sits inside the range.")]
        [SerializeField] private bool m_CatchUpOnEnable = true;

        [SerializeField] private UnityEvent m_Triggered = new();
        [SerializeField] private UnityEvent m_Exited = new();

        private bool m_Fired;
        private bool m_Inside;
        private bool m_HasInsideState;

        public event Action Fired;
        public event Action Left;

        public CelestialSchedule()
        {
        }

        public string Label
        {
            get => m_Label;
            set => m_Label = value;
        }

        public bool Enabled
        {
            get => m_Enabled;
            set => m_Enabled = value;
        }

        public bool Once
        {
            get => m_Once;
            set => m_Once = value;
        }

        public bool CatchUpOnEnable
        {
            get => m_CatchUpOnEnable;
            set => m_CatchUpOnEnable = value;
        }

        public ScheduleTrigger Trigger => m_Trigger;
        public TimeOfDay Time => m_Time;
        public SkyEvent SkyEvent => m_SkyEvent;
        public TimeOfDay RangeStart => m_RangeStart;
        public TimeOfDay RangeEnd => m_RangeEnd;
        public ScheduleInterval Interval => m_Interval;
        public bool IsInside => m_Inside;
        public UnityEvent Triggered => m_Triggered;
        public UnityEvent Exited => m_Exited;

        public static CelestialSchedule At(TimeOfDay time, Action action = null)
        {
            CelestialSchedule schedule = Create(ScheduleTrigger.TimeOfDay, action);
            schedule.m_Time = time;
            schedule.m_Label = time.ToShortString();
            return schedule;
        }

        public static CelestialSchedule At(int hour, int minute, Action action = null) => At(new(hour, minute), action);

        public static CelestialSchedule On(SkyEvent skyEvent, Action action = null)
        {
            CelestialSchedule schedule = Create(ScheduleTrigger.SkyEvent, action);
            schedule.m_SkyEvent = skyEvent;
            schedule.m_Label = skyEvent.ToString();
            return schedule;
        }

        public static CelestialSchedule Between(TimeOfDay start, TimeOfDay end, Action entered = null, Action exited = null)
        {
            CelestialSchedule schedule = Create(ScheduleTrigger.TimeRange, entered);
            schedule.m_RangeStart = start;
            schedule.m_RangeEnd = end;
            schedule.m_Label = $"{start.ToShortString()} - {end.ToShortString()}";

            if (exited != null)
                schedule.Left += exited;

            return schedule;
        }

        public static CelestialSchedule Every(ScheduleInterval interval, Action action = null)
        {
            CelestialSchedule schedule = Create(ScheduleTrigger.Interval, action);
            schedule.m_Interval = interval;
            schedule.m_Label = interval.ToString();

            return schedule;
        }

        public void ResetState()
        {
            m_Fired = false;
            m_Inside = false;
            m_HasInsideState = false;
        }

        public void Prime(float progress, Func<SkyEvent, float> resolveSkyEvent)
        {
            if (m_Trigger != ScheduleTrigger.TimeRange) return;

            m_Inside = IsWithinRange(progress);
            m_HasInsideState = true;

            if (m_Inside && m_CatchUpOnEnable) Fire();
        }

        public void Resync(float progress)
        {
            if (m_Trigger != ScheduleTrigger.TimeRange) return;

            // The clock jumped, so realign the inside/outside state without
            // firing the enter or exit event for a transition that never
            // happened in play.
            m_Inside = IsWithinRange(progress);
            m_HasInsideState = true;
        }

        public void Evaluate(in ClockAdvance advance, Func<SkyEvent, float> resolveSkyEvent)
        {
            if (!m_Enabled)
                return;

            if (m_Once && m_Fired)
                return;

            switch (m_Trigger)
            {
                case ScheduleTrigger.TimeOfDay:
                    EvaluateInstant(advance, m_Time.Progress);
                    break;

                case ScheduleTrigger.SkyEvent:
                    EvaluateInstant(advance, resolveSkyEvent(m_SkyEvent));
                    break;

                case ScheduleTrigger.Interval:
                    EvaluateInterval(advance);
                    break;

                case ScheduleTrigger.TimeRange:
                    EvaluateRange(advance);
                    break;
            }
        }

        private void EvaluateInstant(in ClockAdvance advance, float progress)
        {
            if (progress < 0f)
                return;

            if (!advance.Covers(progress))
                return;

            Fire();
        }

        private void EvaluateInterval(in ClockAdvance advance)
        {
            int steps = advance.CountBoundaries(UnitsPerDay(m_Interval));

            for (int i = 0; i < steps; i++)
            {
                Fire();

                if (m_Once)
                    return;
            }
        }

        private static double UnitsPerDay(ScheduleInterval interval) => interval switch
        {
            ScheduleInterval.EveryMinute => TimeOfDay.MinutesPerHour * TimeOfDay.HoursPerDay,
            ScheduleInterval.EveryHour => TimeOfDay.HoursPerDay,
            _ => 1.0
        };

        private void EvaluateRange(in ClockAdvance advance)
        {
            bool nowInside = IsWithinRange(advance.To);

            if (!m_HasInsideState)
            {
                m_Inside = nowInside;
                m_HasInsideState = true;
                return;
            }

            if (nowInside == m_Inside)
                return;

            m_Inside = nowInside;

            if (nowInside)
                Fire();
            else
                Exit();
        }

        private bool IsWithinRange(float progress)
        {
            float start = m_RangeStart.Progress;
            float end = m_RangeEnd.Progress;
            float value = Mathf.Repeat(progress, 1f);

            if (Mathf.Approximately(start, end))
                return true;

            if (start < end)
                return value >= start && value < end;

            return value >= start || value < end;
        }

        private static CelestialSchedule Create(ScheduleTrigger trigger, Action action)
        {
            CelestialSchedule schedule = new() { m_Trigger = trigger };

            if (action != null)
                schedule.Fired += action;

            return schedule;
        }

        private void Fire()
        {
            m_Fired = true;
            m_Triggered?.Invoke();
            Fired?.Invoke();
        }

        private void Exit()
        {
            m_Exited?.Invoke();
            Left?.Invoke();
        }
    }
}
