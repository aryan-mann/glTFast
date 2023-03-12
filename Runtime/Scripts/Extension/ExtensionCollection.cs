using System.Collections;
using System.Collections.Generic;
using GLTFast.Schema;

namespace GLTFast.Extensions
{
    public class ExtensionCollection: IEnumerable<ExtensionHandler>
    {
        Dictionary<string, ExtensionHandler> m_Handlers;

        public ExtensionCollection(GltfImport gltfImport)
        {
            m_Handlers = new Dictionary<string, ExtensionHandler>();
            ExtensionHandler.LoadAllExtensions(ref m_Handlers, gltfImport);
        }

        public IEnumerator<ExtensionHandler> GetEnumerator()
        {
            return m_Handlers.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
