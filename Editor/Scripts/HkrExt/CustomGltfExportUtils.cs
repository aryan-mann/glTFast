using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTFast;
using GLTFast.Export;
using GLTFast.Logging;
using GLTFast.Schema;
using GLTFast.Utils;
using UnityEditor;
using UnityEngine;
using JToken = Newtonsoft.Json.Linq.JToken;
using Material = UnityEngine.Material;
using RootExtension = GLTFast.Schema.RootExtension;

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
            
            // Requirement: the shoe material used in the prefab model (generated on import) needs to use the imported materials (from the Materials folder) instead of the Gltf ones (under the asset directly). 
            //  Otherwise, we will get an additional material in the gltf list.
            

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
                
                // CUSTOM CODE
                export.Writer.RegisterExtensionUsage(Extension.KhrMaterialsVariants, false);
                RegisterVariantsMaterial(export.Writer, exportableModelData);
                export.Writer.BeforePrimitiveSerializationCallback += (mesh, meshPrimitive) => BeforePrimitiveSerializationCallback(mesh, meshPrimitive, exportableModelData);
                export.Writer.AddExtensionCallback += (extensionRoot) => RegisterVariantsData(extensionRoot, exportableModelData);

                export.AddScene(gameObjects, name);
                AsyncHelpers.RunSync(() => export.SaveToFileAndDispose(path));

#if GLTF_VALIDATOR
                var report = Validator.Validate(path);
                report.Log();
#endif
            }
        }

        // keep a reference of the material id when added to the Gltfwriter material list (used for mesh primitive indexing)
        static Dictionary<string, int> s_MaterialNameToDataIndex;

        static void RegisterVariantsData(RootExtension rootExtension, ExportableModelData exportableModelData)
        {
            var jsonExtData = new Newtonsoft.Json.Linq.JObject();
            var variantsArray = new Newtonsoft.Json.Linq.JArray();
            
            // Note: VariantSets are not separate in this case (KHR model)
            foreach (var variantSet in exportableModelData.materialVariantSets)
            {
                foreach (var matVariant in variantSet.exportableMaterialVariant)
                {
                    var variantArrayEntry = new Newtonsoft.Json.Linq.JObject
                    {
                        { "name", matVariant.code }
                    };
                    
                    variantsArray.Add(variantArrayEntry);
                }
            }
            jsonExtData.Add("variants", variantsArray);
            
            // Add the mesh primitive "KHR_materials_variants" data
            rootExtension.extensionsJson ??= new Dictionary<string, JToken>();
            rootExtension.extensionsJson.Add(Extension.KhrMaterialsVariants.GetName(), jsonExtData);
            
            // Structure example:
            //
            // "extensions": {
            //     "KHR_materials_variants": {
            //         "variants": [
            //         {"name": "midnight"},
            //         {"name": "beach" },
            //         {"name": "street" }
            //         ]
            //     }
        }

        static void RegisterVariantsMaterial(IGltfWritable writer, ExportableModelData exportableModelData)
        {
            s_MaterialNameToDataIndex = new Dictionary<string, int>();
            
            var variantMaterials = exportableModelData.materialVariantSets
                .SelectMany(x => x.exportableMaterialVariant)
                .Select(x => x.material)
                .ToList();
            
            // Add all materials used in variants and that was not found in the model gameObject
            foreach (var mat in variantMaterials)
            {
                if(!writer.AddMaterial(mat, out var materialId, new StandardMaterialExport()))
                    Debug.LogError("AddMaterial Failed!");
                else
                    s_MaterialNameToDataIndex.Add(mat.name, materialId);
            }
        }

        // Called when a mesh primitive is serialized
        static void BeforePrimitiveSerializationCallback(GLTFast.Schema.Mesh mesh, MeshPrimitive meshPrimitive, ExportableModelData exportableModelData)
        {
            // TODO: check if the mesh is the one targeted by the renderer path in 'ExportableMaterialSlotTarget'
            // Compare node path to "rendererPath" and add data to it?
            // var goRendererPathParts = partId.rendererPath.Split("/");

            //if(mesh.name != "shoe")
            //    return;
            
            var jsonExtData = new Newtonsoft.Json.Linq.JObject();
            var mappingArray = new Newtonsoft.Json.Linq.JArray();
            
            // Note: VariantSets are not separate in this case (KHR model)
            foreach (var variantSet in exportableModelData.materialVariantSets)
            {
                foreach (var matVariant in variantSet.exportableMaterialVariant)
                {
                    var variantArrayEntry = new Newtonsoft.Json.Linq.JObject
                    {
                        { "material", s_MaterialNameToDataIndex[matVariant.material.name] },
                        { "variants", new Newtonsoft.Json.Linq.JArray { variantSet.exportableMaterialVariant.IndexOf(matVariant) } }  // Use the same order as the variant list in 'RegisterVariantsData()'
                    };
                    
                    mappingArray.Add(variantArrayEntry);
                }
            }
            jsonExtData.Add("mappings", mappingArray);
            
            // Add the mesh primitive "KHR_materials_variants" data
            meshPrimitive.extensions ??= new MeshPrimitiveExtensions();
            meshPrimitive.extensions.extensionsJson ??= new Dictionary<string, JToken>();
            meshPrimitive.extensions.extensionsJson.Add(Extension.KhrMaterialsVariants.GetName(), jsonExtData);
            
            // Structure example:
            //
            // "extensions": {
            //     "KHR_materials_variants": {
            //         "mappings": [
            //         {
            //             "material": 0,
            //             "variants": [
            //                  0
            //             ]
            //         },
            //         {
            //             "material": 1,
            //             "variants": [
            //                  1
            //             ]
            //         },
            //         {
            //             "material": 2,
            //             "variants": [
            //                  2
            //             ]
            //         }
            //         ]
            //     }
            // },
        }
    }
}
