using System;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Discovered via DMS's "craftable upgrades" (Ceramic Plates, Reinforced Frame,
    /// Synthetic Tendon, etc. - Fortified.CompTargetable_AddHediffOnTarget items that ask
    /// the player to click a target pawn to install onto): vanilla's own
    /// TargetingParameters.CanTarget splits every pawn target into exactly two buckets,
    /// "NonHumanlikeOrWildMan" (animals/mechs, gated by canTargetAnimals/canTargetMechs)
    /// or everything else (gated by canTargetHumans) - see the "if
    /// (!pawn.NonHumanlikeOrWildMan() && !canTargetHumans) return false;" check.
    ///
    /// Big and Small's sapient conversion sets RaceProps.Intelligence to Humanlike, so a
    /// sapient mech is no longer NonHumanlikeOrWildMan and falls into the canTargetHumans
    /// bucket instead of canTargetMechs - which breaks any targeting parameters (like
    /// this DMS item's canTargetMechs=true, canTargetHumans=false) that were written to
    /// accept mechs specifically and exclude ordinary humanlikes. The sapient mech ends
    /// up in neither accepted bucket and becomes untargetable, even though it's still
    /// mechanical by Big and Small's own reckoning.
    ///
    /// This isn't DMS-specific - it's vanilla's own targeting split, so the fix is
    /// generic like the rest of this mod's core patches: temporarily let canTargetHumans
    /// through for the single CanTarget call when the target is a sapient mech and the
    /// caller already opted into canTargetMechs, then restore it immediately after so
    /// nothing else about that TargetingParameters instance is disturbed. Only ever flips
    /// a false to a true, and only for pawns Big and Small still considers mechanical.
    ///
    /// Also covers corpses: CanTarget runs the exact same canTargetMechs/canTargetHumans
    /// split against corpse.InnerPawn.RaceProps when canTargetCorpses is set (confirmed
    /// via the vanilla Apocriton's "Resurrect Mechs" ability - CompAbilityEffect_ResurrectMech
    /// wants mech corpses specifically, and a dead sapient mech's corpse was rejected the
    /// same way a live one was before this patch existed). Targeting a mech corpse is,
    /// in practice, always about resurrecting it, so the corpse half of this fix (only)
    /// respects the "Allow resurrecting sapient mechs" mod setting - see
    /// CompAbilityEffect_ResurrectMech_CanResurrect_Patch.cs and
    /// AlphaMechs_CompResurrectMechMinor_CanResurrect_Patch.cs for the other half
    /// (actually letting the resurrection succeed, not just letting the corpse be
    /// clicked). The live-pawn half of this fix is unrelated to resurrection and always
    /// stays on.
    /// </summary>
    [HarmonyPatch(typeof(TargetingParameters), nameof(TargetingParameters.CanTarget), typeof(TargetInfo), typeof(ITargetingSource))]
    public static class TargetingParameters_CanTarget_Patch
    {
        private static bool ShouldTemporarilyAllowHumans(TargetingParameters __instance, TargetInfo targ)
        {
            if (__instance == null || !__instance.canTargetMechs || __instance.canTargetHumans)
                return false;

            Pawn pawn = targ.Thing as Pawn;
            if (pawn == null && __instance.canTargetCorpses && targ.Thing is Corpse corpse)
            {
                if (!SapientMechanoidFixMod.Settings.allowMechResurrection)
                    return false;
                pawn = corpse.InnerPawn;
            }

            if (pawn == null)
                return false;

            if (pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                return false; // Real mechanoid (already handled by canTargetMechs), or not mechanical at all.

            return true;
        }

        public static void Prefix(TargetingParameters __instance, TargetInfo targ, out bool __state)
        {
            __state = false;
            try
            {
                if (ShouldTemporarilyAllowHumans(__instance, targ))
                {
                    __instance.canTargetHumans = true;
                    __state = true;
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] TargetingParameters.CanTarget prefix failed: " + e, 91274480);
            }
        }

        // The unused Exception __exception parameter tells Harmony to run this postfix
        // even if CanTarget's own body (or another mod's patch on the same method)
        // throws - without it, a throw would skip this cleanup entirely and leave
        // canTargetHumans=true stuck permanently on __instance, which - unlike a local
        // variable - is very often a long-lived, shared TargetingParameters instance (a
        // static field on a Verb/WorkGiver/CompAbilityEffect, reused for every future
        // call). Same pattern as Pawn_GetDisabledWorkTypes_Patch's postfix, which has
        // the identical shared-state-restore shape.
        public static void Postfix(TargetingParameters __instance, bool __state, Exception __exception)
        {
            if (__state && __instance != null)
                __instance.canTargetHumans = false;
        }
    }
}
