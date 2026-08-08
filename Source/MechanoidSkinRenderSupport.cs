using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// [AV] Mechanoid Skins is an optional dependency - see SummonedMechSkinChoiceSupport.cs
    /// for why nothing here references its types at compile time. Resolves just enough of
    /// Comp_MechanoidSkin/MechSkinManager/SkinGraphicManager to let
    /// PawnRenderNode_SapientMechSkin apply a chosen skin on top of Big and Small's own
    /// resolved body graphic - see Defs/MechanoidSkins_RenderTree.xml for why that graft is
    /// needed instead of using AV Mechanoid Skins' own render node directly.
    /// </summary>
    public static class MechanoidSkinRenderSupport
    {
        private static readonly Type CompMechanoidSkinType = AccessTools.TypeByName("AV_MechanoidSkins.Comp_MechanoidSkin");
        private static readonly Type MechSkinManagerType = AccessTools.TypeByName("AV_MechanoidSkins.MechSkinManager");
        private static readonly Type SkinGraphicManagerType = AccessTools.TypeByName("AV_MechanoidSkins.SkinGraphicManager");
        private static readonly Type SkinDefType = AccessTools.TypeByName("AV_MechanoidSkins.SkinDef");

        private static readonly FieldInfo CurrentSkinField = CompMechanoidSkinType == null
            ? null
            : AccessTools.Field(CompMechanoidSkinType, "currentSkin");

        private static readonly FieldInfo SizeField = CompMechanoidSkinType == null
            ? null
            : AccessTools.Field(CompMechanoidSkinType, "size");

        // MechSkinManager.IsValidSkin(SkinDef) - the same check AV Mechanoid Skins' own render
        // node runs before compositing a skin (a skin missing its texPath/maskPath is invalid).
        private static readonly MethodInfo IsValidSkinMethod = MechSkinManagerType == null || SkinDefType == null
            ? null
            : AccessTools.Method(MechSkinManagerType, "IsValidSkin", new[] { SkinDefType });

        // SkinGraphicManager.GetGraphicFromSkin(SkinDef, Graphic baseGraphic, Pawn,
        // Comp_MechanoidSkin, bool useMask) - the overload AV Mechanoid Skins' own render node
        // uses, which composites purely from the given baseGraphic template (drawSize/color/
        // data) and never touches pawn.ageTracker itself.
        private static readonly MethodInfo GetGraphicFromSkinMethod = SkinGraphicManagerType == null || SkinDefType == null || CompMechanoidSkinType == null
            ? null
            : AccessTools.Method(SkinGraphicManagerType, "GetGraphicFromSkin",
                new[] { SkinDefType, typeof(Graphic), typeof(Pawn), CompMechanoidSkinType, typeof(bool) });

        public static bool IsAvailable => CompMechanoidSkinType != null && CurrentSkinField != null && SizeField != null
            && IsValidSkinMethod != null && GetGraphicFromSkinMethod != null;

        private static object GetComp(Pawn pawn)
        {
            if (pawn?.AllComps == null)
                return null;

            foreach (ThingComp comp in pawn.AllComps)
            {
                if (CompMechanoidSkinType.IsInstanceOfType(comp))
                    return comp;
            }
            return null;
        }

        /// <summary>
        /// Null if the pawn has no [AV] Mechanoid Skins comp, no skin chosen, or the chosen
        /// skin isn't valid - caller falls back to baseGraphic unchanged in every case.
        /// </summary>
        public static Graphic TryGetSkinGraphic(Pawn pawn, Graphic baseGraphic)
        {
            if (!IsAvailable || pawn == null || baseGraphic == null)
                return null;

            if (SapientMechanoidFixMod.Settings?.enableSapientMechSkinChoice != true)
                return null;

            try
            {
                object comp = GetComp(pawn);
                if (comp == null)
                    return null;

                object currentSkin = CurrentSkinField.GetValue(comp);
                if (currentSkin == null)
                    return null;

                if (!(bool)IsValidSkinMethod.Invoke(null, new[] { currentSkin }))
                    return null;

                return (Graphic)GetGraphicFromSkinMethod.Invoke(null, new object[] { currentSkin, baseGraphic, pawn, comp, true });
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Failed to resolve sapient mech skin graphic for " + pawn?.LabelShort + ": " + e, 91274560);
                return null;
            }
        }

        /// <summary>1f unless the pawn has a skin chosen with a non-default density.</summary>
        public static float GetDrawSizeMultiplier(Pawn pawn)
        {
            if (!IsAvailable || pawn == null)
                return 1f;

            if (SapientMechanoidFixMod.Settings?.enableSapientMechSkinChoice != true)
                return 1f;

            try
            {
                object comp = GetComp(pawn);
                if (comp == null || CurrentSkinField.GetValue(comp) == null)
                    return 1f;

                return (float)SizeField.GetValue(comp);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Failed to resolve sapient mech skin draw size for " + pawn?.LabelShort + ": " + e, 91274561);
                return 1f;
            }
        }
    }
}
