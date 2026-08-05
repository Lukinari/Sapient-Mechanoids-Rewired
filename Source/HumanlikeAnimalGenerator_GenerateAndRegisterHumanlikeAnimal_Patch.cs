using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Found while checking whether Mechanoid Upgrades' size-restricted upgrades actually
    /// respect a sapient mech's real size. They don't, and the cause is upstream of that
    /// mod entirely - it's in how Big and Small itself builds the sapient ThingDef.
    ///
    /// HumanlikeAnimalGenerator.GenerateAndRegisterHumanlikeAnimal (confirmed by decompiled
    /// source) builds the sapient ThingDef's race in two steps: first a wholesale
    /// reflection copy of every field from vanilla Human's ThingDef (including the race
    /// object reference itself), then a fresh `RaceProperties` object with a specific,
    /// explicit list of fields copied over from the original animal - lifeExpectancy,
    /// intelligence, foodType, lifeStageAges, trainability, fleshType, and about a dozen
    /// others. baseBodySize, baseHealthScale, and mechWeightClass are not on that list, so
    /// they're silently left at whatever the fresh RaceProperties/Human defaults are -
    /// baseBodySize and baseHealthScale both default to 1 (Human-sized), and
    /// mechWeightClass defaults to null (Human isn't a mechanoid and has none) - regardless
    /// of whether the original mechanoid was tiny or enormous, light or ultra-heavy.
    ///
    /// This mostly goes unnoticed because Big and Small separately patches Pawn.BodySize/
    /// HealthScale (the instance properties almost everything actually reads) with its own
    /// per-pawn cached multiplier, compensating at the pawn level without ever touching the
    /// underlying ThingDef field. Mechanoid Upgrades' size/weight-class restrictions
    /// (MechUpgradeDef.CanAdd) are the opposite case - they take a bare ThingDef and read
    /// race.baseBodySize/mechWeightClass directly, bypassing that per-pawn compensation
    /// entirely. A size- or weight-class-restricted upgrade would judge every sapient mech
    /// as exactly Human-sized and weight-classless, accepting or rejecting it for the wrong
    /// reason regardless of the mech's real size.
    ///
    /// Restoring these three fields from the original mechanoid, once, right after Big and
    /// Small finishes generating its sapient ThingDef, fixes this at the source for any
    /// code that reads them directly - not just Mechanoid Upgrades, and not just the debug
    /// tools in this mod's companion MechTestSpawner. Scoped to mechanoids specifically
    /// (checking the original animal's own RaceProps.IsMechanoid) to stay within this mod's
    /// purpose - Big and Small's non-mechanoid sapient animals are out of scope and
    /// deliberately left alone.
    ///
    /// Gated behind SapientMechanoidFixSettings.fixMechSizeAndWeightClass (on by default).
    /// Since GenerateAndRegisterHumanlikeAnimal runs once per animal kind at def-generation
    /// time rather than per pawn, toggling the setting mid-session doesn't retroactively
    /// redo or undo already-generated ThingDefs - it takes effect from the next def
    /// generation pass (a fresh game or save load), which is exactly why the settings menu
    /// tooltip says so.
    /// </summary>
    [HarmonyPatch]
    public static class HumanlikeAnimalGenerator_GenerateAndRegisterHumanlikeAnimal_Patch
    {
        private static readonly Type GeneratorType = AccessTools.TypeByName("BigAndSmall.HumanlikeAnimalGenerator");

        static MethodBase TargetMethod()
        {
            return GeneratorType == null ? null : AccessTools.Method(GeneratorType, "GenerateAndRegisterHumanlikeAnimal");
        }

        public static void Postfix(PawnKindDef aniPawnKind)
        {
            try
            {
                if (!SapientMechanoidFixMod.Settings.fixMechSizeAndWeightClass)
                    return;

                ThingDef originalRace = aniPawnKind?.race;
                if (originalRace?.race == null || !originalRace.race.IsMechanoid)
                    return; // Not a mechanoid (or malformed pawnkind) - out of this mod's scope, leave Big and Small's own result alone.

                ThingDef generatedDef = DefDatabase<ThingDef>.GetNamedSilentFail("HL_" + originalRace.defName);
                if (generatedDef?.race == null)
                    return; // Generation didn't run the way we expect (renamed/changed) - nothing to fix.

                generatedDef.race.baseBodySize = originalRace.race.baseBodySize;
                generatedDef.race.baseHealthScale = originalRace.race.baseHealthScale;
                generatedDef.race.mechWeightClass = originalRace.race.mechWeightClass;
            }
            catch (Exception e)
            {
                Log.Error("[SapientMechanoidFix] HumanlikeAnimalGenerator body-size/weight-class restore failed: " + e);
            }
        }
    }
}
