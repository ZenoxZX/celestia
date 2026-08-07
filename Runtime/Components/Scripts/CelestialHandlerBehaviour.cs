using System;
using UnityEngine;

namespace Celestia
{
    [AddComponentMenu("Celestia/Celestial Handler")]
    [DisallowMultipleComponent]
    public class CelestialHandlerBehaviour : MonoBehaviour, ICelestialSource
    {
        [SerializeField] private WorldClockBehaviour m_Clock;
        [SerializeField] private CelestialPreset m_Preset;

        [Header("Editor Preview")]
        [Tooltip("Progress used for gizmos and inspector preview while not playing.")]
        [SerializeField, Range(0f, 1f)] private float m_PreviewProgress = 0.5f;

        private CelestialEngine m_Engine;
        private CelestialState m_EditorState;

        public event Action<CelestialState> StateChanged
        {
            add => Engine.StateChanged += value;
            remove => Engine.StateChanged -= value;
        }

        public CelestialState State =>
            Application.isPlaying ? Engine.State : m_EditorState;

        public CelestialPreset Preset
        {
            get => m_Preset;
            set
            {
                m_Preset = value;
                if (m_Engine != null) m_Engine.Preset = value;
            }
        }

        public float PreviewProgress => m_PreviewProgress;

        private CelestialEngine Engine
        {
            get
            {
                // Created once and reused. Rebuilding it here would silently
                // drop every subscription taken before this point, which is
                // exactly what happens when another component's OnEnable runs
                // first.
                if (m_Engine == null)
                {
                    m_Engine = new CelestialEngine(ResolveClock(), m_Preset);
                }

                return m_Engine;
            }
        }

        private void OnEnable()
        {
            IWorldClock clock = ResolveClock();

            if (clock == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialHandlerBehaviour)} on '{name}' found no clock.", this);
                return;
            }

            // The engine may have been built before the clock existed, so
            // refresh both before binding.
            Engine.Clock = clock;
            Engine.Preset = m_Preset;
            Engine.Bind();
        }

        private void OnDisable()
        {
            m_Engine?.Unbind();
        }

        public CelestialState Evaluate(float dayProgress)
        {
            return Engine.Evaluate(dayProgress);
        }

        private IWorldClock ResolveClock()
        {
            if (m_Clock != null) return m_Clock;
            return WorldClockBehaviour.Active;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            if (m_Preset == null) return;

            m_EditorState = CelestialEngine.Sample(m_Preset, m_PreviewProgress);
        }
#endif
    }
}
