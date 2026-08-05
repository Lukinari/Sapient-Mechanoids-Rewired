using System;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Optional, off-by-default setting (SapientMechanoidFixSettings.painImmunity).
    ///
    /// HediffSet.CalculatePain() zeroes pain outright for anything that isn't
    /// RaceProps.IsFlesh - which is exactly why a real mechanoid never feels pain in the
    /// first place. Big and Small's sapience conversion clears FleshType away from
    /// Mechanoid (needed for the pawn to eat, bleed, get organic-style hediffs, etc.), so
    /// IsFlesh flips true and the pawn starts computing pain like any human colonist -
    /// pain shock downing, capacity penalties from wounds, all of it. Nothing about
    /// becoming sapient actually gives the mech organic pain nerves, so this is offered
    /// as a pure user choice rather than assumed: when enabled, zero out PainTotal for
    /// sapient mechs only, same as vanilla already does unconditionally for real ones.
    ///
    /// PainTotal is cached internally (HediffSet.cachedPain), but nothing outside this
    /// class ever reads that cache directly - only through this property - so
    /// overriding just the returned value here is safe and doesn't disturb the cache
    /// itself.
    /// </summary>
    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.PainTotal), MethodType.Getter)]
    public static class HediffSet_PainTotal_Patch
    {
        public static void Postfix(HediffSet __instance, ref float __result)
        {
            try
            {
                if (__result <= 0f || SapientMechanoidFixMod.Settings?.painImmunity != true)
                    return;

                Pawn pawn = __instance?.pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid (already pain-immune via IsFlesh==false), or not mechanical at all.

                __result = 0f;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] PainTotal patch failed: " + e, 91274460);
            }
        }
    }
}
