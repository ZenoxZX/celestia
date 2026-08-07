using UnityEngine;

namespace Celestia
{
    public readonly struct ClockAdvance
    {
        public readonly float From;
        public readonly float To;
        public readonly float Delta;
        public readonly int DaysElapsed;

        private readonly double m_From;
        private readonly double m_Delta;

        public ClockAdvance(double from, double to, double delta, int daysElapsed)
        {
            m_From = from;
            m_Delta = delta;

            From = (float)from;
            To = (float)to;
            Delta = (float)delta;
            DaysElapsed = daysElapsed;
        }

        public bool WrappedMidnight => DaysElapsed > 0 || To < From;

        public int CountBoundaries(double unitsPerDay)
        {
            if (m_Delta <= 0d || unitsPerDay <= 0d)
                return 0;

            double start = m_From * unitsPerDay;
            double end = start + m_Delta * unitsPerDay;

            int steps = (int)System.Math.Floor(end) - (int)System.Math.Floor(start);
            return steps > 0 ? steps : 0;
        }

        public bool Covers(float progress)
        {
            if (Delta <= 0f)
                return false;

            if (Delta >= 1f)
                return true;

            float wrapped = Mathf.Repeat(progress, 1f);

            if (From <= To)
                return wrapped > From && wrapped <= To;

            // Crossed midnight: the day boundary itself was passed, so a target
            // sitting exactly on 0 counts even though it is never strictly
            // greater than From.
            if (wrapped <= 0f)
                return true;

            return wrapped > From || wrapped <= To;
        }
    }
}
