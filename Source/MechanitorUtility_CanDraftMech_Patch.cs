using System;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Fixing ShowDraftGizmo (see that patch) makes the Draft gizmo visible again, but
    /// MechanitorUtility.CanDraftMech is what decides whether it's actually enabled -
    /// and it requires mech.GetOverseer() != null, falling through to a flat "false"
    /// otherwise (see its own source: the whole overseer/bandwidth check is nested
    /// inside "if (overseer != null)", with no fallback branch for mechs that were
    /// never meant to have one). A sapient mechanoid never has an Overseer relation, so
    /// this always disabled the toggle even once visible.
    ///
    /// Prefix rather than postfix, since CanDraftMech returns an AcceptanceReport
    /// (implicitly convertible from bool/string, not just a plain bool) and there's
    /// nothing meaningful to compute from vanilla's own "false" to turn into an
    /// acceptance report - for a sapient mechanoid this fully replaces the method
    /// instead of adjusting its result.
    /// </summary>
    [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.CanDraftMech))]
    public static class MechanitorUtility_CanDraftMech_Patch
    {
        public static bool Prefix(Pawn mech, ref AcceptanceReport __result)
        {
            try
            {
                if (mech == null || mech.RaceProps.IsMechanoid || !mech.IsMechanical())
                    return true; // Genuine mechanoid, or not mechanical at all - vanilla's own overseer/bandwidth logic applies unmodified.

                if (!mech.IsColonyMech)
                    return true;

                // Mirrors vanilla's own low-energy self-shutdown check for completeness,
                // though a sapient mech's needs.energy is null by design, so this is
                // realistically always skipped.
                if (mech.needs.energy != null && mech.needs.energy.IsLowEnergySelfShutdown)
                {
                    __result = (AcceptanceReport)"IsLowEnergySelfShutdown".Translate(mech.Named("PAWN"));
                    return false;
                }

                __result = true; // Sapient mechanoid - independent of any overseer/bandwidth requirement by design.
                return false;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] CanDraftMech patch failed, falling back to vanilla: " + e, 91274434);
                return true;
            }
        }
    }
}
