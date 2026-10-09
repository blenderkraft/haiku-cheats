# Haiku Cheats

Haiku Cheats is an in-game cheat menu for **Haiku, the Robot** (single-player). It is a BepInEx 5 plugin with a CRT-terminal style overlay.

Press **F1** in game to open the menu.

| Tab | Cheats |
|---|---|
| Player | God Mode (F2), Auto Repair, Infinite Air Jumps (F3), Fly / Noclip (F4), All Abilities, Move Speed ×1–3, Jump Power ×1–2 |
| Combat | One-Hit Kills (F5), No Overheat (F6), Rapid Attack, Damage ×1–10 |
| World | Game Speed ×1–3, +1000 Spare Parts, Full Repair, Vent Heat |

- Toggles and slider values persist in `BepInEx/config/dev.haikucheats.cfg`.
- Hotkeys can be rebound in the same file.
- "All Abilities" and "Infinite Air Jumps" never write to your save. Spare parts you add *are* saved.

## Install (Linux / Proton)
1. Download [BepInEx 5 win_x64](https://github.com/BepInEx/BepInEx/releases) and extract it into the game folder.
2. In Steam, open Haiku → Properties → Launch Options and set:
   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```
3. Build and deploy:
   ```
   dotnet build src/HaikuCheats -c Release
   ```
   Pass `-p:HaikuDir=...` if the game isn't in the default Steam library. The post-build step copies the DLL to `BepInEx/plugins/HaikuCheats/`.

On Windows, skip the launch option.

## Notes
- The plugin needs the .NET SDK 8+ to build. Game DLLs are referenced in place and never copied into this repo.
- Back up your saves first. They live in `compatdata/1231880/pfx/.../LocalLow/MisterMorrisGames/Haiku`.
- Built with Claude Code and the universal-modder plugin.
