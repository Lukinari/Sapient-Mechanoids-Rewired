using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// The vanilla Apocriton's "Resurrect Mechs" ability wants mech corpses specifically.
    /// TargetingParameters_CanTarget_Patch already lets a dead sapient mech's corpse be
    /// clicked as a target, but CanResurrect has its own separate
    /// "!corpse.InnerPawn.RaceProps.IsMechanoid" gate that rejects it a second time, so
    /// the ability would still silently refuse to actually apply even once the corpse
    /// became selectable. Whether a mechanoid-resurrection ability should be allowed to
    /// bring back a sapient mech at all is a judgment call, not a bug fix, so it's gated
    /// on the "Allow resurrecting sapient mechs" mod setting (on by default) rather than
    /// applied unconditionally.
    ///
    /// The resurrect-charge cost is normally looked up by RaceProps.mechWeightClass
    /// (TryGetResurrectCost, patched alongside this in the same file) - if that weight
    /// class didn't survive Big and Small's clone pipeline, this charges the cheapest
    /// tier that exists rather than refusing to resurrect over an unrelated bookkeeping
    /// gap.
    ///
    /// CompAbilityEffect_ResurrectMech and TryGetResurrectCost are vanilla, but
    /// private - Harmony can target a private method directly by name on a type that IS
    /// referenced at compile time, no reflection needed for the type itself.
    /// </summary>
    [HarmonyPatch(typeof(CompAbilityEffect_ResurrectMech), "TryGetResurrectCost")]
    public static class CompAbilityEffect_ResurrectMech_TryGetResurrectCost_Patch
    {
        private static readonly FieldInfo CostsByWeightClassField = AccessTools.Field(typeof(CompAbilityEffect_ResurrectMech), "costsByWeightClass");

        public static void Postfix(CompAbilityEffect_ResurrectMech __instance, Corpse corpse, ref bool __result, ref int cost)
        {
            try
            {
                if (__result || !SapientMechanoidFixMod.Settings.allowMechResurrection)
                    return;

                Pawn innerPawn = corpse?.InnerPawn;
                if (innerPawn == null || innerPawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(innerPawn))
                    return; // Real mechanoid (already handled by the original), or not mechanical at all.

                if (CostsByWeightClassField?.GetValue(__instance) is Dictionary<MechWeightClassDef, int> costs && costs.Count > 0)
                {
                    cost = costs.Values.Min();
                    __result = true;
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Resurrect-mech cost patch failed: " + e, 91274508);
            }
        }
    }

    [HarmonyPatch(typeof(CompAbilityEffect_ResurrectMech), "CanResurrect")]
    public static class CompAbilityEffect_ResurrectMech_CanResurrect_Patch
    {
        private static readonly MethodInfo TryGetResurrectCostMethod = AccessTools.Method(typeof(CompAbilityEffect_ResurrectMech), "TryGetResurrectCost");
        private static readonly FieldInfo ResurrectChargesField = AccessTools.Field(typeof(CompAbilityEffect_ResurrectMech), "resurrectCharges");

        public static void Postfix(CompAbilityEffect_ResurrectMech __instance, Corpse corpse, ref bool __result)
        {
            try
            {
                if (__result || !SapientMechanoidFixMod.Settings.allowMechResurrection)
                    return;

                Pawn innerPawn = corpse?.InnerPawn;
                if (innerPawn == null || innerPawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(innerPawn))
                    return; // Real mechanoid (already handled by the original), or not mechanical at all.

                Pawn caster = __instance.parent?.pawn;
                if (caster == null || innerPawn.Faction != caster.Faction)
                    return;

                if (innerPawn.kindDef.abilities != null && innerPawn.kindDef.abilities.Contains(AbilityDefOf.ResurrectionMech))
                    return;

                if (corpse.timeOfDeath < Find.TickManager.TicksGame - __instance.Props.maxCorpseAgeTicks)
                    return;

                object[] args = { corpse, 0 };
                bool gotCost = (bool)TryGetResurrectCostMethod.Invoke(__instance, args);
                int cost = (int)args[1];
                int resurrectCharges = (int)ResurrectChargesField.GetValue(__instance);
                if (!gotCost || cost > resurrectCharges)
                    return;

                __result = true;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Apocriton resurrect-sapient-mech patch failed: " + e, 91274507);
            }
        }
    }
}
