using System;
using UnityEngine;

namespace Celestia
{
    public static class CelestialSolver
    {
        public const double AxialTiltDegrees = 23.4397;
        public const double SynodicMonthDays = 29.530588853;

        private const double k_Deg2Rad = Math.PI / 180.0;
        private const double k_Rad2Deg = 180.0 / Math.PI;
        private const double k_Obliquity = AxialTiltDegrees * k_Deg2Rad;
        private const double k_DivisionGuard = 1e-9;

        private const double k_KastenYoungA = 0.50572;
        private const double k_KastenYoungB = 6.07995;
        private const double k_KastenYoungC = 1.6364;
        private const double k_AirMassFloor = 6.0;
        private const double k_MaxAirMass = 40.0;

        private const double k_RayleighBlue = 0.23;
        private const double k_RayleighRed = 0.055;
        private const double k_TintFloor = 0.0;
        private const double k_TintCeiling = 0.84;

        private const double k_AstronomicalNightDegrees = -18.0;
        private const double k_ZenithDegrees = 90.0;

        public static double SunEclipticLongitude(double yearProgress)
        {
            return Wrap360(yearProgress * 360.0);
        }

        public static double SunDeclination(double yearProgress)
        {
            return EclipticToDeclination(SunEclipticLongitude(yearProgress));
        }

        public static double MoonEclipticLongitude(double yearProgress, double moonPhase)
        {
            return Wrap360(SunEclipticLongitude(yearProgress) + moonPhase * 360.0);
        }

        public static double EclipticToDeclination(double longitudeDegrees, double latitudeDegrees = 0.0)
        {
            double lon = longitudeDegrees * k_Deg2Rad;
            double lat = latitudeDegrees * k_Deg2Rad;
            double sinDecl = Math.Sin(lat) * Math.Cos(k_Obliquity)
                           + Math.Cos(lat) * Math.Sin(k_Obliquity) * Math.Sin(lon);
            return Math.Asin(Clamp(sinDecl)) * k_Rad2Deg;
        }

        public static double EclipticToRightAscension(double longitudeDegrees, double latitudeDegrees = 0.0)
        {
            double lon = longitudeDegrees * k_Deg2Rad;
            double lat = latitudeDegrees * k_Deg2Rad;
            double y = Math.Sin(lon) * Math.Cos(k_Obliquity) - Math.Tan(lat) * Math.Sin(k_Obliquity);
            double x = Math.Cos(lon);
            return Wrap360(Math.Atan2(y, x) * k_Rad2Deg);
        }

        public static void Horizontal(double declinationDegrees, double hourAngleDegrees,
                                      double latitudeDegrees,
                                      out double altitudeDegrees, out double azimuthDegrees)
        {
            double decl = declinationDegrees * k_Deg2Rad;
            double hourAngle = hourAngleDegrees * k_Deg2Rad;
            double lat = latitudeDegrees * k_Deg2Rad;

            double sinAlt = Math.Sin(lat) * Math.Sin(decl)
                          + Math.Cos(lat) * Math.Cos(decl) * Math.Cos(hourAngle);
            double altitude = Math.Asin(Clamp(sinAlt));

            double cosAz = (Math.Sin(decl) - Math.Sin(altitude) * Math.Sin(lat))
                         / (Math.Cos(altitude) * Math.Cos(lat) + k_DivisionGuard);
            double azimuth = Math.Acos(Clamp(cosAz));

            if (Math.Sin(hourAngle) > 0.0) azimuth = 2.0 * Math.PI - azimuth;

            altitudeDegrees = altitude * k_Rad2Deg;
            azimuthDegrees = azimuth * k_Rad2Deg;
        }

        public static void SunPosition(double dayProgress, double yearProgress, double latitudeDegrees,
                                       out double altitudeDegrees, out double azimuthDegrees)
        {
            double declination = SunDeclination(yearProgress);
            double hourAngle = HourAngleFromProgress(dayProgress);
            Horizontal(declination, hourAngle, latitudeDegrees, out altitudeDegrees, out azimuthDegrees);
        }

        public static void MoonPosition(double dayProgress, double yearProgress, double moonPhase,
                                        double latitudeDegrees,
                                        out double altitudeDegrees, out double azimuthDegrees)
        {
            double sunLongitude = SunEclipticLongitude(yearProgress);
            double moonLongitude = MoonEclipticLongitude(yearProgress, moonPhase);

            double declination = EclipticToDeclination(moonLongitude);
            double rightAscensionOffset = NormalizeSigned(
                EclipticToRightAscension(moonLongitude) - EclipticToRightAscension(sunLongitude));

            double hourAngle = HourAngleFromProgress(dayProgress) - rightAscensionOffset;
            Horizontal(declination, hourAngle, latitudeDegrees, out altitudeDegrees, out azimuthDegrees);
        }

        public static double MoonIllumination(double moonPhase)
        {
            return (1.0 - Math.Cos(moonPhase * 2.0 * Math.PI)) / 2.0;
        }

        public static Vector3 ToDirection(double altitudeDegrees, double azimuthDegrees)
        {
            double altitude = altitudeDegrees * k_Deg2Rad;
            double azimuth = azimuthDegrees * k_Deg2Rad;
            double horizontal = Math.Cos(altitude);

            return new Vector3(
                (float)(horizontal * Math.Sin(azimuth)),
                (float)Math.Sin(altitude),
                (float)(horizontal * Math.Cos(azimuth)));
        }

        public static float HorizonFade(double altitudeDegrees, double fadeAngleDegrees)
        {
            if (altitudeDegrees <= 0.0) return 0f;
            if (fadeAngleDegrees <= 0.0) return 1f;
            if (altitudeDegrees >= fadeAngleDegrees) return 1f;

            double numerator = Math.Sin(altitudeDegrees * k_Deg2Rad);
            double denominator = Math.Sin(fadeAngleDegrees * k_Deg2Rad);
            if (denominator <= k_DivisionGuard) return 1f;

            return Mathf.Clamp01((float)(numerator / denominator));
        }

        public static double AirMass(double altitudeDegrees)
        {
            if (altitudeDegrees <= -k_AirMassFloor) return k_MaxAirMass;

            double clamped = Math.Max(altitudeDegrees, -k_AirMassFloor);
            double denominator = Math.Sin(clamped * k_Deg2Rad)
                               + k_KastenYoungA * Math.Pow(clamped + k_KastenYoungB, -k_KastenYoungC);

            if (denominator <= 0.0) return k_MaxAirMass;
            return Math.Min(1.0 / denominator, k_MaxAirMass);
        }

        public static float AtmosphericTint(double altitudeDegrees)
        {
            double airMass = AirMass(altitudeDegrees);
            double blue = Math.Exp(-k_RayleighBlue * airMass);
            double red = Math.Exp(-k_RayleighRed * airMass);
            if (red <= 0.0) return 0f;

            double ratio = blue / red;
            double normalized = (ratio - k_TintFloor) / (k_TintCeiling - k_TintFloor);
            return Mathf.Clamp01((float)normalized);
        }

        public static bool SunCrossing(double altitudeDegrees, double yearProgress,
                                       double latitudeDegrees,
                                       out double risingProgress, out double settingProgress)
        {
            double declination = SunDeclination(yearProgress);
            return Crossing(altitudeDegrees, declination, 0.0, latitudeDegrees,
                out risingProgress, out settingProgress);
        }

        public static bool MoonCrossing(double altitudeDegrees, double yearProgress,
                                        double moonPhase, double latitudeDegrees,
                                        out double risingProgress, out double settingProgress)
        {
            double sunLongitude = SunEclipticLongitude(yearProgress);
            double moonLongitude = MoonEclipticLongitude(yearProgress, moonPhase);
            double declination = EclipticToDeclination(moonLongitude);
            double rightAscensionOffset = NormalizeSigned(
                EclipticToRightAscension(moonLongitude) - EclipticToRightAscension(sunLongitude));

            return Crossing(altitudeDegrees, declination, rightAscensionOffset, latitudeDegrees,
                out risingProgress, out settingProgress);
        }

        public static double TransitProgress(double yearProgress, double moonPhase, bool isSun)
        {
            if (isSun) return 0.5;

            double sunLongitude = SunEclipticLongitude(yearProgress);
            double moonLongitude = MoonEclipticLongitude(yearProgress, moonPhase);
            double offset = NormalizeSigned(
                EclipticToRightAscension(moonLongitude) - EclipticToRightAscension(sunLongitude));

            return WrapUnit(0.5 + offset / 360.0);
        }

        public static float SkyPhase(double sunAltitudeDegrees)
        {
            double span = k_ZenithDegrees - k_AstronomicalNightDegrees;
            double normalized = (sunAltitudeDegrees - k_AstronomicalNightDegrees) / span;
            return Mathf.Clamp01((float)normalized);
        }

        public static double ShadowLengthRatio(double altitudeDegrees)
        {
            if (altitudeDegrees <= 0.0) return double.PositiveInfinity;
            return 1.0 / Math.Tan(altitudeDegrees * k_Deg2Rad);
        }

        public static double HourAngleFromProgress(double dayProgress)
        {
            return 15.0 * (dayProgress * TimeOfDay.HoursPerDay - 12.0);
        }

        public static double Wrap360(double degrees)
        {
            double wrapped = degrees % 360.0;
            return wrapped < 0.0 ? wrapped + 360.0 : wrapped;
        }

        public static double NormalizeSigned(double degrees)
        {
            return ((degrees + 540.0) % 360.0) - 180.0;
        }

        private static bool Crossing(double altitudeDegrees, double declinationDegrees,
                                     double rightAscensionOffset, double latitudeDegrees,
                                     out double risingProgress, out double settingProgress)
        {
            double lat = latitudeDegrees * k_Deg2Rad;
            double decl = declinationDegrees * k_Deg2Rad;
            double target = altitudeDegrees * k_Deg2Rad;

            double cosH = (Math.Sin(target) - Math.Sin(lat) * Math.Sin(decl))
                        / (Math.Cos(lat) * Math.Cos(decl) + k_DivisionGuard);

            if (cosH < -1.0 || cosH > 1.0)
            {
                risingProgress = 0.0;
                settingProgress = 0.0;
                return false;
            }

            double hourAngle = Math.Acos(cosH) * k_Rad2Deg;
            double transit = 0.5 + rightAscensionOffset / 360.0;
            double halfSpan = hourAngle / 360.0;

            risingProgress = WrapUnit(transit - halfSpan);
            settingProgress = WrapUnit(transit + halfSpan);
            return true;
        }

        private static double WrapUnit(double value)
        {
            double wrapped = value % 1.0;
            return wrapped < 0.0 ? wrapped + 1.0 : wrapped;
        }

        private static double Clamp(double value)
        {
            if (value < -1.0) return -1.0;
            return value > 1.0 ? 1.0 : value;
        }
    }
}
