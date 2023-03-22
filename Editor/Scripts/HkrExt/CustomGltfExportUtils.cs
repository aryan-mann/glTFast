using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTFast;
using GLTFast.Export;
using GLTFast.Logging;
using GLTFast.Schema;
using GLTFast.Utils;
using Newtonsoft.Json.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using JToken = Newtonsoft.Json.Linq.JToken;
using Material = UnityEngine.Material;

namespace Editor.Scripts
{
    public struct ExportableModelData
    {
        public List<ExportableVariantSet> materialVariantSets;
    }

    public struct ExportableVariantSet
    {
        public Guid id;
        public List<ExportableMaterialSlotTarget> exportableMaterialSlotTarget;
        public List<ExportableMaterialVariant> exportableMaterialVariant;
    }

    public struct ExportableMaterialSlotTarget
    {
        public string rendererPath;
        public int rendererSlotIndex;
    }

    public struct ExportableMaterialVariant
    {
        public Guid variantId;
        public string code;
        public Material material;
    }
    
    public static class CustomGltfExportUtils
    {
        const string k_GltfExtension = "gltf";
        const string k_GltfBinaryExtension = "glb";
        
        public static void ExportGltfFromDTVariants(GameObject gameObject, ExportableModelData exportableModelData)
        {
            // var goRendererPathParts = partId.rendererPath.Split("/");
            Export(false, gameObject.name, new[] { gameObject }, exportableModelData);
        }

        static string SaveFolderPath
        {
            get
            {
                var saveFolderPath = EditorUserSettings.GetConfigValue("glTF.saveFilePath");
                if (string.IsNullOrEmpty(saveFolderPath))
                {
                    saveFolderPath = Application.streamingAssetsPath;
                }
                return saveFolderPath;
            }
            set => EditorUserSettings.SetConfigValue("glTF.saveFilePath", value);
        }

        static ExportSettings GetDefaultSettings(bool binary)
        {
            var settings = new ExportSettings
            {
                Format = binary ? GltfFormat.Binary : GltfFormat.Json
            };
            return settings;
        }
        
        static void Export(bool binary, string name, GameObject[] gameObjects, ExportableModelData exportableModelData)
        {
            var extension = binary ? k_GltfBinaryExtension : k_GltfExtension;
            var path = EditorUtility.SaveFilePanel(
                "glTF Export Path",
                SaveFolderPath,
                $"{name}.{extension}",
                extension
            );
            if (!string.IsNullOrEmpty(path))
            {
                SaveFolderPath = Directory.GetParent(path)?.FullName;
                var settings = GetDefaultSettings(binary);
                var goSettings = new GameObjectExportSettings { OnlyActiveInHierarchy = false };
                var export = new GameObjectExport(settings, gameObjectExportSettings: goSettings, logger: new ConsoleLogger());
                
                // Custom stuff
                export.Writer.BeforePrimitiveSerializationCallback += (mesh, meshPrimitive) => BeforePrimitiveSerializationCallback(mesh, meshPrimitive, exportableModelData);
                export.Writer.RegisterExtensionUsage(Extension.KhrMaterialsVariants, false);
                RegisterVariantsData(export.Writer, exportableModelData);
                
                export.AddScene(gameObjects, name);
                
                // Custom stuff
                RegisterVariantsMaterial(export.Writer, exportableModelData);
                
                AsyncHelpers.RunSync(() => export.SaveToFileAndDispose(path));

#if GLTF_VALIDATOR
                var report = Validator.Validate(path);
                report.Log();
#endif
            }
        }

        // keep a reference of the material id when added to the Gltfwriter material list (used for mesh primitive indexing)
        static Dictionary<Material, int> s_MaterialToIndex;
        static Dictionary<string, string> s_VariantCodeToIndex;

        static void RegisterVariantsData(GltfWriter writer, ExportableModelData exportableModelData)
        {
            // TODO: add the variants to top level 
            // ex:
            // "extensions": {
            //     "KHR_materials_variants": {
            //         "variants": [
            //         {"name": "midnight"},
            //         {"name": "beach" },
            //         {"name": "street" }
            //         ]
            //     }
        }

        static void RegisterVariantsMaterial(GltfWriter writer, ExportableModelData exportableModelData)
        {
            s_MaterialToIndex = new Dictionary<Material, int>();
            
            var allUsedMaterial = exportableModelData.materialVariantSets
                .SelectMany(x => x.exportableMaterialVariant)
                .Select(x => x.material)
                .ToList();
            
            foreach (var mat in allUsedMaterial.Where(uMaterial => !writer.UnityMaterials.Select(m => m.name).Contains(uMaterial.name)))
            {
                if(!writer.AddMaterial(mat, out var materialId, new StandardMaterialExport()))
                    Debug.LogError("AddMaterial Failed!");
                else
                    s_MaterialToIndex.Add(mat, materialId);
            }
        }

        // Called when a mesh primitive is serialized
        static void BeforePrimitiveSerializationCallback(GLTFast.Schema.Mesh mesh, MeshPrimitive meshPrimitive, ExportableModelData exportableModelData)
        {
            // TODO: check if the mesh is the one targeted by the renderer path in 'ExportableMaterialSlotTarget'
            // Compare node path to "rendererPath" and add data to it?
            
            //if(mesh.name != "shoe")
            //    return;
            
            var jsonExtData = new Newtonsoft.Json.Linq.JObject();
            var mappingArray = new Newtonsoft.Json.Linq.JArray();
            foreach (var variantSet in exportableModelData.materialVariantSets)
            {
                foreach (var matVariant in variantSet.exportableMaterialVariant)
                {
                    var variantArrayEntry = new Newtonsoft.Json.Linq.JObject
                    {
                        { "material", 0 },                                          // TODO: find the index of the material     s_MaterialToIndex[matVariant.material]
                        { "variants", new Newtonsoft.Json.Linq.JArray { 0 } }       // TODO: find the index of the variant      s_VariantCodeToIndex[matVariant.variantId]
                    };
                    
                    mappingArray.Add(variantArrayEntry);
                }
            }
            jsonExtData.Add("mappings", mappingArray);
            
            // Add the mesh primitive "KHR_materials_variants" data
            meshPrimitive.extensions ??= new MeshPrimitiveExtensions();
            meshPrimitive.extensions.extensionsJson ??= new Dictionary<string, JToken>();
            meshPrimitive.extensions.extensionsJson.Add(Extension.KhrMaterialsVariants.GetName(), jsonExtData);
        }
    }
}
