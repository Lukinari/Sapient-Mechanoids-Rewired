using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Alpha Mechs' own "Resurrect Mech Minor" ability has the exact same
    /// RaceProps.IsMechanoid gate on the target corpse as the vanilla Apocriton's
    /// ability (see CompAbilityEffect_ResurrectMech_CanResurrect_Patch.cs), but its own
    /// CanResurrect is much simpler - no charge/cost system, just a faction check, an
    /// "already has the ability" check, and (for anything other than a sapient mech, whose
    /// weight class may not have survived conversion) excluding ultra-heavy mechs from
    /// this "minor" version. Same "Allow resurrecting sapient mechs" mod setting gates
    /// this too.
    ///
    /// Alpha Mechs is an optional dependency - CompResurrectMechMinor is resolved by name
    /// at runtime and only ever invoked through cached MethodInfo, never referenced
    /// directly in this patch's own signature (only vanilla CompAbilityEffect/Pawn/Corpse
    /// types), so this class is entirely inert if that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class AlphaMechs_CompResurrectMechMinor_CanResurrect_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("AlphaMechs.CompResurrectMechMinor");

        static MethodBase TargetMethod()
        {
            return CompType == null ? null : AccessTools.Method(CompType, "CanResurrect");
        }

        public static void Postfix(CompAbilityEffect __instance, Corpse corpse, ref bool __result)
        {
            try
            {
                if (__result || !SapientMechanoidFixMod.Settings.allowMechResurrection)
                    return;

                Pawn innerPawn = corpse?.InnerPawn;
                if (innerPawn == null || innerPawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(innerPawn))
                    return; // Real mechanoid (already handled by the original), or not mechanical at all.

                if (innerPawn.RaceProps.mechWeightClass == MechWeightClassDefOf.UltraHeavy)
                    return; // Minor resurrection still excludes ultra-heavy mechs, same as a real one.

                Pawn caster = __instance.parent?.pawn;
                if (caster == null || innerPawn.Faction != caster.Faction)
                    return;

                if (innerPawn.kindDef.abilities != null && innerPawn.kindDef.abilities.Contains(AbilityDefOf.ResurrectionMech))
                    return;

                __result = true;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Alpha Mechs resurrect-sapient-mech patch failed: " + e, 91274509);
            }
        }
    }
}
