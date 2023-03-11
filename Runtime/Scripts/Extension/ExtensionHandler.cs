using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GLTFast.Schema;
using GluonGui;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GLTFast
{
    [Flags]
    public enum ExtensionType
    {
        Root = 0, 
        Node = 1,
        MeshPrimitive = 2,
        Material = 4,
        Texture = 8,
        TextureInfo = 16
    }

    [Serializable]
    public abstract class ExtensionHandler: IGltfSerializable
    {
        static ExtensionHandler()
        {
            var foundHandlers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => typeof(ExtensionHandler).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
            
            handlersByExtensionName.Clear();
            handlers.Clear();
                
            foreach (var extensionHandlerType in foundHandlers)
            {
                var extensionHandlerInstance = Activator.CreateInstance(extensionHandlerType);
                if (extensionHandlerInstance is not ExtensionHandler handler) {
                    Debug.LogError($"Unable to utilize extension handler: {extensionHandlerType.FullName}");
                    continue;
                }

                var extensionName = handler.supportedExtension;
                if (handlersByExtensionName.ContainsKey(extensionName)) {
                    Debug.LogError($"An extension handler already handles the extension '{extensionName}'");
                    continue;
                }
                
                handlers.Add(handler);
                handlersByExtensionName.Add(extensionName, handler);
            }
        }

        public static HashSet<ExtensionHandler> handlers { get; private set; } = new();
        public static Dictionary<string, ExtensionHandler> handlersByExtensionName { get; private set; } = new();
        
        public abstract string supportedExtension { get; }
        public abstract ExtensionType supportedExtensionTypes { get; }
        public abstract Type GetSchemaForContext(ExtensionType context);

        // public abstract void SetRootValue(object val);
        // public abstract void SetNodeValue(object val, int nodeIndex);
        // public abstract void SetMeshPrimitive(object val, int meshIndex, int primitiveIndex);
        // public abstract void SetMaterial(object val, int materialIndex);
        // public abstract void SetTexture(object val, int textureIndex);
        // public abstract void SetTextureInfo(object val, int textureInfoIndex);
        
        protected abstract void Serialize(JsonWriter writer);
        void IGltfSerializable.GltfSerialize(JsonWriter writer) {
            Serialize(writer);
        }
    }
}
