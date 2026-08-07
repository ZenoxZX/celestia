using System;
using UnityEngine;

namespace Celestia
{
    [AddComponentMenu("Celestia/Celestial Handler")]
    [DisallowMultipleComponent]
    public class CelestialHandler : MonoBehaviour
    {
        [SerializeField] private WorldClock m_Clock;
        [SerializeField] private CelestialPreset m_Preset;

        [Header("Editor Preview")]
        [Tooltip("Progress used for gizmos and inspector preview while not playing.")]
        [SerializeField, Range(0f, 1f)] private float m_PreviewProgress = 0.5f;

        private CelestialState m_State;

        public event Action<CelestialState> StateChanged;

        public CelestialState State => m_State;

        public CelestialPreset Preset
        {
            get => m_Preset;
            set
            {
                m_Preset = value;
                Refresh();
            }
        }

        private void OnEnable()
        {
            if (m_Clock == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialHandler)} on '{name}' has no clock assigned.", this);
                return;
            }

            m_Clock.ProgressChanged += OnProgressChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (m_Clock == null) return;
            m_Clock.ProgressChanged -= OnProgressChanged;
        }

        public void Refresh()
        {
            float progress = m_Clock != null && Application.isPlaying
                ? m_Clock.DayProgress
                : m_PreviewProgress;

            Evaluate(progress);
        }

        public CelestialState Evaluate(float dayProgress)
        {
            if (m_Preset == null)
            {
                Debug.LogError($"{nameof(CelestialHandler)} on '{name}' has no preset assigned.", this);
                
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

            CelestialSolver.MoonPosition(dayProgress, preset.YearProgress, preset.MoonPhase, preset.Latitude,
                out double moonAltitude, out double moonAzimuth);

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

        private void OnProgressChanged(float progress)
        {
            Evaluate(progress);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            if (m_Preset == null) return;

            m_State = Sample(m_Preset, m_PreviewProgress);
        }

        public float PreviewProgress => m_PreviewProgress;
#endif
    }
}
