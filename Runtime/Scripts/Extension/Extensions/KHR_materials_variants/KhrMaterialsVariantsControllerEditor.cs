using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace GLTFast.Extensions
{
    [CustomEditor(typeof(KhrMaterialsVariantsController))]
    public class KhrMaterialsVariantsControllerEditor: Editor
    {
        public KhrMaterialsVariantsExtension.RootSchema data;
        [CanBeNull]
        string[] options = Array.Empty<string>();
        
        SerializedProperty m_Variants;
        SerializedProperty m_CurrentVariantIndex;
        
        void OnEnable()
        {
            m_Variants = serializedObject.FindProperty("variants");
            m_CurrentVariantIndex = serializedObject.FindProperty("currentVariantIndex");
        }

        public override void OnInspectorGUI()
        {
            if (options is { Length: 0 }) {
                if (m_Variants.arraySize > 0)
                {
                    options = new string[m_Variants.arraySize];
                    for (var i = 0; i < m_Variants.arraySize; i++) {
                        options[i] = m_Variants.GetArrayElementAtIndex(i).stringValue;
                    }
                }
            }

            var changed = false;
            
            serializedObject.Update();
            
            var activeVariant = EditorGUILayout.Popup("Active Variant", m_CurrentVariantIndex.intValue, options, EditorStyles.popup);
            if (activeVariant != m_CurrentVariantIndex.intValue) {
                changed = true;
                m_CurrentVariantIndex.intValue = activeVariant;
            }
            serializedObject.ApplyModifiedProperties();

            if (changed)
            {
                if (serializedObject.targetObject is KhrMaterialsVariantsController c)
                {
                    c.UpdateVariant();
                }
            }
        }
    }
}
