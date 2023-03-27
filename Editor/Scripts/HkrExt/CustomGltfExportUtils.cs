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
    public static class CustomGltfExportUtils
    {
        const string k_GltfExtension = "gltf";
        const string k_GltfBinaryExtension = "glb";
        
        public static void ExportGltfFromDTVariants(GameObject gameObject, List<string> khrVariants, Dictionary<string, Dictionary<Material, List<int>>> mappingData, Material[] khrMaterials)
        {
            // Requirement: the shoe material used in the prefab model (generated on import) needs to use the imported materials (from the Materials folder) instead of the Gltf ones (under the asset directly). 
            //  Otherwise, we will get an additional material in the gltf list.
            
            Export(false, gameObject.name, new[] { gameObject }, khrVariants, mappingData, khrMaterials);
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
        
        static void Export(bool binary, string name, GameObject[] gameObjects, List<string> khrVariants, Dictionary<string, Dictionary<Material, List<int>>> mappingData, Material[] khrMaterials)
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
                RegisterVariantsMaterial(export.Writer, khrMaterials);
                export.Writer.BeforePrimitiveSerializationCallback += (mesh, meshPrimitive) => BeforePrimitiveSerializationCallback(mesh, meshPrimitive, mappingData, khrMaterials);
                export.Writer.AddExtensionCallback += (extensionRoot) => RegisterVariantsData(extensionRoot, khrVariants);

                export.AddScene(gameObjects, name);
                AsyncHelpers.RunSync(() => export.SaveToFileAndDispose(path));

#if GLTF_VALIDATOR
                var report = Validator.Validate(path);
                report.Log();
#endif
            }
        }
        
        static void RegisterVariantsData(RootExtension rootExtension, List<string> khrVariants)
        {
            var jsonExtData = new Newtonsoft.Json.Linq.JObject();
            var variantsArray = new Newtonsoft.Json.Linq.JArray();

            foreach (var variantCode in khrVariants)
            {
                var variantArrayEntry = new Newtonsoft.Json.Linq.JObject
                {
                    { "name", variantCode }
                };
                    
                variantsArray.Add(variantArrayEntry);
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

        static void RegisterVariantsMaterial(IGltfWritable writer, IEnumerable<Material> khrMaterials)
        {
            // Add all materials used in variants and that was not found in the model gameObject
            foreach (var mat in khrMaterials)
            {
                if(!writer.AddMaterial(mat, out _, new StandardMaterialExport()))
                    Debug.LogError("AddMaterial Failed!");
            }
        }

        // Called when a mesh primitive is serialized
        static void BeforePrimitiveSerializationCallback(GLTFast.Schema.Mesh mesh, MeshPrimitive meshPrimitive, IReadOnlyDictionary<string, Dictionary<Material, List<int>>> mappingData, IEnumerable<Material> khrMaterials)
        {
            if(!mappingData.ContainsKey(mesh.name))
                return;
            
            var jsonExtData = new Newtonsoft.Json.Linq.JObject();
            var mappingArray = new Newtonsoft.Json.Linq.JArray();
            var materialList = khrMaterials.ToList();            
            foreach (var materialToVariantIndexKeyPair in mappingData[mesh.name])
            {
                var variantArrayEntry = new Newtonsoft.Json.Linq.JObject
                {
                    { "material", materialList.IndexOf(materialToVariantIndexKeyPair.Key) },
                    { "variants", new Newtonsoft.Json.Linq.JArray(materialToVariantIndexKeyPair.Value.ToArray()) }
                };
                    
                mappingArray.Add(variantArrayEntry);
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
