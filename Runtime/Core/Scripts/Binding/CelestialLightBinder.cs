using UnityEngine;

namespace Celestia
{
    [AddComponentMenu("Celestia/Celestial Light Binder")]
    [DisallowMultipleComponent]
    public class CelestialLightBinder : MonoBehaviour
    {
        [SerializeField] private CelestialHandler m_Handler;

        [Header("Lights")]
        [SerializeField] private Light m_SunLight;
        [SerializeField] private Light m_MoonLight;

        [Header("Shadows")]
        [SerializeField] private bool m_DriveShadows = true;
        [SerializeField] private LightShadows m_ShadowType = LightShadows.Soft;

        [Header("Color")]
        [SerializeField] private bool m_DriveColor = true;

        [Header("Environment")]
        [Tooltip("Keeps RenderSettings.sun on whichever body currently owns the sky, " +
                 "so ambient and procedural skybox follow the active light.")]
        [SerializeField] private bool m_DriveSunSource = true;

        private bool m_SunOwnsShadows = true;
        private bool m_HasResolvedOwner;
        private Light m_AppliedSunSource;
        private bool m_HasCachedSunSource;
        private Light m_OriginalSunSource;

        public Light SunLight => m_SunLight;

        public Light MoonLight => m_MoonLight;

        public Light ActiveLight => m_SunOwnsShadows ? m_SunLight : m_MoonLight;

        private void OnEnable()
        {
            if (m_Handler == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialLightBinder)} on '{name}' has no handler assigned.", this);
                return;
            }

            m_Handler.StateChanged += OnStateChanged;
            Apply(m_Handler.State);
        }

        private void OnDisable()
        {
            RestoreSunSource();

            if (m_Handler == null) return;
            m_Handler.StateChanged -= OnStateChanged;
        }

        public void Apply(CelestialState state)
        {
            CelestialPreset preset = m_Handler != null ? m_Handler.Preset : null;
            if (preset == null) return;

            ResolveShadowOwner(state, preset);

            ApplyBody(m_SunLight, state.SunLightForward, state.SunAltitude,
                preset.SunIntensity, preset, true, m_SunOwnsShadows);

            float moonScale = state.MoonIllumination;
            ApplyBody(m_MoonLight, state.MoonLightForward, state.MoonAltitude,
                preset.MoonIntensity * moonScale, preset, false, !m_SunOwnsShadows);

            ApplySunSource();
        }

        private void ApplySunSource()
        {
            if (!m_DriveSunSource) return;

            Light target = PickSunSource();
            if (target == null) return;

            if (!m_HasCachedSunSource)
            {
                m_OriginalSunSource = RenderSettings.sun;
                m_HasCachedSunSource = true;
            }

            if (m_AppliedSunSource == target && RenderSettings.sun == target) return;

            RenderSettings.sun = target;
            m_AppliedSunSource = target;
        }

        private Light PickSunSource()
        {
            Light owner = ActiveLight;
            if (IsUsableSunSource(owner)) return owner;

            Light fallback = ReferenceEquals(owner, m_SunLight) ? m_MoonLight : m_SunLight;
            if (IsUsableSunSource(fallback)) return fallback;

            if (owner != null) return owner;
            return m_SunLight != null ? m_SunLight : m_MoonLight;
        }

        private static bool IsUsableSunSource(Light light)
        {
            return light != null && light.enabled && light.intensity > 0f;
        }

        private void RestoreSunSource()
        {
            if (!m_HasCachedSunSource) return;
            if (RenderSettings.sun == m_AppliedSunSource) RenderSettings.sun = m_OriginalSunSource;

            m_HasCachedSunSource = false;
            m_AppliedSunSource = null;
        }

        private void ApplyBody(Light light, Vector3 forward, float altitude,
                               float baseIntensity, CelestialPreset preset,
                               bool isSun, bool ownsShadows)
        {
            if (light == null) return;

            bool isUp = altitude > 0f;
            float fade = CelestialSolver.HorizonFade(altitude, preset.HorizonFadeAngle);
            float intensity = baseIntensity * fade;
            bool active = isUp && intensity > 0f;

            if (active && forward.sqrMagnitude > Mathf.Epsilon)
            {
                light.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }

            light.intensity = intensity;

            if (m_DriveColor)
            {
                light.color = isSun
                    ? preset.EvaluateSunColor(altitude)
                    : preset.EvaluateMoonColor(altitude);
            }

            if (m_DriveShadows)
            {
                light.shadows = active && ownsShadows ? m_ShadowType : LightShadows.None;
            }

            if (light.enabled != active) light.enabled = active;
        }

        private void ResolveShadowOwner(CelestialState state, CelestialPreset preset)
        {
            float hysteresis = preset.ShadowSwapHysteresis;

            if (!m_HasResolvedOwner)
            {
                m_SunOwnsShadows = state.SunAltitude >= state.MoonAltitude;
                m_HasResolvedOwner = true;
                return;
            }

            if (m_SunOwnsShadows)
            {
                if (state.SunAltitude < -hysteresis) m_SunOwnsShadows = false;
                return;
            }

            if (state.SunAltitude > hysteresis) m_SunOwnsShadows = true;
        }

        private void OnStateChanged(CelestialState state)
        {
            Apply(state);
        }
    }
}
