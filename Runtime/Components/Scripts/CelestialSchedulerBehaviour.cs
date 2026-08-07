using System;
using System.Collections.Generic;
using UnityEngine;

namespace Celestia
{
    [AddComponentMenu("Celestia/Celestial Scheduler")]
    [DisallowMultipleComponent]
    public class CelestialSchedulerBehaviour : MonoBehaviour, IScheduleRunner
    {
        [Tooltip("Leave empty to use the first active WorldClock in the scene.")]
        [SerializeField] private WorldClockBehaviour m_Clock;

        [Tooltip("Needed only by Sky Event schedules, which read latitude and season from it.")]
        [SerializeField] private CelestialHandlerBehaviour m_Handler;

        [SerializeField] private List<CelestialSchedule> m_Schedules = new List<CelestialSchedule>();

        private ScheduleRunner m_Runner;

        public IReadOnlyList<CelestialSchedule> Schedules =>
            m_Runner != null ? m_Runner.Schedules : m_Schedules;

        private void OnEnable()
        {
            IWorldClock clock = m_Clock != null ? m_Clock : (IWorldClock)WorldClockBehaviour.Active;

            if (clock == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialSchedulerBehaviour)} on '{name}' found no clock.", this);
                return;
            }

            m_Runner = new ScheduleRunner(clock, m_Handler);

            for (int i = 0; i < m_Schedules.Count; i++)
            {
                m_Runner.Add(m_Schedules[i]);
            }

            m_Runner.Bind();
        }

        private void OnDisable()
        {
            m_Runner?.Unbind();
            m_Runner = null;
        }

        public CelestialSchedule Add(CelestialSchedule schedule)
        {
            if (schedule == null) return null;

            if (m_Runner != null) return m_Runner.Add(schedule);

            m_Schedules.Add(schedule);
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
            m_Schedules.Remove(schedule);
            return m_Runner != null ? m_Runner.Remove(schedule) : true;
        }

        public void Clear()
        {
            m_Schedules.Clear();
            m_Runner?.Clear();
        }

        public CelestialSchedule Find(string label)
        {
            if (m_Runner != null) return m_Runner.Find(label);

            for (int i = 0; i < m_Schedules.Count; i++)
            {
                if (m_Schedules[i].Label == label) return m_Schedules[i];
            }

            return null;
        }
    }
}
