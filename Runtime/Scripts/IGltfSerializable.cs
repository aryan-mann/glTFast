using GLTFast.Schema;

namespace GluonGui
{
    public interface IGltfSerializable
    {
        internal void GltfSerialize(JsonWriter writer);
    }
}
