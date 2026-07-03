using UnityEngine;

namespace Mistforge
{
    /// A rollable affix (§8.7).
    [CreateAssetMenu(menuName = "Mistforge/Affix")]
    public class AffixDef : ScriptableObject
    {
        public string id;
        public string label;
        public string prefix;
        public StatType stat;
        public int min;
        public int max;
    }
}
