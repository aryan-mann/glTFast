using System;
using System.Collections.Generic;
using UnityEditor;

namespace GLTFast.Settings
{
    public class glTFastSettingsProvider: SettingsProvider
    {
        static string[] s_Keywords = { "gltf" };
        const string k_SettingsMenuLocation = "Project/glTFast";

        [SettingsProvider]
        public static SettingsProvider CreateGltfastSettings()
        {
            return new glTFastSettingsProvider(k_SettingsMenuLocation, SettingsScope.Project, s_Keywords);
        }

        public glTFastSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords) { }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            var settings = new SerializedObject(glTFastSettings.instance);
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.TextField("Hello", "poop");
            
            if (EditorGUI.EndChangeCheck()) {
                settings.ApplyModifiedProperties();
            }
        }
    }
}
