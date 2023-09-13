using System.Collections.Generic;
using UnityEditor;

namespace GameEvent
{
    public class GameEventSettingsProvider : SettingsProvider
    {
        public GameEventSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        [SettingsProvider]
        public static SettingsProvider GetSettings()
        {
            return new GameEventSettingsProvider("Project/GameEventSettings", SettingsScope.Project);
        }

        public override void OnGUI(string searchContext)
        {
            var instance = GameEventSettings.Instance;

            EditorGUI.BeginChangeCheck();
            SerializedObject m_SerializedObject = new SerializedObject(instance);
            m_SerializedObject.Update();
            SerializedProperty m_SerializedProperty = m_SerializedObject.GetIterator();

            m_SerializedProperty.NextVisible(true);
            UnityEngine.GUI.enabled = false;
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.PropertyField(rect, m_SerializedProperty);
            UnityEngine.GUI.enabled = true;

            while (m_SerializedProperty.NextVisible(false))
            {
                EditorGUILayout.PropertyField(m_SerializedProperty);
            }

            m_SerializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                instance.Save();
            }
        }

    }
}