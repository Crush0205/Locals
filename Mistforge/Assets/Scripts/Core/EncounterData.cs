using UnityEngine;

namespace Mistforge
{
    /// Everything a battle needs, passed Overworld -> Battle -> back (§5).
    /// Kept as a small struct rather than global mutable fields for testability.
    [System.Serializable]
    public struct EncounterData
    {
        public string[] enemyIds;
        public string returnScene;
        public Vector3Int returnCell;
        /// Highest rarityBoost among the rolled enemies (Stone Cub: +1 tier).
        public int qualityBoost;
    }
}
