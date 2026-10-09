using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace HaikuCheats
{
    internal enum Tab { Player, Combat, World }

    internal sealed class ToggleCheat
    {
        public string Name;
        public string Description;
        public string HudTag;
        public Tab Tab;
        public ConfigEntry<bool> Enabled;
        public ConfigEntry<KeyboardShortcut> Hotkey;
    }

    internal sealed class SliderCheat
    {
        public string Name;
        public string Description;
        public string HudTag;
        public Tab Tab;
        public float Min, Max, Step;
        public ConfigEntry<float> Value;
        public bool IsActive => Mathf.Abs(Value.Value - 1f) > 0.001f;
    }

    internal sealed class ActionCheat
    {
        public string Name;
        public string Description;
        public string ButtonLabel;
        public Tab Tab;
        public System.Action Run;
    }

    /// <summary>All cheat state, backed by BepInEx config so it survives restarts.</summary>
    internal static class Cheats
    {
        public static ConfigEntry<KeyboardShortcut> MenuKey;
        public static ConfigEntry<bool> ShowHud;

        public static ToggleCheat GodMode, AutoRepair, InfiniteHeat, InfiniteJumps, AllAbilities, RapidAttack;
        public static SliderCheat MoveSpeed, JumpHeight, DamageMult, GameSpeed;

        public static readonly List<ToggleCheat> Toggles = new List<ToggleCheat>();
        public static readonly List<SliderCheat> Sliders = new List<SliderCheat>();
        public static readonly List<ActionCheat> Actions = new List<ActionCheat>();

        public static void Init(ConfigFile cfg)
        {
            MenuKey = cfg.Bind("General", "MenuKey", new KeyboardShortcut(KeyCode.F1), "Opens and closes the cheat menu.");
            ShowHud = cfg.Bind("General", "ShowHud", true, "Show the active-cheats badge while the menu is closed.");

            GodMode = Toggle(cfg, Tab.Player, "God Mode", "GOD", "Take no damage from anything.", KeyCode.F2);
            AutoRepair = Toggle(cfg, Tab.Player, "Auto Repair", "REPAIR", "Health instantly refills to max.", KeyCode.None);
            InfiniteJumps = Toggle(cfg, Tab.Player, "Infinite Air Jumps", "JUMPS", "Double jump forever, even before you've earned it.", KeyCode.F3);
            AllAbilities = Toggle(cfg, Tab.Player, "All Abilities", "ABIL", "Temporarily grants every movement ability. Not saved.", KeyCode.None);
            MoveSpeed = Slider(cfg, Tab.Player, "Move Speed", "SPD", "Run speed multiplier.", 1f, 3f, 0.1f);
            JumpHeight = Slider(cfg, Tab.Player, "Jump Power", "JMP", "Jump force multiplier.", 1f, 2f, 0.05f);

            InfiniteHeat = Toggle(cfg, Tab.Combat, "No Overheat", "COOL", "Abilities never build heat.", KeyCode.F6);
            RapidAttack = Toggle(cfg, Tab.Combat, "Rapid Attack", "RAPID", "Remove the sword swing cooldown.", KeyCode.None);
            DamageMult = Slider(cfg, Tab.Combat, "Damage", "DMG", "Outgoing damage multiplier.", 1f, 10f, 0.5f);

            GameSpeed = Slider(cfg, Tab.World, "Game Speed", "TIME", "Turbo the whole game.", 1f, 3f, 0.1f);
            Actions.Add(new ActionCheat { Tab = Tab.World, Name = "Full Repair", ButtonLabel = "REPAIR", Description = "Restore health to max right now.", Run = GameActions.FullRepair });
            Actions.Add(new ActionCheat { Tab = Tab.World, Name = "Vent Heat", ButtonLabel = "VENT", Description = "Instantly cool the heat gauge.", Run = GameActions.VentHeat });
        }

        private static ToggleCheat Toggle(ConfigFile cfg, Tab tab, string name, string tag, string desc, KeyCode key)
        {
            var section = "Cheats." + tab;
            var t = new ToggleCheat
            {
                Name = name, HudTag = tag, Description = desc, Tab = tab,
                Enabled = cfg.Bind(section, name, false, desc),
                Hotkey = cfg.Bind("Hotkeys", name, key == KeyCode.None ? KeyboardShortcut.Empty : new KeyboardShortcut(key), "Quick toggle for " + name + "."),
            };
            Toggles.Add(t);
            return t;
        }

        private static SliderCheat Slider(ConfigFile cfg, Tab tab, string name, string tag, string desc, float min, float max, float step)
        {
            var s = new SliderCheat
            {
                Name = name, HudTag = tag, Description = desc, Tab = tab, Min = min, Max = max, Step = step,
                Value = cfg.Bind("Cheats." + tab, name, 1f, new ConfigDescription(desc, new AcceptableValueRange<float>(min, max))),
            };
            Sliders.Add(s);
            return s;
        }

        public static bool On(this ToggleCheat t) => t.Enabled.Value;
    }
}
