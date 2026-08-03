using System;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SapientMechanoidFix
{
    /// <summary>
    /// MechanitorUtility.InMechanitorCommandRange requires mech.GetOverseer() != null,
    /// falling straight to "false" with no fallback branch otherwise - same shape of
    /// gap as CanDraftMech. A sapient mechanoid never has an Overseer relation, so this
    /// was always false for them, and it gates drafted move orders
    /// (FloatMenuOptionProvider_DraftedMove, MultiPawnGotoController) and attack orders
    /// (FloatMenuOptionProvider_DraftedAttack, FloatMenuUtility.GetRangedAttackAction /
    /// GetMeleeAttackAction) behind pawn.IsColonyMech or IsColonyMechPlayerControlled -
    /// both of which our own patches correctly grant sapient mechanoids now, which is
    /// exactly what exposed this: a drafted sapient mech could no longer be ordered to
    /// move or attack at all, always failing with "Out of command range."
    /// </summary>
    [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.InMechanitorCommandRange))]
    public static class MechanitorUtility_InMechanitorCommandRange_Patch
    {
        public static bool Prefix(Pawn mech, ref bool __result)
        {
            try
            {
                if (mech == null || mech.RaceProps.IsMechanoid || !mech.IsMechanical())
                    return true; // Genuine mechanoid, or not mechanical at all - vanilla's own overseer-range check applies unmodified.

                if (!mech.IsColonyMech)
                    return true;

                __result = true; // Sapient mechanoid - independent of any command-range/overseer requirement by design.
                return false;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] InMechanitorCommandRange patch failed, falling back to vanilla: " + e, 91274436);
                return true;
            }
        }
    }
}
