using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GLTFast.Schema;
using GluonGui;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GLTFast.Extensions
{
    [Serializable]
    public abstract class ExtensionHandler: IGltfSerializable
    {
        public static void LoadAllExtensions(ref Dictionary<string, ExtensionHandler> handlers, GltfImport gltfImport)
        {
            var foundHandlers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => typeof(ExtensionHandler).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
            
            handlers.Clear();
            foreach (var extensionHandlerType in foundHandlers)
            {
                var extensionHandlerInstance = Activator.CreateInstance(extensionHandlerType);
                if (extensionHandlerInstance is not ExtensionHandler handler) {
                    Debug.LogError($"Unable to utilize extension handler: {extensionHandlerType.FullName}");
                    continue;
                }
                
                
                var extensionName = handler.extensionName;
                if (handlers.ContainsKey(extensionName)) {
                    Debug.LogError($"An extension handler already handles the extension '{extensionName}'");
                    continue;
                }
                
                handler.SetGltfImport(gltfImport);
                handlers.Add(extensionName, handler);
            }
        }
        
        public abstract string extensionName { get; }

        public GltfImport gltfImport { get; private set; }

        public void SetGltfImport(GltfImport gltfImport) {
            this.gltfImport ??= gltfImport;
        }

        protected abstract void Serialize(JsonWriter writer);
        void IGltfSerializable.GltfSerialize(JsonWriter writer) {
            Serialize(writer);
        }
    }
    
    public interface IRootExtensionHandler
    {
        public Type rootSchemaType { get; }
        public void HandleRoot(object val, GameObject sceneGameObject);
    }
    
    public interface IMeshPrimitiveExtensionHandler
    {
        public Type primitiveSchemaType { get; }
        public void HandleMeshPrimitive(object val, GameObject meshGameObject);
    }
}
