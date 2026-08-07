using NUnit.Framework;

namespace Celestia.Tests
{
    public class ClockAdvanceTests
    {
        private const float k_FrameStep = 1f / (120f * 60f);

        [Test]
        public void Covers_UsesHalfOpenInterval()
        {
            var advance = new ClockAdvance(0.2, 0.3, 0.1, 0);

            Assert.IsFalse(advance.Covers(0.2f), "start is excluded");
            Assert.IsTrue(advance.Covers(0.25f));
            Assert.IsTrue(advance.Covers(0.3f), "end is included");
            Assert.IsFalse(advance.Covers(0.35f));
        }

        [Test]
        public void Covers_HandlesMidnightWrap()
        {
            var advance = new ClockAdvance(0.95, 0.05, 0.1, 1);

            Assert.IsTrue(advance.Covers(0.98f));
            Assert.IsTrue(advance.Covers(0.02f));
            Assert.IsFalse(advance.Covers(0.5f));
        }

        [Test]
        public void Covers_IncludesMidnightItselfOnWrap()
        {
            var advance = new ClockAdvance(0.999, 0.001, 0.002, 1);

            Assert.IsTrue(advance.Covers(0f), "a target sitting on 00:00 must fire");
        }

        [Test]
        public void Covers_ReturnsFalseForZeroDelta()
        {
            var advance = new ClockAdvance(0.3, 0.3, 0.0, 0);
            Assert.IsFalse(advance.Covers(0.3f));
        }

        [Test]
        public void Covers_ReturnsTrueWhenAFullDayPassed()
        {
            var advance = new ClockAdvance(0.3, 0.3, 1.5, 1);
            Assert.IsTrue(advance.Covers(0.7f));
        }

        [Test]
        public void Covers_FiresOncePerDayAtAnyStepSize()
        {
            float[] steps = { k_FrameStep, 1f / (20f * 60f), 1f / (2f * 30f), 0.5f };
            float target = new TimeOfDay(19, 42).Progress;

            foreach (float step in steps)
            {
                Assert.AreEqual(1, CountFires(target, step, 1), $"step {step}");
            }
        }

        [Test]
        public void Covers_FiresMidnightOncePerDay()
        {
            Assert.AreEqual(1, CountFires(0f, k_FrameStep, 1));
            Assert.AreEqual(3, CountFires(0f, k_FrameStep, 3));
        }

        [Test]
        public void CountBoundaries_CountsEveryHourAcrossADay()
        {
            Assert.AreEqual(24, CountBoundaries(TimeOfDay.HoursPerDay, k_FrameStep, 1));
        }

        [Test]
        public void CountBoundaries_CountsEveryMinuteAcrossADay()
        {
            int perDay = TimeOfDay.MinutesPerHour * TimeOfDay.HoursPerDay;
            Assert.AreEqual(perDay, CountBoundaries(perDay, k_FrameStep, 1));
        }

        [Test]
        public void CountBoundaries_SurvivesLargeSteps()
        {
            Assert.AreEqual(24, CountBoundaries(TimeOfDay.HoursPerDay, 0.5f, 1));
        }

        [Test]
        public void CountBoundaries_ReturnsZeroForIdleAdvance()
        {
            var advance = new ClockAdvance(0.4, 0.4, 0.0, 0);
            Assert.AreEqual(0, advance.CountBoundaries(TimeOfDay.HoursPerDay));
        }

        private static int CountFires(float target, double step, int days)
        {
            int fires = 0;
            double progress = 0d;
            int steps = (int)System.Math.Ceiling(days / step);

            for (int i = 0; i < steps; i++)
            {
                if (Step(ref progress, step).Covers(target)) fires++;
            }

            return fires;
        }

        private static int CountBoundaries(double unitsPerDay, double step, int days)
        {
            int total = 0;
            double progress = 0d;
            int steps = (int)System.Math.Ceiling(days / step);

            for (int i = 0; i < steps; i++)
            {
                total += Step(ref progress, step).CountBoundaries(unitsPerDay);
            }

            return total;
        }

        private static ClockAdvance Step(ref double progress, double step)
        {
            double from = progress;
            double target = progress + step;

            int rollovers = (int)System.Math.Floor(target);
            if (rollovers != 0)
            {
                target -= rollovers;
                if (target < 0d) target += 1d;
            }

            progress = target;
            return new ClockAdvance(from, target, step, System.Math.Max(0, rollovers));
        }
    }
}
