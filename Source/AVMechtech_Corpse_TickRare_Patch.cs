using System;
using System.Linq;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Dead pawns don't get their hediffs ticked through the normal pawn-tick path, so [AV]
    /// Mechtech's own Corpse_TickRare_Patch manually pumps Hediff_MechDeathRefusal.TickRare
    /// on every corpse tick, so its resurrection timer still counts down while the mech is
    /// dead - see AVMechtech_Hediff_MechDeathRefusal_PostAdd_Patch.cs for what that hediff
    /// is. That patch gates itself on `corpse.InnerPawn.RaceProps.IsMechanoid`, which is
    /// false for a sapient mech's corpse, so once a sapient mech actually has the hediff
    /// (now that PostAdd no longer strips it off), its resurrect timer would never advance
    /// while dead without this.
    ///
    /// This is a separate, independent patch on the same vanilla Corpse.TickRare rather
    /// than a modification of AV Mechtech's own patch, since Harmony patches from different
    /// mods can't be edited in place - it just covers the gap for the sapient-mech case
    /// that the original patch's IsMechanoid check skips. Hediff_MechDeathRefusal.TickRare
    /// is a plain instance method (not a virtual override), so invoking it reflectively
    /// here carries none of the "calls back into an override" risk a Harmony
    /// Prefix-skip-and-reimplement would have on a virtual method.
    ///
    /// [AV] Mechtech is an optional dependency - Hediff_MechDeathRefusal is resolved by
    /// name at runtime and only ever invoked through cached MethodInfo, never referenced
    /// directly in this patch's own signature (only vanilla Corpse/Pawn types), so this
    /// class is entirely inert if that mod isn't installed.
    /// </summary>
    [HarmonyPatch(typeof(Corpse), nameof(Corpse.TickRare))]
    public static class AVMechtech_Corpse_TickRare_Patch
    {
        private static readonly Type HediffType = AccessTools.TypeByName("AV_Mechtech.Hediff_MechDeathRefusal");
        private static readonly MethodInfo TickRareMethod = HediffType == null ? null : AccessTools.Method(HediffType, "TickRare");

        public static void Postfix(Corpse __instance)
        {
            try
            {
                if (HediffType == null || TickRareMethod == null)
                    return;

                Pawn innerPawn = __instance.InnerPawn;
                if (innerPawn == null || innerPawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(innerPawn))
                    return; // Real mechanoid already handled by AV Mechtech's own patch, or not mechanical at all.

                Hediff hediff = innerPawn.health?.hediffSet?.hediffs?.FirstOrDefault(h => HediffType.IsInstanceOfType(h));
                if (hediff == null)
                    return;

                TickRareMethod.Invoke(hediff, null);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] AV Mechtech Corpse.TickRare patch failed: " + e, 91274513);
            }
        }
    }
}
