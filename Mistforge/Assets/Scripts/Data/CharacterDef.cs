using UnityEngine;

namespace Mistforge
{
    /// A playable character (§8.1).
    [CreateAssetMenu(menuName = "Mistforge/Character")]
    public class CharacterDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public string role;
        public int baseHP;
        public int baseAP;
        public int baseATK;
        public int baseDEF;
        public float baseCritPct;
        public WeaponType weaponType;

        [Header("Direction -> attack name")]
        public string attackLeft;
        public string attackRight;
        public string attackUp;
        public string attackDown;

        public ArtDef[] arts;

        [Header("Placeholder visuals (until sprite sheets land)")]
        public Color placeholderColor = Color.white;

        public string AttackName(ComboDirection dir)
        {
            switch (dir)
            {
                case ComboDirection.Left: return attackLeft;
                case ComboDirection.Right: return attackRight;
                case ComboDirection.Up: return attackUp;
                default: return attackDown;
            }
        }
    }
}
