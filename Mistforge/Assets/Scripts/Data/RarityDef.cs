using UnityEngine;

namespace Mistforge
{
    /// A rarity tier (§8.6).
    [CreateAssetMenu(menuName = "Mistforge/Rarity")]
    public class RarityDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("Base roll weight before any quality boost.")]
        public int weight;
        public int affixCount;
        public float statMultiplier = 1f;
        [Tooltip("Set items carry a fixed set identity in addition to their affix.")]
        public bool isSet;
        public Color color = Color.white;

        [Header("Quality boost tuning (per boost tier, same curve as the POC's rollRarity)")]
        [Tooltip("Added to weight per quality-boost tier. Negative for Common so boosted crafts shift toward Rare/Set.")]
        public int weightPerBoost;
        public int minWeight = 4;

        public int EffectiveWeight(int qualityBoost)
        {
            return Mathf.Max(minWeight, weight + weightPerBoost * qualityBoost);
        }
    }
}
