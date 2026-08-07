using System;
using UnityEngine;

namespace Celestia
{
    [AddComponentMenu("Celestia/World Clock")]
    [DisallowMultipleComponent]
    public class WorldClock : MonoBehaviour
    {
        private const float k_MinRealSecondsPerDay = 0.01f;
        private const int k_MaxSecondEventsPerTick = 600;
        private const int k_MaxMinuteEventsPerTick = TimeOfDay.MinutesPerHour * TimeOfDay.HoursPerDay;
        private const int k_MaxHourEventsPerTick = TimeOfDay.HoursPerDay;

        [Header("Tick")]
        [SerializeField] private ClockTickMode m_TickMode = ClockTickMode.SelfTick;
        [SerializeField] private bool m_PlayOnAwake = true;
        [SerializeField] private bool m_UseUnscaledTime;

        [Header("Speed")]
        [SerializeField, Min(k_MinRealSecondsPerDay)] private float m_RealSecondsPerDay = 120f;
        [SerializeField, Min(0f)] private float m_TimeScale = 1f;

        [Header("Start")]
        [SerializeField, Range(0f, 1f)] private float m_StartProgress = 0.20834f;

        private double m_Progress;
        private int m_DayCount;
        private bool m_IsRunning;
        private int m_LastSecond = -1;
        private int m_LastMinute = -1;
        private int m_LastHour = -1;

        public static WorldClock Active => s_Active;

        private static WorldClock s_Active;

        public event Action<float> ProgressChanged;
        public event Action<ClockAdvance> Advanced;
        public event Action<TimeOfDay> SecondChanged;
        public event Action<TimeOfDay> MinuteChanged;
        public event Action<TimeOfDay> HourChanged;
        public event Action<int> DayElapsed;
        public event Action<bool> RunStateChanged;

        public float DayProgress => (float)m_Progress;

        public TimeOfDay Time => TimeOfDay.FromProgress((float)m_Progress);

        public int DayCount => m_DayCount;

        public bool IsRunning => m_IsRunning;

        public ClockTickMode TickMode
        {
            get => m_TickMode;
            set => m_TickMode = value;
        }

        public float TimeScale
        {
            get => m_TimeScale;
            set => m_TimeScale = Mathf.Max(0f, value);
        }

        public float RealSecondsPerDay
        {
            get => m_RealSecondsPerDay;
            set => m_RealSecondsPerDay = Mathf.Max(k_MinRealSecondsPerDay, value);
        }

        private void Awake()
        {
            m_Progress = Mathf.Repeat(m_StartProgress, 1f);
            m_IsRunning = m_PlayOnAwake;
            CacheBoundaries();
        }

        private void OnEnable()
        {
            if (s_Active == null) s_Active = this;
        }

        private void OnDisable()
        {
            if (s_Active == this) s_Active = null;
        }

        private void Update()
        {
            if (m_TickMode != ClockTickMode.SelfTick) return;

            float delta = m_UseUnscaledTime
                ? UnityEngine.Time.unscaledDeltaTime
                : UnityEngine.Time.deltaTime;

            Tick(delta);
        }

        public void Play()
        {
            if (m_IsRunning) return;
            m_IsRunning = true;
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

        public void SetProgress(float progress)
        {
            double wrapped = WrapProgress(progress);
            m_Progress = wrapped;
            CacheBoundaries();
            ProgressChanged?.Invoke((float)m_Progress);
        }

        public void SetTime(int hour, int minute, int second = 0)
        {
            SetProgress(new TimeOfDay(hour, minute, second).Progress);
        }

        public void SetTime(TimeOfDay time)
        {
            SetProgress(time.Progress);
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_RealSecondsPerDay = Mathf.Max(k_MinRealSecondsPerDay, m_RealSecondsPerDay);
            m_TimeScale = Mathf.Max(0f, m_TimeScale);

            if (!Application.isPlaying)
            {
                m_Progress = Mathf.Repeat(m_StartProgress, 1f);
            }
        }
#endif
    }
}
