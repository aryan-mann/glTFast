using System;
using System.Collections.Generic;
using System.Linq;
using GLTFast.Schema;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Material = UnityEngine.Material;

namespace GLTFast.Extensions
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class KhrMaterialsVariantsExtension: ExtensionHandler, IMeshPrimitiveExtensionHandler, IRootExtensionHandler
    {
        #region Schema
        [Serializable]
        public sealed class PrimitiveSchema
        {
            [Serializable]
            public sealed class MaterialMapping
            {
                public int material;
                public int[] variants;
            }
            public List<MaterialMapping> mappings;
        }

        [Serializable]
        public sealed class RootSchema
        {
            [Serializable]
            public sealed class Variant {
                public string name;
            }
            public List<Variant> variants;
        }
        #endregion
        
        public override string extensionName => "KHR_materials_variants";

        public Type rootSchemaType => typeof(RootSchema);
        public void HandleRoot(object val, GameObject sceneGameObject)
        {
            if (val is RootSchema rs) {
                var monoData = sceneGameObject.AddComponent<KhrMaterialsVariantsController>();
                monoData.data = rs;
                monoData.variants = rs.variants.Select(x => x.name).ToArray();
            }
        }
        
        public Type primitiveSchemaType => typeof(PrimitiveSchema);
        public void HandleMeshPrimitive(object val, GameObject meshGameObject)
        {
            if (val is PrimitiveSchema schema)
            {
                var monoData = meshGameObject.AddComponent<KhrMaterialsVariantsPrimitiveData>();
                monoData.primitiveSchema = schema;
                monoData.variantIndexToMaterial = new Dictionary<int, Material>();
                
                foreach (var materialMapping in schema.mappings)
                {
                    
                    var gltfRootMaterial = gltfImport.GetMaterial(materialMapping.material);
                    foreach (var variantIndex in materialMapping.variants)
                    {
                        if (!monoData.variantIndexToMaterial.ContainsKey(variantIndex))
                            monoData.variantIndexToMaterial[variantIndex] = gltfRootMaterial;
                        else
                            Debug.LogError("TODO: Show error since why does two different materials belong to the same variant");
                    }
                }
            }
        }
        
        protected override void Serialize(JsonWriter writer) { }
    }
}
