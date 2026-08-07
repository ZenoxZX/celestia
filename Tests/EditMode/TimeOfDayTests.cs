using NUnit.Framework;

namespace Celestia.Tests
{
    public class TimeOfDayTests
    {
        [Test]
        public void Constructor_StoresComponents()
        {
            var time = new TimeOfDay(19, 30, 15);

            Assert.AreEqual(19, time.Hour);
            Assert.AreEqual(30, time.Minute);
            Assert.AreEqual(15, time.Second);
        }

        [Test]
        public void Constructor_WrapsOverflowingComponents()
        {
            var time = new TimeOfDay(25, 0);
            Assert.AreEqual(1, time.Hour);

            var rolled = new TimeOfDay(23, 59, 60);
            Assert.AreEqual(0, rolled.Hour);
            Assert.AreEqual(0, rolled.Minute);
            Assert.AreEqual(0, rolled.Second);
        }

        [Test]
        public void Constructor_WrapsNegativeValues()
        {
            var time = new TimeOfDay(-1, 0);
            Assert.AreEqual(23, time.Hour);
        }

        [Test]
        public void Progress_MapsMidnightAndNoon()
        {
            Assert.AreEqual(0f, new TimeOfDay(0, 0).Progress, 0.0001f);
            Assert.AreEqual(0.5f, new TimeOfDay(12, 0).Progress, 0.0001f);
            Assert.AreEqual(0.75f, new TimeOfDay(18, 0).Progress, 0.0001f);
        }

        [Test]
        public void FromProgress_RoundTrips()
        {
            for (int hour = 0; hour < 24; hour++)
            {
                var original = new TimeOfDay(hour, 0);
                var restored = TimeOfDay.FromProgress(original.Progress);
                Assert.AreEqual(original, restored, $"hour {hour}");
            }
        }

        [Test]
        public void FromProgress_WrapsOutOfRangeInput()
        {
            Assert.AreEqual(new TimeOfDay(0, 0), TimeOfDay.FromProgress(0f));
            Assert.AreEqual(new TimeOfDay(0, 0), TimeOfDay.FromProgress(1f));
            Assert.AreEqual(new TimeOfDay(12, 0), TimeOfDay.FromProgress(1.5f));
            Assert.AreEqual(new TimeOfDay(12, 0), TimeOfDay.FromProgress(-0.5f));
        }

        [Test]
        public void FromHours_ConvertsFractionalHours()
        {
            var time = TimeOfDay.FromHours(6.5f);
            Assert.AreEqual(6, time.Hour);
            Assert.AreEqual(30, time.Minute);
        }

        [Test]
        public void Comparison_OrdersByTotalSeconds()
        {
            var morning = new TimeOfDay(8, 0);
            var evening = new TimeOfDay(20, 0);

            Assert.IsTrue(morning < evening);
            Assert.IsTrue(evening > morning);
            Assert.IsTrue(morning != evening);
            Assert.IsTrue(morning == new TimeOfDay(8, 0));
        }

        [Test]
        public void ToString_UsesZeroPaddedFormat()
        {
            Assert.AreEqual("09:05:03", new TimeOfDay(9, 5, 3).ToString());
            Assert.AreEqual("09:05", new TimeOfDay(9, 5, 3).ToShortString());
        }
    }
}
