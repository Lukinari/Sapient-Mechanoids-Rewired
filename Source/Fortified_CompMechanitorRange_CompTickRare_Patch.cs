using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Fortified Feature Framework's CompMechanitorRange applies a worsening hediff
    /// based on distance from the pawn's mechanitor while drafted. Its own SameMap
    /// check does ((Thing)MechanitorUtility.GetOverseer(Pawn)).Map with zero
    /// null-check - a sapient mech never has an Overseer relation by design, so this
    /// would NRE every rare tick (~250 ticks) the moment it's drafted.
    ///
    /// Unlike the gizmo-visibility bugs elsewhere in this mod, this mechanic itself
    /// ("how far from your mechanitor are you") is meaningless for a pawn that
    /// deliberately has no mechanitor - so the fix is to skip the whole method for a
    /// sapient mech rather than try to simulate a fake overseer distance.
    ///
    /// Currently unwired: no Dead Man's Switch ThingDef uses this comp, so it never
    /// actually triggers today - fixed preemptively in case another Fortified-consuming
    /// mod does. Fortified is an optional dependency - resolved by name at runtime,
    /// never referenced directly in this Prefix's own signature (only vanilla
    /// ThingComp), so this class is entirely inert if that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class Fortified_CompMechanitorRange_CompTickRare_Patch
    {
        static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("Fortified.CompMechanitorRange");
            return type == null ? null : AccessTools.Method(type, "CompTickRare");
        }

        public static bool Prefix(ThingComp __instance)
        {
            try
            {
                Pawn pawn = __instance.parent as Pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return true; // Real mechanoid, or not mechanical - original behavior applies unmodified.

                return false; // Sapient mechanoid - no mechanitor to be in range of, skip entirely.
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Fortified CompMechanitorRange tick patch failed, falling back to original: " + e, 91274452);
                return true;
            }
        }
    }
}
