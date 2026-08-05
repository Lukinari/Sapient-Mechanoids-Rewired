using System;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Big and Small's sapience conversion (RaceSwapUtility.SwapAnimalToSapientVersion /
    /// SwapThingDef, see BigAndSmall.dll) deliberately clears a sapient mechanoid's
    /// FleshType away from Mechanoid as part of turning it into a proper humanlike
    /// colonist - which means RaceProps.IsMechanoid, and therefore vanilla's own
    /// Pawn.IsColonyMech, both go false. Any mod that gates mechanoid-only gizmos or
    /// behavior behind IsColonyMech (as [AV] Framework's mech-queen comps do, for the
    /// steel reserve and "release worker urchins" commands) loses that behavior
    /// entirely on a sapient pawn, even once its comps are otherwise intact.
    ///
    /// Big and Small ships its own answer to exactly this: RaceHelper.IsMechanical(pawn)
    /// additionally checks for a PawnExtension flagged isMechanical, which a sapient
    /// mechanoid keeps carrying despite the FleshType change. This postfixes
    /// IsColonyMech to also return true under that same condition, re-deriving the rest
    /// of vanilla's own logic (player faction, no mental break, HostFaction/IsSlave)
    /// unchanged - so this only ever flips a false to a true for pawns Big and Small
    /// itself still considers mechanical, and never touches ordinary sapient animals or
    /// genuinely non-mechanical pawns.
    ///
    /// One exception: while SapientMechWorkTypeGuard has a pawn marked suppressed (see
    /// Pawn_GetDisabledWorkTypes_Patch), this reports the real, unforced value instead -
    /// vanilla's own GetDisabledWorkTypes treats IsColonyMech==true as "restrict this
    /// pawn to its narrow mech work-type whitelist," which is correct for a real
    /// mechanoid but not for a sapient one meant to work like any other colonist.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.IsColonyMech), MethodType.Getter)]
    public static class Pawn_IsColonyMech_Patch
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            // IsColonyMech is read from all over vanilla (job assignment, needs, mech
            // control checks) for every pawn, mechanical or not - a single uncaught
            // exception here would propagate into whichever of those happened to be
            // asking, and since it's checked this often, that could repeat constantly
            // instead of failing once. Never let anything escape; on failure just leave
            // vanilla's own result alone.
            try
            {
                if (__result || __instance == null)
                    return;

                if (SapientMechWorkTypeGuard.IsSuppressed(__instance))
                    return;

                if (!ModsConfig.BiotechActive)
                    return; // Same base requirement vanilla's own getter has.

                if (__instance.Faction != Faction.OfPlayer || __instance.MentalStateDef != null)
                    return;

                if (!IsMechanicalCache.Get(__instance))
                    return; // Not a mechanoid by Big and Small's own reckoning either - leave it false.

                __result = __instance.HostFaction == null || __instance.IsSlave;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] IsColonyMech patch failed, leaving the original result alone: " + e, 87604411);
            }
        }
    }
}
