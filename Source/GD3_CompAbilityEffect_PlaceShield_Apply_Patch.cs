using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Fixing the Centurion's "Deploy high-angle shield" gizmo being disabled
    /// (GD3_CompAbilityEffect_PlaceShield_GizmoDisabled_Patch.cs) wasn't the whole story -
    /// the ability's own private PlaceShield(Pawn) does `if (pawn.needs.energy == null)
    /// return;` as its very first line, before ever placing the shield projector. A
    /// sapient mech has no energy need at all, so casting the ability was silently doing
    /// nothing even once the gizmo itself could be clicked.
    ///
    /// Rather than trying to skip past just that one early-return (private method, no
    /// good seam to resume mid-body from), this replaces the whole method for a sapient
    /// mech: places the shield exactly like the original does, just without the
    /// energy-deduction step, which was already meaningless for a pawn with no energy to
    /// spend. Real mechanoids are untouched - they still run the original unmodified.
    ///
    /// Glitterworld Destroyer 5 is an optional dependency - CompAbilityEffect_PlaceShield
    /// is resolved by name at runtime and only ever invoked through cached MethodInfo,
    /// never referenced directly in this patch's own signature (only vanilla Pawn/Thing
    /// types), so this class is entirely inert if that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class GD3_CompAbilityEffect_PlaceShield_Apply_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("GD3.CompAbilityEffect_PlaceShield");

        static MethodBase TargetMethod()
        {
            return CompType == null ? null : AccessTools.Method(CompType, "PlaceShield");
        }

        public static bool Prefix(Pawn pawn)
        {
            try
            {
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return true; // Real mechanoid - original energy-based logic is correct, run it.

                if (pawn.needs.energy != null)
                    return true; // Sapient mech that somehow still has the need - let the original handle it normally.

                IntVec3 pos = pawn.Position;
                Map map = pawn.Map;
                if (pos.IsValid && pos.InBounds(map))
                {
                    ThingDef shieldDef = DefDatabase<ThingDef>.GetNamedSilentFail("GD_AbilityShieldProjector");
                    if (shieldDef != null)
                    {
                        Thing shield = ThingMaker.MakeThing(shieldDef);
                        if (pawn.Faction != null)
                            shield.SetFaction(pawn.Faction);
                        GenPlace.TryPlaceThing(shield, pos, map, ThingPlaceMode.Near);
                        FleckMaker.Static(shield.TrueCenter(), shield.Map, FleckDefOf.BroadshieldActivation, 1f);
                        SoundDefOf.Broadshield_Startup.PlayOneShot(new TargetInfo(shield.Position, shield.Map));
                    }
                }
                return false; // Skip the original - we handled placement ourselves.
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] GD3 PlaceShield apply patch failed: " + e, 91274505);
                return true;
            }
        }
    }
}
