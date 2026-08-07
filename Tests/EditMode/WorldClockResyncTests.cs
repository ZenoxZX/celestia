using System.Collections.Generic;
using NUnit.Framework;

namespace Celestia.Tests
{
    public class WorldClockResyncTests
    {
        private WorldClock m_Clock;

        [SetUp]
        public void SetUp()
        {
            m_Clock = new WorldClock(TimeOfDay.SecondsPerDay, 0.5f);
            m_Clock.TimeScale = 1f;
        }

        [Test]
        public void SetProgress_DefaultsToResync()
        {
            int advanced = 0;
            int resynced = 0;

            m_Clock.Advanced += _ => advanced++;
            m_Clock.Resynced += _ => resynced++;

            m_Clock.SetProgress(0.27f);

            Assert.AreEqual(0, advanced, "resync must not replay the span");
            Assert.AreEqual(1, resynced);
            Assert.AreEqual(0.27f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void Resync_DoesNotEmitBoundaryEvents()
        {
            int hours = 0;
            int minutes = 0;

            m_Clock.HourChanged += _ => hours++;
            m_Clock.MinuteChanged += _ => minutes++;

            m_Clock.SetProgress(0.27f);

            Assert.AreEqual(0, hours);
            Assert.AreEqual(0, minutes);
        }

        [Test]
        public void Resync_ReportsTheNewProgress()
        {
            float reported = -1f;
            m_Clock.Resynced += p => reported = p;

            m_Clock.SetProgress(0.27f);

            Assert.AreEqual(0.27f, reported, 0.0001f);
        }

        [Test]
        public void Replay_AdvancesThroughTheSpan()
        {
            int advanced = 0;
            int resynced = 0;
            var hours = new List<int>();

            m_Clock.Advanced += _ => advanced++;
            m_Clock.Resynced += _ => resynced++;
            m_Clock.HourChanged += time => hours.Add(time.Hour);

            m_Clock.SetProgress(0.75f, TimeChangeMode.Replay);

            Assert.AreEqual(1, advanced);
            Assert.AreEqual(0, resynced);
            CollectionAssert.AreEqual(new[] { 13, 14, 15, 16, 17, 18 }, hours);
            Assert.AreEqual(0.75f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void Replay_WrapsAcrossMidnight()
        {
            m_Clock.SetProgress(0.9f);

            int days = 0;
            m_Clock.DayElapsed += _ => days++;

            m_Clock.SetProgress(0.1f, TimeChangeMode.Replay);

            Assert.AreEqual(1, days);
            Assert.AreEqual(0.1f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void Resync_WorksWhilePaused()
        {
            m_Clock.Pause();

            int resynced = 0;
            m_Clock.Resynced += _ => resynced++;

            m_Clock.SetProgress(0.1f);

            Assert.AreEqual(1, resynced);
            Assert.AreEqual(0.1f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void PauseThenResume_ContinuesFromTheResyncedTime()
        {
            m_Clock.Play();
            m_Clock.Tick(TimeOfDay.SecondsPerHour);

            m_Clock.SetProgress(0.27f);
            m_Clock.Pause();
            m_Clock.Tick(TimeOfDay.SecondsPerHour);

            Assert.AreEqual(0.27f, m_Clock.DayProgress, 0.0001f, "paused clock must not advance");

            m_Clock.Play();
            m_Clock.Tick(TimeOfDay.SecondsPerHour);

            Assert.Greater(m_Clock.DayProgress, 0.27f);
        }

        [Test]
        public void Resync_RealignsRangeScheduleWithoutFiring()
        {
            var schedule = CelestialSchedule.Between(
                new TimeOfDay(19, 0), new TimeOfDay(6, 0), null, null);

            int entered = 0;
            int exited = 0;
            schedule.Fired += () => entered++;
            schedule.Left += () => exited++;

            schedule.CatchUpOnEnable = false;
            schedule.Prime(0.5f, _ => -1f);
            Assert.IsFalse(schedule.IsInside, "noon sits outside the night range");

            schedule.Resync(new TimeOfDay(23, 0).Progress);

            Assert.IsTrue(schedule.IsInside, "state must follow the jump");
            Assert.AreEqual(0, entered, "a jump is not a transition");
            Assert.AreEqual(0, exited);
        }
    }
}
