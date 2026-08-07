using System;

namespace Celestia
{
    public interface IWorldClock
    {
        float DayProgress { get; }

        TimeOfDay Time { get; }

        int DayCount { get; }

        bool IsRunning { get; }

        float TimeScale { get; set; }

        event Action<float> ProgressChanged;

        event Action<ClockAdvance> Advanced;

        event Action<float> Resynced;

        event Action<TimeOfDay> SecondChanged;

        event Action<TimeOfDay> MinuteChanged;

        event Action<TimeOfDay> HourChanged;

        event Action<int> DayElapsed;

        event Action<bool> RunStateChanged;

        void Play();

        void Pause();

        void Toggle();

        void Tick(float deltaSeconds);

        void SetProgress(float progress, TimeChangeMode mode = TimeChangeMode.Resync);

        void SetTime(TimeOfDay time, TimeChangeMode mode = TimeChangeMode.Resync);

        void SetTime(int hour, int minute, int second = 0,
                     TimeChangeMode mode = TimeChangeMode.Resync);

        void StepSeconds(float seconds);

        void StepMinutes(float minutes);

        void StepHours(float hours);
    }
}
