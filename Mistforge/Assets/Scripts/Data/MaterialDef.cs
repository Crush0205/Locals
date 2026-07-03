using UnityEngine;

namespace Mistforge
{
    /// A crafting material (§8.4). qualityTier feeds the rarity roll boost.
    [CreateAssetMenu(menuName = "Mistforge/Material")]
    public class MaterialDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public int qualityTier;
        public Color placeholderColor = Color.gray;
    }
}
