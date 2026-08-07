using UnityEngine;

namespace Celestia
{
    [CreateAssetMenu(fileName = "CelestialPreset", menuName = "Celestia/Celestial Preset")]
    public class CelestialPreset : ScriptableObject
    {
        private const float k_NightPhase = 0f;
        private const float k_HorizonPhase = 0.1667f;
        private const float k_GoldenPhase = 0.2222f;
        private const float k_DayPhase = 0.4444f;

        [Header("Location")]
        [SerializeField, Range(-90f, 90f)] private float m_Latitude = 25.7617f;

        [Tooltip("Not used by the solver yet. Hours are local solar time, so longitude " +
                 "only matters once time zone and equation of time are modelled.")]
        [SerializeField, Range(-180f, 180f)] private float m_Longitude = -80.1918f;

        [Header("Sky")]
        [Tooltip("0 = spring equinox, 0.25 = summer solstice, 0.5 = autumn equinox, 0.75 = winter solstice.")]
        [SerializeField, Range(0f, 1f)] private float m_YearProgress = 0.25f;

        [Tooltip("0 = new moon, 0.25 = first quarter, 0.5 = full moon, 0.75 = last quarter.")]
        [SerializeField, Range(0f, 1f)] private float m_MoonPhase = 0.5f;

        [Header("Light Intensity")]
        [SerializeField, Min(0f)] private float m_SunIntensity = 3f;
        [SerializeField, Min(0f)] private float m_MoonIntensity = 0.4f;

        [Header("Horizon")]
        [Tooltip("Below this altitude a light fades out, avoiding harsh shadows at grazing angles.")]
        [SerializeField, Range(0f, 30f)] private float m_HorizonFadeAngle = 8f;

        [Tooltip("Hysteresis band around the horizon for swapping the shadow casting light.")]
        [SerializeField, Range(0f, 10f)] private float m_ShadowSwapHysteresis = 1f;

        [Header("Color")]
        [SerializeField] private Gradient m_SunColor = CreateDefaultSunGradient();
        [SerializeField] private Gradient m_MoonColor = CreateDefaultMoonGradient();

        [Header("White Balance")]
        [Tooltip("Curves are sampled with sky phase: 0 = astronomical night, " +
                 "0.17 = horizon, 1 = sun at zenith. Add a CelestialPostProcessBinder " +
                 "to a volume to drive these.")]
        [SerializeField] private AnimationCurve m_Temperature = CreateDefaultTemperatureCurve();
        [SerializeField] private AnimationCurve m_Tint = CreateDefaultTintCurve();

        [Header("Color Adjustments")]
        [SerializeField] private AnimationCurve m_PostExposure = CreateDefaultPostExposureCurve();
        [SerializeField] private AnimationCurve m_Contrast = CreateDefaultContrastCurve();
        [SerializeField] private Gradient m_ColorFilter = CreateDefaultColorFilterGradient();
        [SerializeField] private AnimationCurve m_HueShift = CreateFlatCurve(0f);
        [SerializeField] private AnimationCurve m_Saturation = CreateDefaultSaturationCurve();

        public float Latitude => m_Latitude;

        public float Longitude => m_Longitude;

        public float YearProgress => m_YearProgress;

        public float MoonPhase => m_MoonPhase;

        public float SunIntensity => m_SunIntensity;

        public float MoonIntensity => m_MoonIntensity;

        public float HorizonFadeAngle => m_HorizonFadeAngle;

        public float ShadowSwapHysteresis => m_ShadowSwapHysteresis;

        public Gradient SunColor => m_SunColor;

        public Gradient MoonColor => m_MoonColor;

        public float EvaluateTemperature(float skyPhase) => m_Temperature.Evaluate(skyPhase);

        public float EvaluateTint(float skyPhase) => m_Tint.Evaluate(skyPhase);

        public float EvaluatePostExposure(float skyPhase) => m_PostExposure.Evaluate(skyPhase);

        public float EvaluateContrast(float skyPhase) => m_Contrast.Evaluate(skyPhase);

        public Color EvaluateColorFilter(float skyPhase) => m_ColorFilter.Evaluate(skyPhase);

        public float EvaluateHueShift(float skyPhase) => m_HueShift.Evaluate(skyPhase);

        public float EvaluateSaturation(float skyPhase) => m_Saturation.Evaluate(skyPhase);

        public void SetLatitude(float latitude)
        {
            m_Latitude = Mathf.Clamp(latitude, -90f, 90f);
        }

        public void SetYearProgress(float yearProgress)
        {
            m_YearProgress = Mathf.Repeat(yearProgress, 1f);
        }

        public void SetSeason(Season season)
        {
            m_YearProgress = season.ToYearProgress();
        }

        public void SetMoonPhase(float moonPhase)
        {
            m_MoonPhase = Mathf.Repeat(moonPhase, 1f);
        }

        public void SetMoonPhase(MoonPhasePreset preset)
        {
            m_MoonPhase = preset.ToPhase();
        }

        public Color EvaluateSunColor(float altitudeDegrees)
        {
            return m_SunColor.Evaluate(NormalizeAltitude(altitudeDegrees));
        }

        public Color EvaluateMoonColor(float altitudeDegrees)
        {
            return m_MoonColor.Evaluate(NormalizeAltitude(altitudeDegrees));
        }

        private static float NormalizeAltitude(float altitudeDegrees)
        {
            return CelestialSolver.AtmosphericTint(altitudeDegrees);
        }

        private static Gradient CreateDefaultSunGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.45f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.72f, 0.45f), 0.08f),
                    new GradientColorKey(new Color(1f, 0.96f, 0.9f), 0.35f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static Gradient CreateDefaultMoonGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.78f, 0.68f, 0.62f), 0f),
                    new GradientColorKey(new Color(0.80f, 0.80f, 0.85f), 0.35f),
                    new GradientColorKey(new Color(0.80f, 0.86f, 1f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static AnimationCurve CreateFlatCurve(float value)
        {
            return new AnimationCurve(new Keyframe(0f, value), new Keyframe(1f, value));
        }

        private static AnimationCurve CreateDefaultTemperatureCurve()
        {
            return new AnimationCurve(
                new Keyframe(k_NightPhase, -18f),
                new Keyframe(k_HorizonPhase, 22f),
                new Keyframe(k_GoldenPhase, 14f),
                new Keyframe(k_DayPhase, 0f),
                new Keyframe(1f, 0f));
        }

        private static AnimationCurve CreateDefaultTintCurve()
        {
            return new AnimationCurve(
                new Keyframe(k_NightPhase, 6f),
                new Keyframe(k_HorizonPhase, -4f),
                new Keyframe(k_DayPhase, 0f),
                new Keyframe(1f, 0f));
        }

        private static AnimationCurve CreateDefaultPostExposureCurve()
        {
            return new AnimationCurve(
                new Keyframe(k_NightPhase, 0.6f),
                new Keyframe(k_HorizonPhase, 0.2f),
                new Keyframe(k_DayPhase, 0f),
                new Keyframe(1f, 0f));
        }

        private static AnimationCurve CreateDefaultContrastCurve()
        {
            return new AnimationCurve(
                new Keyframe(k_NightPhase, -8f),
                new Keyframe(k_HorizonPhase, 4f),
                new Keyframe(k_DayPhase, 0f),
                new Keyframe(1f, 0f));
        }

        private static AnimationCurve CreateDefaultSaturationCurve()
        {
            return new AnimationCurve(
                new Keyframe(k_NightPhase, -32f),
                new Keyframe(k_HorizonPhase, 8f),
                new Keyframe(k_GoldenPhase, 6f),
                new Keyframe(k_DayPhase, 0f),
                new Keyframe(1f, 0f));
        }

        private static Gradient CreateDefaultColorFilterGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.74f, 0.82f, 1f), k_NightPhase),
                    new GradientColorKey(new Color(1f, 0.88f, 0.78f), k_HorizonPhase),
                    new GradientColorKey(Color.white, k_DayPhase),
                    new GradientColorKey(Color.white, 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_YearProgress = Mathf.Clamp01(m_YearProgress);
            m_MoonPhase = Mathf.Clamp01(m_MoonPhase);
        }
#endif
    }
}
