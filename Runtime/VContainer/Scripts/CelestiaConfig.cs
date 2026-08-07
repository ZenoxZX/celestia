using UnityEngine;

namespace Celestia.VContainer
{
    [CreateAssetMenu(fileName = nameof(CelestiaConfig), menuName = "Celestia/Celestia Config")]
    public class CelestiaConfig : ScriptableObject
    {
        [Header("Sky")]
        [SerializeField] private CelestialPreset m_Preset;

        [Header("Clock")]
        [SerializeField, Min(WorldClock.MinRealSecondsPerDay)]
        private float m_RealSecondsPerDay = 120f;

        [SerializeField, Min(0f)] private float m_TimeScale = 1f;
        [SerializeField, Range(0f, 1f)] private float m_StartProgress = 0.20834f;
        [SerializeField] private bool m_PlayOnStart = true;

        [Header("Lights")]
        [Tooltip("Leave empty to create a directional light at runtime.")]
        [SerializeField] private Light m_SunLight;

        [Tooltip("Leave empty to create a directional light at runtime.")]
        [SerializeField] private Light m_MoonLight;

        [Tooltip("Name of the GameObject created when lights are generated at runtime.")]
        [SerializeField] private string m_GeneratedLightsName = "[Celestia Lights]";

        [SerializeField] private bool m_DontDestroyGeneratedLights = true;

        [Header("Light Driving")]
        [SerializeField] private bool m_DriveShadows = true;
        [SerializeField] private LightShadows m_ShadowType = LightShadows.Soft;
        [SerializeField] private bool m_DriveColor = true;
        [SerializeField] private bool m_DriveSunSource = true;

        public CelestialPreset Preset => m_Preset;

        public float RealSecondsPerDay => m_RealSecondsPerDay;

        public float TimeScale => m_TimeScale;

        public float StartProgress => m_StartProgress;

        public bool PlayOnStart => m_PlayOnStart;

        public Light SunLight => m_SunLight;

        public Light MoonLight => m_MoonLight;

        public string GeneratedLightsName => m_GeneratedLightsName;

        public bool DontDestroyGeneratedLights => m_DontDestroyGeneratedLights;

        public bool DriveShadows => m_DriveShadows;

        public LightShadows ShadowType => m_ShadowType;

        public bool DriveColor => m_DriveColor;

        public bool DriveSunSource => m_DriveSunSource;

        public bool HasLights => m_SunLight != null && m_MoonLight != null;
    }
}
