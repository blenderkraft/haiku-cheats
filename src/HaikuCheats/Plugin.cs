using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HaikuCheats
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "dev.haikucheats";
        public const string Name = "Haiku Cheats";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;

        private readonly CheatMenu menu = new CheatMenu();
        private float appliedTimeScale = -1f;

        private void Awake()
        {
            Log = Logger;
            Cheats.Init(Config);
            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            int count = 0;
            foreach (var _ in harmony.GetPatchedMethods()) count++;
            Log.LogInfo($"{Name} {Version} loaded - {count} methods patched. Press {Cheats.MenuKey.Value} in game.");
        }

        private void Update()
        {
            if (Cheats.MenuKey.Value.IsDown()) menu.Toggle();

            menu.Update();
            PauseCheats.Update();
            ApplyGameSpeed();
        }

        private void OnGUI() => menu.OnGUI();

        // The game drives Time.timeScale for hit-stop slow-mo (fading back to 1) and pause (0).
        // Only bump it while the game is at normal speed, and only undo our own value.
        private void ApplyGameSpeed()
        {
            bool paused = TimeManager.instance != null && TimeManager.instance.paused;
            float ts = Time.timeScale;
            if (Cheats.GameSpeed.IsActive)
            {
                float target = Cheats.GameSpeed.Value.Value;
                if (!paused && ts >= 0.999f && Mathf.Abs(ts - target) > 0.001f)
                {
                    Time.timeScale = target;
                    appliedTimeScale = target;
                }
            }
            else if (appliedTimeScale > 0f)
            {
                if (Mathf.Abs(ts - appliedTimeScale) < 0.001f) Time.timeScale = 1f;
                appliedTimeScale = -1f;
            }
        }

        internal static void SetToggle(ToggleCheat t, bool on)
        {
            t.Enabled.Value = on;
            Toasts.Show($"{t.Name.ToUpperInvariant()}: {(on ? "ON" : "OFF")}", on ? Theme.Teal : Theme.Dim);
            Log.LogInfo($"{t.Name} -> {(on ? "ON" : "OFF")}");
        }

        internal static void SetSlider(SliderCheat s, float value, bool announce)
        {
            s.Value.Value = value;
            if (!announce) return;
            Toasts.Show($"{s.Name.ToUpperInvariant()}: x{value:0.0#}", s.IsActive ? Theme.Amber : Theme.Dim);
            Log.LogInfo($"{s.Name} -> x{value:0.##}");
        }

        internal static void ResetAll()
        {
            foreach (var t in Cheats.Toggles) t.Enabled.Value = false;
            foreach (var s in Cheats.Sliders) s.Value.Value = 1f;
            Toasts.Show("ALL OVERRIDES CLEARED", Theme.Red);
            Log.LogInfo("All cheats reset");
        }
    }
}
