using UnityEditor;
using UnityEngine;

namespace Celestia.Editor
{
    [CustomPropertyDrawer(typeof(TimeOfDay))]
    public class TimeOfDayPropertyDrawer : PropertyDrawer
    {
        private const float k_ColonWidth = 10f;
        private const float k_Gap = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect field = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            SerializedProperty hour = property.FindPropertyRelative("m_Hour");
            SerializedProperty minute = property.FindPropertyRelative("m_Minute");

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float partWidth = (field.width - k_ColonWidth - k_Gap * 2f) * 0.5f;
            partWidth = Mathf.Min(partWidth, 48f);

            var hourRect = new Rect(field.x, field.y, partWidth, field.height);
            var colonRect = new Rect(hourRect.xMax + k_Gap, field.y, k_ColonWidth, field.height);
            var minuteRect = new Rect(colonRect.xMax + k_Gap, field.y, partWidth, field.height);

            EditorGUI.BeginChangeCheck();
            int newHour = EditorGUI.IntField(hourRect, hour.intValue);
            EditorGUI.LabelField(colonRect, ":");
            int newMinute = EditorGUI.IntField(minuteRect, minute.intValue);

            if (EditorGUI.EndChangeCheck())
            {
                hour.intValue = Mathf.Clamp(newHour, 0, TimeOfDay.HoursPerDay - 1);
                minute.intValue = Mathf.Clamp(newMinute, 0, TimeOfDay.MinutesPerHour - 1);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
