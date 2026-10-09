using System.Collections.Generic;
using HarmonyLib;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HaikuCheats
{
    /// <summary>
    /// Adds a CHEATS entry to the game's own pause menu and a native-looking cheats page built by cloning the
    /// game's pause buttons. Navigation goes through the game's Rewired UI input module, so it works with a
    /// controller exactly like the rest of the menu: up/down to move, A to toggle, left/right for sliders,
    /// B to go back.
    /// </summary>
    internal static class PauseCheats
    {
        private const string ButtonName = "HC_CheatsButton";
        private const float RowH = 16f, RowW = 160f, HeaderH = 13f;
        private const float RepeatDelay = 0.35f, RepeatRate = 0.08f;

        private static readonly Color Cream = new Color(1f, 0.925f, 0.839f, 1f);
        private static readonly Color On = new Color(0.45f, 0.95f, 0.8f, 1f);
        private static readonly Color Off = new Color(1f, 0.925f, 0.839f, 0.6f);
        private static readonly Color Amber = new Color(1f, 0.74f, 0.35f, 1f);

        private sealed class Row
        {
            public GameObject Go;
            public RectTransform Rect;
            public Image Bg;
            public Color BgColor;
            public Text Label, Value;
            public ToggleCheat Toggle;
            public SliderCheat Slider;
            public ActionCheat Action;
            public string Name, Desc;
        }

        private static CameraBehavior cam;
        private static GameObject panel, cheatsButton;
        private static RectTransform content;
        private static Text descText;
        private static float viewportH, contentH, scroll;
        private static readonly List<Row> rows = new List<Row>();
        private static float lastHorizontal, nextRepeat;

        public static bool IsOpen => panel != null && panel.activeSelf;

        // ───────────────────────────── injection ─────────────────────────────

        [HarmonyPatch(typeof(CameraBehavior), nameof(CameraBehavior.DisplayPauseUI))]
        private static class InjectButton
        {
            private static void Postfix(CameraBehavior __instance)
            {
                try { EnsureButton(__instance); }
                catch (System.Exception e) { Plugin.Log.LogError("Pause menu injection failed: " + e); }
            }
        }

        // B / Start while on the cheats page should step back to the pause menu, not unpause the game.
        [HarmonyPatch(typeof(CameraBehavior), nameof(CameraBehavior.ResumeHideUI))]
        private static class BackOutOfCheats
        {
            private static bool Prefix()
            {
                if (!IsOpen) return true;
                Close();
                return false;
            }
        }

        private static void EnsureButton(CameraBehavior c)
        {
            cam = c;
            var pause = c.pauseUI.transform;
            if (pause.Find(ButtonName) != null) return;

            var quit = c.quitButton;
            var resume = c.resumeButton;
            cheatsButton = Object.Instantiate(quit, quit.transform.parent);
            cheatsButton.name = ButtonName;
            cheatsButton.transform.SetSiblingIndex(quit.transform.GetSiblingIndex());

            // Slot between RESUME and QUIT using the game's own spacing (RESUME -> QUIT is one step apart).
            var rrt = (RectTransform)resume.transform;
            var qrt = (RectTransform)quit.transform;
            var crt = (RectTransform)cheatsButton.transform;
            var step = new Vector2(0f, rrt.anchoredPosition.y - qrt.anchoredPosition.y);
            crt.anchoredPosition = rrt.anchoredPosition - step;
            qrt.anchoredPosition = rrt.anchoredPosition - step * 2f;

            SetText(cheatsButton, "C H E A T S");
            var b = cheatsButton.GetComponent<Button>();
            b.onClick = new Button.ButtonClickedEvent();
            b.onClick.AddListener(Open);

            var rb = resume.GetComponent<Button>();
            var qb = quit.GetComponent<Button>();
            Link(rb, down: b);
            Link(b, up: rb, down: qb);
            Link(qb, up: b);
            if (qb.navigation.selectOnDown == rb) Link(rb, up: qb);

            panel = null; // new scene/camera: rebuild the page on demand
            Plugin.Log.LogInfo("Injected CHEATS into pause menu");
        }

        // ───────────────────────────── page ─────────────────────────────

        private static void Open()
        {
            if (cam == null) return;
            if (panel == null) Build();
            Sfx("event:/UI/UI Submit");
            cam.pauseUI.SetActive(false);
            panel.SetActive(true);
            scroll = 0f;
            content.anchoredPosition = Vector2.zero;
            cam.SwapEventSystemButton(rows[0].Go);
        }

        private static void Close()
        {
            panel.SetActive(false);
            cam.pauseUI.SetActive(true);
            cam.SwapEventSystemButton(cheatsButton);
        }

        private static void Build()
        {
            rows.Clear();
            var pauseRt = (RectTransform)cam.pauseUI.transform;
            var parent = (RectTransform)pauseRt.parent;
            float h = parent.rect.height;

            panel = Object.Instantiate(cam.pauseUI, parent);
            panel.name = "HC_CheatsUI";
            foreach (Transform child in panel.transform) Object.Destroy(child.gameObject);
            panel.SetActive(false);
            var bg = panel.GetComponent<Image>();
            if (bg != null) bg.color = new Color(bg.color.r * 0.7f, bg.color.g * 0.7f, bg.color.b * 0.7f, 0.94f);

            var pausedText = cam.pauseUI.transform.Find("PausedText");
            var title = Object.Instantiate(pausedText.gameObject, panel.transform);
            StripTranslate(title);
            title.GetComponent<Text>().text = "C H E A T S";
            ((RectTransform)title.transform).anchoredPosition = new Vector2(0f, h / 2f - 16f);

            viewportH = h - 58f;
            var viewport = NewRect("Viewport", panel.transform, new Vector2(RowW + 16f, viewportH), new Vector2(0f, 2f));
            viewport.gameObject.AddComponent<RectMask2D>();
            content = NewRect("Content", viewport, new Vector2(RowW, 0f), Vector2.zero);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            var template = cam.quitButton;
            float y = 0f;
            foreach (Tab tab in new[] { Tab.Player, Tab.Combat, Tab.World })
            {
                y = Header(title, tab.ToString().ToUpperInvariant(), y);
                foreach (var t in Cheats.Toggles) if (t.Tab == tab) y = AddRow(template, y, new Row { Toggle = t, Name = t.Name, Desc = t.Description });
                foreach (var s in Cheats.Sliders) if (s.Tab == tab) y = AddRow(template, y, new Row { Slider = s, Name = s.Name, Desc = s.Description });
                foreach (var a in Cheats.Actions) if (a.Tab == tab) y = AddRow(template, y, new Row { Action = a, Name = a.Name, Desc = a.Description });
            }
            contentH = y;
            content.sizeDelta = new Vector2(RowW, contentH);

            for (int i = 0; i < rows.Count; i++)
            {
                var up = rows[(i - 1 + rows.Count) % rows.Count].Go.GetComponent<Button>();
                var down = rows[(i + 1) % rows.Count].Go.GetComponent<Button>();
                var nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down };
                rows[i].Go.GetComponent<Button>().navigation = nav;
            }

            var descGo = Object.Instantiate(title, panel.transform);
            descText = descGo.GetComponent<Text>();
            descText.fontSize = 6;
            descText.color = new Color(Cream.r, Cream.g, Cream.b, 0.75f);
            descText.alignment = TextAnchor.MiddleCenter;
            var drt = (RectTransform)descGo.transform;
            drt.sizeDelta = new Vector2(RowW + 40f, 20f);
            drt.anchoredPosition = new Vector2(0f, -h / 2f + 14f);
        }

        private static float Header(GameObject titleTemplate, string text, float y)
        {
            var go = Object.Instantiate(titleTemplate, content);
            go.name = "Header " + text;
            var t = go.GetComponent<Text>();
            t.text = "-  " + text + "  -";
            t.fontSize = 7;
            t.color = Amber;
            t.alignment = TextAnchor.LowerCenter;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(RowW, HeaderH);
            rt.anchoredPosition = new Vector2(0f, -y);
            return y + HeaderH + 1f;
        }

        private static float AddRow(GameObject template, float y, Row row)
        {
            var go = Object.Instantiate(template, content);
            go.name = "Row " + row.Name;
            go.SetActive(true);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(RowW, RowH);
            rt.anchoredPosition = new Vector2(0f, -y);

            var labelGo = go.GetComponentInChildren<Text>(true).gameObject;
            StripTranslate(labelGo);
            row.Label = labelGo.GetComponent<Text>();
            row.Label.alignment = TextAnchor.MiddleLeft;
            row.Label.verticalOverflow = VerticalWrapMode.Overflow;
            row.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            Inset((RectTransform)labelGo.transform, 8f, 0f);

            var valueGo = Object.Instantiate(labelGo, go.transform);
            row.Value = valueGo.GetComponent<Text>();
            row.Value.alignment = TextAnchor.MiddleRight;
            Inset((RectTransform)valueGo.transform, 0f, 8f);

            row.Go = go;
            row.Rect = rt;
            row.Bg = go.GetComponent<Image>();
            row.BgColor = row.Bg != null ? row.Bg.color : Color.clear;
            var b = go.GetComponent<Button>();
            b.onClick = new Button.ButtonClickedEvent();
            b.onClick.AddListener(() => Submit(row));
            rows.Add(row);
            return y + RowH + 1f;
        }

        // ───────────────────────────── per-frame ─────────────────────────────

        public static void Update()
        {
            if (!IsOpen) return;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            Row current = null;
            foreach (var r in rows)
            {
                bool sel = r.Go == selected;
                if (sel) current = r;
                Refresh(r, sel);
            }
            if (current == null) return;

            descText.text = current.Desc + "\n" + Hint(current);
            HandleHorizontal(current);
            KeepVisible(current);
        }

        private static void Refresh(Row r, bool selected)
        {
            r.Label.text = (selected ? "> " : "") + r.Name.ToUpperInvariant();
            r.Label.color = selected ? Color.white : Cream;
            if (r.Bg != null) r.Bg.color = selected ? Color.Lerp(r.BgColor, On, 0.25f) : r.BgColor;

            if (r.Toggle != null)
            {
                bool on = r.Toggle.On();
                r.Value.text = on ? "ON" : "OFF";
                r.Value.color = on ? On : Off;
            }
            else if (r.Slider != null)
            {
                string v = "x" + r.Slider.Value.Value.ToString("0.0#");
                r.Value.text = selected ? "<  " + v + "  >" : v;
                r.Value.color = r.Slider.IsActive ? Amber : Off;
            }
            else
            {
                r.Value.text = "[ " + r.Action.ButtonLabel + " ]";
                r.Value.color = Amber;
            }
        }

        private static string Hint(Row r) =>
            r.Slider != null ? "LEFT / RIGHT  adjust     B  back" :
            r.Toggle != null ? "A  toggle     B  back" : "A  activate     B  back";

        private static void Submit(Row r)
        {
            if (r.Toggle != null) Plugin.SetToggle(r.Toggle, !r.Toggle.On());
            else if (r.Slider != null) Step(r.Slider, +1, wrap: true);
            else r.Action.Run();
            Sfx("event:/UI/UI Submit");
        }

        private static void HandleHorizontal(Row r)
        {
            if (!ReInput.isReady) return;
            float axis = ReInput.players.GetPlayer(0).GetAxisRaw("UIHorizontal");
            int dir = axis > 0.5f ? 1 : axis < -0.5f ? -1 : 0;
            float now = Time.unscaledTime;
            bool fire = false;
            if (dir != 0 && dir != (int)lastHorizontal) { fire = true; nextRepeat = now + RepeatDelay; }
            else if (dir != 0 && now >= nextRepeat) { fire = true; nextRepeat = now + RepeatRate; }
            lastHorizontal = dir;
            if (!fire) return;

            if (r.Slider != null) Step(r.Slider, dir, wrap: false);
            else if (r.Toggle != null && r.Toggle.On() != (dir > 0)) Plugin.SetToggle(r.Toggle, dir > 0);
            else return;
            Sfx("event:/UI/Move cursor");
        }

        private static void Step(SliderCheat s, int dir, bool wrap)
        {
            float v = Mathf.Round((s.Value.Value + dir * s.Step) / s.Step) * s.Step;
            if (wrap && v > s.Max + 0.0001f) v = s.Min;
            Plugin.SetSlider(s, Mathf.Clamp(v, s.Min, s.Max), announce: true);
        }

        private static void KeepVisible(Row r)
        {
            const float margin = 16f;
            float top = -r.Rect.anchoredPosition.y;
            float bottom = top + RowH;
            if (top - scroll < margin) scroll = top - margin;
            if (bottom - scroll > viewportH - margin) scroll = bottom - viewportH + margin;
            scroll = Mathf.Clamp(scroll, 0f, Mathf.Max(0f, contentH - viewportH));
            float y = Mathf.Lerp(content.anchoredPosition.y, scroll, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            content.anchoredPosition = new Vector2(0f, y);
        }

        // ───────────────────────────── helpers ─────────────────────────────

        private static void SetText(GameObject button, string text)
        {
            var label = button.GetComponentInChildren<Text>(true);
            StripTranslate(label.gameObject);
            label.text = text;
        }

        // TranslateText overwrites the label with a localisation lookup on Start.
        private static void StripTranslate(GameObject go)
        {
            var tt = go.GetComponent<TranslateText>();
            if (tt != null) Object.DestroyImmediate(tt);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        private static void Inset(RectTransform rt, float left, float right)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, 0f);
            rt.offsetMax = new Vector2(-right, 0f);
        }

        private static void Link(Button b, Selectable up = null, Selectable down = null)
        {
            var n = b.navigation;
            n.mode = Navigation.Mode.Explicit;
            if (up != null) n.selectOnUp = up;
            if (down != null) n.selectOnDown = down;
            b.navigation = n;
        }

        private static void Sfx(string path)
        {
            if (SoundManager.instance != null) SoundManager.instance.PlayOneShot(path);
        }
    }
}
