using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GLTFast.Extensions
{
    public class RootExtensionJsonConverter: JsonConverter<Schema.RootExtension>
    {
        public override void WriteJson(JsonWriter writer, Schema.RootExtension value, JsonSerializer serializer)
        {
            // TODO: Write exporter
        }

        public override Schema.RootExtension ReadJson(JsonReader reader, Type objectType, Schema.RootExtension existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new Exception("Invalid GLTF JSON format for root extensions.");

            var extensions = JToken.ReadFrom(reader).Value<JObject>();
            if (extensions == null)
                throw new Exception("Unable to parse root extension. Check your GLTF file.");
            
            var extJson = new Dictionary<string, JToken>();
            foreach (var (extensionName, extensionJson) in extensions) {
                if (string.IsNullOrWhiteSpace(extensionName) || extensionJson == null) {
                    Debug.LogError($"Unable to parse root extension: {extensionName} (JSON: {extensionJson})");
                    continue;
                }

                // if (ExtensionHandler.handlersByExtensionName.ContainsKey(extensionName)) {
                //     var handler = ExtensionHandler.handlersByExtensionName[extensionName];
                //     var schemaType = handler.GetSchemaForContext(ExtensionType.Root);
                //     var deserializedSchema = extensionJson.ToObject(schemaType);
                //
                //     if (deserializedSchema == null) {
                //         Debug.LogError($"Could not deserialize root schema for {extensionName}");
                //         continue;
                //     }
                // }
                
                extJson.Add(extensionName, extensionJson);
            }

            return new Schema.RootExtension(extJson);
        }
    }
}
