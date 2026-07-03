using UnityEngine;

namespace Mistforge
{
    /// A named directional-combo special move (§8.2).
    [CreateAssetMenu(menuName = "Mistforge/Art")]
    public class ArtDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public ComboDirection[] sequence;
        public float damageMultiplier = 1.8f;
        public int apCost = 12;

        public string SequenceLabel()
        {
            var parts = new string[sequence.Length];
            for (int i = 0; i < sequence.Length; i++)
                parts[i] = sequence[i].ToString().Substring(0, 1);
            return string.Join(" ", parts);
        }
    }
}
