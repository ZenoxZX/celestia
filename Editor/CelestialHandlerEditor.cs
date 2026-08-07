using UnityEditor;
using UnityEngine;

namespace Celestia.Editor
{
    [CustomEditor(typeof(CelestialHandlerBehaviour))]
    public class CelestialHandlerEditor : UnityEditor.Editor
    {
        private const float k_DomeRadius = 15f;
        private const int k_ArcSamples = 144;
        private const float k_BodyHandleSize = 0.035f;
        private const float k_TickLength = 0.06f;
        private const float k_LabelOffset = 0.12f;
        private const float k_ZenithOvershoot = 1.08f;
        private const float k_BodyRadiusRatio = 0.035f;

        private static readonly Color s_HorizonColor = new Color(0.45f, 0.55f, 0.65f, 0.9f);
        private static readonly Color s_GridColor = new Color(0.45f, 0.55f, 0.65f, 0.28f);
        private static readonly Color s_SunArcColor = new Color(1f, 0.68f, 0.25f, 0.95f);
        private static readonly Color s_MoonArcColor = new Color(0.68f, 0.8f, 1f, 0.95f);
        private static readonly Color s_BelowColor = new Color(0.35f, 0.42f, 0.58f, 0.45f);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var handler = (CelestialHandlerBehaviour)target;
            if (handler.Preset == null)
            {
                EditorGUILayout.HelpBox("Assign a CelestialPreset to preview the sky.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            DrawReadout(handler);
        }

        private static void DrawReadout(CelestialHandlerBehaviour handler)
        {
            CelestialState state = Application.isPlaying
                ? handler.State
                : CelestialEngine.Sample(handler.Preset, handler.PreviewProgress);

            float progress = ResolveProgress(handler);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Readout", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Time", TimeOfDay.FromProgress(progress).ToShortString());

                EditorGUILayout.LabelField("Sun",
                    $"alt {state.SunAltitude:F1}°  az {state.SunAzimuth:F0}°  " +
                    (state.IsSunUp ? "up" : "down"));
                EditorGUILayout.LabelField("Moon",
                    $"alt {state.MoonAltitude:F1}°  az {state.MoonAzimuth:F0}°  " +
                    (state.IsMoonUp ? "up" : "down"));
                EditorGUILayout.LabelField("Illumination", $"{state.MoonIllumination * 100f:F0}%");
                EditorGUILayout.LabelField("Sky phase",
                    $"{state.SkyPhase:F3}   ({DescribePhase(state.SunAltitude)})");

                Vector3 euler = state.IsSunUp || state.IsMoonUp
                    ? Quaternion.LookRotation(
                        (state.IsSunUp ? state.SunLightForward : state.MoonLightForward).normalized,
                        Vector3.up).eulerAngles
                    : Vector3.zero;

                EditorGUILayout.LabelField("Active light euler",
                    $"({euler.x:F1}, {euler.y:F1}, {euler.z:F1})");

                if (!state.IsSunUp && !state.IsMoonUp)
                {
                    EditorGUILayout.HelpBox("Both bodies are below the horizon.", MessageType.Info);
                }
            }
        }

        private static float ResolveProgress(CelestialHandlerBehaviour handler)
        {
            return Application.isPlaying
                ? handler.State.DayProgress
                : handler.PreviewProgress;
        }

        private static string DescribePhase(float sunAltitude)
        {
            if (sunAltitude < -18f) return "astronomical night";
            if (sunAltitude < -12f) return "astronomical twilight";
            if (sunAltitude < -6f) return "nautical twilight";
            if (sunAltitude < 0f) return "civil twilight";
            if (sunAltitude < 6f) return "golden hour";
            return "daylight";
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawGizmos(CelestialHandlerBehaviour handler, GizmoType gizmoType)
        {
            CelestialPreset preset = handler.Preset;
            if (preset == null) return;

            Vector3 origin = handler.transform.position;
            float progress = ResolveProgress(handler);

            DrawHorizon(origin);
            DrawCardinals(origin);
            DrawArc(origin, preset, true, s_SunArcColor);
            DrawArc(origin, preset, false, s_MoonArcColor);

            CelestialState state = CelestialEngine.Sample(preset, progress);
            DrawBody(origin, state.SunDirection, state.SunAltitude, s_SunArcColor, "Sun");
            DrawBody(origin, state.MoonDirection, state.MoonAltitude, s_MoonArcColor, "Moon");
        }

        private static void DrawHorizon(Vector3 origin)
        {
            Handles.color = s_HorizonColor;
            Handles.DrawWireDisc(origin, Vector3.up, k_DomeRadius);

            Handles.color = s_GridColor;
            for (int elevation = 30; elevation <= 60; elevation += 30)
            {
                float radians = elevation * Mathf.Deg2Rad;
                float radius = Mathf.Cos(radians) * k_DomeRadius;
                float height = Mathf.Sin(radians) * k_DomeRadius;
                Handles.DrawWireDisc(origin + Vector3.up * height, Vector3.up, radius);
            }

            Handles.DrawDottedLine(origin, origin + Vector3.up * (k_DomeRadius * k_ZenithOvershoot), 3f);
        }

        private static void DrawCardinals(Vector3 origin)
        {
            DrawCardinal(origin, Vector3.forward, "N");
            DrawCardinal(origin, Vector3.right, "E");
            DrawCardinal(origin, Vector3.back, "S");
            DrawCardinal(origin, Vector3.left, "W");
        }

        private static void DrawCardinal(Vector3 origin, Vector3 direction, string label)
        {
            Handles.color = s_HorizonColor;
            Vector3 point = origin + direction * k_DomeRadius;
            Handles.DrawLine(point, point + direction * (k_DomeRadius * k_TickLength));
            Handles.Label(point + direction * (k_DomeRadius * k_LabelOffset), label);
        }

        private static void DrawArc(Vector3 origin, CelestialPreset preset, bool isSun, Color color)
        {
            Vector3 previous = Vector3.zero;
            bool hasPrevious = false;
            double previousAltitude = 0.0;

            for (int i = 0; i <= k_ArcSamples; i++)
            {
                float progress = i / (float)k_ArcSamples;
                double altitude, azimuth;

                if (isSun)
                {
                    CelestialSolver.SunPosition(progress, preset.YearProgress, preset.Latitude,
                        out altitude, out azimuth);
                }
                else
                {
                    CelestialSolver.MoonPosition(progress, preset.YearProgress, preset.MoonPhase,
                        preset.Latitude, out altitude, out azimuth);
                }

                Vector3 point = origin + CelestialSolver.ToDirection(altitude, azimuth) * k_DomeRadius;

                if (hasPrevious)
                {
                    bool bothAbove = altitude > 0.0 && previousAltitude > 0.0;
                    Handles.color = bothAbove ? color : s_BelowColor;
                    Handles.DrawLine(previous, point);
                }

                previous = point;
                previousAltitude = altitude;
                hasPrevious = true;
            }
        }

        private static void DrawBody(Vector3 origin, Vector3 direction, float altitude,
                                     Color color, string label)
        {
            Vector3 position = origin + direction * k_DomeRadius;
            bool isUp = altitude > 0f;

            Handles.color = isUp ? color : s_BelowColor;
            Handles.DrawLine(origin, position);

            float cameraSize = HandleUtility.GetHandleSize(position) * k_BodyHandleSize;
            float domeSize = k_DomeRadius * k_BodyRadiusRatio;
            float size = Mathf.Max(cameraSize, domeSize);

            Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
            Handles.Label(position + Vector3.up * (size * 1.6f), $"{label} {altitude:F1}°");
        }
    }
}
