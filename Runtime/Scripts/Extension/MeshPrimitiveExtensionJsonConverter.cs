using System;
using System.Collections.Generic;
using GLTFast.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using JsonWriter = Newtonsoft.Json.JsonWriter;

namespace GLTFast
{
    public class MeshPrimitiveExtensionJsonConverter: JsonConverter<Schema.MeshPrimitiveExtensions>
    {
        public override void WriteJson(JsonWriter writer, MeshPrimitiveExtensions value, JsonSerializer serializer)
        {
            // TODO: Write exporter of Json
        }

        public override MeshPrimitiveExtensions ReadJson(JsonReader reader, Type objectType, MeshPrimitiveExtensions existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new Exception("Invalid GLTF JSON format for mesh primitive extensions.");

            var extensions = JToken.ReadFrom(reader).Value<JObject>();
            if (extensions == null)
                throw new Exception("Unable to parse mesh primitive extension. Check your GLTF file.");
            
            var extJson = new Dictionary<string, JToken>();
            foreach (var (extensionName, extensionJson) in extensions) {
                if (string.IsNullOrWhiteSpace(extensionName) || extensionJson == null) {
                    Debug.LogError($"Unable to parse mesh primitive extension: {extensionName} (JSON: {extensionJson})");
                    continue;
                }
                
                extJson.Add(extensionName, extensionJson);
            }

            return new MeshPrimitiveExtensions { extensionsJson = extJson };
        }
    }
}
