using System;
using UnityEngine;

namespace DuckovLuckyBox.Core
{
    [Serializable]
    public class Hotkey
    {
        public KeyCode Key { get; set; } = KeyCode.None;
        public bool Ctrl { get; set; } = false;
        public bool Shift { get; set; } = false;
        public bool Alt { get; set; } = false;

        public Hotkey()
        {
        }

        public Hotkey(KeyCode key, bool ctrl = false, bool shift = false, bool alt = false)
        {
            Key = key;
            Ctrl = ctrl;
            Shift = shift;
            Alt = alt;
        }

        /// <summary>
        /// Check if the hotkey combination is currently pressed
        /// </summary>
        public bool IsPressed()
        {
            if (Key == KeyCode.None) return false;

            // Check if the main key is pressed
            if (!Input.GetKeyDown(Key)) return false;

            // Check modifier keys match
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            return ctrlPressed == Ctrl && shiftPressed == Shift && altPressed == Alt;
        }

        /// <summary>
        /// Get a human-readable string representation of the hotkey
        /// </summary>
        public override string ToString()
        {
            if (Key == KeyCode.None) return "None";

            var parts = new System.Collections.Generic.List<string>();
            if (Ctrl) parts.Add("Ctrl");
            if (Shift) parts.Add("Shift");
            if (Alt) parts.Add("Alt");
            parts.Add(Key.ToString());

            return string.Join("+", parts);
        }

        /// <summary>
        /// Parse a hotkey string (e.g., "Ctrl+F1") into a Hotkey object
        /// </summary>
        public static Hotkey Parse(string hotkeyString)
        {
            if (string.IsNullOrEmpty(hotkeyString))
                return new Hotkey();

            var parts = hotkeyString.Split('+');
            var hotkey = new Hotkey();

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("Control", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Ctrl = true;
                }
                else if (trimmed.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Shift = true;
                }
                else if (trimmed.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Alt = true;
                }
                else if (Enum.TryParse<KeyCode>(trimmed, true, out var keyCode))
                {
                    hotkey.Key = keyCode;
                }
            }

            return hotkey;
        }

        public override bool Equals(object obj)
        {
            if (obj is Hotkey other)
            {
                return Key == other.Key && Ctrl == other.Ctrl && Shift == other.Shift && Alt == other.Alt;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Ctrl, Shift, Alt);
        }
    }
}
