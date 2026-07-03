using System;
using System.Collections.Generic;

namespace Mistforge
{
    /// A rolled, owned piece of equipment. Plain serializable data so it can
    /// live in save state without asset references.
    [Serializable]
    public class ItemInstance
    {
        [Serializable]
        public class AffixRoll
        {
            public string affixId;
            public string label;
            public StatType stat;
            public int value;
        }

        public string baseId;
        public string displayName;
        public EquipSlot slot;
        public WeaponType weaponType;   // meaningful for weapons only
        public string rarityId;
        public string setId;            // empty unless a Set-rarity item
        public StatType primaryStat;
        public int primaryValue;
        public List<AffixRoll> affixes = new List<AffixRoll>();

        public bool IsSetPiece => !string.IsNullOrEmpty(setId);

        public string Describe(GameDatabase db)
        {
            var lines = new List<string> { StatLabel(primaryStat, primaryValue) };
            foreach (var a in affixes)
                lines.Add(StatLabel(a.stat, a.value));
            if (IsSetPiece)
            {
                var set = db.Set(setId);
                lines.Add((set != null ? set.displayName : setId) + " set piece");
            }
            return string.Join(", ", lines);
        }

        public static string StatLabel(StatType stat, int value)
        {
            switch (stat)
            {
                case StatType.ATK: return "+" + value + " ATK";
                case StatType.DEF: return "+" + value + " DEF";
                case StatType.MaxAP: return "+" + value + " Max AP";
                case StatType.CritPct: return "+" + value + "% Crit";
                case StatType.APGainPct: return "+" + value + "% AP Gain";
                default: return "+" + value + "% Art Power";
            }
        }
    }
}
