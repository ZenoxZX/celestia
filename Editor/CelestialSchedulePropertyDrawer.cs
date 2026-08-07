using UnityEditor;
using UnityEngine;

namespace Celestia.Editor
{
    [CustomPropertyDrawer(typeof(CelestialSchedule))]
    public class CelestialSchedulePropertyDrawer : PropertyDrawer
    {
        private const float k_Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty enabled = property.FindPropertyRelative("m_Enabled");
            SerializedProperty labelProp = property.FindPropertyRelative("m_Label");

            Rect header = new Rect(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded,
                BuildHeader(property, labelProp), true);

            Rect toggle = new Rect(position.xMax - 18f, position.y, 18f, LineHeight);
            enabled.boolValue = EditorGUI.Toggle(toggle, enabled.boolValue);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            float y = position.y + LineHeight + k_Spacing;

            y = DrawField(position, y, labelProp);

            SerializedProperty trigger = property.FindPropertyRelative("m_Trigger");
            y = DrawField(position, y, trigger);

            switch ((ScheduleTrigger)trigger.enumValueIndex)
            {
                case ScheduleTrigger.TimeOfDay:
                    y = DrawField(position, y, property.FindPropertyRelative("m_Time"));
                    break;

                case ScheduleTrigger.SkyEvent:
                    y = DrawField(position, y, property.FindPropertyRelative("m_SkyEvent"));
                    break;

                case ScheduleTrigger.TimeRange:
                    y = DrawField(position, y, property.FindPropertyRelative("m_RangeStart"));
                    y = DrawField(position, y, property.FindPropertyRelative("m_RangeEnd"));
                    y = DrawField(position, y, property.FindPropertyRelative("m_CatchUpOnEnable"));
                    break;

                case ScheduleTrigger.Interval:
                    y = DrawField(position, y, property.FindPropertyRelative("m_Interval"));
                    break;
            }

            y = DrawField(position, y, property.FindPropertyRelative("m_Once"));
            y = DrawField(position, y, property.FindPropertyRelative("m_Triggered"));

            if ((ScheduleTrigger)trigger.enumValueIndex == ScheduleTrigger.TimeRange)
            {
                DrawField(position, y, property.FindPropertyRelative("m_Exited"));
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return LineHeight;

            float height = LineHeight + k_Spacing;
            height += Height(property.FindPropertyRelative("m_Label"));

            SerializedProperty trigger = property.FindPropertyRelative("m_Trigger");
            height += Height(trigger);

            switch ((ScheduleTrigger)trigger.enumValueIndex)
            {
                case ScheduleTrigger.TimeOfDay:
                    height += Height(property.FindPropertyRelative("m_Time"));
                    break;

                case ScheduleTrigger.SkyEvent:
                    height += Height(property.FindPropertyRelative("m_SkyEvent"));
                    break;

                case ScheduleTrigger.TimeRange:
                    height += Height(property.FindPropertyRelative("m_RangeStart"));
                    height += Height(property.FindPropertyRelative("m_RangeEnd"));
                    height += Height(property.FindPropertyRelative("m_CatchUpOnEnable"));
                    break;

                case ScheduleTrigger.Interval:
                    height += Height(property.FindPropertyRelative("m_Interval"));
                    break;
            }

            height += Height(property.FindPropertyRelative("m_Once"));
            height += Height(property.FindPropertyRelative("m_Triggered"));

            if ((ScheduleTrigger)trigger.enumValueIndex == ScheduleTrigger.TimeRange)
            {
                height += Height(property.FindPropertyRelative("m_Exited"));
            }

            return height;
        }

        private static float LineHeight => EditorGUIUtility.singleLineHeight;

        private static float Height(SerializedProperty property)
        {
            return EditorGUI.GetPropertyHeight(property, true) + k_Spacing;
        }

        private static float DrawField(Rect position, float y, SerializedProperty property)
        {
            float height = EditorGUI.GetPropertyHeight(property, true);
            var rect = new Rect(position.x, y, position.width, height);
            EditorGUI.PropertyField(rect, property, true);
            return y + height + k_Spacing;
        }

        private static GUIContent BuildHeader(SerializedProperty property, SerializedProperty label)
        {
            SerializedProperty trigger = property.FindPropertyRelative("m_Trigger");
            var type = (ScheduleTrigger)trigger.enumValueIndex;
            string detail = Describe(property, type);
            string name = string.IsNullOrEmpty(label.stringValue) ? "Schedule" : label.stringValue;

            return new GUIContent($"{name}   ({detail})");
        }

        private static string Describe(SerializedProperty property, ScheduleTrigger type)
        {
            switch (type)
            {
                case ScheduleTrigger.TimeOfDay:
                    return FormatTime(property.FindPropertyRelative("m_Time"));

                case ScheduleTrigger.SkyEvent:
                    return ObjectNames.NicifyVariableName(
                        ((SkyEvent)property.FindPropertyRelative("m_SkyEvent").enumValueIndex).ToString());

                case ScheduleTrigger.TimeRange:
                    return FormatTime(property.FindPropertyRelative("m_RangeStart")) + " – " +
                           FormatTime(property.FindPropertyRelative("m_RangeEnd"));

                default:
                    return ObjectNames.NicifyVariableName(
                        ((ScheduleInterval)property.FindPropertyRelative("m_Interval").enumValueIndex).ToString());
            }
        }

        private static string FormatTime(SerializedProperty time)
        {
            int hour = time.FindPropertyRelative("m_Hour").intValue;
            int minute = time.FindPropertyRelative("m_Minute").intValue;
            return $"{hour:00}:{minute:00}";
        }
    }
}
