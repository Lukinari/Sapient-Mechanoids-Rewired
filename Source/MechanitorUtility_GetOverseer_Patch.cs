using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// AV Framework's CompMechReloadableResourceHolder/CompMechCarrierChoice and
    /// Glitterworld Destroyer 5's CompAttackMode all gate their gizmos (steel reserve,
    /// release-urchins, auto-release toggle) directly on
    /// MechanitorUtility.GetOverseer(pawn) == null, bypassing our IsColonyMechPlayerControlled
    /// fix entirely. A global override here would be unsafe (many vanilla callers
    /// dereference the returned pawn's .mechanitor without a null-check), so this only
    /// ever returns a fallback while SapientMechOverseerGizmoGuard has explicitly
    /// suppressed the null result for this exact pawn - a window opened only around the
    /// three gizmo enumerations that need it (see OverseerGizmoSuppressionPatches).
    /// </summary>
    [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.GetOverseer))]
    public static class MechanitorUtility_GetOverseer_Patch
    {
        public static void Postfix(Pawn pawn, ref Pawn __result)
        {
            try
            {
                if (__result != null)
                    return;

                if (!SapientMechOverseerGizmoGuard.IsSuppressed(pawn))
                    return;

                __result = pawn; // Self-overseen sentinel, valid only within the suppression window.
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] GetOverseer suppression patch failed: " + e, 91274437);
            }
        }
    }
}
