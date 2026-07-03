using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;

namespace Mistforge
{
    /// Owns the battle scene: builds backdrop/actors/HUD at runtime from the
    /// pending EncounterData, runs the phase loop (§6.4), resolves combos and
    /// Arts (§6.2, §9), and hands rewards back through SceneFlow (§6.5).
    public class BattleManager : MonoBehaviour
    {
        class PendingAction
        {
            public bool focus;
            public List<ComboDirection> sequence;
        }

        public int startingAP = 12;
        [Range(0f, 1f)] public float igniteChance = 0.25f;
        public int igniteDamage = 3;

        GameManager gm;
        EncounterData encounter;
        TurnController turns;
        ComboInput combo;
        Camera cam;

        readonly List<BattleActor> players = new List<BattleActor>();
        readonly List<BattleActor> enemies = new List<BattleActor>();

        BattlePhase phase = BattlePhase.PlayerInput;
        BattleActor activeActor;
        BattleActor currentTarget;
        PendingAction pendingAction;

        TextMeshProUGUI message;
        TextMeshProUGUI partyStrip;
        Transform targetMarker;
        Canvas canvas;

        void Start()
        {
            gm = GameManager.Ensure();
            ScreenFader.Ensure();

            if (gm.hasPendingEncounter)
            {
                encounter = gm.pendingEncounter;
            }
            else
            {
                // Battle scene opened directly in the editor: debug encounter.
                encounter = new EncounterData
                {
                    enemyIds = new[] { "wisp", "hound" },
                    returnScene = SceneFlow.TownScene,
                    returnCell = new Vector3Int(9, 5, 0),
                    qualityBoost = 0,
                };
                gm.pendingEncounter = encounter;
                gm.hasPendingEncounter = true;
            }

            BuildCamera();
            BuildBackdrop();
            SpawnActors();
            BuildHUD();

            turns = new TurnController(players, enemies);
            currentTarget = turns.FirstAlive(enemies);
            StartCoroutine(BattleLoop());
        }

        // ---------------------------------------------------------------- setup

        void BuildCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.10f, 0.12f);
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            var pixelPerfect = camGO.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = 16;
            pixelPerfect.refResolutionX = 320;
            pixelPerfect.refResolutionY = 180;
            pixelPerfect.upscaleRT = true;
            pixelPerfect.pixelSnapping = true;

            camGO.AddComponent<CameraShake>();
        }

        void BuildBackdrop()
        {
            // Misty field-edge backdrop reusing the overworld palette (§6.1).
            var sky = new GameObject("Backdrop");
            var skySr = sky.AddComponent<SpriteRenderer>();
            skySr.sprite = PlaceholderArt.Centered(320, 180, new Color(0.30f, 0.36f, 0.38f), false);
            skySr.sortingOrder = -20;

            var ground = new GameObject("Ground");
            var groundSr = ground.AddComponent<SpriteRenderer>();
            groundSr.sprite = PlaceholderArt.Centered(320, 70, new Color(0.33f, 0.40f, 0.30f), false, true);
            groundSr.sortingOrder = -10;
            ground.transform.position = new Vector3(0f, -3.4f, 0f);

            MistDrift.Scatter(transform, new Rect(-10f, -4f, 20f, 8f), 4);
        }

        void SpawnActors()
        {
            // Party: left third, slight diagonal, front character closer (§6.1).
            var partySlots = new[] { new Vector3(-3.6f, -2.8f, 0f), new Vector3(-6.0f, -1.6f, 0f) };
            for (int i = 0; i < gm.party.Count && i < partySlots.Length; i++)
                players.Add(BattleActor.CreatePlayer(gm.db, gm.party[i], partySlots[i], startingAP));

            var enemySlots = new[] { new Vector3(4.2f, -2.4f, 0f), new Vector3(6.6f, -1.3f, 0f) };
            for (int i = 0; i < encounter.enemyIds.Length && i < enemySlots.Length; i++)
            {
                var def = gm.db.Enemy(encounter.enemyIds[i]);
                if (def != null) enemies.Add(BattleActor.CreateEnemy(def, enemySlots[i]));
            }

            // Enemies face left: flip their placeholder (§6.1 profile view).
            foreach (var e in enemies)
            {
                var sr = e.GetComponent<SpriteRenderer>();
                if (sr != null) sr.flipX = true;
            }
        }

        void BuildHUD()
        {
            canvas = UIFactory.CreateCanvas("BattleHUD", 100);

            message = UIFactory.Text(canvas.transform, "", 24, TextAlignmentOptions.Center);
            UIFactory.Place(message.rectTransform, new Vector2(0.1f, 0.9f), new Vector2(0.9f, 1f),
                Vector2.zero, new Vector2(0f, -6f));

            // Bottom-of-screen party status strip (§6.1) — the partybar equivalent.
            var stripPanel = UIFactory.Panel(canvas.transform, "PartyStrip", new Color(0.08f, 0.09f, 0.12f, 0.85f));
            UIFactory.Place(stripPanel, new Vector2(0f, 0f), new Vector2(0.25f, 0f),
                new Vector2(4f, 8f), new Vector2(-4f, 168f));
            partyStrip = UIFactory.Text(stripPanel, "", 17, TextAlignmentOptions.TopLeft);
            UIFactory.Place(partyStrip.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(10f, 6f), new Vector2(-6f, -6f));

            combo = gameObject.AddComponent<ComboInput>();
            combo.BuildUI(canvas.transform);
            combo.onConfirm = seq => pendingAction = new PendingAction { sequence = seq };
            combo.onFocus = () => pendingAction = new PendingAction { focus = true };

            var marker = new GameObject("TargetMarker");
            var markerSr = marker.AddComponent<SpriteRenderer>();
            markerSr.sprite = PlaceholderArt.Centered(8, 8, new Color(1f, 0.78f, 0.25f), false);
            markerSr.sortingOrder = 40;
            marker.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            targetMarker = marker.transform;
        }

        // ---------------------------------------------------------------- loop

        IEnumerator BattleLoop()
        {
            yield return null; // let the scene settle one frame

            while (true)
            {
                foreach (var player in players)
                {
                    if (!player.Alive) continue;
                    player.guarding = false;

                    phase = BattlePhase.PlayerInput;
                    activeActor = player;
                    EnsureTarget();
                    SetMessage(player.displayName + "'s turn — queue up to " + ComboInput.MaxInputs + " inputs");
                    pendingAction = null;
                    combo.Begin(player);
                    while (pendingAction == null) yield return null;
                    combo.End();

                    phase = BattlePhase.Animating;
                    yield return ResolvePlayerAction(player, pendingAction);

                    if (turns.AllEnemiesDown())
                    {
                        yield return Victory();
                        yield break;
                    }
                }

                phase = BattlePhase.EnemyTurn;
                foreach (var enemy in enemies)
                {
                    if (!enemy.Alive) continue;
                    yield return TickIgnite(enemy);
                    if (!enemy.Alive)
                    {
                        if (turns.AllEnemiesDown())
                        {
                            yield return Victory();
                            yield break;
                        }
                        continue;
                    }

                    yield return EnemyAct(enemy);
                    if (turns.AllPlayersDown())
                    {
                        yield return Defeat();
                        yield break;
                    }
                }
            }
        }

        void Update()
        {
            RefreshPartyStrip();
            UpdateTargetMarker();

            // Click an enemy to change target during input (§6.2).
            if (phase == BattlePhase.PlayerInput && InputHelper.PointerClicked(out var screenPos))
            {
                Vector2 world = cam.ScreenToWorldPoint(screenPos);
                foreach (var hit in Physics2D.OverlapPointAll(world))
                {
                    var actor = hit.GetComponent<BattleActor>();
                    if (actor != null && !actor.isPlayer && actor.Alive)
                    {
                        currentTarget = actor;
                        break;
                    }
                }
            }
        }

        void EnsureTarget()
        {
            if (currentTarget == null || !currentTarget.Alive)
                currentTarget = turns.FirstAlive(enemies);
        }

        void UpdateTargetMarker()
        {
            bool show = phase == BattlePhase.PlayerInput && currentTarget != null && currentTarget.Alive;
            targetMarker.gameObject.SetActive(show);
            if (show)
                targetMarker.position = currentTarget.Center +
                    Vector3.up * (1.3f + Mathf.Sin(Time.time * 5f) * 0.12f);
        }

        void RefreshPartyStrip()
        {
            if (partyStrip == null) return;
            var lines = new List<string>();
            foreach (var p in players)
            {
                string marker = p == activeActor && phase == BattlePhase.PlayerInput ? "> " : "  ";
                lines.Add(marker + p.displayName + (p.Alive ? "" : " (down)"));
                lines.Add("   HP " + p.hp + "/" + p.stats.maxHP + "   AP " + p.ap + "/" + p.stats.maxAP);
            }
            partyStrip.text = string.Join("\n", lines);
        }

        void SetMessage(string text)
        {
            if (message != null) message.text = text;
        }

        // ---------------------------------------------------------------- player actions

        IEnumerator ResolvePlayerAction(BattleActor player, PendingAction action)
        {
            if (action.focus)
            {
                // Focus (§6.2/§9): guarded stance, flat +16 AP.
                player.guarding = true;
                player.GainAP(16);
                SetMessage(player.displayName + " focuses.");
                FloatingText.Spawn(player.Center + Vector3.up * 0.5f, "+16 AP", new Color(0.5f, 0.75f, 1f));
                yield return player.GuardPose();
                yield return new WaitForSeconds(0.25f);
                yield break;
            }

            EnsureTarget();
            if (currentTarget == null) yield break;

            var art = ArtMatcher.Match(player.character.arts, action.sequence);
            if (art != null && player.ap >= art.apCost)
                yield return ResolveArt(player, art);
            else
                yield return ResolveNormalCombo(player, action.sequence);

            yield return new WaitForSeconds(0.3f);
        }

        IEnumerator ResolveArt(BattleActor player, ArtDef art)
        {
            // Exact match + enough AP -> Art finisher (§6.2).
            player.ap -= art.apCost;
            player.UpdateBars();
            SetMessage(player.displayName + " unleashes " + art.displayName + "!");
            FloatingText.Spawn(player.Center + Vector3.up * 0.8f, art.displayName + "!",
                new Color(1f, 0.72f, 0.2f), 8f, true);

            yield return player.Lunge(currentTarget.transform.position, 3.2f, 0.34f);
            HitPause.Do();   // freeze-frame beat before the hit lands (§6.3)
            yield return new WaitForSecondsRealtime(0.1f);

            // Art damage (§9): atk × mult × (1 + artDmg%) × random(0.9–1.1).
            float dmg = player.stats.atk * art.damageMultiplier
                        * (1f + player.stats.artDmgPct / 100f)
                        * Random.Range(0.9f, 1.1f);
            bool crit = Random.Range(0f, 100f) < player.stats.critPct;
            if (crit) dmg *= 1.5f;

            currentTarget.TakeDamage(Mathf.RoundToInt(dmg), crit, true);
            TryIgnite(player, currentTarget);
            yield return new WaitForSeconds(0.25f);
        }

        IEnumerator ResolveNormalCombo(BattleActor player, List<ComboDirection> sequence)
        {
            // No match (or not enough AP): each queued input lands as its own
            // hit with its own damage number (§6.2, §9 split damage).
            float total = player.stats.atk * 0.42f * sequence.Count * Random.Range(0.85f, 1.15f);
            int perHit = Mathf.Max(1, Mathf.RoundToInt(total / sequence.Count));

            foreach (var dir in sequence)
            {
                EnsureTarget();
                if (currentTarget == null) yield break;

                SetMessage(player.displayName + " — " + player.character.AttackName(dir));
                yield return player.Lunge(currentTarget.transform.position, 2.2f, 0.22f);

                bool crit = Random.Range(0f, 100f) < player.stats.critPct;
                currentTarget.TakeDamage(crit ? Mathf.RoundToInt(perHit * 1.5f) : perHit, crit, false);
                TryIgnite(player, currentTarget);
                yield return new WaitForSeconds(0.08f);
            }

            // AP gain for a normal attack (§9).
            int gain = 5 + Mathf.RoundToInt(player.stats.apGainPct / 100f * 5f);
            player.GainAP(gain);
            FloatingText.Spawn(player.Center + Vector3.up * 0.5f, "+" + gain + " AP",
                new Color(0.5f, 0.75f, 1f), 6f);
        }

        void TryIgnite(BattleActor player, BattleActor target)
        {
            // Emberwake 3-piece: attacks may ignite foes (§8.8).
            if (!player.stats.igniteOnHit || target == null || !target.Alive) return;
            if (Random.value >= igniteChance) return;
            target.igniteTurns = 2;
            target.SetIgnited(true);
            FloatingText.Spawn(target.Center + Vector3.up * 0.9f, "Ignited!",
                new Color(1f, 0.5f, 0.15f), 7f);
        }

        // ---------------------------------------------------------------- enemy turn

        IEnumerator TickIgnite(BattleActor enemy)
        {
            if (enemy.igniteTurns <= 0) yield break;
            enemy.igniteTurns--;
            SetMessage(enemy.displayName + " burns!");
            enemy.TakeDamage(igniteDamage, false, false);
            if (enemy.igniteTurns <= 0 && enemy.Alive) enemy.SetIgnited(false);
            yield return new WaitForSeconds(0.35f);
        }

        IEnumerator EnemyAct(BattleActor enemy)
        {
            var target = turns.RandomAlive(players);
            if (target == null) yield break;

            SetMessage(enemy.displayName + " attacks " + target.displayName + "!");
            yield return new WaitForSeconds(0.25f);
            yield return enemy.Lunge(target.transform.position, 2.6f, 0.3f);

            int dmg = turns.RollEnemyDamage(enemy.enemyDef, target);
            target.TakeDamage(dmg, false, false);
            yield return new WaitForSeconds(0.3f);
        }

        // ---------------------------------------------------------------- resolution

        IEnumerator Victory()
        {
            phase = BattlePhase.Resolved;
            SetMessage("Victory!");
            yield return new WaitForSeconds(0.6f);

            var lines = new List<string>();

            int gold = 0;
            foreach (var e in enemies)
                gold += Random.Range(e.enemyDef.goldMin, e.enemyDef.goldMax + 1);
            gm.gold += gold;
            lines.Add("+" + gold + " gold");

            foreach (var e in enemies)
            {
                foreach (var entry in e.enemyDef.loot)
                {
                    if (entry.material == null || Random.value >= entry.dropChance) continue;
                    gm.AddMaterial(entry.material.id, entry.quantity);
                    lines.Add("+" + entry.quantity + " " + entry.material.displayName);
                }
            }

            // Item drop (§9): 22% base, rarity-quality-boosted by the encounter.
            if (Random.value < 0.22f && gm.db.itemBases.Length > 0)
            {
                var baseDef = gm.db.itemBases[Random.Range(0, gm.db.itemBases.Length)];
                var item = ItemGenerator.Generate(gm.db, baseDef, encounter.qualityBoost);
                gm.inventory.Add(item);
                var rarity = gm.db.Rarity(item.rarityId);
                lines.Add("Found: " + item.displayName +
                    (rarity != null ? " (" + rarity.displayName + ")" : ""));
            }

            // Persist HP; anyone who fell gets back up at 1 HP after a win.
            foreach (var p in players)
                p.memberState.currentHP = Mathf.Max(1, p.hp);

            ShowPopup("Victory!", lines, () => SceneFlow.Ensure().EndEncounter(true));
        }

        IEnumerator Defeat()
        {
            phase = BattlePhase.Resolved;
            SetMessage("The party falls...");
            yield return new WaitForSeconds(0.8f);

            ShowPopup("Defeated", new List<string>
            {
                "The mist closes in...",
                "You awaken back in Rimwatch.",
                "Some gold was lost.",
            }, () => SceneFlow.Ensure().EndEncounter(false));
        }

        void ShowPopup(string title, List<string> lines, System.Action onContinue)
        {
            var popup = UIFactory.Panel(canvas.transform, "Popup", new Color(0.07f, 0.08f, 0.11f, 0.95f));
            UIFactory.Place(popup, new Vector2(0.3f, 0.25f), new Vector2(0.7f, 0.75f),
                Vector2.zero, Vector2.zero);

            var titleText = UIFactory.Text(popup, title, 28, TextAlignmentOptions.Center,
                new Color(0.95f, 0.8f, 0.4f));
            UIFactory.Place(titleText.rectTransform, new Vector2(0f, 0.8f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);

            var body = UIFactory.Text(popup, string.Join("\n", lines), 19, TextAlignmentOptions.Center);
            UIFactory.Place(body.rectTransform, new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.78f),
                Vector2.zero, Vector2.zero);

            var button = UIFactory.Button(popup, "Continue", () => onContinue(), new Color(0.55f, 0.35f, 0.15f));
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.24f),
                Vector2.zero, Vector2.zero);
        }
    }
}
