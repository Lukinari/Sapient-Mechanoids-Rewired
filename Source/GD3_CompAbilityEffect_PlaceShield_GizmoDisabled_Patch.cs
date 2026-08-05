using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Glitterworld Destroyer 5 grants the vanilla Centurion its "Deploy high-angle
    /// shield" ability (MechPlaceShield -> GD3.CompAbilityEffect_PlaceShield, added via
    /// an XML patch on Mech_Centurion's PawnKindDef, not one of GD5's own races). Its
    /// GizmoDisabled directly checks MechanitorUtility.GetOverseer(pawn) == null and
    /// disables the gizmo with "This unit is not controlled" if so - the same recurring
    /// pattern this mod fixes everywhere else, but this time on an ability's own gizmo
    /// rather than a ThingComp's, so OverseerGizmoSuppressionPatches' suppression window
    /// (scoped to specific ThingComp.CompGetGizmosExtra calls) never covers it.
    ///
    /// It also disables itself below 20 energy (Power reads from
    /// pawn.needs.energy.CurLevel, defaulting to 0 when that need doesn't exist) - which
    /// permanently locks the ability out for a sapient mech, since it has no energy need
    /// to ever fill. The ability's own energy-spend step already no-ops safely when
    /// energy is null (confirmed by reading GD3's source), so there's no cost to pay
    /// either way.
    ///
    /// Only overrides the result when the pawn is downed-and-otherwise-fine is NOT the
    /// reason (that disable is legitimate and left alone) - i.e. only clears the
    /// GetOverseer/energy-driven disables, which are the only ones meaningless for a
    /// sapient mech.
    ///
    /// Glitterworld Destroyer 5 is an optional dependency - CompAbilityEffect_PlaceShield
    /// is resolved by name at runtime and only ever invoked through cached MethodInfo,
    /// never referenced directly in this patch's own signature (only vanilla
    /// CompAbilityEffect/Pawn types), so this class is entirely inert if that mod isn't
    /// installed.
    /// </summary>
    [HarmonyPatch]
    public static class GD3_CompAbilityEffect_PlaceShield_GizmoDisabled_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("GD3.CompAbilityEffect_PlaceShield");

        static MethodBase TargetMethod()
        {
            return CompType == null ? null : AccessTools.Method(CompType, "GizmoDisabled");
        }

        public static void Postfix(CompAbilityEffect __instance, ref bool __result, ref string reason)
        {
            try
            {
                if (!__result)
                    return;

                Pawn pawn = __instance.parent?.pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid (GetOverseer/energy are meaningful for it), or not mechanical at all.

                if (pawn.Downed)
                    return; // Legitimate disable reason - leave it.

                __result = false;
                reason = null;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] GD3 PlaceShield gizmo-disabled patch failed: " + e, 91274502);
            }
        }
    }
}
