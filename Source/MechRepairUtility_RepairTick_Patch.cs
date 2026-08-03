using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Recharge hook for CompSapientMechHitArmor, mirroring Glitterworld Destroyer 5's
    /// own MechRepairUtility.RepairTick postfix. Harmless no-op for any pawn without
    /// the comp (real mechanoids, or anything not carrying it at all).
    ///
    /// RepairTick has more than one overload (confirmed via a HarmonyException in the
    /// wild: "Ambiguous match ... methodname=RepairTick"), so a bare
    /// [HarmonyPatch(type, nameof(...))] can't resolve which one to target - Harmony
    /// throws, and since PatchAll() aborts entirely on a throwing patch class, that one
    /// ambiguous reference silently took every other patch in this mod down with it.
    /// TargetMethod pins down the exact (Pawn, int) overload instead.
    /// </summary>
    [HarmonyPatch]
    public static class MechRepairUtility_RepairTick_Patch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(MechRepairUtility), nameof(MechRepairUtility.RepairTick), new[] { typeof(Pawn), typeof(int) });
        }

        public static void Postfix(Pawn mech)
        {
            try
            {
                mech?.GetComp<CompSapientMechHitArmor>()?.Notify_RepairMech();
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Reactive armor repair-recharge patch failed: " + e, 91274447);
            }
        }
    }
}
