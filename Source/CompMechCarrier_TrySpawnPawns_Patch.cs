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

        public static void Postfix(CompMechCarrier __instance, int __state)
        {
            try
            {
                if (!SummonedMechSkinChoiceSupport.IsAvailable)
                    return;

                if (SapientMechanoidFixMod.Settings?.enableSummonedMechSkinChoice != true)
                    return;

                CompSummonedMechSkinChoice skinChoice = __instance.parent?.GetComp<CompSummonedMechSkinChoice>();
                if (skinChoice?.chosenSkin == null)
                    return;

                List<Pawn> spawnedPawns = SpawnedPawnsRef(__instance);
                if (spawnedPawns == null)
                    return;

                for (int i = __state; i < spawnedPawns.Count; i++)
                    SummonedMechSkinChoiceSupport.ApplySkin(spawnedPawns[i], skinChoice.chosenSkin);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Applying chosen urchin skin failed: " + e, 91274552);
            }
        }
    }
}
