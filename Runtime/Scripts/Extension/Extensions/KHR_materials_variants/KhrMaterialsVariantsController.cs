using UnityEngine;

namespace GLTFast.Extensions
{
    public class KhrMaterialsVariantsController: MonoBehaviour
    {
        public int currentVariantIndex = 0;
        public KhrMaterialsVariantsExtension.RootSchema data;
        public string[] variants;

        public void UpdateVariant()
        {
            if (currentVariantIndex < 0 || currentVariantIndex >= data.variants.Count) {
                Debug.LogError($"Invalid variant index: {currentVariantIndex}. Resetting.");
                currentVariantIndex = 0;
                return;
            }

            foreach (var pd in GetComponentsInChildren<KhrMaterialsVariantsPrimitiveData>()) {
                pd.SetMaterial(currentVariantIndex);
            }
        }
    }
}
