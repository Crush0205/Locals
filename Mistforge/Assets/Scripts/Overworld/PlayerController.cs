using System.Collections;
using UnityEngine;

namespace Mistforge
{
    /// Grid-aligned overworld movement (§4.3): exactly one tile per step,
    /// cardinal directions only. Drives Animator parameters when an Animator
    /// with the spec's parameters (Direction int, IsMoving bool) is present;
    /// with placeholder art it just flips the sprite for left/right.
    public class PlayerController : MonoBehaviour
    {
        public float stepDuration = 0.16f;

        OverworldBuilder world;
        SpriteRenderer sr;
        Animator animator;   // optional until real sheets land
        Vector3Int cell;
        bool moving;
        bool hasIgnoreCell;
        Vector3Int ignoreEventCell;

        static readonly Vector3Int Invalid = new Vector3Int(int.MinValue, int.MinValue, 0);

        public Vector3Int Cell => cell;

        public void Init(OverworldBuilder builder, Vector3Int startCell)
        {
            world = builder;
            sr = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            cell = startCell;
            ignoreEventCell = startCell;
            hasIgnoreCell = true;
            transform.position = CellToWorld(cell);
        }

        void Update()
        {
            if (moving || world == null) return;
            if (SceneFlow.I != null && SceneFlow.I.IsTransitioning) return;
            if (ForgeUI.AnyOpen) return;

            var input = InputHelper.HeldDirection();
            if (input == Vector2Int.zero)
            {
                SetAnimator(false);
                return;
            }

            Face(input);
            var target = cell + new Vector3Int(input.x, input.y, 0);
            if (world.IsBlocked(target))
            {
                SetAnimator(false);
                return;
            }
            StartCoroutine(Step(target));
        }

        void Face(Vector2Int dir)
        {
            // Left is the right-facing sheet flipped on X (§3.1).
            if (sr != null && dir.x != 0) sr.flipX = dir.x < 0;
            if (animator != null)
            {
                // Direction: 0 = Down, 1 = Up, 2 = Side (§4.3).
                int direction = dir.y < 0 ? 0 : dir.y > 0 ? 1 : 2;
                animator.SetInteger("Direction", direction);
            }
        }

        void SetAnimator(bool isMoving)
        {
            if (animator != null) animator.SetBool("IsMoving", isMoving);
        }

        IEnumerator Step(Vector3Int target)
        {
            moving = true;
            SetAnimator(true);
            Vector3 from = CellToWorld(cell);
            Vector3 to = CellToWorld(target);
            float t = 0f;
            while (t < stepDuration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / stepDuration));
                yield return null;
            }
            transform.position = to;
            cell = target;
            if (sr != null) sr.sortingOrder = 10 - cell.y; // simple y-sort vs. props
            moving = false;

            // Don't re-fire the tile we spawned on (gates, battle-return grass)
            // until the player has stepped off it once.
            if (hasIgnoreCell && cell == ignoreEventCell) yield break;
            hasIgnoreCell = false;
            ignoreEventCell = Invalid;
            world.OnPlayerArrived(cell, this);
        }

        public static Vector3 CellToWorld(Vector3Int cell)
        {
            // Bottom-center pivot: feet at the cell's floor line.
            return new Vector3(cell.x + 0.5f, cell.y, 0f);
        }
    }
}
