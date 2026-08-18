using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// The same double-gate GenConstruct_CanConstruct_Patch exists to work around, in the other
    /// place vanilla writes it - WorkGiver_GrowerSow.JobOnCell (decompiled):
    ///
    ///     if (sowMinSkill > 0 &amp;&amp; ((pawn.skills != null &amp;&amp; realPlantsLevel &lt; sowMinSkill)
    ///                            || (pawn.IsColonyMech &amp;&amp; mechFixedSkillLevel &lt; sowMinSkill)))
    ///
    /// Written as an or rather than as two ifs, but with identical effect: a pawn that is both
    /// skilled and a colony mech is judged by whichever of the two is worse. A Big and Small
    /// sapient mechanoid is both, so its trained Plants skill is overridden by the fixed skill
    /// level of the race it was built from, and it refuses to sow anything with a sowMinSkill
    /// above that - devilstrand and healroot being the ones a player actually notices.
    ///
    /// Fixed the same way and for the same reason: suppress for the duration of the call so
    /// vanilla's own mech clause evaluates false and drops out of the or, leaving the real-skill
    /// clause to decide. See SapientMechWorkTypeGuard.
    ///
    /// Bill.cs and QualityUtility.cs read the same pair of values but pick between them with a
    /// ternary that prefers a non-null skills tracker, so they already do the right thing for a
    /// sapient mechanoid and are deliberately left unpatched.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell))]
    public static class WorkGiver_GrowerSow_JobOnCell_Patch
    {
        public static void Prefix(Pawn pawn, out bool __state)
        {
            __state = false;
            try
            {
                __state = SapientMechWorkTypeGuard.TrySuppress(pawn);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] GrowerSow JobOnCell prefix failed: " + e, 91274569);
            }
        }

        // Exception parameter present so Harmony runs this even if the original throws - see
        // GenConstruct_CanConstruct_Patch for why leaving a pawn suppressed would be bad.
        public static void Postfix(Pawn pawn, bool __state, Exception __exception)
        {
            if (__state)
                SapientMechWorkTypeGuard.Unsuppress(pawn);
        }
    }
}
