using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Glitterworld Destroyer 5's own CompHitArmor already has everything needed to
    /// recharge its reactive-armor charges via repair - a Notify_RepairMech() method,
    /// and its own MechRepair_Patch.Postfix that's supposed to call it every time a
    /// mechanitor repairs the pawn. That patch never actually attaches to anything,
    /// though: its TargetMethod looks for MechRepairUtility.RepairTick(Pawn) - a
    /// single-argument overload that doesn't exist in the current RimWorld version (only
    /// RepairTick(Pawn, int), confirmed via decompile) - the same overload-drift class of
    /// bug this mod's own MechRepairUtility_RepairTick_Patch.cs doc comment already
    /// flagged elsewhere. AccessTools.Method silently returns null for the nonexistent
    /// overload, Harmony logs it and skips patching, and GD3's own recharge hook simply
    /// never fires - for ANY mechanoid, sapient or not. Confirmed by testing: a sapient
    /// War Queen's reactive-armor charges deplete correctly on hit but never recharge
    /// even while a mechanitor is actively, successfully repairing it.
    ///
    /// This isn't a sapience-specific bug in the first place, but per this mod's own
    /// scope (see README's Compatibility notes - never touch a real, non-sapient
    /// mechanoid), the fix here only ever fires for a sapient mechanical pawn; a real
    /// mechanoid is left exactly as broken as GD5 itself currently ships it, rather than
    /// this mod silently changing behavior for pawns outside its stated scope.
    ///
    /// MechRepairUtility.CanRepair(Pawn) has no such overload ambiguity - GD3's own
    /// MechCanRepair_Patch.Prefix resolves and works correctly as-is, which is exactly
    /// why a mechanitor successfully starts and runs a repair job in the first place;
    /// only the recharge notification itself was silently dead.
    ///
    /// Glitterworld Destroyer 5 is an optional dependency - GD3.CompHitArmor is resolved
    /// by name at runtime and only ever invoked through a cached MethodInfo, never
    /// referenced directly in this patch's own signature (only vanilla Pawn types), so
    /// this class is entirely inert if that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class GD3_CompHitArmor_RepairRecharge_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("GD3.CompHitArmor");
        private static readonly MethodInfo NotifyRepairMechMethod = CompType == null ? null : AccessTools.Method(CompType, "Notify_RepairMech");

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(MechRepairUtility), nameof(MechRepairUtility.RepairTick), new[] { typeof(Pawn), typeof(int) });
        }

        public static void Postfix(Pawn mech)
        {
            try
            {
                if (NotifyRepairMechMethod == null || mech == null)
                    return;

                if (mech.RaceProps.IsMechanoid || !IsMechanicalCache.Get(mech))
                    return; // Real mechanoid - leave GD5's own (currently broken) hook alone, out of this mod's scope.

                object comp = mech.AllComps?.Find(c => CompType.IsInstanceOfType(c));
                if (comp == null)
                    return;

                NotifyRepairMechMethod.Invoke(comp, null);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] GD3 CompHitArmor repair-recharge patch failed: " + e, 91274525);
            }
        }
    }
}
