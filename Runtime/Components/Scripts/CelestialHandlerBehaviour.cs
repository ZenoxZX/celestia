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

        private CelestialEngine Engine => m_Engine ??= new CelestialEngine(ResolveClock(), m_Preset);

        private void OnEnable()
        {
            IWorldClock clock = ResolveClock();

            if (clock == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialHandlerBehaviour)} on '{name}' found no clock.", this);
                return;
            }

            m_Engine = new CelestialEngine(clock, m_Preset);
            m_Engine.Bind();
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
