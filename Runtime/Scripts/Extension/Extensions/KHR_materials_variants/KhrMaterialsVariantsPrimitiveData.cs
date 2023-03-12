using System;
using System.Collections.Generic;
using UnityEngine;

namespace GLTFast.Extensions
{
    public class KhrMaterialsVariantsPrimitiveData : MonoBehaviour
    {
        public KhrMaterialsVariantsExtension.PrimitiveSchema primitiveSchema;
        public Dictionary<int, Material> variantIndexToMaterial;

        Renderer m_Renderer;
        void OnEnable()
        {
            m_Renderer = GetComponent<Renderer>();
        }

        public void SetMaterial(int variantIndex)
        {
            if (variantIndexToMaterial.ContainsKey(variantIndex))
                m_Renderer.sharedMaterial = variantIndexToMaterial[variantIndex];
        }
    }
}
