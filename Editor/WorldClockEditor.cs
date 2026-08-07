using UnityEditor;
using UnityEngine;

namespace Celestia.Editor
{
    [CustomEditor(typeof(WorldClockBehaviour))]
    public class WorldClockEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var clock = (WorldClockBehaviour)target;

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Clock", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Time", clock.Time.ToString());
                EditorGUILayout.LabelField("Progress", clock.DayProgress.ToString("F4"));
                EditorGUILayout.LabelField("Day", clock.DayCount.ToString());
                EditorGUILayout.LabelField("Running", clock.IsRunning ? "yes" : "paused");
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Transport controls are available in play mode.", MessageType.None);
                return;
            }

            EditorGUILayout.Space();
            DrawTransport(clock);
            DrawJumpButtons(clock);
        }

        private static void DrawTransport(IWorldClock clock)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(clock.IsRunning ? "Pause" : "Play")) clock.Toggle();
                if (GUILayout.Button("-1h")) clock.StepHours(-1f);
                if (GUILayout.Button("+1h")) clock.StepHours(1f);
                if (GUILayout.Button("+10m")) clock.StepMinutes(10f);
            }
        }

        private static void DrawJumpButtons(IWorldClock clock)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Midnight")) clock.SetTime(0, 0);
                if (GUILayout.Button("Sunrise")) clock.SetTime(6, 0);
                if (GUILayout.Button("Noon")) clock.SetTime(12, 0);
                if (GUILayout.Button("Sunset")) clock.SetTime(18, 0);
            }
        }
    }
}
