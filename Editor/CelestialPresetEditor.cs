using UnityEditor;
using UnityEngine;

namespace Celestia.Editor
{
    [CustomEditor(typeof(CelestialPreset))]
    public class CelestialPresetEditor : UnityEditor.Editor
    {
        private static readonly Season[] s_Seasons =
        {
            Season.SpringEquinox, Season.SummerSolstice,
            Season.AutumnEquinox, Season.WinterSolstice
        };

        private static readonly string[] s_SeasonLabels = { "Spring", "Summer", "Autumn", "Winter" };

        private static readonly MoonPhasePreset[] s_Phases =
        {
            MoonPhasePreset.NewMoon, MoonPhasePreset.FirstQuarter,
            MoonPhasePreset.FullMoon, MoonPhasePreset.LastQuarter
        };

        private static readonly string[] s_PhaseLabels = { "New", "First Qtr", "Full", "Last Qtr" };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var preset = (CelestialPreset)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Set", EditorStyles.boldLabel);

            DrawSeasonButtons(preset);
            DrawPhaseButtons(preset);

            EditorGUILayout.Space();
            DrawSummary(preset);
        }

        private void DrawSeasonButtons(CelestialPreset preset)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < s_Seasons.Length; i++)
                {
                    if (!GUILayout.Button(s_SeasonLabels[i])) continue;

                    Undo.RecordObject(preset, "Set Season");
                    preset.SetSeason(s_Seasons[i]);
                    EditorUtility.SetDirty(preset);
                }
            }
        }

        private void DrawPhaseButtons(CelestialPreset preset)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < s_Phases.Length; i++)
                {
                    if (!GUILayout.Button(s_PhaseLabels[i])) continue;

                    Undo.RecordObject(preset, "Set Moon Phase");
                    preset.SetMoonPhase(s_Phases[i]);
                    EditorUtility.SetDirty(preset);
                }
            }
        }

        private static void DrawSummary(CelestialPreset preset)
        {
            CelestialSolver.SunPosition(0.5, preset.YearProgress, preset.Latitude,
                out double noonAltitude, out _);
            CelestialSolver.MoonPosition(0.0, preset.YearProgress, preset.MoonPhase, preset.Latitude,
                out double midnightMoon, out _);

            double illumination = CelestialSolver.MoonIllumination(preset.MoonPhase) * 100.0;
            MoonPhasePreset nearest = MoonPhasePresetExtensions.FromPhase(preset.MoonPhase);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Sun at solar noon", $"{noonAltitude:F1}°");
                EditorGUILayout.LabelField("Moon at midnight", $"{midnightMoon:F1}°");
                EditorGUILayout.LabelField("Moon phase", $"{nearest.ToDisplayName()} · {illumination:F0}%");

                double shadow = CelestialSolver.ShadowLengthRatio(noonAltitude);
                string shadowText = double.IsPositiveInfinity(shadow) ? "—" : $"{shadow:F2}x height";
                EditorGUILayout.LabelField("Noon shadow", shadowText);
            }
        }
    }
}
