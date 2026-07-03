using System.Collections.Generic;
using UnityEngine;

namespace Mistforge
{
    /// Item generation math carried over from the prototype's rollRarity /
    /// affix rolls (§8.6–8.8, §9).
    public static class ItemGenerator
    {
        public static ItemInstance Generate(GameDatabase db, ItemBaseDef baseDef, int qualityBoost)
        {
            var rarity = RollRarity(db, qualityBoost);

            var item = new ItemInstance
            {
                baseId = baseDef.id,
                slot = baseDef.slot,
                weaponType = baseDef.weaponType,
                rarityId = rarity.id,
                primaryStat = baseDef.primaryStat,
                primaryValue = Mathf.Max(1, Mathf.RoundToInt(
                    Random.Range(baseDef.statMin, baseDef.statMax + 1) * rarity.statMultiplier)),
            };

            // Roll distinct affixes from the pool.
            var pool = new List<AffixDef>(db.affixes);
            for (int i = 0; i < rarity.affixCount && pool.Count > 0; i++)
            {
                var affix = pool[Random.Range(0, pool.Count)];
                pool.Remove(affix);
                item.affixes.Add(new ItemInstance.AffixRoll
                {
                    affixId = affix.id,
                    label = affix.label,
                    stat = affix.stat,
                    value = Random.Range(affix.min, affix.max + 1),
                });
            }

            if (rarity.isSet && db.sets.Length > 0)
                item.setId = db.sets[Random.Range(0, db.sets.Length)].id;

            item.displayName = BuildName(db, baseDef, item);
            return item;
        }

        public static RarityDef RollRarity(GameDatabase db, int qualityBoost)
        {
            int total = 0;
            foreach (var r in db.rarities) total += r.EffectiveWeight(qualityBoost);
            int roll = Random.Range(0, total);
            foreach (var r in db.rarities)
            {
                roll -= r.EffectiveWeight(qualityBoost);
                if (roll < 0) return r;
            }
            return db.rarities[0];
        }

        static string BuildName(GameDatabase db, ItemBaseDef baseDef, ItemInstance item)
        {
            if (item.IsSetPiece)
            {
                var set = db.Set(item.setId);
                return (set != null ? set.displayName : item.setId) + " " + baseDef.displayName;
            }
            if (item.affixes.Count > 0)
            {
                // Prefix from the first affix (Honed / Vicious / ...), §8.7.
                foreach (var affix in db.affixes)
                    if (affix.id == item.affixes[0].affixId)
                        return affix.prefix + " " + baseDef.displayName;
            }
            return baseDef.displayName;
        }
    }
}
