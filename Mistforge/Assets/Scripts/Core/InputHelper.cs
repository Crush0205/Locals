using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mistforge
{
    /// Thin input abstraction. Compiles against the new Input System when the
    /// project's Active Input Handling enables it, and falls back to the legacy
    /// Input Manager otherwise, so the slice runs before project settings are touched.
    public static class InputHelper
    {
#if ENABLE_INPUT_SYSTEM
        public static Vector2Int HeldDirection()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2Int.zero;
            // Cardinal only — no diagonals (§4.3).
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) return Vector2Int.up;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) return Vector2Int.down;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) return Vector2Int.left;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) return Vector2Int.right;
            return Vector2Int.zero;
        }

        public static bool DirectionPressed(out ComboDirection dir)
        {
            dir = ComboDirection.Left;
            var kb = Keyboard.current;
            if (kb == null) return false;
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) { dir = ComboDirection.Left; return true; }
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) { dir = ComboDirection.Right; return true; }
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) { dir = ComboDirection.Up; return true; }
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) { dir = ComboDirection.Down; return true; }
            return false;
        }

        public static bool ConfirmPressed()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame);
        }

        public static bool FocusPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.fKey.wasPressedThisFrame;
        }

        public static bool ClearPressed()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.backspaceKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame);
        }

        public static bool CancelPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
        }

        public static bool PointerClicked(out Vector2 screenPos)
        {
            screenPos = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return false;
            screenPos = mouse.position.ReadValue();
            return true;
        }
#else
        public static Vector2Int HeldDirection()
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) return Vector2Int.up;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) return Vector2Int.down;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) return Vector2Int.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) return Vector2Int.right;
            return Vector2Int.zero;
        }

        public static bool DirectionPressed(out ComboDirection dir)
        {
            dir = ComboDirection.Left;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) { dir = ComboDirection.Left; return true; }
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) { dir = ComboDirection.Right; return true; }
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) { dir = ComboDirection.Up; return true; }
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) { dir = ComboDirection.Down; return true; }
            return false;
        }

        public static bool ConfirmPressed()
        {
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        }

        public static bool FocusPressed() { return Input.GetKeyDown(KeyCode.F); }

        public static bool ClearPressed()
        {
            return Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.C);
        }

        public static bool CancelPressed() { return Input.GetKeyDown(KeyCode.Escape); }

        public static bool PointerClicked(out Vector2 screenPos)
        {
            screenPos = Input.mousePosition;
            return Input.GetMouseButtonDown(0);
        }
#endif
    }
}
