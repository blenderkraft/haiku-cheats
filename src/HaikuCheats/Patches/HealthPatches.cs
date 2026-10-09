using HarmonyLib;

namespace HaikuCheats.Patches
{
    // Every hit on the player funnels through these two methods (see PlayerHealth in Assembly-CSharp).
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.TakeDamage))]
    internal static class GodMode_TakeDamage
    {
        private static bool Prefix() => !Cheats.GodMode.On();
    }

    // Hazards (spikes, pits) use StunAndTakeDamage, which also respawns you at the last safe platform.
    // Keep the respawn so you never get stuck inside a hazard, but zero the damage.
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.StunAndTakeDamage))]
    internal static class GodMode_Stun
    {
        private static void Prefix(ref int damage)
        {
            if (Cheats.GodMode.On()) damage = 0;
        }
    }

    [HarmonyPatch(typeof(PlayerHealth), "Update")]
    internal static class AutoRepair_Update
    {
        private static void Postfix(PlayerHealth __instance)
        {
            if (!Cheats.AutoRepair.On() || PlayerScript.instance == null || PlayerScript.instance.isDead) return;
            int missing = __instance.calculateMaxHealth() - __instance.currentHealth;
            if (missing > 0 && __instance.currentHealth > 0) __instance.AddHealth(missing);
        }
    }
}
