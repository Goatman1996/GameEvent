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
            var settings = new GameEventSettingsProvider("Project/GameEventSettings", SettingsScope.Project);
            settings.instance = GameEventSettings.Instance;
            settings.m_SerializedObject = new SerializedObject(settings.instance);
            return settings;
        }

        GameEventSettings instance;
        SerializedObject m_SerializedObject;

        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            if (m_SerializedObject == null || instance == null)
            {
                this.instance = GameEventSettings.Instance;
                this.m_SerializedObject = new SerializedObject(this.instance);
            }
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