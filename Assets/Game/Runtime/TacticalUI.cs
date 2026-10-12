using System;
using UnityEngine;

namespace Outbreak
{
    // Shared presentation layer; domain operations remain in the existing station/raid adapters.
    // A reference canvas keeps the same readable layout at 720p, 1080p and larger output sizes.
    public static class TacticalUI
    {
        public const float Width = 1280, Height = 720;
        private static GUISkin skin;
        public static readonly Color Accent = new Color(.72f, .76f, .56f);
        // Labels only: never change catalog IDs, definitions or saved equipment.
        public static string Name(string value)
        {
            switch (value)
            {
                case "DebugRifle": return "AKM";
                case "DebugPistol": return "Pistol1";
                case "DebugRifleAmmo": return "Rifle ammunition";
                case "DebugPistolAmmo": return "Pistol ammunition";
                default: return value != null && value.StartsWith("Debug ", StringComparison.Ordinal) ? value.Substring(6) : value;
            }
        }
        private static Texture2D Flat(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color); texture.Apply(); return texture;
        }
        private static void Initialize()
        {
            if (skin != null) return;
            skin = UnityEngine.Object.Instantiate(GUI.skin); skin.hideFlags = HideFlags.HideAndDontSave;
            var panel = Flat(new Color(.055f, .068f, .072f, .96f));
            var button = Flat(new Color(.14f, .17f, .17f, .98f));
            // A restrained olive hover makes the active card/action visible without turning the UI into a bright HUD.
            var hover = Flat(new Color(.32f, .37f, .28f));
            skin.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            skin.label.fontSize = 16; skin.label.normal.textColor = new Color(.86f, .88f, .85f);
            skin.label.padding = new RectOffset(4, 4, 2, 2); skin.label.wordWrap = true;
            skin.box.normal.background = panel; skin.box.normal.textColor = Accent;
            skin.box.fontSize = 17; skin.box.alignment = TextAnchor.UpperCenter;
            skin.box.padding = new RectOffset(18, 18, 14, 14);
            skin.button.normal.background = button; skin.button.hover.background = hover;
            skin.button.active.background = hover; skin.button.focused.background = hover;
            skin.button.normal.textColor = Color.white; skin.button.hover.textColor = Accent;
            skin.button.fontSize = 15; skin.button.padding = new RectOffset(14, 14, 6, 6);
            skin.button.margin = new RectOffset(4, 4, 4, 4); skin.button.wordWrap = true;
            skin.verticalScrollbar.fixedWidth = 12;
        }
        public sealed class Scope : IDisposable
        {
            private readonly GUISkin previousSkin;
            private readonly Matrix4x4 previousMatrix;
            public Scope()
            {
                previousSkin = GUI.skin; previousMatrix = GUI.matrix; Initialize(); GUI.skin = skin;
                float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
                GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Width * scale) / 2, (Screen.height - Height * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            }
            public void Dispose() { GUI.skin = previousSkin; GUI.matrix = previousMatrix; }
        }
        public static void Heading(string text)
        {
            var color = GUI.color; GUI.color = Accent;
            GUILayout.Label(text, new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold }); GUI.color = color;
        }
        public static void BeginPanel(string title, string subtitle)
        {
            GUI.Box(new Rect(110, 60, 1060, 600), "");
            GUILayout.BeginArea(new Rect(138, 78, 1004, 560));
            Heading(title); GUILayout.Label(subtitle); GUILayout.Space(10);
        }
        public static void EndPanel() => GUILayout.EndArea();
        public static void Prompt(string text, float y = 642) => GUI.Box(new Rect(400, y, 480, 48), text);
    }
}
