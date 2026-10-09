using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaikuCheats
{
    /// <summary>Slide-in notifications at the top of the screen, plus the active-cheats HUD badge.</summary>
    internal static class Toasts
    {
        public static Color Teal => Theme.Teal;
        public static Color Amber => Theme.Amber;
        public static Color Warn => Theme.Red;

        private const float Life = 2.4f;

        private sealed class Toast
        {
            public string Text;
            public Color Color;
            public float Born;
            public float Y;
        }

        private static readonly List<Toast> live = new List<Toast>();

        public static void Show(string text, Color color)
        {
            live.RemoveAll(t => t.Text == text);
            live.Insert(0, new Toast { Text = text, Color = color, Born = Time.unscaledTime, Y = -60f });
            if (live.Count > 4) live.RemoveAt(live.Count - 1);
        }

        public static void Draw(float virtualWidth)
        {
            live.RemoveAll(t => Time.unscaledTime - t.Born > Life);
            for (int i = 0; i < live.Count; i++)
            {
                var t = live[i];
                float age = Time.unscaledTime - t.Born;
                float alpha = Mathf.Clamp01(age * 8f) * Mathf.Clamp01((Life - age) * 3f);
                t.Y = Theme.Smooth(t.Y, 40f + i * 54f, 14f);

                var size = Theme.Center.CalcSize(new GUIContent(t.Text));
                float w = size.x + 64f;
                var r = new Rect((virtualWidth - w) / 2f, t.Y, w, 42f);

                Theme.Glow(r, Theme.A(t.Color, alpha), 10f, 14f);
                Theme.Rect(r, Theme.A(Theme.Bg, alpha), 10f);
                Theme.Outline(r, Theme.A(t.Color, alpha * 0.7f), 1.5f, 10f);
                Theme.Rect(new Rect(r.x + 14f, r.y + 15f, 12f, 12f), Theme.A(t.Color, alpha), 6f);
                Theme.Print(new Rect(r.x + 34f, r.y, w - 40f, r.height), t.Text, Theme.Center, Theme.A(Theme.Text, alpha));
            }
        }

        public static void DrawHud(float virtualWidth)
        {
            var tags = Cheats.Toggles.Where(t => t.On()).Select(t => t.HudTag)
                .Concat(Cheats.Sliders.Where(s => s.IsActive).Select(s => s.HudTag + " x" + s.Value.Value.ToString("0.#")))
                .ToList();
            if (tags.Count == 0) return;

            string text = string.Join("  ", tags);
            var size = Theme.Tiny.CalcSize(new GUIContent(text));
            float w = size.x + 44f;
            var r = new Rect(virtualWidth - w - 24f, 20f, w, 28f);
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);

            Theme.Rect(r, Theme.A(Theme.Bg, 0.8f), 14f);
            Theme.Outline(r, Theme.A(Theme.Teal, 0.35f), 1f, 14f);
            Theme.Rect(new Rect(r.x + 12f, r.y + 10f, 8f, 8f), Theme.A(Theme.Teal, pulse), 4f);
            Theme.Print(new Rect(r.x + 28f, r.y, w - 30f, r.height), text, Theme.Tiny, Theme.Teal);
        }
    }
}
