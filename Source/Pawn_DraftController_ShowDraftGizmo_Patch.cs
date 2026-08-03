using System;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Pawn_DraftController.ShowDraftGizmo hides the Draft gizmo entirely (not just
    /// disables it) whenever pawn.IsColonyMech is true and pawn.GetMechControlGroup()
    /// is null - i.e. a mech recognized as mechanoid-tier but with no mechanitor
    /// control group assigned. This check doesn't go through
    /// IsColonyMechPlayerControlled at all, so patching that (see
    /// Pawn_IsColonyMechPlayerControlled_Patch) doesn't reach it - a sapient
    /// mechanoid, which by design never has a control group, was losing the Draft
    /// gizmo entirely the moment our own IsColonyMech patch started recognizing it as
    /// a mech in the first place.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.ShowDraftGizmo), MethodType.Getter)]
    public static class Pawn_DraftController_ShowDraftGizmo_Patch
    {
        public static void Postfix(Pawn_DraftController __instance, ref bool __result)
        {
            try
            {
                if (__result || __instance?.pawn == null)
                    return;

                Pawn pawn = __instance.pawn;
                if (pawn.RaceProps.IsMechanoid || !pawn.IsMechanical())
                    return; // Genuine mechanoid (leave vanilla's control-group requirement alone) or not mechanical at all.

                if (!pawn.IsColonyMech)
                    return;

                // The only other thing ShowDraftGizmo checks is IsColonySubhuman, which
                // doesn't apply to a mechanoid pawn - safe to just grant this outright
                // rather than re-deriving the rest of the getter.
                __result = true;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] ShowDraftGizmo patch failed: " + e, 91274433);
            }
        }
    }
}
