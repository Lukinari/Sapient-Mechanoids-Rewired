using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Vanilla's GenConstruct.CanConstruct runs its two skill checks as separate ifs, not as
    /// alternatives (decompiled, GenConstruct.cs):
    ///
    ///     if (p.skills != null) { ...fail if the real Construction level is under the prereq... }
    ///     if (p.IsColonyMech)   { ...fail if RaceProps.mechFixedSkillLevel is under it... }
    ///
    /// The assumption behind that is that a pawn is either a skilled humanlike or a fixed-skill
    /// mechanoid, never both. A Big and Small sapient mechanoid is both: it has a real skills
    /// tracker from the sapience conversion, and Pawn_IsColonyMech_Patch deliberately keeps
    /// IsColonyMech reporting true so that mech-gated comps (the [AV] Framework mech-queen
    /// gizmos, among others) keep working on it.
    ///
    /// So a sapient mechanoid clears the first check on its actual trained skill and is then
    /// failed by the second on RaceProps.mechFixedSkillLevel - which defaults to 10
    /// (RaceProperties.cs) and is a property of the race it was built from, not of anything it
    /// has since learned. Reported from a save as a War Queen with Construction 18 being refused
    /// a building whose constructionSkillPrerequisite is 12.
    ///
    /// Rather than reimplement vanilla's skill logic, this marks the pawn suppressed for the
    /// duration of the call (see SapientMechWorkTypeGuard) so the mech branch's own condition
    /// evaluates false and skips itself, leaving the real-skill check as the only one that runs.
    ///
    /// Only the (Thing, Pawn, bool, bool, JobDef) overload is patched because the WorkTypeDef
    /// overload delegates straight to it after its own work-assignment check - patching both
    /// would suppress twice for one logical call.
    /// </summary>
    [HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanConstruct),
        new[] { typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef) })]
    public static class GenConstruct_CanConstruct_Patch
    {
        public static void Prefix(Pawn p, out bool __state)
        {
            __state = false;
            try
            {
                __state = SapientMechWorkTypeGuard.TrySuppress(p);
            }
            catch (Exception e)
            {
                // CanConstruct is asked constantly by work scanning, for every pawn and every
                // blueprint - letting anything escape here would repeat rather than fail once.
                Log.ErrorOnce("[SapientMechanoidFix] CanConstruct prefix failed: " + e, 91274568);
            }
        }

        // The Exception parameter is unused, but its presence makes Harmony run this even when
        // the original (or another mod's patch on it) throws. Without it a throw would skip the
        // cleanup and leave the pawn permanently suppressed, silently disabling every other fix
        // in this mod for that pawn for the rest of the session.
        public static void Postfix(Pawn p, bool __state, Exception __exception)
        {
            if (__state)
                SapientMechWorkTypeGuard.Unsuppress(p);
        }
    }
}
