using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Fortified Feature Framework (the shared library Dead Man's Switch and its
    /// addons depend on for most of their mechanoid comps) has its own self-repair
    /// job, JobDriver_RepairSelf, triggered whenever CompDeadManSwitch.woken is true
    /// (via JobGiver_RepairSelf). Its tick action unconditionally does
    /// pawn.needs.energy.CurLevel -= ... with no null check - the same class of bug
    /// vanilla's own JobDriver_RepairMech had, just in a completely separate class
    /// (JobDriver_RepairSelf : JobDriver, not : JobDriver_RepairMech), so the existing
    /// JobDriver_RepairMech_MakeNewToils_Patch.cs doesn't cover it. A sapient DMS mech
    /// that ever gets woken would NRE the first tick of its self-repair job.
    ///
    /// An earlier version of this fix called MechRepairUtility.RepairTick(Pawn) - the
    /// exact single-argument overload Fortified's own decompiled source calls - resolved
    /// by reflection to sidestep a suspected compile-time mismatch. That overload
    /// doesn't actually exist in the currently installed RimWorld version at all (only
    /// RepairTick(Pawn, int) does, confirmed via decompile - the same overload-drift bug
    /// found in GD5's own CompHitArmor recharge hook), so the reflected MethodInfo was
    /// always null and the guarded `?.Invoke` call silently did nothing: the tick timer
    /// still counted down and reset, Crafting XP still ticked up, but RepairTick itself
    /// was never actually called - a sapient DMS mech's self-repair job ran forever
    /// without ever healing anything. Fixed by calling the real, current overload
    /// directly instead - RepairTick(Pawn, int) is a public vanilla method already
    /// referenced by name elsewhere in this mod (JobDriver_RepairMech_MakeNewToils_Patch.cs),
    /// so no reflection is needed for it at all, only for Fortified's own type/method.
    /// Passing the same tick-interval delta this callback itself received matches
    /// exactly how vanilla's own JobDriver_RepairMech calls it.
    ///
    /// Fortified Feature Framework is an optional dependency - resolved by name at
    /// runtime via TargetMethod(), never referenced directly in this Postfix's own
    /// signature (which only uses vanilla JobDriver/Toil/MechRepairUtility types plus
    /// reflection for the one Fortified-specific field), so this class is entirely inert
    /// if that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class Fortified_JobDriver_RepairSelf_MakeNewToils_Patch
    {
        static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("Fortified.JobDriver_RepairSelf");
            return type == null ? null : AccessTools.Method(type, "MakeNewToils");
        }

        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> values, JobDriver __instance)
        {
            foreach (Toil toil in values)
            {
                if (toil.tickIntervalAction != null)
                    WrapTickAction(toil, __instance);
                yield return toil;
            }
        }

        private static void WrapTickAction(Toil toil, JobDriver driver)
        {
            Action<int> original = toil.tickIntervalAction;
            toil.tickIntervalAction = delegate (int delta)
            {
                try
                {
                    if (driver.pawn?.needs?.energy != null)
                    {
                        original(delta); // Real energy need present - vanilla's own behavior is already correct, untouched.
                        return;
                    }

                    // Sapient mechanoid: replicate the rest of the original tick action
                    // exactly, just without the line that unconditionally drains
                    // needs.energy.CurLevel. MechRepairUtility.RepairTick doesn't touch
                    // that need either way, so nothing about the actual repair is being
                    // skipped - only the power-bar bookkeeping that doesn't apply here.
                    Traverse ticksToNextRepairField = Traverse.Create(driver).Field("ticksToNextRepair");
                    int ticksToNextRepair = ticksToNextRepairField.GetValue<int>() - delta;
                    if (ticksToNextRepair <= 0)
                    {
                        MechRepairUtility.RepairTick(driver.pawn, delta);
                        ticksToNextRepair = Mathf.RoundToInt(1f / driver.pawn.GetStatValue(StatDefOf.MechRepairSpeed) * 120f);
                    }
                    ticksToNextRepairField.SetValue(ticksToNextRepair);

                    if (driver.pawn.skills != null)
                        driver.pawn.skills.Learn(SkillDefOf.Crafting, 0.05f * delta);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[SapientMechanoidFix] Fortified JobDriver_RepairSelf tick patch failed: " + e, 91274449);
                }
            };
        }
    }
}
