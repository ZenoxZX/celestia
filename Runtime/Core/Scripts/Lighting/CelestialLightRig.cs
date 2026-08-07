using System;
using UnityEngine;

namespace Celestia
{
    public sealed class CelestialLightRig : IDisposable
    {
        private readonly ICelestialSource m_Source;

        private Light m_SunLight;
        private Light m_MoonLight;

        private bool m_DriveShadows = true;
        private bool m_DriveColor = true;
        private bool m_DriveSunSource = true;
        private LightShadows m_ShadowType = LightShadows.Soft;

        private bool m_SunOwnsShadows = true;
        private bool m_HasResolvedOwner;

        private Light m_AppliedSunSource;
        private Light m_OriginalSunSource;
        private bool m_HasCachedSunSource;

        private bool m_Bound;

        public CelestialLightRig(ICelestialSource source, Light sunLight, Light moonLight)
        {
            m_Source = source;
            m_SunLight = sunLight;
            m_MoonLight = moonLight;
        }

        public Light SunLight
        {
            get => m_SunLight;
            set => m_SunLight = value;
        }

        public Light MoonLight
        {
            get => m_MoonLight;
            set => m_MoonLight = value;
        }

        public bool DriveShadows
        {
            get => m_DriveShadows;
            set => m_DriveShadows = value;
        }

        public bool DriveColor
        {
            get => m_DriveColor;
            set => m_DriveColor = value;
        }

        public bool DriveSunSource
        {
            get => m_DriveSunSource;
            set => m_DriveSunSource = value;
        }

        public LightShadows ShadowType
        {
            get => m_ShadowType;
            set => m_ShadowType = value;
        }

        public Light ActiveLight => m_SunOwnsShadows ? m_SunLight : m_MoonLight;

        public void Bind()
        {
            if (m_Bound || m_Source == null) return;

            m_Source.StateChanged += Apply;
            m_Bound = true;

            Apply(m_Source.State);
        }

        public void Unbind()
        {
            if (!m_Bound) return;

            m_Source.StateChanged -= Apply;
            m_Bound = false;

            RestoreSunSource();
        }

        public void Apply(CelestialState state)
        {
            CelestialPreset preset = m_Source?.Preset;
            if (preset == null) return;

            ResolveShadowOwner(state, preset);

            ApplyBody(m_SunLight, state.SunLightForward, state.SunAltitude,
                preset.SunIntensity, preset, true, m_SunOwnsShadows);

            ApplyBody(m_MoonLight, state.MoonLightForward, state.MoonAltitude,
                preset.MoonIntensity * state.MoonIllumination, preset, false, !m_SunOwnsShadows);

            ApplySunSource();
        }

        void IDisposable.Dispose()
        {
            Unbind();
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
    }
}
