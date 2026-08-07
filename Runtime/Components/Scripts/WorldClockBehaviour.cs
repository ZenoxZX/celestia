using System;
using UnityEngine;

namespace Celestia
{
    [AddComponentMenu("Celestia/World Clock")]
    [DisallowMultipleComponent]
    public class WorldClockBehaviour : MonoBehaviour, IWorldClock
    {
        [Header("Tick")]
        [SerializeField] private ClockTickMode m_TickMode = ClockTickMode.SelfTick;
        [SerializeField] private bool m_PlayOnAwake = true;
        [SerializeField] private bool m_UseUnscaledTime;

        [Header("Speed")]
        [SerializeField, Min(WorldClock.MinRealSecondsPerDay)]
        private float m_RealSecondsPerDay = 120f;

        [SerializeField, Min(0f)] private float m_TimeScale = 1f;

        [Header("Start")]
        [SerializeField, Range(0f, 1f)] private float m_StartProgress = 0.20834f;

        private WorldClock m_Clock;

        public static WorldClockBehaviour Active => s_Active;

        private static WorldClockBehaviour s_Active;

        public event Action<float> ProgressChanged
        {
            add => Clock.ProgressChanged += value;
            remove => Clock.ProgressChanged -= value;
        }

        public event Action<ClockAdvance> Advanced
        {
            add => Clock.Advanced += value;
            remove => Clock.Advanced -= value;
        }

        public event Action<float> Resynced
        {
            add => Clock.Resynced += value;
            remove => Clock.Resynced -= value;
        }

        public event Action<TimeOfDay> SecondChanged
        {
            add => Clock.SecondChanged += value;
            remove => Clock.SecondChanged -= value;
        }

        public event Action<TimeOfDay> MinuteChanged
        {
            add => Clock.MinuteChanged += value;
            remove => Clock.MinuteChanged -= value;
        }

        public event Action<TimeOfDay> HourChanged
        {
            add => Clock.HourChanged += value;
            remove => Clock.HourChanged -= value;
        }

        public event Action<int> DayElapsed
        {
            add => Clock.DayElapsed += value;
            remove => Clock.DayElapsed -= value;
        }

        public event Action<bool> RunStateChanged
        {
            add => Clock.RunStateChanged += value;
            remove => Clock.RunStateChanged -= value;
        }

        public WorldClock Clock => m_Clock ??= CreateClock();

        public float DayProgress => Clock.DayProgress;

        public TimeOfDay Time => Clock.Time;

        public int DayCount => Clock.DayCount;

        public bool IsRunning => Clock.IsRunning;

        public ClockTickMode TickMode
        {
            get => m_TickMode;
            set => m_TickMode = value;
        }

        public float TimeScale
        {
            get => Clock.TimeScale;
            set
            {
                m_TimeScale = Mathf.Max(0f, value);
                Clock.TimeScale = m_TimeScale;
            }
        }

        public float RealSecondsPerDay
        {
            get => Clock.RealSecondsPerDay;
            set
            {
                m_RealSecondsPerDay = Mathf.Max(WorldClock.MinRealSecondsPerDay, value);
                Clock.RealSecondsPerDay = m_RealSecondsPerDay;
            }
        }

        private void Awake()
        {
            m_Clock ??= CreateClock();

            // Claimed in Awake, not OnEnable: another component's OnEnable may
            // run first and would otherwise find no active clock.
            if (s_Active == null) s_Active = this;
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

            Clock.Tick(delta);
        }

        public void Play() => Clock.Play();

        public void Pause() => Clock.Pause();

        public void Toggle() => Clock.Toggle();

        public void Tick(float deltaSeconds) => Clock.Tick(deltaSeconds);

        public void StepSeconds(float seconds) => Clock.StepSeconds(seconds);

        public void StepMinutes(float minutes) => Clock.StepMinutes(minutes);

        public void StepHours(float hours) => Clock.StepHours(hours);

        public void SetProgress(float progress, TimeChangeMode mode = TimeChangeMode.Resync)
        {
            Clock.SetProgress(progress, mode);
        }

        public void SetTime(TimeOfDay time, TimeChangeMode mode = TimeChangeMode.Resync)
        {
            Clock.SetTime(time, mode);
        }

        public void SetTime(int hour, int minute, int second = 0,
                            TimeChangeMode mode = TimeChangeMode.Resync)
        {
            Clock.SetTime(hour, minute, second, mode);
        }

        private WorldClock CreateClock()
        {
            var clock = new WorldClock(m_RealSecondsPerDay, m_StartProgress, m_PlayOnAwake)
            {
                TimeScale = m_TimeScale
            };

            return clock;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_RealSecondsPerDay = Mathf.Max(WorldClock.MinRealSecondsPerDay, m_RealSecondsPerDay);
            m_TimeScale = Mathf.Max(0f, m_TimeScale);

            if (m_Clock == null) return;

            m_Clock.RealSecondsPerDay = m_RealSecondsPerDay;
            m_Clock.TimeScale = m_TimeScale;
        }
#endif
    }
}
