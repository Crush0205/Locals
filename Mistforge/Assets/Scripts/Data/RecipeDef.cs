using System;
using UnityEngine;

namespace Mistforge
{
    /// A Forge recipe (§8.9).
    [CreateAssetMenu(menuName = "Mistforge/Recipe")]
    public class RecipeDef : ScriptableObject
    {
        [Serializable]
        public class MaterialCost
        {
            public MaterialDef material;
            public int quantity = 1;
        }

        public ItemBaseDef result;
        public MaterialCost[] costs;
        public int goldCost;

        /// Highest quality tier among cost materials; feeds the rarity roll.
        public int BaseQualityBoost()
        {
            int boost = 0;
            foreach (var c in costs)
                if (c.material != null && c.material.qualityTier > boost)
                    boost = c.material.qualityTier;
            return boost;
        }
    }
}
