using UnityEngine;
using UnityEngine.InputSystem;

namespace F3.Interrogation
{
    // =====================================================================
    // InterrogationInput.cs
    // ---------------------------------------------------------------------
    // Your project uses the NEW Input System only (Project Settings ->
    // Active Input Handling = Input System Package), so the old
    // UnityEngine.Input class would throw errors. This helper reads the new
    // devices directly ("polling") so you do not have to wire up an
    // InputActions asset or an EventSystem for this mini-game.
    //
    // Works with keyboard, mouse, gamepad and touch (Pointer covers mouse,
    // pen and touchscreen).
    // =====================================================================
    public static class InterrogationInput
    {
        // Remembers whether the gamepad stick was already pushed, so one push = one step.
        static bool stickWasPushed;

        /// <summary>Call when a menu opens so a leftover stick push does not instantly spin the wheel.</summary>
        public static void ResetLatches()
        {
            stickWasPushed = false;
        }

        /// <summary>Enter / Numpad Enter / Space / gamepad South button (A on Xbox, Cross on PlayStation).</summary>
        public static bool SubmitPressed()
        {
            var kb = Keyboard.current;
            if (kb != null &&
                (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                return true;

            var pad = Gamepad.current;
            return pad != null && pad.buttonSouth.wasPressedThisFrame;
        }

        /// <summary>True the frame the mouse button / finger goes down. 'position' is in screen pixels.</summary>
        public static bool PointerPressed(out Vector2 position)
        {
            position = Vector2.zero;
            var pointer = Pointer.current; // last-used mouse, pen or touchscreen
            if (pointer == null) return false;

            if (pointer.press.wasPressedThisFrame)
            {
                position = pointer.position.ReadValue();
                return true;
            }
            return false;
        }

        /// <summary>"Continue" in dialogue: any submit key, or click / tap anywhere.</summary>
        public static bool AdvancePressed()
        {
            Vector2 ignored;
            return SubmitPressed() || PointerPressed(out ignored);
        }

        /// <summary>The R key (shortcut for RETRY).</summary>
        public static bool RetryKeyPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.rKey.wasPressedThisFrame;
        }

        /// <summary>
        /// Wheel navigation. Returns -1 (turn to previous option), +1 (next) or 0.
        /// Keys: A/D, Left/Right, W/S, Up/Down. Also d-pad, left stick and mouse scroll.
        /// </summary>
        public static int NavigateStep()
        {
            int step = 0;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame ||
                    kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) step = -1;

                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
                    kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) step = +1;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame) step = -1;
                if (pad.dpad.right.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame) step = +1;

                // Stick: only fire when it crosses the threshold, not every frame it is held.
                float x = pad.leftStick.x.ReadValue();
                bool pushed = Mathf.Abs(x) > 0.6f;
                if (pushed && !stickWasPushed) step = x > 0f ? +1 : -1;
                stickWasPushed = pushed;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f) step = -1;       // scroll up   = previous
                else if (scroll < -0.1f) step = +1; // scroll down = next
            }

            return step;
        }
    }
}
