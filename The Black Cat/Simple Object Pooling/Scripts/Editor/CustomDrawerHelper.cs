using UnityEditor;
using UnityEngine;

namespace BlackCatPool
{
    public static class CustomDrawerHelper
    {
        public static float CalculateLabelWidth(string label)
        {
            return EditorStyles.label.CalcSize(new GUIContent(label)).x + 5;
        }

        public static void Space(int time)
        {
            for (int i = 0; i < time; i++)
            {
                EditorGUILayout.Space();
            }
        }
    }

}