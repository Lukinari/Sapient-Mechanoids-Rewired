using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// A sapient Ascension Megacorp mech keeps its real USAC.Need_Readiness need (restored
    /// via this mod's compWhitelist, same as every other mechanoid-specific comp) - a real,
    /// non-sapient one is AI-managed and auto-resupplies itself, so the need is invisible
    /// day-to-day, but a sapient one is a full colonist and the need becomes a manual
    /// component-hauling chore like any other.
    ///
    /// An earlier version of this setting stripped USAC.CompProperties_MechReadiness off
    /// the generated ThingDef at def-generation time - correct for a new pawn, but useless
    /// for a pawn (and Need_Readiness instance) that already existed in a save from before
    /// the setting was toggled, since removing a comp from a ThingDef doesn't retroactively
    /// remove an already-saved Need instance from a pawn that already has one. Confirmed
    /// broken this way on an existing save.
    ///
    /// This version instead Prefixes Need_Readiness.NeedInterval() - the method that
    /// subtracts from CurLevel every need-tick - and skips it entirely for a sapient
    /// mechanical pawn when the setting is on. The need's bar and its current value are
    /// left exactly where they are: it never drains, never demands a resupply chore, but
    /// it also isn't removed from the pawn's need list. This works at the per-pawn instance
    /// level rather than the def-generation level, so it applies immediately, live, even on
    /// an existing save with pawns that predate the setting - not just the next time defs
    /// are generated.
    ///
    /// A real, non-sapient Ascension Megacorp mech is left alone regardless of this setting
    /// - only ever applies to a sapient mechanical pawn (IsMechanicalCache.Get, not
    /// RaceProps.IsMechanoid, which sapience always clears).
    ///
    /// Gated behind SapientMechanoidFixSettings.freezeAscensionMegacorpReadiness (off by
    /// default - a sapient Ascension Megacorp mech's Readiness need behaves normally,
    /// draining and needing resupply, unless the player opts into freezing it).
    /// </summary>
    [HarmonyPatch]
    public static class Need_Readiness_NeedInterval_AscensionMegacorpFreeze_Patch
    {
        private static readonly Type NeedReadinessType = AccessTools.TypeByName("USAC.Need_Readiness");
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Need), "pawn");

        static MethodBase TargetMethod()
        {
            return NeedReadinessType == null ? null : AccessTools.Method(NeedReadinessType, "NeedInterval");
        }

        public static bool Prefix(Need __instance)
        {
            try
            {
                if (!(SapientMechanoidFixMod.Settings?.freezeAscensionMegacorpReadiness ?? false))
                    return true; // Setting off - let the need decay normally.

                Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return true; // Not a sapient mechanical pawn - leave it alone.

                return false; // Frozen - skip the original entirely, CurLevel stays exactly where it is.
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Ascension Megacorp Readiness freeze patch failed: " + e, 91274533);
                return true;
            }
        }
    }
}
