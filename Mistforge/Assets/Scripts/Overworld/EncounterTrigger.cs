using System.Collections.Generic;
using UnityEngine;

namespace Mistforge
{
    /// Rolls random encounters when the player steps into tall grass (§4.2/§4.3).
    /// encounterChance is Inspector-tunable per the spec (~16% from the POC).
    public class EncounterTrigger : MonoBehaviour
    {
        [Range(0f, 1f)] public float encounterChance = 0.16f;
        [Range(0f, 1f)] public float pairChance = 0.3f;   // §8.3: 70% single, 30% pair

        /// Returns true if an encounter fired (the scene transition has begun).
        public bool TryStartEncounter(string returnScene, Vector3Int cell)
        {
            if (Random.value >= encounterChance) return false;

            var db = GameManager.Ensure().db;
            var ids = new List<string> { RollEnemy(db).id };
            if (Random.value < pairChance)
                ids.Add(RollEnemy(db).id);

            int boost = 0;
            foreach (var id in ids)
                boost = Mathf.Max(boost, db.Enemy(id).rarityBoost);

            SceneFlow.Ensure().StartEncounter(new EncounterData
            {
                enemyIds = ids.ToArray(),
                returnScene = returnScene,
                returnCell = cell,
                qualityBoost = boost,
            });
            return true;
        }

        static EnemyDef RollEnemy(GameDatabase db)
        {
            int total = 0;
            foreach (var e in db.enemies) total += e.spawnWeight;
            int roll = Random.Range(0, total);
            foreach (var e in db.enemies)
            {
                roll -= e.spawnWeight;
                if (roll < 0) return e;
            }
            return db.enemies[0];
        }
    }
}
