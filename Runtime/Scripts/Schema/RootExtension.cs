// Copyright 2020-2022 Andreas Atteneder
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTFast.Schema
{

    /// <summary>
    /// glTF root extensions
    /// </summary>
    [System.Serializable]
    public class RootExtension
    {
        // CUSTOM CODE ----------------------------
        public Action<RootExtension> AddExtensionCallback;
        
        
        /// <inheritdoc cref="LightsPunctual"/>
        // ReSharper disable once InconsistentNaming
        public LightsPunctual KHR_lights_punctual;
        
        // TODO: Loads in all the extensions as a raw representation of the json. Refactor in future.
        [JsonIgnore]
        public Dictionary<string, JToken> extensionsJson;

        public RootExtension()
        {
            extensionsJson = new Dictionary<string, JToken>(); 
        }
        
        public RootExtension(Dictionary<string, JToken> values)
        {
            extensionsJson = values;
            foreach (var (extensionName, extensionValue) in extensionsJson)
            {
                switch (extensionName)
                {
                    case nameof(KHR_lights_punctual):
                        KHR_lights_punctual = extensionValue.Value<LightsPunctual>();
                        break;
                }
            }
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (KHR_lights_punctual != null)
            {
                writer.AddProperty("KHR_lights_punctual");
                KHR_lights_punctual.GltfSerialize(writer);
            }
            
            // CUSTOM CODE ----------------------------
            AddExtensionCallback?.Invoke(this);

            // CUSTOM CODE ----------------------------------------------------------
            if (extensionsJson.Count > 0)
            {
                // Ignore the light extension that is serialized above
                foreach (var extensionKeyPair in extensionsJson.Where(ext => ext.Key != "KHR_lights_punctual"))
                {
                    writer.AddProperty(extensionKeyPair.Key, extensionKeyPair.Value);
                }
            }
            
            writer.Close();
        }
    }
}
