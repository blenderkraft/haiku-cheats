# Haiku Cheats: modding notes

## Game facts
- **Game:** Haiku, the Robot (Steam app 1231880). Developer folder: `MisterMorrisGames/Haiku`.
- **Engine:** Unity 2019.4.36f1 with the Mono backend. The code is not obfuscated, there is no anti-cheat, and the game is single-player only.
- **Platform:** Windows build running through Proton on Linux.
- **Saves:** `steamapps/compatdata/1231880/pfx/drive_c/users/steamuser/AppData/LocalLow/MisterMorrisGames/Haiku/` (`001.haiku`, and so on).
  - A backup was taken with `um backup create … --name haiku-saves`, stored at `~/.universal-modder/backups/haiku-saves/`.

## Route
- **Loader:** BepInEx 5.4.23.5 (win_x64) plus HarmonyX.
- **Proton launch option:** `WINEDLLOVERRIDES="winhttp=n,b" %command%`.
- **Build:** the plugin is built on Linux with the .NET 8 SDK, targeting net472.

## Hook points (Assembly-CSharp)
| Cheat | Hook |
|---|---|
| God mode | `PlayerHealth.TakeDamage(int,int)`: prefix skips the call. `PlayerHealth.StunAndTakeDamage(int)`: damage set to 0, and the hazard respawn is kept. |
| Auto repair | `PlayerHealth.Update` postfix calls `AddHealth(calculateMaxHealth() - currentHealth)`. |
| No overheat | `ManaManager.AddHeat` is skipped. The gauge is `currentRotation` (405 = cool, 135 or below = overheated). |
| Infinite air jumps | `PlayerScript.Update`: `hasDoubleJumped` is reset on the frame Jump is pressed, and `GameManager.canDoubleJump` is forced on for the duration of the call. |
| All abilities | The `GameManager.can*` flags are overridden inside `PlayerScript.Update` only, then restored, so they are never saved. |
| Move speed | `PlayerScript.Walk`: `speed` is scaled for the call, then restored. |
| Jump power | `PlayerScript.Jump(Vector2)`: `jumpForce` is scaled for the call, then restored. Wall jumps and pogo jumps also route through it. |
| Fly / noclip | `PlayerScript.FixedUpdate` postfix sets velocity from `dirRaw` with gravityScale 0. The Update postfix disables `boxColl`, `circleColl` and `BetterJumping`. |
| Rapid attack | Private `PlayerScript.nextAttackTime` is set to 0 before Update. |
| Damage / one-hit kill | Every `IDamageable.TakeDamage` and `INonSwordDmg.TakeNonSwordDamage` implementation is patched (about 100 methods), with `__args[0]` scaled. |
| Spare parts | `InventoryManager.AddSpareParts(int)`. |
| Game speed | `Time.timeScale`, applied only while the game is at 1.0. `TimeManager.TriggerSlowMo` fades the time scale back to 1 using unscaled time. |

## Gotchas
1. ilspycmd 8.x targets .NET 6. Run it with `DOTNET_ROLL_FORWARD=Major` on newer runtimes.
2. The double-jump input buffer lasts about 0.1 s, so resetting `hasDoubleJumped` on every frame spams jumps. Only reset it when Jump is pressed.
3. `PlayerScript.Update` re-enables the colliders and `BetterJumping` every frame, so fly mode has to win in a postfix.
4. Menu input blocking uses Rewired `Player.isPlaying = false`. The game ignores the legacy Input API except for one debug key.

## Pause menu integration
- `CameraBehavior.pauseUI` is a uGUI panel on a pixel-scale canvas (about 384×216 units at 1080p). Buttons are 80×16 at y=+1 (Resume) and y=-16 (Quit), with explicit navigation. The font is Jaldi-Regular at size 8, colour (1, .925, .839).
- `RewiredStandaloneInputModule` drives selection. The game's arrow cursor follows any button that carries `MoveCursorOnSelect`, so clones of `quitButton` get the cursor for free.
- `UICancel` / `ClosePanel` call `CameraBehavior.ResumeHideUI`. That method is prefixed so B on the cheats page goes back to the pause menu instead of unpausing.
- The `TranslateText` component on button labels overwrites `text` with a localisation lookup in `Start`. Destroy it on clones.
- Gotcha: a size-8 Jaldi `Text` vanishes entirely in a rect shorter than about 15 units, because vertical overflow is set to Truncate. Use 16-unit rows or Overflow.
