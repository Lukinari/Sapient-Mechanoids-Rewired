using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Mechanoids: Total Warfare's "Area Repair" ability (NCL_MechSelfRepair_PsycastsMilitor /
    /// TW_MechSelfRepair_PsycastsMilitor, comp NCL.CompAbilityEffect_GroupRepair) loops
    /// over nearby allies and only repairs ones matching pawn.RaceProps.IsMechanoid
    /// directly - the same recurring pattern as everywhere else in this mod, just inside
    /// an ability effect instead of a ThingComp. A sapient mech in range never gets
    /// repaired, and if the caster itself is the only mechanoid nearby, the ability casts
    /// successfully but visibly does nothing.
    ///
    /// Rather than replacing Apply() outright (its own targeting/looping logic is
    /// otherwise correct and worth keeping untouched), this postfixes it and does a
    /// second, narrower pass over the same radius for exactly the pawns the original
    /// would have skipped for being sapient - reusing its own private RepairMech/
    /// SpawnRepairEffect methods via reflection so the actual repair/effect behavior
    /// stays identical to what a real mechanoid gets.
    ///
    /// Mechanoids: Total Warfare is an optional dependency - CompAbilityEffect_GroupRepair
    /// is resolved by name at runtime and only ever invoked through cached MethodInfo,
    /// never referenced directly in this patch's own signature (only vanilla
    /// CompAbilityEffect/Pawn/LocalTargetInfo types), so this class is entirely inert if
    /// that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class NCL_CompAbilityEffect_GroupRepair_Apply_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("NCL.CompAbilityEffect_GroupRepair");
        private static readonly MethodInfo RepairMechMethod = CompType == null ? null : AccessTools.Method(CompType, "RepairMech");
        private static readonly MethodInfo SpawnRepairEffectMethod = CompType == null ? null : AccessTools.Method(CompType, "SpawnRepairEffect");

        static MethodBase TargetMethod()
        {
            // CompAbilityEffect declares two Apply overloads (LocalTargetInfo+LocalTargetInfo,
            // and GlobalTargetInfo) - AccessTools.Method(type, "Apply") without parameter
            // types throws AmbiguousMatchException instead of picking one. Confirmed via
            // debug log: this silently failed to patch at all, so the Area Repair fix was
            // never actually active in-game despite building cleanly.
            return CompType == null ? null : AccessTools.Method(CompType, "Apply", new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) });
        }

        public static void Postfix(CompAbilityEffect __instance, LocalTargetInfo target)
        {
            try
            {
                if (RepairMechMethod == null || SpawnRepairEffectMethod == null)
                    return;

                IntVec3 cell = target.Cell;
                if (!cell.IsValid)
                    return;

                Pawn caster = __instance.parent?.pawn;
                if (caster?.Map == null)
                    return;

                Faction faction = caster.Faction;
                foreach (Thing thing in GenRadial.RadialDistinctThingsAround(cell, caster.Map, 25f, true))
                {
                    if (thing is not Pawn pawn || pawn.Faction != faction)
                        continue;
                    if (pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                        continue; // Real mechanoid (already repaired by the original), or not mechanical at all.

                    RepairMechMethod.Invoke(__instance, new object[] { pawn });
                    SpawnRepairEffectMethod.Invoke(__instance, new object[] { pawn.Position, caster.Map });
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] NCL GroupRepair sapient-mech postfix failed: " + e, 91274470);
            }
        }
    }
}
