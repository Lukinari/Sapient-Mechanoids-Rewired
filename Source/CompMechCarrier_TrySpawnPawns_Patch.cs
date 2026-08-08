using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// RimWorld.CompMechCarrier.TrySpawnPawns() is vanilla, shared by both the War Queen
    /// and Alpha Mechs' War Empress (confirmed via decompile/source - Empress's ThingDef
    /// uses the exact same RimWorld.CompProperties_MechCarrier, not a custom comp of its
    /// own), so patching it once covers both. [AV] Framework's Work Queen uses its own,
    /// separate AV_Framework.CompMechCarrierChoice.TrySpawnPawns(PawnSpawnerdef) instead -
    /// deliberately not covered here, since it can summon several different urchin kinds
    /// from one mech, and "one chosen skin for all future summons" doesn't map cleanly
    /// onto that.
    ///
    /// spawnedPawns is a private field on CompMechCarrier with no public accessor, so this
    /// captures its count before the call (Prefix) and applies the chosen skin (if any) to
    /// whatever got added past that point (Postfix) - the same shape as reading any other
    /// vanilla private field this mod needs, via AccessTools.FieldRefAccess.
    /// </summary>
    [HarmonyPatch(typeof(CompMechCarrier), nameof(CompMechCarrier.TrySpawnPawns))]
    public static class CompMechCarrier_TrySpawnPawns_Patch
    {
        private static readonly AccessTools.FieldRef<CompMechCarrier, List<Pawn>> SpawnedPawnsRef =
            AccessTools.FieldRefAccess<CompMechCarrier, List<Pawn>>("spawnedPawns");

        public static void Prefix(CompMechCarrier __instance, out int __state)
        {
            __state = 0;
            try
            {
                __state = SpawnedPawnsRef(__instance)?.Count ?? 0;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Reading CompMechCarrier.spawnedPawns failed: " + e, 91274555);
            }
        }

        /// <summary>
        /// A Finalizer, not a Postfix - confirmed from a real crash log that vanilla's own
        /// TrySpawnPawns() can throw a NullReferenceException partway through a multi-pawn
        /// spawn batch. It snapshots innerContainer's contents into tmpResources once before
        /// the spawn loop; if the steel needed is split across multiple stacks, spawning an
        /// earlier pawn in the same batch can fully consume a stack that snapshot still
        /// references, so a later pawn's ingredient-consumption loop calls
        /// innerContainer.Take() on an already-gone Thing and gets null back, then
        /// dereferences it. Pure vanilla bug, nothing to do with sapience - happens for any
        /// War Queen spawning more than one urchin per click with split steel stacks.
        ///
        /// A plain Postfix never runs when the original method throws, which silently
        /// skipped skin application even for pawns that DID successfully spawn before the
        /// crash point (they're already in spawnedPawns by then - see vanilla's own
        /// spawnedPawns.Add(pawn), which happens before the crash-prone ingredient loop for
        /// that same iteration). A Finalizer runs regardless of whether the original threw,
        /// so those already-spawned pawns still get their chosen skin. Returning __exception
        /// unchanged means this doesn't swallow or alter the crash itself in any way - it's
        /// purely a "still do our part" fix, not an attempt to fix vanilla's own bug.
        /// </summary>
        public static Exception Finalizer(CompMechCarrier __instance, int __state, Exception __exception)
        {
            try
            {
                if (!SummonedMechSkinChoiceSupport.IsAvailable)
                    return __exception;

                if (SapientMechanoidFixMod.Settings?.enableSummonedMechSkinChoice != true)
                    return __exception;

                CompSummonedMechSkinChoice skinChoice = __instance.parent?.GetComp<CompSummonedMechSkinChoice>();
                if (skinChoice?.chosenSkin == null)
                    return __exception;

                List<Pawn> spawnedPawns = SpawnedPawnsRef(__instance);
                if (spawnedPawns == null)
                    return __exception;

                for (int i = __state; i < spawnedPawns.Count; i++)
                    SummonedMechSkinChoiceSupport.ApplySkin(spawnedPawns[i], skinChoice.chosenSkin);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Applying chosen urchin skin failed: " + e, 91274552);
            }

            return __exception;
        }
    }
}
