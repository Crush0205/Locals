using System.Collections.Generic;
using UnityEngine;

namespace Mistforge
{
    /// Turn order and enemy AI (§6.4). Same simple AI as the POC:
    /// random alive target, random damage in the enemy's range.
    public class TurnController
    {
        public readonly List<BattleActor> players;
        public readonly List<BattleActor> enemies;

        public TurnController(List<BattleActor> players, List<BattleActor> enemies)
        {
            this.players = players;
            this.enemies = enemies;
        }

        public bool AllPlayersDown()
        {
            foreach (var p in players) if (p.Alive) return false;
            return true;
        }

        public bool AllEnemiesDown()
        {
            foreach (var e in enemies) if (e.Alive) return false;
            return true;
        }

        public BattleActor RandomAlive(List<BattleActor> pool)
        {
            var alive = new List<BattleActor>();
            foreach (var a in pool) if (a.Alive) alive.Add(a);
            if (alive.Count == 0) return null;
            return alive[Random.Range(0, alive.Count)];
        }

        public BattleActor FirstAlive(List<BattleActor> pool)
        {
            foreach (var a in pool) if (a.Alive) return a;
            return null;
        }

        /// Enemy damage (§9): random(atkMin, atkMax) − def × 0.4, floor of 1.
        /// Focus stance halves it.
        public int RollEnemyDamage(EnemyDef def, BattleActor target)
        {
            float dmg = Random.Range(def.atkMin, def.atkMax + 1) - target.stats.def * 0.4f;
            if (target.guarding) dmg *= 0.5f;
            return Mathf.Max(1, Mathf.FloorToInt(dmg));
        }
    }
}
