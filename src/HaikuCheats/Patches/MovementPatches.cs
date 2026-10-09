using HarmonyLib;
using Rewired;
using UnityEngine;

namespace HaikuCheats.Patches
{
    internal static class MovementPatches
    {
        private static readonly AccessTools.FieldRef<PlayerScript, float> NextAttackTime =
            AccessTools.FieldRefAccess<PlayerScript, float>("nextAttackTime");

        /// <summary>GameManager ability flags as they were before we overrode them for one Update.</summary>
        internal struct AbilityState
        {
            public bool Overridden;
            public bool DoubleJump, WallJump, Dash, Roll, Bomb, Grapple, Heal, AttackDown;
        }

        // PlayerScript.Update reads input and all the GameManager.can* flags. We force flags on for the
        // duration of the call only and restore them afterwards, so the save file never sees them.
        [HarmonyPatch(typeof(PlayerScript), "Update")]
        private static class PlayerUpdate
        {
            private static void Prefix(PlayerScript __instance, out AbilityState __state)
            {
                __state = default;
                var gm = GameManager.instance;
                if (gm == null) return;

                bool jumps = Cheats.InfiniteJumps.On();
                bool all = Cheats.AllAbilities.On();
                if (jumps || all)
                {
                    __state = new AbilityState
                    {
                        Overridden = true,
                        DoubleJump = gm.canDoubleJump, WallJump = gm.canWallJump, Dash = gm.canDash, Roll = gm.canRoll,
                        Bomb = gm.canBomb, Grapple = gm.canGrapple, Heal = gm.canHeal, AttackDown = gm.canAttackDown,
                    };
                    gm.canDoubleJump = true;
                    if (all)
                    {
                        gm.canWallJump = gm.canDash = gm.canRoll = gm.canBomb = true;
                        gm.canGrapple = gm.canHeal = gm.canAttackDown = true;
                    }
                }

                // Only reset on the frame Jump is pressed: the jump buffer stays live for ~0.1 s and would
                // otherwise fire a double jump every frame of it.
                if (jumps && !__instance.disableControls && ReInput.isReady && ReInput.players.GetPlayer(0).GetButtonDown("Jump"))
                    __instance.hasDoubleJumped = false;

                if (Cheats.RapidAttack.On())
                    NextAttackTime(__instance) = 0f;
            }

            private static void Postfix(AbilityState __state)
            {
                var gm = GameManager.instance;
                if (!__state.Overridden || gm == null) return;
                gm.canDoubleJump = __state.DoubleJump; gm.canWallJump = __state.WallJump; gm.canDash = __state.Dash;
                gm.canRoll = __state.Roll; gm.canBomb = __state.Bomb; gm.canGrapple = __state.Grapple;
                gm.canHeal = __state.Heal; gm.canAttackDown = __state.AttackDown;
            }
        }

        // Walk() writes rb.velocity from `speed`; scale it for this call only. The game itself mutates
        // `speed` while rolling, so we never persist our multiplier into the field.
        [HarmonyPatch(typeof(PlayerScript), "Walk")]
        private static class SpeedWalk
        {
            private static void Prefix(PlayerScript __instance, out float __state)
            {
                __state = __instance.speed;
                if (Cheats.MoveSpeed.IsActive) __instance.speed *= Cheats.MoveSpeed.Value.Value;
            }

            private static void Postfix(PlayerScript __instance, float __state) => __instance.speed = __state;
        }

        // Jump() covers normal, double, wall and pogo jumps (WallJump and the down-attack both call it).
        [HarmonyPatch(typeof(PlayerScript), nameof(PlayerScript.Jump))]
        private static class JumpPower
        {
            private static void Prefix(PlayerScript __instance, out float __state)
            {
                __state = __instance.jumpForce;
                if (Cheats.JumpHeight.IsActive) __instance.jumpForce *= Cheats.JumpHeight.Value.Value;
            }

            private static void Postfix(PlayerScript __instance, float __state) => __instance.jumpForce = __state;
        }
    }
}
