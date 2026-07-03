using System.Collections;
using UnityEngine;

namespace Mistforge
{
    /// One combatant in the battle scene: sprite, HP/AP, world-space bars,
    /// hit-flash. Created at runtime by BattleManager.
    public class BattleActor : MonoBehaviour
    {
        public string displayName;
        public bool isPlayer;
        public StatBlock stats;
        public int hp;
        public int ap;
        public bool guarding;
        public int igniteTurns;   // Emberwake burn on enemies

        public CharacterDef character;                       // players only
        public EnemyDef enemyDef;                            // enemies only
        public GameManager.PartyMemberState memberState;     // players only

        SpriteRenderer sr;
        Color baseColor;
        Transform hpFill;
        Transform apFill;
        Vector3 homePosition;

        public bool Alive => hp > 0;
        public Vector3 HomePosition => homePosition;
        public Vector3 Center => transform.position + Vector3.up * (isPlayer ? 1.5f : 1f);

        public static BattleActor CreatePlayer(GameDatabase db, GameManager.PartyMemberState member, Vector3 pos, int startingAP)
        {
            var character = db.Character(member.charId);
            var stats = StatsCalculator.Compute(db, character, member.equipped);

            var actor = Build(character.displayName, pos,
                PlaceholderArt.Solid(32, 48, character.placeholderColor), true);
            actor.isPlayer = true;
            actor.character = character;
            actor.memberState = member;
            actor.stats = stats;
            actor.hp = Mathf.Clamp(member.currentHP, 0, stats.maxHP);
            actor.ap = Mathf.Min(startingAP, stats.maxAP);
            actor.UpdateBars();
            return actor;
        }

        public static BattleActor CreateEnemy(EnemyDef def, Vector3 pos)
        {
            var actor = Build(def.displayName, pos,
                PlaceholderArt.Solid(def.framePixels.x, def.framePixels.y, def.placeholderColor), false);
            actor.isPlayer = false;
            actor.enemyDef = def;
            actor.stats = new StatBlock { maxHP = def.maxHP };
            actor.hp = def.maxHP;
            actor.UpdateBars();

            // Clickable for target selection.
            var collider = actor.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(def.framePixels.x / 16f, def.framePixels.y / 16f);
            collider.offset = new Vector2(0f, def.framePixels.y / 32f);
            return actor;
        }

        static BattleActor Build(string name, Vector3 pos, Sprite sprite, bool showAP)
        {
            var go = new GameObject("Actor_" + name);
            go.transform.position = pos;
            var actor = go.AddComponent<BattleActor>();
            actor.displayName = name;
            actor.homePosition = pos;
            actor.sr = go.AddComponent<SpriteRenderer>();
            actor.sr.sprite = sprite;
            actor.sr.sortingOrder = 10;
            actor.baseColor = Color.white;
            actor.BuildBars(showAP, sprite.rect.height / PlaceholderArt.PPU);
            return actor;
        }

        void BuildBars(bool showAP, float spriteHeight)
        {
            var barsGO = new GameObject("Bars");
            barsGO.transform.SetParent(transform, false);
            barsGO.transform.localPosition = new Vector3(0f, spriteHeight + 0.25f, 0f);

            hpFill = MakeBar(barsGO.transform, 0f, new Color(0.35f, 0.85f, 0.4f));
            if (showAP)
                apFill = MakeBar(barsGO.transform, -0.25f, new Color(0.35f, 0.6f, 0.95f));
        }

        Transform MakeBar(Transform parent, float yOffset, Color fillColor)
        {
            var back = new GameObject("BarBack");
            back.transform.SetParent(parent, false);
            back.transform.localPosition = new Vector3(-0.75f, yOffset, 0f);
            var backSr = back.AddComponent<SpriteRenderer>();
            backSr.sprite = PlaceholderArt.LeftPivot(24, 3, new Color(0.1f, 0.1f, 0.12f));
            backSr.sortingOrder = 20;

            var fill = new GameObject("BarFill");
            fill.transform.SetParent(back.transform, false);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = PlaceholderArt.LeftPivot(24, 3, fillColor);
            fillSr.sortingOrder = 21;
            return fill.transform;
        }

        public void UpdateBars()
        {
            if (hpFill != null)
                hpFill.localScale = new Vector3(stats.maxHP > 0 ? Mathf.Clamp01(hp / (float)stats.maxHP) : 0f, 1f, 1f);
            if (apFill != null)
                apFill.localScale = new Vector3(stats.maxAP > 0 ? Mathf.Clamp01(ap / (float)stats.maxAP) : 0f, 1f, 1f);
        }

        public void GainAP(int amount)
        {
            if (stats.maxAP <= 0) return;
            ap = Mathf.Clamp(ap + amount, 0, stats.maxAP);
            UpdateBars();
        }

        /// Applies damage with full hit feedback (§6.3). Returns actual damage.
        public int TakeDamage(int amount, bool crit, bool fromArt)
        {
            amount = Mathf.Max(1, amount);
            hp = Mathf.Max(0, hp - amount);
            UpdateBars();

            StartCoroutine(FlashWhite());
            Color numberColor = crit || fromArt
                ? new Color(1f, 0.72f, 0.2f)   // gold/ember for crits and Arts
                : Color.white;
            FloatingText.Spawn(Center + Vector3.up * 0.6f,
                amount + (crit ? "!" : ""), numberColor, 8f, crit || fromArt);
            CameraShake.Shake(crit || fromArt ? 0.22f : 0.08f, crit || fromArt ? 0.25f : 0.12f);

            if (isPlayer && Alive) GainAP(6);   // §9: AP on taking damage
            if (!Alive) StartCoroutine(Collapse());
            return amount;
        }

        IEnumerator FlashWhite()
        {
            // 2–3 frame white flash (§6.3). Placeholder sprites are colored
            // textures, so swap to a white sprite of the same size rather than
            // tinting; with real sheets this becomes a flash shader/material.
            if (sr == null) yield break;
            var original = sr.sprite;
            sr.sprite = PlaceholderArt.Solid((int)original.rect.width, (int)original.rect.height, Color.white);
            yield return new WaitForSeconds(0.09f);
            if (sr != null)
            {
                sr.sprite = original;
                sr.color = igniteTurns > 0 ? IgniteTint : baseColor;
            }
        }

        static readonly Color IgniteTint = new Color(1f, 0.75f, 0.55f);

        public void SetIgnited(bool ignited)
        {
            if (sr != null) sr.color = ignited ? IgniteTint : baseColor;
        }

        IEnumerator Collapse()
        {
            // Placeholder defeat animation: tip over and fade.
            float t = 0f;
            var start = transform.rotation;
            var end = Quaternion.Euler(0f, 0f, isPlayer ? 80f : -80f);
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(start, end, t / 0.35f);
                if (sr != null)
                {
                    var c = sr.color;
                    c.a = Mathf.Lerp(1f, isPlayer ? 0.5f : 0f, t / 0.35f);
                    sr.color = c;
                }
                yield return null;
            }
        }

        /// Quick lunge toward a point and back — placeholder attack animation.
        public IEnumerator Lunge(Vector3 toward, float distance, float duration)
        {
            Vector3 dir = (toward - homePosition).normalized;
            Vector3 peak = homePosition + dir * distance;
            float half = duration * 0.5f;
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(homePosition, peak, t / half);
                yield return null;
            }
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(peak, homePosition, t / half);
                yield return null;
            }
            transform.position = homePosition;
        }

        public IEnumerator GuardPose()
        {
            // Brace tint instead of an attack animation (§6.2 Focus).
            if (sr != null) sr.color = new Color(0.7f, 0.8f, 1f);
            yield return new WaitForSeconds(0.35f);
            if (sr != null && Alive) sr.color = baseColor;
        }
    }
}
