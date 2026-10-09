using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaikuCheats.Patches
{
    /// <summary>
    /// Scales damage the player deals. Enemies, bosses, doors and switches each implement
    /// IDamageable.TakeDamage (sword) and/or INonSwordDmg.TakeNonSwordDamage (bombs, orbs, trails),
    /// so we patch every implementation instead of the attack code paths.
    /// Some enemies (e.g. Disruptor, drill boss parts) ignore the amount and lose 1 HP per hit; they are unaffected.
    /// </summary>
    [HarmonyPatch]
    internal static class DamagePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var asm = typeof(IDamageable).Assembly;
            foreach (var type in AccessTools.GetTypesFromAssembly(asm))
            {
                if (type.IsInterface || type.IsAbstract) continue;
                if (typeof(IDamageable).IsAssignableFrom(type))
                {
                    var m = AccessTools.Method(type, nameof(IDamageable.TakeDamage), new[] { typeof(int), typeof(int), typeof(Vector2) });
                    if (m != null && m.DeclaringType == type) yield return m;
                }
                if (typeof(INonSwordDmg).IsAssignableFrom(type))
                {
                    var m = AccessTools.Method(type, nameof(INonSwordDmg.TakeNonSwordDamage), new[] { typeof(int), typeof(int), typeof(Vector2) });
                    if (m != null && m.DeclaringType == type) yield return m;
                }
            }
        }

        // Parameter names differ between implementations, so edit the first argument through __args.
        private static void Prefix(object[] __args)
        {
            if (!(__args[0] is int dmg) || dmg <= 0) return;
            if (Cheats.DamageMult.IsActive)
                __args[0] = Mathf.Max(1, Mathf.RoundToInt(dmg * Cheats.DamageMult.Value.Value));
        }
    }
}
