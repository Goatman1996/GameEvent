using UnityEngine;
using System.Collections.Generic;

namespace GameEvent
{
    public class GameEventSettings : ScriptableObject
    {
        [Header("包含了【事件的定义】及【事件的使用】的程序集，默认 Assembly-CSharp")]
        public List<string> assemblyList = new List<string>() { "Assembly-CSharp" };
        [Header("打印注入成功的Log")]
        public bool needInjectedLog;

        private static GameEventSettings _Instance;
        public static GameEventSettings Instance
        {
            get
            {
                if (_Instance == null)
                {
                    LoadInstance();
                }
                return _Instance;
            }
        }

        private const string SettingsPath = "ProjectSettings/GameEventSettings.asset";
        private static void LoadInstance()
        {
            var objArray = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
            if (objArray.Length == 0)
            {
                _Instance = GameEventSettings.CreateInstance<GameEventSettings>();
                UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { _Instance }, SettingsPath, true);
            }
            else
            {
                _Instance = objArray[0] as GameEventSettings;
            }
        }

        public void Save()
        {
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { _Instance }, SettingsPath, true);
        }
    }
}