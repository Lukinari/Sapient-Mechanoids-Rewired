using System;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// See SapientMechWorkTypeGuard for the full explanation. This wraps the original
    /// call: Prefix marks the pawn suppressed (only for a Big and Small sapient
    /// mechanoid - a real mechanoid's own whitelist restriction is left completely
    /// alone), Postfix un-marks it once the original method - and therefore every
    /// IsColonyMech read within it - has finished running.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetDisabledWorkTypes))]
    public static class Pawn_GetDisabledWorkTypes_Patch
    {
        public static void Prefix(Pawn __instance, out bool __state)
        {
            __state = false;
            try
            {
                if (__instance == null || __instance.RaceProps.IsMechanoid || !__instance.IsMechanical())
                    return;

                if (!__instance.IsColonyMech)
                    return;

                SapientMechWorkTypeGuard.Suppress(__instance);
                __state = true;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] GetDisabledWorkTypes prefix failed: " + e, 91274435);
            }
        }

        // The Exception parameter isn't used directly, but its mere presence tells
        // Harmony to run this postfix even if the original method (or another mod's
        // patch on it) throws - without it, a throw would skip this cleanup entirely
        // and leave the pawn permanently suppressed, silently undoing every other fix
        // in this mod for just that one pawn for the rest of the session.
        public static void Postfix(Pawn __instance, bool __state, Exception __exception)
        {
            if (__state)
                SapientMechWorkTypeGuard.Unsuppress(__instance);
        }
    }
}
