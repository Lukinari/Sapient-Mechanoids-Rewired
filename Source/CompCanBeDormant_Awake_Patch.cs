using System;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// A real mechanoid's CompCanBeDormant wakes itself up once, in PostPostMake, the
    /// first time it's actually constructed (unless its def says it should start
    /// dormant) - that's what sets wokeUpTick away from its int.MinValue default and
    /// makes Awake report true from then on. Big and Small's sapient clone doesn't go
    /// through that same construction path for a comp that was only added back onto it
    /// by this mod's whitelist (see HumanlikeAnimalSettings_MechQueens.xml -
    /// RimWorld.CompProperties_CanBeDormant/CompProperties_WakeUpDormant, added for
    /// Alpha Mechs' Guttersnipe), so wokeUpTick is left at its untouched default and
    /// Awake reports false - which is exactly the "still asleep, not yet activated"
    /// state a genuinely dormant ambush mechanoid is in, and CompCanBeDormant.
    /// TickRareWorker throws the vanilla sleeping "Z" fleck for precisely that state.
    ///
    /// Dormancy is a "hasn't been activated yet" mechanic for hostile/ambush
    /// mechanoids in the first place - fundamentally not something an active, working
    /// sapient colonist should ever be in, whether or not its construction happened to
    /// leave it in that state. Forcing Awake to true for a sapient mech sidesteps the
    /// construction-order gap entirely and keeps every other CompCanBeDormant-driven
    /// behavior (the "may go feral" inspect string, ToSleep/WakeUp calls from
    /// elsewhere) consistent with "this mech is awake," rather than special-casing the
    /// Z fleck alone.
    /// </summary>
    [HarmonyPatch(typeof(CompCanBeDormant), nameof(CompCanBeDormant.Awake), MethodType.Getter)]
    public static class CompCanBeDormant_Awake_Patch
    {
        public static void Postfix(CompCanBeDormant __instance, ref bool __result)
        {
            try
            {
                if (__result)
                    return;

                Pawn pawn = __instance.parent as Pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid - its own dormancy state is correct, leave it alone.

                __result = true;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] CompCanBeDormant.Awake patch failed: " + e, 91274523);
            }
        }
    }
}
