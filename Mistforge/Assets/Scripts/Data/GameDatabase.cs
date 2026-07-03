using UnityEngine;

namespace Mistforge
{
    /// Root content container, loaded from Resources/GameDatabase at runtime.
    /// All rows are individual ScriptableObjects under Assets/ScriptableObjects/
    /// so content can be added without touching code (§8 preamble).
    [CreateAssetMenu(menuName = "Mistforge/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        public CharacterDef[] characters;
        public EnemyDef[] enemies;
        public MaterialDef[] materials;
        public ItemBaseDef[] itemBases;
        public RarityDef[] rarities;
        public AffixDef[] affixes;
        public SetDef[] sets;
        public RecipeDef[] recipes;

        static GameDatabase cached;

        public static GameDatabase Load()
        {
            if (cached == null)
                cached = Resources.Load<GameDatabase>("GameDatabase");
            if (cached == null)
                Debug.LogError("GameDatabase not found in Resources. Run Mistforge > Generate All from the editor menu.");
            return cached;
        }

        public CharacterDef Character(string id)
        {
            foreach (var c in characters) if (c.id == id) return c;
            return null;
        }

        public EnemyDef Enemy(string id)
        {
            foreach (var e in enemies) if (e.id == id) return e;
            return null;
        }

        public MaterialDef Material(string id)
        {
            foreach (var m in materials) if (m.id == id) return m;
            return null;
        }

        public ItemBaseDef ItemBase(string id)
        {
            foreach (var b in itemBases) if (b.id == id) return b;
            return null;
        }

        public RarityDef Rarity(string id)
        {
            foreach (var r in rarities) if (r.id == id) return r;
            return null;
        }

        public SetDef Set(string id)
        {
            foreach (var s in sets) if (s.id == id) return s;
            return null;
        }
    }
}
