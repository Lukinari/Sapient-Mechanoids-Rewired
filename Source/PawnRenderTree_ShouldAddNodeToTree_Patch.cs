using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// The real cause of Lady's missing head, confirmed via debug log: the
    /// comp-constructed Fortified.PawnRenderNode_Head (see
    /// CompSapientHumanlikeMechHead) builds without error, but
    /// PawnRenderTree.ShouldAddNodeToTree - the gate CompRenderNodes-returned nodes have
    /// to pass before PawnRenderTree ever calls AddChild on them - was logged returning
    /// false for it (pawnType=HumanlikeOnly, RaceProps.Humanlike=True), even though
    /// vanilla's own decompiled source says HumanlikeOnly should just check
    /// RaceProps.Humanlike and pass. Something else in this heavily patched modlist is
    /// also touching that result, and chasing exactly what isn't worth it - it's cheaper
    /// to just guarantee the answer for this one specific node.
    ///
    /// [HarmonyPriority(Priority.Last)] makes this postfix the last one to run for this
    /// method, so it always wins regardless of what else patches it. Scoped tightly:
    /// only overrides the result for Fortified's own Head node class, and only for a
    /// pawn Big and Small still considers mechanical (never touches a real mechanoid,
    /// which already renders its head through the static tree and never reaches this
    /// comp-injection path at all).
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.ShouldAddNodeToTree))]
    [HarmonyPriority(Priority.Last)]
    public static class PawnRenderTree_ShouldAddNodeToTree_Patch
    {
        public static void Postfix(PawnRenderTree __instance, PawnRenderNodeProperties props, ref bool __result)
        {
            if (__result || props?.nodeClass == null || props.nodeClass.FullName != "Fortified.PawnRenderNode_Head")
                return;

            Pawn pawn = __instance.pawn;
            if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                return; // Real mechanoid (never reaches this comp-injected path anyway), or not mechanical at all.

            __result = true;
        }
    }
}
