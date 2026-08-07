using NUnit.Framework;
using UnityEngine;

namespace Celestia.Tests
{
    public class CelestialSolverTests
    {
        private const double k_MiamiLatitude = 25.7617;
        private const double k_SummerProgress = 0.25;
        private const double k_SpringProgress = 0.0;
        private const double k_AutumnProgress = 0.5;
        private const double k_WinterProgress = 0.75;
        private const double k_FullMoon = 0.5;
        private const double k_Tolerance = 0.01;

        [Test]
        public void SunDeclination_MatchesAxialTiltAtSolstices()
        {
            Assert.AreEqual(23.44, CelestialSolver.SunDeclination(k_SummerProgress), k_Tolerance);
            Assert.AreEqual(-23.44, CelestialSolver.SunDeclination(k_WinterProgress), k_Tolerance);
            Assert.AreEqual(0.0, CelestialSolver.SunDeclination(k_SpringProgress), k_Tolerance);
            Assert.AreEqual(0.0, CelestialSolver.SunDeclination(k_AutumnProgress), k_Tolerance);
        }

        [Test]
        public void SunNoonAltitude_MatchesReferenceValuesForMiami()
        {
            AssertSunAltitude(0.5, k_SummerProgress, 87.68);
            AssertSunAltitude(0.5, k_SpringProgress, 64.24);
            AssertSunAltitude(0.5, k_AutumnProgress, 64.24);
            AssertSunAltitude(0.5, k_WinterProgress, 40.80);
        }

        [Test]
        public void SunAltitude_IsSymmetricAroundNoon()
        {
            CelestialSolver.SunPosition(9.0 / 24.0, k_SummerProgress, k_MiamiLatitude,
                out double morning, out _);
            CelestialSolver.SunPosition(15.0 / 24.0, k_SummerProgress, k_MiamiLatitude,
                out double afternoon, out _);

            Assert.AreEqual(morning, afternoon, k_Tolerance);
            Assert.AreEqual(49.21, morning, k_Tolerance);
        }

        [Test]
        public void FullMoon_PeaksAtMidnight()
        {
            AssertMoonAltitude(0.0, k_WinterProgress, 87.68);
            AssertMoonAltitude(0.0, k_SummerProgress, 40.80);
            AssertMoonAltitude(0.0, k_SpringProgress, 64.24);
        }

        [Test]
        public void NewMoon_TracksTheSun()
        {
            const double newMoon = 0.0;

            for (int hour = 0; hour < 24; hour += 3)
            {
                double progress = hour / 24.0;

                CelestialSolver.SunPosition(progress, k_SummerProgress, k_MiamiLatitude,
                    out double sunAltitude, out double sunAzimuth);
                CelestialSolver.MoonPosition(progress, k_SummerProgress, newMoon, k_MiamiLatitude,
                    out double moonAltitude, out double moonAzimuth);

                Assert.AreEqual(sunAltitude, moonAltitude, k_Tolerance,
                    $"altitude mismatch at hour {hour}");
                Assert.AreEqual(sunAzimuth, moonAzimuth, k_Tolerance,
                    $"azimuth mismatch at hour {hour}");
            }
        }

        [Test]
        public void FullMoon_IsOppositeTheSun()
        {
            for (int hour = 0; hour < 24; hour += 2)
            {
                double progress = hour / 24.0;

                CelestialSolver.SunPosition(progress, k_SummerProgress, k_MiamiLatitude,
                    out double sunAltitude, out _);
                CelestialSolver.MoonPosition(progress, k_SummerProgress, k_FullMoon, k_MiamiLatitude,
                    out double moonAltitude, out _);

                Assert.AreEqual(-sunAltitude, moonAltitude, k_Tolerance,
                    $"full moon should mirror the sun at hour {hour}");
            }
        }

        [Test]
        public void SummerFullMoon_LeavesNoDarkGap()
        {
            for (int minute = 0; minute < 1440; minute += 5)
            {
                double progress = minute / 1440.0;

                CelestialSolver.SunPosition(progress, k_SummerProgress, k_MiamiLatitude,
                    out double sunAltitude, out _);
                CelestialSolver.MoonPosition(progress, k_SummerProgress, k_FullMoon, k_MiamiLatitude,
                    out double moonAltitude, out _);

                Assert.IsTrue(sunAltitude > 0.0 || moonAltitude > 0.0,
                    $"both bodies below horizon at minute {minute}");
            }
        }

        [Test]
        public void ToDirection_RoundTripsAltitudeAndAzimuth()
        {
            for (int minute = 0; minute < 1440; minute += 7)
            {
                double progress = minute / 1440.0;
                CelestialSolver.SunPosition(progress, k_SummerProgress, k_MiamiLatitude,
                    out double altitude, out double azimuth);

                Vector3 direction = CelestialSolver.ToDirection(altitude, azimuth);

                double recoveredAltitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
                double recoveredAzimuth = CelestialSolver.Wrap360(
                    Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);

                double azimuthDelta = System.Math.Abs(recoveredAzimuth - azimuth);
                if (azimuthDelta > 180.0) azimuthDelta = 360.0 - azimuthDelta;

                Assert.AreEqual(altitude, recoveredAltitude, 0.01, $"altitude at minute {minute}");
                Assert.Less(azimuthDelta, 0.01, $"azimuth at minute {minute}");
            }
        }

        [Test]
        public void ToDirection_ReturnsUnitVectors()
        {
            for (int minute = 0; minute < 1440; minute += 30)
            {
                double progress = minute / 1440.0;
                CelestialSolver.SunPosition(progress, k_SummerProgress, k_MiamiLatitude,
                    out double altitude, out double azimuth);

                Vector3 direction = CelestialSolver.ToDirection(altitude, azimuth);
                Assert.AreEqual(1f, direction.magnitude, 0.001f);
            }
        }

        [Test]
        public void MoonIllumination_MatchesPhase()
        {
            Assert.AreEqual(0.0, CelestialSolver.MoonIllumination(0.0), k_Tolerance);
            Assert.AreEqual(0.5, CelestialSolver.MoonIllumination(0.25), k_Tolerance);
            Assert.AreEqual(1.0, CelestialSolver.MoonIllumination(0.5), k_Tolerance);
            Assert.AreEqual(0.5, CelestialSolver.MoonIllumination(0.75), k_Tolerance);
        }

        [Test]
        public void HorizonFade_ClampsAtBoundaries()
        {
            Assert.AreEqual(0f, CelestialSolver.HorizonFade(-5.0, 8.0));
            Assert.AreEqual(0f, CelestialSolver.HorizonFade(0.0, 8.0));
            Assert.AreEqual(1f, CelestialSolver.HorizonFade(8.0, 8.0));
            Assert.AreEqual(1f, CelestialSolver.HorizonFade(45.0, 8.0));

            float midpoint = CelestialSolver.HorizonFade(4.0, 8.0);
            Assert.Greater(midpoint, 0f);
            Assert.Less(midpoint, 1f);
        }

        [Test]
        public void AirMass_MatchesKastenYoungReference()
        {
            Assert.AreEqual(1.0, CelestialSolver.AirMass(90.0), 0.01);
            Assert.AreEqual(2.0, CelestialSolver.AirMass(30.0), 0.05);
            Assert.AreEqual(5.6, CelestialSolver.AirMass(10.0), 0.1);
        }

        [Test]
        public void AirMass_IncreasesTowardTheHorizon()
        {
            Assert.Greater(CelestialSolver.AirMass(5.0), CelestialSolver.AirMass(20.0));
            Assert.Greater(CelestialSolver.AirMass(20.0), CelestialSolver.AirMass(60.0));
        }

        [Test]
        public void AirMass_IsBoundedBelowHorizon()
        {
            Assert.LessOrEqual(CelestialSolver.AirMass(-30.0), 40.0);
            Assert.Greater(CelestialSolver.AirMass(-30.0), 0.0);
        }

        [Test]
        public void AtmosphericTint_IsWarmAtHorizonAndNeutralAtZenith()
        {
            Assert.Less(CelestialSolver.AtmosphericTint(0.5), 0.05f);
            Assert.Greater(CelestialSolver.AtmosphericTint(90.0), 0.99f);
        }

        [Test]
        public void AtmosphericTint_ClearsQuicklyAboveFifteenDegrees()
        {
            Assert.Greater(CelestialSolver.AtmosphericTint(15.0), 0.5f);
            Assert.Greater(CelestialSolver.AtmosphericTint(40.0), 0.85f);
        }

        [Test]
        public void AtmosphericTint_IsMonotonic()
        {
            float previous = -1f;

            for (int altitude = 0; altitude <= 90; altitude += 5)
            {
                float tint = CelestialSolver.AtmosphericTint(altitude);
                Assert.GreaterOrEqual(tint, previous, $"tint dropped at {altitude}°");
                previous = tint;
            }
        }

        [Test]
        public void AtmosphericTint_StaysWithinUnitRange()
        {
            for (int altitude = -20; altitude <= 90; altitude += 5)
            {
                float tint = CelestialSolver.AtmosphericTint(altitude);
                Assert.GreaterOrEqual(tint, 0f);
                Assert.LessOrEqual(tint, 1f);
            }
        }

        [Test]
        public void ShadowLengthRatio_ShrinksAsSunClimbs()
        {
            double low = CelestialSolver.ShadowLengthRatio(10.0);
            double high = CelestialSolver.ShadowLengthRatio(80.0);

            Assert.Greater(low, high);
            Assert.AreEqual(5.67, low, 0.05);
            Assert.IsTrue(double.IsPositiveInfinity(CelestialSolver.ShadowLengthRatio(0.0)));
        }

        [Test]
        public void Wrap360_NormalizesNegativeAndLargeValues()
        {
            Assert.AreEqual(350.0, CelestialSolver.Wrap360(-10.0), k_Tolerance);
            Assert.AreEqual(10.0, CelestialSolver.Wrap360(370.0), k_Tolerance);
            Assert.AreEqual(0.0, CelestialSolver.Wrap360(720.0), k_Tolerance);
        }

        [Test]
        public void NormalizeSigned_MapsToMinusOneEightyToOneEighty()
        {
            Assert.AreEqual(-10.0, CelestialSolver.NormalizeSigned(350.0), k_Tolerance);
            Assert.AreEqual(10.0, CelestialSolver.NormalizeSigned(10.0), k_Tolerance);
            Assert.AreEqual(-180.0, CelestialSolver.NormalizeSigned(180.0), k_Tolerance);
        }

        private static void AssertSunAltitude(double dayProgress, double yearProgress, double expected)
        {
            CelestialSolver.SunPosition(dayProgress, yearProgress, k_MiamiLatitude,
                out double altitude, out _);
            Assert.AreEqual(expected, altitude, k_Tolerance);
        }

        private static void AssertMoonAltitude(double dayProgress, double yearProgress, double expected)
        {
            CelestialSolver.MoonPosition(dayProgress, yearProgress, k_FullMoon, k_MiamiLatitude,
                out double altitude, out _);
            Assert.AreEqual(expected, altitude, k_Tolerance);
        }
    }
}
