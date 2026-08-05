using System;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Pawn.IsColonyMech being true (see Pawn_IsColonyMech_Patch) turned out not to be
    /// enough on its own - a lot of vanilla mech-specific behavior (CompMechCarrier's
    /// release-urchins gizmo, CompTurretGun's fire-at-will, combat AI targeting,
    /// tending/feeding eligibility, etc.) is actually gated on the separate
    /// IsColonyMechPlayerControlled instead, which vanilla defines as requiring
    /// OverseerSubject != null AND that subject's State == Overseen - i.e. a real,
    /// active mechanitor-bandwidth link. Sapient mechanoids deliberately have neither
    /// (per user confirmation: independent of any overseer/bandwidth requirement), so
    /// this was always false for them even with IsColonyMech fixed.
    ///
    /// Same scoping as the IsColonyMech patch: only intervenes for a pawn that is NOT a
    /// genuine vanilla mechanoid (RaceProps.IsMechanoid still true) but which Big and
    /// Small's own IsMechanical() still recognizes as mechanical, and only once our own
    /// IsColonyMech patch has already granted it that status. A real mechanoid that's
    /// legitimately out of bandwidth or unlinked from its overseer is left exactly as
    /// vanilla says - this never overrides an actual bandwidth/overseer check.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.IsColonyMechPlayerControlled), MethodType.Getter)]
    public static class Pawn_IsColonyMechPlayerControlled_Patch
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            try
            {
                if (__result || __instance == null || !__instance.Spawned)
                    return;

                if (__instance.RaceProps.IsMechanoid || !IsMechanicalCache.Get(__instance))
                    return; // Genuine mechanoid (leave vanilla's own overseer/bandwidth answer alone) or not mechanical at all.

                if (!__instance.IsColonyMech)
                    return; // Reflects our own IsColonyMech patch - if that's somehow still false, there's nothing consistent to do here either.

                __result = true;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] IsColonyMechPlayerControlled patch failed: " + e, 91274432);
            }
        }
    }
}
