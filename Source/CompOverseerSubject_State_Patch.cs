using System;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Mechanoid Upgrades' entire "Mech Upgrader" building workflow (Building_MechUpgrader.
    /// CanAcceptPawn/TickInterval) gates on `pawn.OverseerSubject.State == Overseen` - the
    /// vanilla "this mech is actively supervised by a mechanitor" state. A sapient mech
    /// deliberately has no mechanitor relation (see the rest of this mod - a sapient mech
    /// is a full colonist, not mechanitor-controlled), so without a fix it would either
    /// null-reference crash (CompOverseerSubject itself isn't whitelisted, so
    /// OverseerSubject is null and CanAcceptPawn dereferences .State directly) or, if that
    /// comp IS whitelisted, get immediately ejected from the building the instant it enters
    /// (TickInterval treats State != Overseen as "no longer supervised, kick it out").
    ///
    /// This is also exactly the vanilla comp behind the "feral mechanoid" mechanic
    /// (CompOverseerSubject.CompTick -> CanGoFeral -> IsColonyMechRequiringMechanitor,
    /// which returns true whenever State != Overseen) - so whitelisting the comp on its own,
    /// without this patch, would have made every sapient mech eventually go feral and
    /// leave the colony for lack of an overseer it was never supposed to need. Forcing
    /// State to Overseen for a sapient mech fixes both problems with one change: the
    /// feral check now always sees "already supervised" (never triggers), and the Mech
    /// Upgrader building's own checks pass the same way a real, actively-overseen
    /// mechanoid's would.
    ///
    /// CompOverseerSubject/OverseerSubjectState are vanilla Biotech types, safe to
    /// reference directly - only Mechanoid Upgrades' own use of the comp is the reason
    /// this patch exists, but the fix itself has nothing mod-specific about it.
    /// </summary>
    [HarmonyPatch(typeof(CompOverseerSubject), nameof(CompOverseerSubject.State), MethodType.Getter)]
    public static class CompOverseerSubject_State_Patch
    {
        public static void Postfix(CompOverseerSubject __instance, ref OverseerSubjectState __result)
        {
            try
            {
                Pawn pawn = __instance.parent as Pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid - original overseer-based result is correct.

                __result = OverseerSubjectState.Overseen;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] CompOverseerSubject.State patch failed: " + e, 91274520);
            }
        }
    }
}
