using System;
using UnityEngine;

namespace Celestia
{
    public sealed class CelestialEngine : ICelestialSource, IDisposable
    {
        private readonly IWorldClock m_Clock;

        private CelestialPreset m_Preset;
        private CelestialState m_State;
        private bool m_Bound;

        public event Action<CelestialState> StateChanged;

        public CelestialEngine(IWorldClock clock, CelestialPreset preset)
        {
            m_Clock = clock;
            m_Preset = preset;
        }

        public CelestialState State => m_State;

        public CelestialPreset Preset
        {
            get => m_Preset;
            set
            {
                m_Preset = value;
                if (m_Clock != null) Evaluate(m_Clock.DayProgress);
            }
        }

        public void Bind()
        {
            if (m_Bound || m_Clock == null) return;

            m_Clock.ProgressChanged += OnProgressChanged;
            m_Bound = true;

            Evaluate(m_Clock.DayProgress);
        }

        public void Unbind()
        {
            if (!m_Bound) return;

            m_Clock.ProgressChanged -= OnProgressChanged;
            m_Bound = false;
        }

        public CelestialState Evaluate(float dayProgress)
        {
            if (m_Preset == null)
            {
                Debug.LogError($"{nameof(CelestialEngine)} has no {nameof(CelestialPreset)} assigned.");
                return m_State;
            }

            m_State = Sample(m_Preset, dayProgress);
            StateChanged?.Invoke(m_State);
            return m_State;
        }

        public static CelestialState Sample(CelestialPreset preset, float dayProgress)
        {
            CelestialSolver.SunPosition(dayProgress, preset.YearProgress, preset.Latitude,
                out double sunAltitude, out double sunAzimuth);

            CelestialSolver.MoonPosition(dayProgress, preset.YearProgress, preset.MoonPhase,
                preset.Latitude, out double moonAltitude, out double moonAzimuth);

            Vector3 sunDirection = CelestialSolver.ToDirection(sunAltitude, sunAzimuth);
            Vector3 moonDirection = CelestialSolver.ToDirection(moonAltitude, moonAzimuth);
            float illumination = (float)CelestialSolver.MoonIllumination(preset.MoonPhase);

            return new CelestialState(
                sunDirection, moonDirection,
                (float)sunAltitude, (float)sunAzimuth,
                (float)moonAltitude, (float)moonAzimuth,
                illumination, dayProgress,
                CelestialSolver.SkyPhase(sunAltitude));
        }

        void IDisposable.Dispose()
        {
            Unbind();
        }

        private void OnProgressChanged(float progress)
        {
            Evaluate(progress);
        }
    }
}
