using UnityEngine;

namespace Mistforge
{
    /// A craftable base item (§8.5).
    [CreateAssetMenu(menuName = "Mistforge/Item Base")]
    public class ItemBaseDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public EquipSlot slot;
        [Tooltip("Weapons only: which character's weapon type this is. Ignored for armor/accessories.")]
        public WeaponType weaponType;
        public StatType primaryStat;
        public int statMin;
        public int statMax;
    }
}
