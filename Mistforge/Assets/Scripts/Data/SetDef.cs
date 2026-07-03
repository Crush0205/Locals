using System;
using UnityEngine;

namespace Mistforge
{
    /// An equipment set with piece-count bonuses (§8.8).
    [CreateAssetMenu(menuName = "Mistforge/Set")]
    public class SetDef : ScriptableObject
    {
        [Serializable]
        public class SetBonus
        {
            public int pieces = 2;
            public string description;
            public StatType stat;
            public float value;
            [Tooltip("Emberwake 3-piece: attacks may ignite foes (burn DoT in battle).")]
            public bool grantsIgnite;
        }

        public string id;
        public string displayName;
        public Color color = new Color(1f, 0.55f, 0.15f);
        public SetBonus[] bonuses;
    }
}
