using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

#if UNITY_2021_1_OR_NEWER
            while (m_SerializedProperty.NextVisible(true))
#else
            while (m_SerializedProperty.NextVisible(false))
#endif

            {
                EditorGUILayout.PropertyField(m_SerializedProperty);
            }

            m_SerializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                instance.Save();
            }

            var noReloadBtn = Application.isPlaying || EditorApplication.isCompiling;

            UnityEngine.GUI.enabled = noReloadBtn == false;
            var needReload = GUILayout.Button("重新编译脚本\nRecompile & Reload Assembly");
            if (needReload)
            {
                if (noReloadBtn == false)
                {
                    UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                }
            }
            UnityEngine.GUI.enabled = true;
        }

    }
}