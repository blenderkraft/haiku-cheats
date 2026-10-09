using UnityEngine;

namespace HaikuCheats
{
    /// <summary>One-shot actions triggered from the menu. All guard against being outside gameplay.</summary>
    internal static class GameActions
    {
        public static bool InGameplay => PlayerScript.instance != null && PlayerHealth.instance != null && GameManager.instance != null;

        public static void GiveParts()
        {
            if (!InGameplay || InventoryManager.instance == null) { Toasts.Show("Load a save first", Toasts.Warn); return; }
            InventoryManager.instance.AddSpareParts(1000);
            Toasts.Show("+1000 SPARE PARTS", Toasts.Amber);
        }

        public static void FullRepair()
        {
            if (!InGameplay) { Toasts.Show("Load a save first", Toasts.Warn); return; }
            var hp = PlayerHealth.instance;
            int missing = hp.calculateMaxHealth() - hp.currentHealth;
            if (missing > 0 && !PlayerScript.instance.isDead) hp.AddHealth(missing);
            Toasts.Show("SYSTEMS REPAIRED", Toasts.Teal);
        }

        public static void VentHeat()
        {
            if (ManaManager.instance == null) { Toasts.Show("Load a save first", Toasts.Warn); return; }
            Patches.HeatPatches.Vent(ManaManager.instance);
            Toasts.Show("HEAT VENTED", Toasts.Teal);
        }
    }
}
