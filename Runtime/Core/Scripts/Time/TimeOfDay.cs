using System;
using UnityEngine;

namespace Celestia
{
    [Serializable]
    public struct TimeOfDay : IEquatable<TimeOfDay>, IComparable<TimeOfDay>
    {
        public const int SecondsPerMinute = 60;
        public const int MinutesPerHour = 60;
        public const int HoursPerDay = 24;
        public const int SecondsPerHour = SecondsPerMinute * MinutesPerHour;
        public const int SecondsPerDay = SecondsPerHour * HoursPerDay;

        [SerializeField, Range(0, HoursPerDay - 1)] private int m_Hour;
        [SerializeField, Range(0, MinutesPerHour - 1)] private int m_Minute;
        [SerializeField, Range(0, SecondsPerMinute - 1)] private int m_Second;

        public TimeOfDay(int hour, int minute, int second = 0)
        {
            int total = hour * SecondsPerHour + minute * SecondsPerMinute + second;
            total = WrapSeconds(total);
            m_Hour = total / SecondsPerHour;
            m_Minute = total % SecondsPerHour / SecondsPerMinute;
            m_Second = total % SecondsPerMinute;
        }

        public int Hour => m_Hour;

        public int Minute => m_Minute;

        public int Second => m_Second;

        public int TotalSeconds => m_Hour * SecondsPerHour + m_Minute * SecondsPerMinute + m_Second;

        public float Progress => (float)TotalSeconds / SecondsPerDay;

        public static TimeOfDay FromProgress(float progress)
        {
            float wrapped = Mathf.Repeat(progress, 1f);
            int total = Mathf.FloorToInt(wrapped * SecondsPerDay);
            return FromSeconds(total);
        }

        public static TimeOfDay FromSeconds(int totalSeconds)
        {
            int wrapped = WrapSeconds(totalSeconds);
            return new TimeOfDay(0, 0, wrapped);
        }

        public static TimeOfDay FromHours(float hours)
        {
            return FromProgress(hours / HoursPerDay);
        }

        public bool Equals(TimeOfDay other)
        {
            return TotalSeconds == other.TotalSeconds;
        }

        public override bool Equals(object obj)
        {
            return obj is TimeOfDay other && Equals(other);
        }

        public override int GetHashCode()
        {
            return TotalSeconds;
        }

        public int CompareTo(TimeOfDay other)
        {
            return TotalSeconds.CompareTo(other.TotalSeconds);
        }

        public override string ToString()
        {
            return $"{m_Hour:00}:{m_Minute:00}:{m_Second:00}";
        }

        public string ToShortString()
        {
            return $"{m_Hour:00}:{m_Minute:00}";
        }

        public static bool operator ==(TimeOfDay a, TimeOfDay b) => a.Equals(b);

        public static bool operator !=(TimeOfDay a, TimeOfDay b) => !a.Equals(b);

        public static bool operator <(TimeOfDay a, TimeOfDay b) => a.TotalSeconds < b.TotalSeconds;

        public static bool operator >(TimeOfDay a, TimeOfDay b) => a.TotalSeconds > b.TotalSeconds;

        public static bool operator <=(TimeOfDay a, TimeOfDay b) => a.TotalSeconds <= b.TotalSeconds;

        public static bool operator >=(TimeOfDay a, TimeOfDay b) => a.TotalSeconds >= b.TotalSeconds;

        private static int WrapSeconds(int seconds)
        {
            int wrapped = seconds % SecondsPerDay;
            return wrapped < 0 ? wrapped + SecondsPerDay : wrapped;
        }
    }
}
