using System;
using System.Linq;
using BigAndSmall;
using RimWorld;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// DMS's Nuclear Battery module (DMS_NuclearBattery) reduces a real mechanoid's
    /// MechEnergyUsageFactor to 0.25 - but a sapient mech doesn't run on mech energy at
    /// all anymore, it eats like any other colonist, so that reduction does nothing for
    /// it. Requested feature: let the same module cut a sapient mech's hunger rate by
    /// the same factor it would have cut energy use by.
    ///
    /// Reads the factor straight off the hediff's own current stage
    /// (statFactors[MechEnergyUsageFactor]) instead of hardcoding 0.25, so this stays in
    /// sync automatically if DMS ever rebalances the module - "same amount" is derived,
    /// not duplicated.
    ///
    /// DMS is an optional dependency - DMS_NuclearBattery is resolved by name via
    /// DefDatabase (a plain data lookup, not a reflection-resolved type), so this class
    /// only ever finds a null def and no-ops if DMS isn't installed.
    /// </summary>
    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.GetHungerRateFactor))]
    public static class HediffSet_GetHungerRateFactor_Patch
    {
        private static HediffDef nuclearBatteryDef;
        private static bool lookedUp;

        private static HediffDef NuclearBatteryDef
        {
            get
            {
                if (!lookedUp)
                {
                    lookedUp = true;
                    nuclearBatteryDef = DefDatabase<HediffDef>.GetNamedSilentFail("DMS_NuclearBattery");
                }
                return nuclearBatteryDef;
            }
        }

        public static void Postfix(HediffSet __instance, ref float __result)
        {
            try
            {
                if (NuclearBatteryDef == null)
                    return;

                Pawn pawn = __instance.pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid already gets the intended MechEnergyUsageFactor effect, or not mechanical at all.

                HediffStage stage = __instance.hediffs.FirstOrDefault(h => h.def == NuclearBatteryDef)?.CurStage;
                StatModifier energyFactor = stage?.statFactors?.FirstOrDefault(m => m.stat == StatDefOf.MechEnergyUsageFactor);
                if (energyFactor == null)
                    return;

                __result *= energyFactor.value;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] DMS Nuclear Battery hunger patch failed: " + e, 91274490);
            }
        }
    }
}
