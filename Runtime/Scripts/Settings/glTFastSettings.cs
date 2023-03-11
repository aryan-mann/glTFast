using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GLTFast.Settings
{
    [FilePath("glTFastConfig", FilePathAttribute.Location.ProjectFolder)]
    [Serializable]
    public class glTFastSettings: ScriptableSingleton<glTFastSettings>
    {
        [SerializeField]
        public List<Type> extensionHandlers = new();
    }
}
