using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// [AV] Mechanoid Skins' own skin-changer gizmo would otherwise still appear on a
    /// sapient War Queen using SapientMechanoidFix_MechSkinRenderTree (see
    /// PawnRenderNode_SapientMechSkin.cs) even while the "Let a sapient War Queen use her
    /// own [AV] Mechanoid Skins design" setting is off - MechanoidSkinRenderSupport only
    /// gates whether a chosen design actually renders, not whether Comp_MechanoidSkin's own
    /// GizmoCanWork check thinks the tree is compatible (it always does, via the
    /// HasSkinCompatibleRendertree tag on that tree - see Defs/MechanoidSkins_RenderTree.
    /// xml). Without this, picking a design in that gizmo while the setting's off would look
    /// like it worked but silently do nothing.
    ///
    /// Scoped narrowly to pawns actually using this mod's own render tree by defName, so
    /// this never touches a real War Queen, any other sapient mechanoid, or any other mod's
    /// render tree that happens to be Mechanoid Skins-compatible on its own.
    ///
    /// [AV] Mechanoid Skins is an optional dependency - Comp_MechanoidSkin is resolved by
    /// name at runtime, and TargetMethod returns null (so this patch is simply never
    /// applied) if that type doesn't exist, same pattern as this mod's other optional-mod
    /// Harmony patches.
    /// </summary>
    [HarmonyPatch]
    public static class Comp_MechanoidSkin_CompGetGizmosExtra_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("AV_MechanoidSkins.Comp_MechanoidSkin");

        static MethodBase TargetMethod()
        {
            return CompType == null ? null : AccessTools.Method(CompType, "CompGetGizmosExtra");
        }

        static bool Prefix(ThingComp __instance, ref IEnumerable<Gizmo> __result)
        {
            try
            {
                if (SapientMechanoidFixMod.Settings?.enableSapientMechSkinChoice == true)
                    return true;

                if (!(__instance.parent is Pawn pawn) || pawn.RaceProps?.renderTree?.defName != "SapientMechanoidFix_MechSkinRenderTree")
                    return true; // Not one of ours - leave AV Mechanoid Skins' own behavior alone.

                __result = Array.Empty<Gizmo>();
                return false;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Comp_MechanoidSkin gizmo-suppression patch failed: " + e, 91274562);
                return true;
            }
        }
    }
}
