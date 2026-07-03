using System.Collections.Generic;
using UnityEngine;

namespace Mistforge
{
    /// Persistent singleton (DontDestroyOnLoad): party state, inventory, gold,
    /// materials, and the encounter/spawn hand-off between scenes.
    public class GameManager : MonoBehaviour
    {
        public class PartyMemberState
        {
            public string charId;
            public int currentHP;
            /// Indexed by EquipSlot: Weapon / Armor / Accessory.
            public ItemInstance[] equipped = new ItemInstance[3];
        }

        public static GameManager I { get; private set; }

        public GameDatabase db;
        public int gold;
        public Dictionary<string, int> materials = new Dictionary<string, int>();
        public List<ItemInstance> inventory = new List<ItemInstance>();
        public List<PartyMemberState> party = new List<PartyMemberState>();

        // --- Scene hand-off state ---
        public bool hasPendingEncounter;
        public EncounterData pendingEncounter;
        public bool hasPendingCellSpawn;
        public string pendingSpawnScene;
        public Vector3Int pendingSpawnCell;
        /// Set when passing through a scene gate: spawn at the target map's gate tile.
        public bool spawnAtGate;

        public static GameManager Ensure()
        {
            if (I != null) return I;
            var go = new GameObject("GameManager");
            I = go.AddComponent<GameManager>();
            DontDestroyOnLoad(go);
            I.Init();
            return I;
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            if (I == null) { I = this; DontDestroyOnLoad(gameObject); Init(); }
        }

        bool initialized;

        void Init()
        {
            if (initialized) return;
            initialized = true;

            db = GameDatabase.Load();

            // Starting purse — enough to forge one early item after a couple of fights.
            gold = 20;
            foreach (var m in db.materials) materials[m.id] = 0;
            materials["dust"] = 3;
            materials["fang"] = 2;
            materials["spore"] = 1;

            foreach (var c in db.characters)
                party.Add(new PartyMemberState { charId = c.id, currentHP = c.baseHP });

            SceneFlow.Ensure();
            ScreenFader.Ensure();
        }

        public PartyMemberState Member(string charId)
        {
            foreach (var m in party) if (m.charId == charId) return m;
            return null;
        }

        public StatBlock StatsFor(PartyMemberState member)
        {
            return StatsCalculator.Compute(db, db.Character(member.charId), member.equipped);
        }

        public void HealParty()
        {
            foreach (var m in party)
                m.currentHP = StatsFor(m).maxHP;
        }

        public int MaterialCount(string id)
        {
            materials.TryGetValue(id, out int n);
            return n;
        }

        public void AddMaterial(string id, int amount)
        {
            materials.TryGetValue(id, out int n);
            materials[id] = n + amount;
        }
    }
}
