using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Celestia.PostProcessing
{
    [AddComponentMenu("Celestia/Celestial Post Process Binder")]
    [DisallowMultipleComponent]
    public class CelestialPostProcessBinder : MonoBehaviour
    {
        [SerializeField] private CelestialHandlerBehaviour m_Handler;

        [Header("Volume")]
        [Tooltip("Leave empty to fall back to a Volume on this GameObject.")]
        [SerializeField] private Volume m_Volume;

        [Tooltip("Adds White Balance and Color Adjustments to the runtime profile copy when the " +
                 "profile has no such override. The source asset is never modified. " +
                 "Turn off to drive only the overrides the profile already declares.")]
        [SerializeField] private bool m_AddMissingOverrides = true;

        private Volume m_ResolvedVolume;
        private WhiteBalance m_WhiteBalance;
        private ColorAdjustments m_ColorAdjustments;
        private bool m_HasProfile;
        private bool m_HasResolved;
        private VolumeProfile m_RuntimeProfile;
        private VolumeProfile m_SourceProfile;

        public Volume Volume => m_ResolvedVolume != null ? m_ResolvedVolume : m_Volume;

        private void Reset()
        {
            m_Volume = GetComponent<Volume>();
        }

        private void OnEnable()
        {
            if (m_Handler == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialPostProcessBinder)} on '{name}' has no handler assigned.", this);
                return;
            }

            m_Handler.StateChanged += OnStateChanged;

            ResolveOverrides();
            Apply(m_Handler.State);
        }

        private void OnDisable()
        {
            m_HasResolved = false;
            m_HasProfile = false;
            m_WhiteBalance = null;
            m_ColorAdjustments = null;

            ReleaseRuntimeProfile();

            if (m_Handler == null) return;
            m_Handler.StateChanged -= OnStateChanged;
        }

        private void OnDestroy()
        {
            ReleaseRuntimeProfile();
        }

        public void Apply(CelestialState state)
        {
            if (!Application.isPlaying) return;

            CelestialPreset preset = m_Handler != null ? m_Handler.Preset : null;
            if (preset == null) return;

            if (!m_HasResolved || NeedsMissingOverride()) ResolveOverrides();
            if (!m_HasProfile) return;

            float phase = state.SkyPhase;

            ApplyWhiteBalance(preset, phase);
            ApplyColorAdjustments(preset, phase);
        }

        private bool NeedsMissingOverride()
        {
            if (!m_AddMissingOverrides) return false;
            if (m_RuntimeProfile == null) return false;

            return m_WhiteBalance == null || m_ColorAdjustments == null;
        }

        public void RebindVolume(Volume volume)
        {
            ReleaseRuntimeProfile();

            m_Volume = volume;
            m_HasResolved = false;
            m_HasProfile = false;

            if (!isActiveAndEnabled) return;

            ResolveOverrides();
            if (m_Handler != null) Apply(m_Handler.State);
        }

        private void ApplyWhiteBalance(CelestialPreset preset, float phase)
        {
            if (m_WhiteBalance == null) return;

            m_WhiteBalance.active = true;
            m_WhiteBalance.temperature.overrideState = true;
            m_WhiteBalance.tint.overrideState = true;
            m_WhiteBalance.temperature.value = preset.EvaluateTemperature(phase);
            m_WhiteBalance.tint.value = preset.EvaluateTint(phase);
        }

        private void ApplyColorAdjustments(CelestialPreset preset, float phase)
        {
            if (m_ColorAdjustments == null) return;

            m_ColorAdjustments.active = true;
            m_ColorAdjustments.postExposure.overrideState = true;
            m_ColorAdjustments.contrast.overrideState = true;
            m_ColorAdjustments.colorFilter.overrideState = true;
            m_ColorAdjustments.hueShift.overrideState = true;
            m_ColorAdjustments.saturation.overrideState = true;

            m_ColorAdjustments.postExposure.value = preset.EvaluatePostExposure(phase);
            m_ColorAdjustments.contrast.value = preset.EvaluateContrast(phase);
            m_ColorAdjustments.colorFilter.value = preset.EvaluateColorFilter(phase);
            m_ColorAdjustments.hueShift.value = preset.EvaluateHueShift(phase);
            m_ColorAdjustments.saturation.value = preset.EvaluateSaturation(phase);
        }

        private VolumeProfile AcquireProfile()
        {
            VolumeProfile source = m_ResolvedVolume.sharedProfile;
            if (source == null) return null;

            if (m_RuntimeProfile != null && m_ResolvedVolume.sharedProfile == m_RuntimeProfile)
            {
                return m_RuntimeProfile;
            }

            ReleaseRuntimeProfile();

            m_SourceProfile = source;
            m_RuntimeProfile = Instantiate(source);
            m_RuntimeProfile.name = source.name + " (Celestia Runtime)";
            m_RuntimeProfile.hideFlags = HideFlags.HideAndDontSave;
            m_ResolvedVolume.sharedProfile = m_RuntimeProfile;

            return m_RuntimeProfile;
        }

        private void ReleaseRuntimeProfile()
        {
            if (m_RuntimeProfile == null)
            {
                m_SourceProfile = null;
                return;
            }

            if (m_ResolvedVolume != null && m_ResolvedVolume.sharedProfile == m_RuntimeProfile)
            {
                m_ResolvedVolume.sharedProfile = m_SourceProfile;
            }

            if (Application.isPlaying) Destroy(m_RuntimeProfile);
            else DestroyImmediate(m_RuntimeProfile);

            m_RuntimeProfile = null;
            m_SourceProfile = null;
        }

        private void ResolveOverrides()
        {
            m_HasResolved = true;
            m_HasProfile = false;
            m_WhiteBalance = null;
            m_ColorAdjustments = null;

            m_ResolvedVolume = m_Volume != null ? m_Volume : GetComponent<Volume>();

            if (m_ResolvedVolume == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialPostProcessBinder)} on '{name}' has no Volume assigned " +
                    "and none on this GameObject.", this);
                return;
            }

            VolumeProfile profile = AcquireProfile();

            if (profile == null)
            {
                Debug.LogError(
                    $"{nameof(CelestialPostProcessBinder)} on '{name}' has no volume profile.", this);
                return;
            }

            m_WhiteBalance = ResolveOverride<WhiteBalance>(profile);
            m_ColorAdjustments = ResolveOverride<ColorAdjustments>(profile);

            m_HasProfile = m_WhiteBalance != null || m_ColorAdjustments != null;
        }

        private T ResolveOverride<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            if (profile.TryGet(out T existing)) return existing;
            if (!m_AddMissingOverrides) return null;

            T added = profile.Add<T>();
            added.active = true;
            return added;
        }

        private void OnStateChanged(CelestialState state)
        {
            Apply(state);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;

            m_HasResolved = false;
            if (m_Handler != null) Apply(m_Handler.State);
        }
#endif
    }
}
