using HarmonyLib;

namespace HaikuCheats.Patches
{
    // The heat gauge is ManaManager.currentRotation: 405 = cool, <=135 = overheated.
    internal static class HeatPatches
    {
        private const float Cool = 405f;

        public static void Vent(ManaManager mm)
        {
            mm.currentRotation = Cool;
            mm.cooldown = false;
        }

        [HarmonyPatch(typeof(ManaManager), nameof(ManaManager.AddHeat))]
        private static class NoHeat_AddHeat
        {
            private static bool Prefix() => !Cheats.InfiniteHeat.On();
        }

        [HarmonyPatch(typeof(ManaManager), "Update")]
        private static class NoHeat_Update
        {
            private static void Prefix(ManaManager __instance)
            {
                if (Cheats.InfiniteHeat.On()) Vent(__instance);
            }
        }
    }
}
