using System;
using UnityEngine;

namespace Mistforge
{
    /// An enemy type (§8.3).
    [CreateAssetMenu(menuName = "Mistforge/Enemy")]
    public class EnemyDef : ScriptableObject
    {
        [Serializable]
        public class LootEntry
        {
            public MaterialDef material;
            public int quantity = 1;
            [Range(0f, 1f)] public float dropChance = 0.65f;
        }

        public string id;
        public string displayName;
        public int maxHP;
        public int atkMin;
        public int atkMax;
        public int goldMin;
        public int goldMax;
        [Tooltip("Shifts item-drop rarity weights toward Rare/Set (+1 tier for Stone Cub).")]
        public int rarityBoost;
        public LootEntry[] loot;

        [Header("Encounter rolling")]
        public int spawnWeight = 30;

        [Header("Placeholder visuals")]
        public Color placeholderColor = Color.magenta;
        [Tooltip("Battle-scene frame size in pixels (32x32 standard, 40x40 Stone Cub).")]
        public Vector2Int framePixels = new Vector2Int(32, 32);
    }
}
