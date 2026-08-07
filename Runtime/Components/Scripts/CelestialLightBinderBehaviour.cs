using UnityEngine;

namespace Celestia
{
    [AddComponentMenu("Celestia/Celestial Light Binder")]
    [DisallowMultipleComponent]
    public class CelestialLightBinderBehaviour : MonoBehaviour
    {
        [SerializeField] private CelestialHandlerBehaviour m_Handler;

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

        private CelestialLightRig m_Rig;

        public Light SunLight => m_SunLight;

        public Light MoonLight => m_MoonLight;

        public CelestialLightRig Rig => m_Rig;

        private void OnEnable()
        {
            if (m_Handler == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialLightBinderBehaviour)} on '{name}' has no handler assigned.",
                    this);
                return;
            }

            m_Rig = new CelestialLightRig(m_Handler, m_SunLight, m_MoonLight)
            {
                DriveShadows = m_DriveShadows,
                DriveColor = m_DriveColor,
                DriveSunSource = m_DriveSunSource,
                ShadowType = m_ShadowType
            };

            m_Rig.Bind();
        }

        private void OnDisable()
        {
            m_Rig?.Unbind();
            m_Rig = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_Rig == null) return;

            m_Rig.DriveShadows = m_DriveShadows;
            m_Rig.DriveColor = m_DriveColor;
            m_Rig.DriveSunSource = m_DriveSunSource;
            m_Rig.ShadowType = m_ShadowType;
        }
#endif
    }
}
