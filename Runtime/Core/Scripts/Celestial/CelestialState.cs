using UnityEngine;

namespace Celestia
{
    public readonly struct CelestialState
    {
        public readonly Vector3 SunDirection;
        public readonly Vector3 MoonDirection;
        public readonly float SunAltitude;
        public readonly float SunAzimuth;
        public readonly float MoonAltitude;
        public readonly float MoonAzimuth;
        public readonly float MoonIllumination;
        public readonly float DayProgress;
        public readonly float SkyPhase;

        public CelestialState(Vector3 sunDirection, Vector3 moonDirection,
                              float sunAltitude, float sunAzimuth,
                              float moonAltitude, float moonAzimuth,
                              float moonIllumination, float dayProgress,
                              float skyPhase)
        {
            SkyPhase = skyPhase;
            SunDirection = sunDirection;
            MoonDirection = moonDirection;
            SunAltitude = sunAltitude;
            SunAzimuth = sunAzimuth;
            MoonAltitude = moonAltitude;
            MoonAzimuth = moonAzimuth;
            MoonIllumination = moonIllumination;
            DayProgress = dayProgress;
        }

        public bool IsSunUp => SunAltitude > 0f;

        public bool IsMoonUp => MoonAltitude > 0f;

        public Vector3 SunLightForward => -SunDirection;

        public Vector3 MoonLightForward => -MoonDirection;
    }
}
