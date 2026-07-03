using System.Collections.Generic;

namespace Mistforge
{
    public static class StatsCalculator
    {
        public static StatBlock Compute(GameDatabase db, CharacterDef character, IEnumerable<ItemInstance> equipped)
        {
            var stats = new StatBlock
            {
                maxHP = character.baseHP,
                maxAP = character.baseAP,
                atk = character.baseATK,
                def = character.baseDEF,
                critPct = character.baseCritPct,
            };

            var setCounts = new Dictionary<string, int>();
            foreach (var item in equipped)
            {
                if (item == null) continue;
                Apply(ref stats, item.primaryStat, item.primaryValue);
                foreach (var a in item.affixes)
                    Apply(ref stats, a.stat, a.value);
                if (item.IsSetPiece)
                {
                    setCounts.TryGetValue(item.setId, out int n);
                    setCounts[item.setId] = n + 1;
                }
            }

            foreach (var pair in setCounts)
            {
                var set = db.Set(pair.Key);
                if (set == null) continue;
                foreach (var bonus in set.bonuses)
                {
                    if (pair.Value < bonus.pieces) continue;
                    Apply(ref stats, bonus.stat, (int)bonus.value);
                    if (bonus.grantsIgnite) stats.igniteOnHit = true;
                }
            }
            return stats;
        }

        /// Active set bonus descriptions for the Forge readout (§7).
        public static List<string> ActiveSetBonuses(GameDatabase db, IEnumerable<ItemInstance> equipped)
        {
            var setCounts = new Dictionary<string, int>();
            foreach (var item in equipped)
            {
                if (item == null || !item.IsSetPiece) continue;
                setCounts.TryGetValue(item.setId, out int n);
                setCounts[item.setId] = n + 1;
            }

            var lines = new List<string>();
            foreach (var pair in setCounts)
            {
                var set = db.Set(pair.Key);
                if (set == null) continue;
                foreach (var bonus in set.bonuses)
                {
                    string state = pair.Value >= bonus.pieces ? "ACTIVE" : pair.Value + "/" + bonus.pieces;
                    lines.Add(set.displayName + " (" + bonus.pieces + "pc, " + state + "): " + bonus.description);
                }
            }
            return lines;
        }

        static void Apply(ref StatBlock stats, StatType stat, int value)
        {
            switch (stat)
            {
                case StatType.ATK: stats.atk += value; break;
                case StatType.DEF: stats.def += value; break;
                case StatType.MaxAP: stats.maxAP += value; break;
                case StatType.CritPct: stats.critPct += value; break;
                case StatType.APGainPct: stats.apGainPct += value; break;
                case StatType.ArtDmgPct: stats.artDmgPct += value; break;
            }
        }
    }
}
