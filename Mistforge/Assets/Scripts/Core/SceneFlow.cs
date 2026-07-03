using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mistforge
{
    /// Owns overworld <-> battle transitions: fade, scene load, and the
    /// return-to cache (§5). Lives on the persistent GameManager object graph.
    public class SceneFlow : MonoBehaviour
    {
        public const string BattleScene = "Battle";
        public const string TownScene = "Town_Rimwatch";
        public const string FieldScene = "Field_Hushfields";

        public static SceneFlow I { get; private set; }

        public float fadeDuration = 0.3f;

        public static SceneFlow Ensure()
        {
            if (I != null) return I;
            var go = new GameObject("SceneFlow");
            I = go.AddComponent<SceneFlow>();
            DontDestroyOnLoad(go);
            return I;
        }

        bool transitioning;

        public bool IsTransitioning => transitioning;

        public void StartEncounter(EncounterData data)
        {
            if (transitioning) return;
            var gm = GameManager.Ensure();
            gm.pendingEncounter = data;
            gm.hasPendingEncounter = true;
            StartCoroutine(FadeAndLoad(BattleScene));
        }

        /// Called by BattleManager once the reward/defeat popup is dismissed.
        public void EndEncounter(bool victory)
        {
            if (transitioning) return;
            var gm = GameManager.Ensure();
            var data = gm.pendingEncounter;
            gm.hasPendingEncounter = false;

            if (victory)
            {
                gm.hasPendingCellSpawn = true;
                gm.pendingSpawnScene = data.returnScene;
                gm.pendingSpawnCell = data.returnCell;
                StartCoroutine(FadeAndLoad(data.returnScene));
            }
            else
            {
                // Soft fail (§6.5): revive in town at partial HP, small gold penalty.
                gm.gold = Mathf.FloorToInt(gm.gold * 0.8f);
                foreach (var m in gm.party)
                    m.currentHP = Mathf.Max(1, Mathf.RoundToInt(gm.StatsFor(m).maxHP * 0.4f));
                gm.hasPendingCellSpawn = false;
                gm.spawnAtGate = false;
                StartCoroutine(FadeAndLoad(TownScene));
            }
        }

        /// Overworld gate: walk from town to field or back, spawning at the
        /// destination map's gate tile.
        public void TravelThroughGate(string targetScene)
        {
            if (transitioning) return;
            var gm = GameManager.Ensure();
            gm.spawnAtGate = true;
            gm.hasPendingCellSpawn = false;
            StartCoroutine(FadeAndLoad(targetScene));
        }

        IEnumerator FadeAndLoad(string scene)
        {
            transitioning = true;
            yield return ScreenFader.Ensure().FadeOut(fadeDuration);
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return ScreenFader.Ensure().FadeIn(fadeDuration);
            transitioning = false;
        }
    }
}
