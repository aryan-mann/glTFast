using System;
using System.Collections.Generic;
using GLTFast.Schema;
using Newtonsoft.Json.Linq;

namespace GLTFast
{
    public class KhrMaterialsVariantsExtension: ExtensionHandler
    {
        public sealed class PrimitiveSchema
        {
            public sealed class MaterialMapping
            {
                public int material;
                public int[] variants;
            }
            public List<MaterialMapping> mappings;
        }

        public sealed class RootSchema
        {
            public sealed class Variant {
                public string name;
            }
            public List<Variant> variants;
        }
        
        public override string supportedExtension => "KHR_materials_variants";
        public override ExtensionType supportedExtensionTypes => ExtensionType.Root & ExtensionType.MeshPrimitive;

        public override Type GetSchemaForContext(ExtensionType context)
        {
            return context switch
            {
                ExtensionType.MeshPrimitive => typeof(PrimitiveSchema),
                ExtensionType.Root => typeof(RootSchema),
                _ => throw new Exception("Unsupported context for KhrMaterialsVariants")
            };
        }

        protected override void Serialize(JsonWriter writer)
        {
            
        }
    }
}
