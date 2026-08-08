using System;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Body node for SapientMechanoidFix_MechSkinRenderTree (see
    /// Defs/MechanoidSkins_RenderTree.xml). Subclasses Big and Small's own
    /// PawnRenderNode_HAnimalBody rather than [AV] Mechanoid Skins' own
    /// PawnRenderNode_MechanoidSkin - the latter extends PawnRenderNode_AnimalPart and calls
    /// Pawn.ageTracker.CurKindLifeStage directly (confirmed by decompile), which throws for a
    /// genuinely humanlike sapient pawn - the same crash class already hit and reverted for
    /// Lady's head (see HumanlikeAnimalSettings_MechQueens.xml).
    ///
    /// base.GraphicFor(pawn) resolves Big and Small's own default sapient-mech body graphic -
    /// correct color, rot state, and corpse handling, all already humanlike-safe, since it
    /// resolves through HumanlikeAnimalGenerator's own original-animal-kindDef lookup instead
    /// of ageTracker. This only steps in afterward, swapping an [AV] Mechanoid Skins design on
    /// top of that resolved graphic if the pawn has one chosen. No AV Mechanoid Skins type is
    /// referenced directly here - it's an optional dependency, resolved through
    /// MechanoidSkinRenderSupport by reflection, same pattern as this mod's other optional-mod
    /// integrations.
    ///
    /// Both overrides below run inside RimWorld's own render-tree graph setup, the exact same
    /// call path that turned one uncaught exception in Lady's forced render tree into a crash
    /// for every pawn's rendering on the map, not just hers (see
    /// HumanlikeAnimalSettings_MechQueens.xml). base.GraphicFor(pawn) is Big and Small's own
    /// code, not something this mod controls - wrapped here so a future edge case there can't
    /// propagate past this node. Falls back to null on failure, the same "safe" return value
    /// Big and Small's own GraphicFor already uses for its own error path (confirmed by
    /// decompile), so the render tree is already known to tolerate it gracefully.
    /// </summary>
    public class PawnRenderNode_SapientMechSkin : BigAndSmall.PawnRenderNode_HAnimalBody
    {
        public PawnRenderNode_SapientMechSkin(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            try
            {
                Graphic baseGraphic = base.GraphicFor(pawn);
                if (baseGraphic == null)
                    return null;

                return MechanoidSkinRenderSupport.TryGetSkinGraphic(pawn, baseGraphic) ?? baseGraphic;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] PawnRenderNode_SapientMechSkin.GraphicFor failed for " + pawn?.LabelShort + ": " + e, 91274564);
                return null;
            }
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            try
            {
                Graphic graphic = GraphicFor(pawn);
                if (graphic == null)
                    return null;

                float drawSize = MechanoidSkinRenderSupport.GetDrawSizeMultiplier(pawn);
                return MeshPool.GetMeshSetForSize(graphic.drawSize.x * drawSize, graphic.drawSize.y * drawSize);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] PawnRenderNode_SapientMechSkin.MeshSetFor failed for " + pawn?.LabelShort + ": " + e, 91274565);
                return null;
            }
        }
    }
}
