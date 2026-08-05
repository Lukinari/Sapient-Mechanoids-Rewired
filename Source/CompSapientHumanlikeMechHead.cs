using System;
using System.Collections.Generic;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    public class CompProperties_SapientHumanlikeMechHead : CompProperties
    {
        public CompProperties_SapientHumanlikeMechHead()
        {
            compClass = typeof(CompSapientHumanlikeMechHead);
        }
    }

    /// <summary>
    /// Third attempt at DMS_Mech_Lady's missing head texture. Later confirmed to be the
    /// same underlying issue for any mech built on Fortified's "HumanlikeMech" rendering
    /// system, not something specific to Lady or Dead Man's Switch - Ascension Megacorp's
    /// Rocky uses the identical thingClass/renderTree/modExtension setup and hit the
    /// exact same missing head, fixed by attaching this same comp to it too (see
    /// Patches/HumanlikeMechHead.xml).
    ///
    /// Attempt 1 (forcing Big and Small to keep her original "HumanlikeMech"
    /// PawnRenderTreeDef via renderTreeWhitelist) crashed pawn rendering entirely for
    /// every pawn on the map - that tree's body node (PawnRenderNode_AnimalPart) calls
    /// Pawn.ageTracker.CurKindLifeStage, which throws for a pawn Big and Small has also
    /// made RaceProps.Humanlike. Reverted.
    ///
    /// Attempt 2 patched vanilla's own PawnRenderNode_Head.GraphicFor, assuming her
    /// sapient version would fall back to vanilla's "Human" tree. Wrong - her original
    /// race has intelligence ToolUser, not Humanlike, so
    /// HumanlikeAnimalGenerator.SetRenderTree routes her to "BS_HumanlikeAnimal"
    /// instead, a tree with no head node at all. No-op.
    ///
    /// This comp (via CompRenderNodes, the same sanctioned extension point vanilla
    /// mechanoid turret guns/equipment use to add extra visual layers to any pawn
    /// regardless of its base tree) constructs a real instance of Fortified's own
    /// PawnRenderNode_Head/PawnRenderNodeWorker_Head and grafts it onto whatever tree
    /// Big and Small actually built - confirmed working: the node builds and attaches
    /// with no errors. But logging what GraphicFor actually returned exposed the real
    /// bug: Fortified.PawnRenderNode_Head.GraphicFor only reads
    /// HumanlikeMech.HeadGraphic when `pawn is HumanlikeMech` - a literal C# type
    /// check against the runtime object, which is false for our sapient pawn even
    /// though thingClass was preserved (RimWorld renders through the render tree's own
    /// pawn reference, and whatever type Big and Small's clone pipeline actually hands
    /// back for a genuinely sapient conversion isn't a Fortified.HumanlikeMech
    /// instance). So it silently fell through to vanilla's story.headType path and drew
    /// a generic male head instead. See
    /// Fortified_PawnRenderNode_Head_GraphicFor_Patch below for the actual fix.
    ///
    /// Attached (see Patches/HumanlikeMechHead.xml) to each affected mech's original
    /// ThingDef so it flows through the same clone pipeline as its other comps - see
    /// compWhitelist in HumanlikeAnimalSettings_MechQueens.xml for why that's necessary.
    /// Self-gates to sapient-only via IsSapientMech, so its presence on the real,
    /// non-sapient original (which already renders its head correctly through the static
    /// tree) is inert - it would otherwise draw a duplicate head layer.
    /// </summary>
    public class CompSapientHumanlikeMechHead : ThingComp
    {
        private static bool IsSapientMech(Pawn pawn)
        {
            if (pawn == null)
                return false;
            if (pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                return false; // Real mechanoid (already has its head via the static tree), or not mechanical at all.
            return true;
        }

        public override List<PawnRenderNode> CompRenderNodes()
        {
            try
            {
                Pawn pawn = parent as Pawn;

                if (!IsSapientMech(pawn))
                    return null;

                PawnRenderNodeProperties headProps = FortifiedHumanlikeMechHeadNode.HeadProps;
                PawnRenderTree tree = pawn.Drawer?.renderer?.renderTree;
                if (headProps?.nodeClass == null || tree == null)
                    return null;

                var node = (PawnRenderNode)Activator.CreateInstance(headProps.nodeClass, pawn, headProps, tree);
                return new List<PawnRenderNode> { node };
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Failed to construct sapient HumanlikeMech head node: " + e, 91274463);
                return null;
            }
        }
    }

    /// <summary>
    /// One-time (lazy, cached) lookup of Fortified's own "Head"-tagged node definition
    /// inside its "HumanlikeMech" PawnRenderTreeDef - the exact PawnRenderNodeProperties
    /// subtree (head graphic node plus its tattoo/beard/hair/wounds/apparel/status
    /// children) that a real HumanlikeMech pawn already uses. Reusing the live Def object
    /// Fortified itself loaded means no part of this needs to know Fortified's node/
    /// worker class names ahead of time, or duplicate any of its layout -
    /// PawnRenderNodeProperties, PawnRenderTreeDef and PawnRenderNodeTagDef are all
    /// vanilla types, so nothing here requires a compile-time reference to Fortified.
    /// </summary>
    internal static class FortifiedHumanlikeMechHeadNode
    {
        private static PawnRenderNodeProperties cached;
        private static bool lookedUp;

        public static PawnRenderNodeProperties HeadProps
        {
            get
            {
                if (!lookedUp)
                {
                    lookedUp = true;
                    cached = Find();
                }
                return cached;
            }
        }

        private static PawnRenderNodeProperties Find()
        {
            try
            {
                PawnRenderTreeDef tree = DefDatabase<PawnRenderTreeDef>.GetNamedSilentFail("HumanlikeMech");
                return tree?.root == null ? null : FindByTag(tree.root, "Head");
            }
            catch (Exception e)
            {
                Log.Error("[SapientMechanoidFix] Failed to locate Fortified's HumanlikeMech head node: " + e);
                return null;
            }
        }

        private static PawnRenderNodeProperties FindByTag(PawnRenderNodeProperties props, string tagDefName)
        {
            if (props.tagDef?.defName == tagDefName)
                return props;

            if (props.children == null)
                return null;

            foreach (PawnRenderNodeProperties child in props.children)
            {
                PawnRenderNodeProperties found = FindByTag(child, tagDefName);
                if (found != null)
                    return found;
            }
            return null;
        }
    }

    /// <summary>
    /// The actual fix (see CompSapientHumanlikeMechHead's doc comment for how this was
    /// found). Fortified.PawnRenderNode_Head.GraphicFor only reads
    /// HumanlikeMech.HeadGraphic (itself just Extension.headGraphic/headGraphicHaired)
    /// when `pawn is HumanlikeMech`. For a Big and Small sapient conversion that's
    /// false, so this postfix re-derives the same result directly from
    /// HumanlikeMechExtension instead of relying on the type check - independent of
    /// what runtime class the pawn actually is, as long as its def still carries the
    /// extension (confirmed present via modExtensionWhitelist).
    ///
    /// Fortified is an optional dependency - HumanlikeMechExtension and
    /// PawnRenderNode_Head are both resolved by name at runtime and only ever read
    /// through cached FieldInfo/TargetMethod, never referenced directly in this
    /// patch's own signature (only vanilla Pawn/Graphic types), so this class is
    /// entirely inert if Fortified isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class Fortified_PawnRenderNode_Head_GraphicFor_Patch
    {
        private static readonly Type NodeType = AccessTools.TypeByName("Fortified.PawnRenderNode_Head");
        private static readonly Type ExtensionType = AccessTools.TypeByName("Fortified.HumanlikeMechExtension");
        private static readonly FieldInfo HeadGraphicField = ExtensionType == null ? null : AccessTools.Field(ExtensionType, "headGraphic");
        private static readonly FieldInfo HeadGraphicHairedField = ExtensionType == null ? null : AccessTools.Field(ExtensionType, "headGraphicHaired");
        private static readonly FieldInfo CanChangeHairStyleField = ExtensionType == null ? null : AccessTools.Field(ExtensionType, "canChangeHairStyle");

        static MethodBase TargetMethod()
        {
            return NodeType == null ? null : AccessTools.Method(NodeType, "GraphicFor");
        }

        public static void Postfix(Pawn pawn, ref Graphic __result)
        {
            try
            {
                if (ExtensionType == null)
                    return;
                if (pawn == null)
                    return;
                if (pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid - `pawn is HumanlikeMech` already true, Fortified's own result is correct.

                object extension = null;
                if (pawn.def.modExtensions != null)
                {
                    foreach (DefModExtension ext in pawn.def.modExtensions)
                    {
                        if (ext.GetType() == ExtensionType)
                        {
                            extension = ext;
                            break;
                        }
                    }
                }
                if (extension == null)
                    return;

                bool hasHair = pawn.story?.hairDef != null && pawn.story.hairDef != HairDefOf.Bald;
                bool canChangeHairStyle = CanChangeHairStyleField != null && (bool)CanChangeHairStyleField.GetValue(extension);
                FieldInfo graphicField = (canChangeHairStyle && hasHair) ? HeadGraphicHairedField : HeadGraphicField;
                GraphicData data = graphicField?.GetValue(extension) as GraphicData;
                if (data?.Graphic != null)
                    __result = data.Graphic;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] HumanlikeMech head graphic patch failed: " + e, 91274461);
            }
        }
    }
}
