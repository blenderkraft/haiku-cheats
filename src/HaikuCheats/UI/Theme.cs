using System.Linq;
using UnityEngine;

namespace HaikuCheats
{
    /// <summary>Palette, fonts and IMGUI drawing primitives. Everything is generated at runtime; no asset bundles.</summary>
    internal static class Theme
    {
        public static readonly Color Bg = new Color(0.03f, 0.055f, 0.07f, 0.95f);
        public static readonly Color Row = new Color(0.08f, 0.13f, 0.15f, 1f);
        public static readonly Color RowHover = new Color(0.11f, 0.19f, 0.21f, 1f);
        public static readonly Color Teal = new Color(0.21f, 0.96f, 0.78f, 1f);
        public static readonly Color Amber = new Color(1f, 0.71f, 0.28f, 1f);
        public static readonly Color Red = new Color(1f, 0.32f, 0.42f, 1f);
        public static readonly Color Text = new Color(0.86f, 0.95f, 0.93f, 1f);
        public static readonly Color Dim = new Color(0.46f, 0.6f, 0.62f, 1f);
        public static readonly Color Track = new Color(0.16f, 0.22f, 0.25f, 1f);

        public static Texture2D White;
        private static Texture2D scanlines;
        private static Font mono;
        private static bool ready;

        public static GUIStyle Title, Subtitle, Label, Small, Tiny, Center, Right;

        public static void Ensure()
        {
            if (ready) return;
            ready = true;

            White = Texture2D.whiteTexture;
            scanlines = new Texture2D(1, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.HideAndDontSave };
            scanlines.SetPixels(new[] { new Color(0, 0, 0, 0.22f), Color.clear, Color.clear, Color.clear });
            scanlines.Apply();

            // Prefer a monospace font for the terminal look. Proton prefixes usually ship Courier New or Lucida.
            var installed = Font.GetOSInstalledFontNames();
            var pick = new[] { "Consolas", "Lucida Console", "Cascadia Mono", "Courier New", "DejaVu Sans Mono", "Liberation Mono" }
                .FirstOrDefault(n => installed.Contains(n));
            if (pick != null) mono = Font.CreateDynamicFontFromOSFont(pick, 16);

            Title = Style(30, FontStyle.Bold);
            Subtitle = Style(12, FontStyle.Normal);
            Label = Style(17, FontStyle.Bold);
            Small = Style(13, FontStyle.Normal);
            Small.wordWrap = true;
            Tiny = Style(11, FontStyle.Bold);
            Center = Style(15, FontStyle.Bold);
            Center.alignment = TextAnchor.MiddleCenter;
            Right = Style(16, FontStyle.Bold);
            Right.alignment = TextAnchor.MiddleRight;
        }

        private static GUIStyle Style(int size, FontStyle fs)
        {
            var s = new GUIStyle { fontSize = size, fontStyle = fs, alignment = TextAnchor.MiddleLeft, richText = true, clipping = TextClipping.Clip };
            if (mono != null) s.font = mono;
            s.normal.textColor = Color.white;
            return s;
        }

        public static Color A(Color c, float alpha) => new Color(c.r, c.g, c.b, c.a * alpha);

        public static void Rect(Rect r, Color c, float radius = 0f)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, White, ScaleMode.StretchToFill, true, 0f, c, 0f, radius);
        }

        public static void Outline(Rect r, Color c, float width, float radius = 0f)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, White, ScaleMode.StretchToFill, true, 0f, c, width, radius);
        }

        /// <summary>Cheap bloom: stacked, expanding translucent rounded rects.</summary>
        public static void Glow(Rect r, Color c, float radius, float spread = 18f, int layers = 6)
        {
            if (Event.current.type != EventType.Repaint) return;
            for (int i = layers; i >= 1; i--)
            {
                float t = i / (float)layers;
                float e = spread * t;
                var gr = new Rect(r.x - e, r.y - e, r.width + e * 2, r.height + e * 2);
                GUI.DrawTexture(gr, White, ScaleMode.StretchToFill, true, 0f, A(c, (1f - t) * 0.12f), 0f, radius + e);
            }
        }

        public static void Scanlines(Rect r, float scroll, float alpha)
        {
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.DrawTextureWithTexCoords(r, scanlines, new Rect(0, scroll, 1, r.height / 4f));
            GUI.color = old;
        }

        public static void Print(Rect r, string text, GUIStyle style, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.Label(r, text, style);
            GUI.color = old;
        }

        public static float Smooth(float current, float target, float speed) =>
            Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.4f, c3 = c1 + 1f;
            t -= 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }
    }
}
