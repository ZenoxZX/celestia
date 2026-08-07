using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Celestia.Tests
{
    public class WorldClockTests
    {
        private GameObject m_Host;
        private WorldClock m_Clock;

        [SetUp]
        public void SetUp()
        {
            m_Host = new GameObject("WorldClockTestHost");
            m_Clock = m_Host.AddComponent<WorldClock>();
            m_Clock.TickMode = ClockTickMode.ExternalTick;
            m_Clock.RealSecondsPerDay = TimeOfDay.SecondsPerDay;
            m_Clock.TimeScale = 1f;
            m_Clock.SetProgress(0f);
            m_Clock.Play();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Host);
        }

        [Test]
        public void SetProgress_UpdatesTime()
        {
            m_Clock.SetProgress(0.5f);

            Assert.AreEqual(0.5f, m_Clock.DayProgress, 0.0001f);
            Assert.AreEqual(12, m_Clock.Time.Hour);
        }

        [Test]
        public void SetTime_UpdatesProgress()
        {
            m_Clock.SetTime(18, 0);

            Assert.AreEqual(0.75f, m_Clock.DayProgress, 0.0001f);
            Assert.AreEqual(18, m_Clock.Time.Hour);
        }

        [Test]
        public void SetProgress_WrapsOutOfRangeValues()
        {
            m_Clock.SetProgress(1.25f);
            Assert.AreEqual(0.25f, m_Clock.DayProgress, 0.0001f);

            m_Clock.SetProgress(-0.25f);
            Assert.AreEqual(0.75f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void Tick_AdvancesProgress()
        {
            m_Clock.Tick(TimeOfDay.SecondsPerHour);

            Assert.AreEqual(1f / 24f, m_Clock.DayProgress, 0.0001f);
            Assert.AreEqual(1, m_Clock.Time.Hour);
        }

        [Test]
        public void Tick_RespectsTimeScale()
        {
            m_Clock.TimeScale = 2f;
            m_Clock.Tick(TimeOfDay.SecondsPerHour);

            Assert.AreEqual(2f / 24f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void Tick_DoesNothingWhilePaused()
        {
            m_Clock.Pause();
            m_Clock.Tick(TimeOfDay.SecondsPerHour);

            Assert.AreEqual(0f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void Tick_DoesNothingWithZeroTimeScale()
        {
            m_Clock.TimeScale = 0f;
            m_Clock.Tick(TimeOfDay.SecondsPerHour);

            Assert.AreEqual(0f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void Progress_WrapsAtEndOfDay()
        {
            m_Clock.SetProgress(0.99f);
            m_Clock.Tick(TimeOfDay.SecondsPerDay * 0.02f);

            Assert.Less(m_Clock.DayProgress, 0.5f);
            Assert.AreEqual(0.01f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void DayElapsed_FiresOnWrap()
        {
            int dayCount = 0;
            m_Clock.DayElapsed += _ => dayCount++;

            m_Clock.SetProgress(0.9f);
            m_Clock.Tick(TimeOfDay.SecondsPerDay * 0.2f);

            Assert.AreEqual(1, dayCount);
            Assert.AreEqual(1, m_Clock.DayCount);
        }

        [Test]
        public void DayElapsed_FiresOncePerWrappedDay()
        {
            int dayCount = 0;
            m_Clock.DayElapsed += _ => dayCount++;

            m_Clock.Tick(TimeOfDay.SecondsPerDay * 3.5f);

            Assert.AreEqual(3, dayCount);
            Assert.AreEqual(3, m_Clock.DayCount);
        }

        [Test]
        public void HourChanged_FiresForEachCrossedHour()
        {
            var hours = new List<int>();
            m_Clock.HourChanged += time => hours.Add(time.Hour);

            m_Clock.Tick(TimeOfDay.SecondsPerHour * 3);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, hours);
        }

        [Test]
        public void MinuteChanged_FiresForEachCrossedMinute()
        {
            int minutes = 0;
            m_Clock.MinuteChanged += _ => minutes++;

            m_Clock.Tick(TimeOfDay.SecondsPerMinute * 5);

            Assert.AreEqual(5, minutes);
        }

        [Test]
        public void SecondChanged_FiresForEachCrossedSecond()
        {
            int seconds = 0;
            m_Clock.SecondChanged += _ => seconds++;

            m_Clock.Tick(10f);

            Assert.AreEqual(10, seconds);
        }

        [Test]
        public void BoundaryEvents_DoNotFireWithoutElapsedTime()
        {
            int seconds = 0;
            m_Clock.SecondChanged += _ => seconds++;

            m_Clock.Tick(0f);

            Assert.AreEqual(0, seconds);
        }

        [Test]
        public void ProgressChanged_FiresOnTick()
        {
            float reported = -1f;
            m_Clock.ProgressChanged += p => reported = p;

            m_Clock.Tick(TimeOfDay.SecondsPerHour * 6);

            Assert.AreEqual(0.25f, reported, 0.0001f);
        }

        [Test]
        public void ProgressChanged_FiresOnSetProgress()
        {
            float reported = -1f;
            m_Clock.ProgressChanged += p => reported = p;

            m_Clock.SetProgress(0.3f);

            Assert.AreEqual(0.3f, reported, 0.0001f);
        }

        [Test]
        public void RunStateChanged_FiresOnPauseAndPlay()
        {
            var states = new List<bool>();
            m_Clock.RunStateChanged += s => states.Add(s);

            m_Clock.Pause();
            m_Clock.Play();

            CollectionAssert.AreEqual(new[] { false, true }, states);
        }

        [Test]
        public void RunStateChanged_DoesNotFireForRedundantCalls()
        {
            int calls = 0;
            m_Clock.RunStateChanged += _ => calls++;

            m_Clock.Play();
            m_Clock.Play();

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void StepHours_AdvancesRegardlessOfTimeScale()
        {
            m_Clock.TimeScale = 0f;
            m_Clock.StepHours(6f);

            Assert.AreEqual(0.25f, m_Clock.DayProgress, 0.0001f);
        }

        [Test]
        public void StepMinutes_AdvancesByExactAmount()
        {
            m_Clock.StepMinutes(90f);

            Assert.AreEqual(1, m_Clock.Time.Hour);
            Assert.AreEqual(30, m_Clock.Time.Minute);
        }

        [Test]
        public void LargeTick_CoalescesSecondEvents()
        {
            int seconds = 0;
            m_Clock.SecondChanged += _ => seconds++;

            m_Clock.Tick(TimeOfDay.SecondsPerDay * 2f);

            Assert.LessOrEqual(seconds, 1);
            Assert.AreEqual(2, m_Clock.DayCount);
        }

        [Test]
        public void LargeTick_StillEmitsHourEvents()
        {
            int hours = 0;
            m_Clock.HourChanged += _ => hours++;

            m_Clock.Tick(TimeOfDay.SecondsPerDay * 2f);

            Assert.Greater(hours, 0);
            Assert.LessOrEqual(hours, TimeOfDay.HoursPerDay);
        }

        [Test]
        public void FastForward_KeepsEveryHourBoundary()
        {
            var hours = new List<int>();
            m_Clock.HourChanged += time => hours.Add(time.Hour);

            m_Clock.Tick(TimeOfDay.SecondsPerHour * 6);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, hours);
        }

        [Test]
        public void FastForward_KeepsEveryMinuteBoundary()
        {
            int minutes = 0;
            m_Clock.MinuteChanged += _ => minutes++;

            m_Clock.Tick(TimeOfDay.SecondsPerHour * 2);

            Assert.AreEqual(120, minutes);
        }

        [Test]
        public void HourChanged_WrapsAcrossMidnight()
        {
            var hours = new List<int>();
            m_Clock.SetTime(22, 0);
            m_Clock.HourChanged += time => hours.Add(time.Hour);

            m_Clock.Tick(TimeOfDay.SecondsPerHour * 3);

            CollectionAssert.AreEqual(new[] { 23, 0, 1 }, hours);
        }

        [Test]
        public void Toggle_FlipsRunState()
        {
            Assert.IsTrue(m_Clock.IsRunning);

            m_Clock.Toggle();
            Assert.IsFalse(m_Clock.IsRunning);

            m_Clock.Toggle();
            Assert.IsTrue(m_Clock.IsRunning);
        }
    }
}
