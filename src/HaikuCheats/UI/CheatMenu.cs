using System.Collections.Generic;
using System.Linq;
using Rewired;
using UnityEngine;

namespace HaikuCheats
{
    /// <summary>
    /// The F1 overlay. Hand-laid-out IMGUI in a virtual 1920x1080 space, scaled to the real screen,
    /// so every widget can be animated and styled without asset bundles.
    /// </summary>
    internal sealed class CheatMenu
    {
        private const float RefHeight = 1080f;
        private const float PanelW = 480f;
        private const float PanelH = 660f;
        private const float Pad = 22f;
        private const string TitleA = "HAIKU", TitleB = "//CHEATS";

        public bool IsOpen { get; private set; }

        private float openT;          // 0 closed .. 1 open, animated
        private float openedAt;
        private Tab tab = Tab.Player;
        private float tabIndicatorX = -1f;
        private SliderCheat dragging;
        private readonly Dictionary<object, float> anim = new Dictionary<object, float>();

        private bool savedCursorVisible;
        private CursorLockMode savedCursorLock;
        private bool blockedInput;

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool open)
        {
            if (open == IsOpen) return;
            IsOpen = open;
            dragging = null;
            if (open)
            {
                openedAt = Time.unscaledTime;
                savedCursorVisible = Cursor.visible;
                savedCursorLock = Cursor.lockState;
            }
            else
            {
                Cursor.visible = savedCursorVisible;
                Cursor.lockState = savedCursorLock;
            }
            SetGameInput(!open);
        }

        public void Update()
        {
            openT = Theme.Smooth(openT, IsOpen ? 1f : 0f, IsOpen ? 9f : 14f);
            if (!IsOpen) return;
            // The game re-hides the cursor when it thinks a controller is active; keep it visible while open.
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (!blockedInput) SetGameInput(false); // Rewired may not have been ready on open
        }

        private void SetGameInput(bool enabled)
        {
            if (!ReInput.isReady) return;
            var p = ReInput.players.GetPlayer(0);
            if (p == null) return;
            if (!enabled) { p.isPlaying = false; blockedInput = true; }
            else if (blockedInput) { p.isPlaying = true; blockedInput = false; }
        }

        public void OnGUI()
        {
            Theme.Ensure();
            float scale = Mathf.Max(0.55f, Screen.height / RefHeight);
            float vw = Screen.width / scale;
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            GUI.depth = -1000;

            if (openT > 0.005f) DrawPanel();
            else if (Cheats.ShowHud.Value) Toasts.DrawHud(vw);
            Toasts.Draw(vw);

            GUI.matrix = oldMatrix;
        }

        // ───────────────────────────── panel ─────────────────────────────

        private void DrawPanel()
        {
            float t = Mathf.Clamp01(openT);
            float slide = IsOpen ? Theme.EaseOutBack(t) : t;
            float x = Mathf.LerpUnclamped(-PanelW - 60f, 40f, slide);
            float y = (RefHeight - PanelH) / 2f;
            float alpha = t;
            // Rare CRT flicker for flavour.
            if (IsOpen && Random.value < 0.004f) alpha *= 0.82f;

            var panel = new Rect(x, y, PanelW, PanelH);
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 2.2f);

            Theme.Glow(panel, Theme.A(Theme.Teal, alpha * pulse), 16f, 26f, 8);
            Theme.Rect(panel, Theme.A(Theme.Bg, alpha), 16f);
            Theme.Outline(panel, Theme.A(Theme.Teal, alpha * 0.55f), 1.5f, 16f);

            // Corner brackets
            var c = Theme.A(Theme.Amber, alpha);
            Theme.Rect(new Rect(x + 10, y + 10, 18, 2), c); Theme.Rect(new Rect(x + 10, y + 10, 2, 18), c);
            Theme.Rect(new Rect(x + PanelW - 28, y + PanelH - 12, 18, 2), c); Theme.Rect(new Rect(x + PanelW - 12, y + PanelH - 28, 2, 18), c);

            DrawHeader(x, y, alpha);
            DrawTabs(new Rect(x + Pad, y + 104f, PanelW - Pad * 2, 38f), alpha);

            float cy = y + 156f;
            foreach (var tc in Cheats.Toggles.Where(e => e.Tab == tab)) cy = ToggleRow(tc, x + Pad, cy, alpha);
            foreach (var sc in Cheats.Sliders.Where(e => e.Tab == tab)) cy = SliderRow(sc, x + Pad, cy, alpha);
            foreach (var ac in Cheats.Actions.Where(e => e.Tab == tab)) cy = ActionRow(ac, x + Pad, cy, alpha);

            DrawFooter(x, y + PanelH - 48f, alpha);

            Theme.Scanlines(panel, -Time.unscaledTime * 0.6f, alpha * 0.9f);

            // Swallow clicks on the panel so they never reach anything behind it.
            var e = Event.current;
            if (panel.Contains(e.mousePosition) && (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel))
                e.Use();
            if (e.type == EventType.MouseUp) dragging = null;
        }

        private void DrawHeader(float x, float y, float alpha)
        {
            float since = Time.unscaledTime - openedAt;
            string full = TitleA + TitleB;
            int shown = Mathf.Clamp(Mathf.FloorToInt(since * 28f), 0, full.Length);
            string a = full.Substring(0, Mathf.Min(shown, TitleA.Length));
            string b = shown > TitleA.Length ? full.Substring(TitleA.Length, shown - TitleA.Length) : "";
            bool cursorOn = Mathf.FloorToInt(Time.unscaledTime * 2.5f) % 2 == 0;
            string title = $"<color=#36F5C8>{a}</color><color=#FFB547>{b}</color>{(cursorOn ? "<color=#36F5C8>_</color>" : " ")}";
            Theme.Print(new Rect(x + Pad, y + 22f, 360f, 40f), title, Theme.Title, Theme.A(Color.white, alpha));

            string mode = GameActions.InGameplay ? "LINK ESTABLISHED" : "AWAITING ROBOT...";
            Theme.Print(new Rect(x + Pad + 2f, y + 62f, 340f, 20f),
                $"SYS.OVERRIDE v1.0  ·  {mode}  ·  [{Cheats.MenuKey.Value}] CLOSE", Theme.Subtitle, Theme.A(Theme.Dim, alpha));

            // Signal bars
            for (int i = 0; i < 5; i++)
            {
                float h = 6f + 18f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (3f + i * 0.9f) + i));
                var br = new Rect(x + PanelW - Pad - 50f + i * 10f, y + 52f - h, 6f, h);
                Theme.Rect(br, Theme.A(i % 2 == 0 ? Theme.Teal : Theme.Amber, alpha * 0.85f), 2f);
            }

            Theme.Rect(new Rect(x + Pad, y + 90f, PanelW - Pad * 2, 1f), Theme.A(Theme.Teal, alpha * 0.35f));
        }

        private void DrawTabs(Rect r, float alpha)
        {
            var tabs = new[] { Tab.Player, Tab.Combat, Tab.World };
            float w = r.width / tabs.Length;
            float targetX = r.x + w * (int)tab;
            tabIndicatorX = tabIndicatorX < 0 ? targetX : Theme.Smooth(tabIndicatorX, targetX, 16f);

            Theme.Rect(r, Theme.A(Theme.Row, alpha), 10f);
            Theme.Rect(new Rect(tabIndicatorX + 4f, r.y + 4f, w - 8f, r.height - 8f), Theme.A(Theme.Teal, alpha * 0.16f), 8f);
            Theme.Rect(new Rect(tabIndicatorX + 18f, r.yMax - 4f, w - 36f, 2f), Theme.A(Theme.Teal, alpha), 1f);

            for (int i = 0; i < tabs.Length; i++)
            {
                var tr = new Rect(r.x + w * i, r.y, w, r.height);
                bool sel = tabs[i] == tab;
                bool hover = tr.Contains(Event.current.mousePosition);
                int count = CountActive(tabs[i]);
                string label = tabs[i].ToString().ToUpperInvariant() + (count > 0 ? $" <color=#FFB547>{count}</color>" : "");
                Theme.Print(tr, label, Theme.Center, Theme.A(sel ? Theme.Teal : hover ? Theme.Text : Theme.Dim, alpha));
                if (Clicked(tr)) tab = tabs[i];
            }
        }

        private float ToggleRow(ToggleCheat tc, float x, float y, float alpha)
        {
            const float h = 58f;
            var r = new Rect(x, y, PanelW - Pad * 2, h - 6f);
            bool on = tc.On();
            DrawRowBg(r, tc, alpha, on);

            Theme.Print(new Rect(r.x + 16f, r.y + 6f, r.width - 100f, 24f), tc.Name.ToUpperInvariant(), Theme.Label, Theme.A(on ? Theme.Text : Theme.Dim, alpha));
            Theme.Print(new Rect(r.x + 16f, r.y + 28f, r.width - 100f, 18f), tc.Description, Theme.Small, Theme.A(Theme.Dim, alpha * 0.9f));

            // Switch
            float k = Anim(tc, on ? 1f : 0f, 18f);
            var track = new Rect(r.xMax - 70f, r.y + (r.height - 26f) / 2f, 52f, 26f);
            var trackCol = Color.Lerp(Theme.Track, Theme.Teal, k);
            if (k > 0.05f) Theme.Glow(track, Theme.A(Theme.Teal, alpha * k), 13f, 10f, 4);
            Theme.Rect(track, Theme.A(trackCol, alpha), 13f);
            var knob = new Rect(track.x + 3f + k * 26f, track.y + 3f, 20f, 20f);
            Theme.Rect(knob, Theme.A(Color.Lerp(Theme.Dim, Theme.Bg, k), alpha), 10f);

            if (Clicked(r)) Plugin.SetToggle(tc, !on);
            return y + h;
        }

        private float SliderRow(SliderCheat sc, float x, float y, float alpha)
        {
            const float h = 74f;
            var r = new Rect(x, y, PanelW - Pad * 2, h - 6f);
            bool active = sc.IsActive;
            DrawRowBg(r, sc, alpha, active);

            float v = sc.Value.Value;
            Theme.Print(new Rect(r.x + 16f, r.y + 6f, r.width - 120f, 24f), sc.Name.ToUpperInvariant(), Theme.Label, Theme.A(active ? Theme.Text : Theme.Dim, alpha));
            Theme.Print(new Rect(r.xMax - 116f, r.y + 6f, 100f, 24f), "x" + v.ToString("0.0#"), Theme.Right, Theme.A(active ? Theme.Amber : Theme.Dim, alpha));
            Theme.Print(new Rect(r.x + 16f, r.y + 27f, r.width - 32f, 16f), sc.Description, Theme.Small, Theme.A(Theme.Dim, alpha * 0.9f));

            var track = new Rect(r.x + 16f, r.y + 50f, r.width - 32f, 6f);
            var hit = new Rect(track.x - 8f, track.y - 10f, track.width + 16f, track.height + 20f);
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && hit.Contains(e.mousePosition)) { dragging = sc; e.Use(); }
            if (dragging == sc && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp))
            {
                float f = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                float raw = Mathf.Lerp(sc.Min, sc.Max, f);
                float snapped = Mathf.Clamp(Mathf.Round(raw / sc.Step) * sc.Step, sc.Min, sc.Max);
                if (Mathf.Abs(snapped - sc.Value.Value) > 0.0001f) Plugin.SetSlider(sc, snapped, announce: false);
                if (e.type == EventType.MouseUp) { dragging = null; Plugin.SetSlider(sc, snapped, announce: true); }
                e.Use();
            }

            float fill = Anim(sc, Mathf.InverseLerp(sc.Min, sc.Max, v), 20f);
            Theme.Rect(track, Theme.A(Theme.Track, alpha), 3f);
            var fillR = new Rect(track.x, track.y, Mathf.Max(6f, track.width * fill), track.height);
            if (active) Theme.Glow(fillR, Theme.A(Theme.Amber, alpha), 3f, 8f, 3);
            Theme.Rect(fillR, Theme.A(active ? Theme.Amber : Theme.Teal, alpha), 3f);
            // Ticks
            int ticks = 4;
            for (int i = 1; i < ticks; i++)
                Theme.Rect(new Rect(track.x + track.width * i / ticks, track.y + 8f, 1f, 4f), Theme.A(Theme.Dim, alpha * 0.5f));
            var knob = new Rect(track.x + track.width * fill - 8f, track.y - 5f, 16f, 16f);
            Theme.Rect(knob, Theme.A(Theme.Text, alpha), 8f);
            Theme.Outline(knob, Theme.A(active ? Theme.Amber : Theme.Teal, alpha), 2f, 8f);
            return y + h;
        }

        private float ActionRow(ActionCheat ac, float x, float y, float alpha)
        {
            const float h = 58f;
            var r = new Rect(x, y, PanelW - Pad * 2, h - 6f);
            DrawRowBg(r, ac, alpha, false);
            Theme.Print(new Rect(r.x + 16f, r.y + 6f, r.width - 140f, 24f), ac.Name.ToUpperInvariant(), Theme.Label, Theme.A(Theme.Text, alpha));
            Theme.Print(new Rect(r.x + 16f, r.y + 28f, r.width - 140f, 18f), ac.Description, Theme.Small, Theme.A(Theme.Dim, alpha * 0.9f));

            var b = new Rect(r.xMax - 112f, r.y + (r.height - 32f) / 2f, 96f, 32f);
            bool hover = b.Contains(Event.current.mousePosition);
            float flash = Anim(ac, 0f, 6f);
            if (hover || flash > 0.02f) Theme.Glow(b, Theme.A(Theme.Amber, alpha * Mathf.Max(flash, 0.6f)), 8f, 10f, 4);
            Theme.Rect(b, Theme.A(Color.Lerp(Theme.Bg, Theme.Amber, Mathf.Max(flash, hover ? 0.2f : 0f)), alpha), 8f);
            Theme.Outline(b, Theme.A(Theme.Amber, alpha), 1.5f, 8f);
            Theme.Print(b, ac.ButtonLabel, Theme.Center, Theme.A(flash > 0.5f ? Theme.Bg : Theme.Amber, alpha));
            if (Clicked(b)) { anim[ac] = 1f; ac.Run(); }
            return y + h;
        }

        private void DrawFooter(float x, float y, float alpha)
        {
            Theme.Rect(new Rect(x + Pad, y, PanelW - Pad * 2, 1f), Theme.A(Theme.Teal, alpha * 0.25f));
            int active = Cheats.Toggles.Count(t => t.On()) + Cheats.Sliders.Count(s => s.IsActive);
            Theme.Print(new Rect(x + Pad, y + 8f, 260f, 30f), $"ACTIVE OVERRIDES: <color=#FFB547>{active}</color>", Theme.Tiny, Theme.A(Theme.Dim, alpha));

            var reset = new Rect(x + PanelW - Pad - 110f, y + 8f, 110f, 30f);
            bool hover = reset.Contains(Event.current.mousePosition);
            Theme.Print(reset, "RESET ALL", Theme.Right, Theme.A(hover ? Theme.Red : Theme.Dim, alpha));
            if (Clicked(reset)) Plugin.ResetAll();
        }

        private void DrawRowBg(Rect r, object key, float alpha, bool active)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            float h = Anim(("hover", key), hover ? 1f : 0f, 14f);
            Theme.Rect(r, Theme.A(Color.Lerp(Theme.Row, Theme.RowHover, h), alpha), 10f);
            float a = Anim(("active", key), active ? 1f : 0f, 12f);
            if (a > 0.01f) Theme.Rect(new Rect(r.x, r.y + 10f, 3f, r.height - 20f), Theme.A(Theme.Teal, alpha * a), 1.5f);
        }

        // ───────────────────────────── helpers ─────────────────────────────

        private float Anim(object key, float target, float speed)
        {
            if (!anim.TryGetValue(key, out var v)) v = target;
            // Only advance on Repaint so the animation speed doesn't depend on how many events arrive.
            if (Event.current.type == EventType.Repaint) v = Theme.Smooth(v, target, speed);
            anim[key] = v;
            return v;
        }

        private static bool Clicked(Rect r)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                e.Use();
                return true;
            }
            return false;
        }

        private static int CountActive(Tab t) =>
            Cheats.Toggles.Count(c => c.Tab == t && c.On()) + Cheats.Sliders.Count(s => s.Tab == t && s.IsActive);
    }
}
