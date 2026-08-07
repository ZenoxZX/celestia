using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Celestia.VContainer
{
    [UsedImplicitly]
    public sealed class CelestiaLightProvider : ICelestiaLightProvider, IDisposable
    {
        private const float k_DefaultSunIntensity = 3f;
        private const float k_DefaultMoonIntensity = 0.4f;

        private readonly CelestiaConfig m_Config;

        private GameObject m_Owned;
        private Light m_SunLight;
        private Light m_MoonLight;
        private bool m_Resolved;

        public CelestiaLightProvider(CelestiaConfig config)
        {
            m_Config = config;
        }

        public Light SunLight
        {
            get
            {
                Resolve();
                return m_SunLight;
            }
        }

        public Light MoonLight
        {
            get
            {
                Resolve();
                return m_MoonLight;
            }
        }

        public bool OwnsLights => m_Owned != null;

        void IDisposable.Dispose()
        {
            if (m_Owned == null)
                return;

            if (Application.isPlaying) Object.Destroy(m_Owned);
            else Object.DestroyImmediate(m_Owned);

            m_Owned = null;
            m_SunLight = null;
            m_MoonLight = null;
            m_Resolved = false;
        }

        private void Resolve()
        {
            if (m_Resolved)
                return;

            m_Resolved = true;

            m_SunLight = m_Config.SunLight;
            m_MoonLight = m_Config.MoonLight;

            if (m_SunLight != null && m_MoonLight != null)
                return;

            m_Owned = new(m_Config.GeneratedLightsName);

            if (m_Config.DontDestroyGeneratedLights && Application.isPlaying)
                Object.DontDestroyOnLoad(m_Owned);

            m_SunLight ??= CreateLight("Sun Light", k_DefaultSunIntensity);
            m_MoonLight ??= CreateLight("Moon Light", k_DefaultMoonIntensity);
        }

        private Light CreateLight(string lightName, float intensity)
        {
            GameObject go = new(lightName);
            go.transform.SetParent(m_Owned.transform, false);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.shadows = m_Config.ShadowType;

            return light;
        }
    }
}
