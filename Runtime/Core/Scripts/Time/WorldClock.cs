using System;
using UnityEngine;

namespace Celestia
{
    public class WorldClock : IWorldClock
    {
        public const float MinRealSecondsPerDay = 0.01f;

        private const int k_MaxSecondEventsPerTick = 600;
        private const int k_MaxMinuteEventsPerTick = TimeOfDay.MinutesPerHour * TimeOfDay.HoursPerDay;
        private const int k_MaxHourEventsPerTick = TimeOfDay.HoursPerDay;

        private double m_Progress;
        private float m_RealSecondsPerDay = 120f;
        private float m_TimeScale = 1f;
        private int m_DayCount;
        private bool m_IsRunning;
        private int m_LastSecond = -1;
        private int m_LastMinute = -1;
        private int m_LastHour = -1;

        public event Action<float> ProgressChanged;
        public event Action<ClockAdvance> Advanced;
        public event Action<float> Resynced;
        public event Action<TimeOfDay> SecondChanged;
        public event Action<TimeOfDay> MinuteChanged;
        public event Action<TimeOfDay> HourChanged;
        public event Action<int> DayElapsed;
        public event Action<bool> RunStateChanged;

        public WorldClock()
        {
        }

        public WorldClock(float realSecondsPerDay, float startProgress, bool running = true)
        {
            m_RealSecondsPerDay = Mathf.Max(MinRealSecondsPerDay, realSecondsPerDay);
            m_Progress = WrapProgress(startProgress);
            m_IsRunning = running;
            CacheBoundaries();
        }

        public float DayProgress => (float)m_Progress;

        public TimeOfDay Time => TimeOfDay.FromProgress((float)m_Progress);

        public int DayCount => m_DayCount;

        public bool IsRunning => m_IsRunning;

        public float TimeScale
        {
            get => m_TimeScale;
            set => m_TimeScale = Mathf.Max(0f, value);
        }

        public float RealSecondsPerDay
        {
            get => m_RealSecondsPerDay;
            set => m_RealSecondsPerDay = Mathf.Max(MinRealSecondsPerDay, value);
        }

        public void Play()
        {
            if (m_IsRunning) return;

            m_IsRunning = true;
            CacheBoundaries();
            RunStateChanged?.Invoke(true);
        }

        public void Pause()
        {
            if (!m_IsRunning) return;

            m_IsRunning = false;
            RunStateChanged?.Invoke(false);
        }

        public void Toggle()
        {
            if (m_IsRunning) Pause();
            else Play();
        }

        public void Tick(float deltaSeconds)
        {
            if (!m_IsRunning || deltaSeconds <= 0f) return;
            if (m_TimeScale <= 0f) return;

            double dayFraction = deltaSeconds * m_TimeScale / m_RealSecondsPerDay;
            if (dayFraction <= 0d) return;

            Advance(dayFraction);
        }

        public void StepSeconds(float seconds)
        {
            if (seconds == 0f) return;
            Advance(seconds / (double)TimeOfDay.SecondsPerDay);
        }

        public void StepMinutes(float minutes)
        {
            StepSeconds(minutes * TimeOfDay.SecondsPerMinute);
        }

        public void StepHours(float hours)
        {
            StepSeconds(hours * TimeOfDay.SecondsPerHour);
        }

        public void SetProgress(float progress, TimeChangeMode mode = TimeChangeMode.Resync)
        {
            double target = WrapProgress(progress);

            if (mode == TimeChangeMode.Replay)
            {
                double distance = target - m_Progress;
                if (distance < 0d) distance += 1d;
                if (distance > 0d) Advance(distance);
                return;
            }

            m_Progress = target;
            CacheBoundaries();

            ProgressChanged?.Invoke((float)m_Progress);
            Resynced?.Invoke((float)m_Progress);
        }

        public void SetTime(TimeOfDay time, TimeChangeMode mode = TimeChangeMode.Resync)
        {
            SetProgress(time.Progress, mode);
        }

        public void SetTime(int hour, int minute, int second = 0,
                            TimeChangeMode mode = TimeChangeMode.Resync)
        {
            SetProgress(new TimeOfDay(hour, minute, second).Progress, mode);
        }

        private void Advance(double dayFraction)
        {
            double from = m_Progress;
            double target = m_Progress + dayFraction;

            int dayRollovers = (int)Math.Floor(target);
            if (dayRollovers != 0)
            {
                target -= dayRollovers;
                if (target < 0d) target += 1d;
            }

            m_Progress = target;
            ProgressChanged?.Invoke((float)m_Progress);

            Advanced?.Invoke(new ClockAdvance(
                from, m_Progress, dayFraction, Math.Max(0, dayRollovers)));

            EmitBoundaryEvents(dayFraction);

            if (dayRollovers <= 0) return;

            for (int i = 0; i < dayRollovers; i++)
            {
                m_DayCount++;
                DayElapsed?.Invoke(m_DayCount);
            }
        }

        private void EmitBoundaryEvents(double dayFraction)
        {
            bool hasSecondListener = SecondChanged != null;
            bool hasMinuteListener = MinuteChanged != null;
            bool hasHourListener = HourChanged != null;

            if (!hasSecondListener && !hasMinuteListener && !hasHourListener)
            {
                CacheBoundaries();
                return;
            }

            int elapsedSeconds = (int)Math.Round(dayFraction * TimeOfDay.SecondsPerDay);
            if (elapsedSeconds <= 0)
            {
                CacheBoundaries();
                return;
            }

            int startSeconds = m_LastSecond >= 0 ? m_LastSecond : Time.TotalSeconds;

            EmitUnitBoundaries(hasSecondListener, elapsedSeconds, startSeconds,
                1, k_MaxSecondEventsPerTick, SecondChanged);

            EmitUnitBoundaries(hasMinuteListener, elapsedSeconds, startSeconds,
                TimeOfDay.SecondsPerMinute, k_MaxMinuteEventsPerTick, MinuteChanged);

            EmitUnitBoundaries(hasHourListener, elapsedSeconds, startSeconds,
                TimeOfDay.SecondsPerHour, k_MaxHourEventsPerTick, HourChanged);

            CacheBoundaries();
        }

        private static void EmitUnitBoundaries(bool hasListener, int elapsedSeconds, int startSeconds,
                                               int unitSeconds, int maxEvents,
                                               Action<TimeOfDay> callback)
        {
            if (!hasListener) return;

            int firstBoundary = startSeconds / unitSeconds + 1;
            int lastBoundary = (startSeconds + elapsedSeconds) / unitSeconds;
            int count = lastBoundary - firstBoundary + 1;
            if (count <= 0) return;

            if (count > maxEvents)
            {
                callback.Invoke(TimeOfDay.FromSeconds(lastBoundary * unitSeconds));
                return;
            }

            for (int i = firstBoundary; i <= lastBoundary; i++)
            {
                callback.Invoke(TimeOfDay.FromSeconds(i * unitSeconds));
            }
        }

        private void CacheBoundaries()
        {
            TimeOfDay time = Time;
            m_LastSecond = time.TotalSeconds;
            m_LastMinute = time.Minute;
            m_LastHour = time.Hour;
        }

        private static double WrapProgress(double progress)
        {
            double wrapped = progress % 1d;
            return wrapped < 0d ? wrapped + 1d : wrapped;
        }
    }
}
