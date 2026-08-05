using System;
using BigAndSmall;
using RimWorld;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Dead Man's Switch's Lady and Ascension Megacorp's Rocky (both built on Fortified's
    /// "HumanlikeMech" custom-head system - see CompSapientHumanlikeMechHead.cs and
    /// Fortified_PawnRenderNode_Head_GraphicFor_Patch.cs for the rendering half of this)
    /// can open the styling station once sapient, but the hairstyle list only ever shows
    /// their current hair (Bald by default) - nothing else is ever offered.
    ///
    /// Confirmed via decompile: Dialog_StylingStation.DrawStylingItemType builds its
    /// candidate list as `PawnStyleItemChooser.WantsToUseStyle(pawn, x) ||
    /// hadStyleItem(x)` - hadStyleItem is just "was this the pawn's hair when the dialog
    /// opened," which is why the current hairstyle always appears regardless. Every other
    /// hairstyle depends entirely on WantsToUseStyle returning true, and Fortified's own
    /// HumanlikeMech class forces a fresh pawn's hairDef to Bald specifically when its own
    /// HumanlikeMechExtension.canChangeHairStyle is false (see CheckTracker()) - the real,
    /// non-sapient automaton's own default, unrelated to what a sapient, fully-colonist
    /// version of the same pawn should be limited to. Whichever exact combination of
    /// checks inside WantsToUseStyle is currently returning false for these pawns, this
    /// is the single funnel point the styling station's own hair list always goes
    /// through - forcing it true here fixes the symptom regardless of the precise cause,
    /// the same way this mod's other fixes force a specific vanilla check to succeed for
    /// a sapient mechanical pawn without touching anyone else's result.
    ///
    /// Scoped tightly: only overrides HairDef results, and only for a pawn that (a) Big
    /// and Small still considers mechanical, (b) isn't a real mechanoid, and (c) actually
    /// carries Fortified's HumanlikeMechExtension - i.e. exactly the pawns subject to this
    /// specific "custom head, hair off by default" mechanic. A sapient mech without this
    /// extension already has ordinary hairstyle freedom and is untouched here.
    /// </summary>
    [HarmonyPatch(typeof(PawnStyleItemChooser), nameof(PawnStyleItemChooser.WantsToUseStyle))]
    public static class PawnStyleItemChooser_WantsToUseStyle_HumanlikeMechHair_Patch
    {
        private static readonly Type ExtensionType = AccessTools.TypeByName("Fortified.HumanlikeMechExtension");

        public static void Postfix(Pawn pawn, StyleItemDef styleItemDef, ref bool __result)
        {
            try
            {
                if (__result || ExtensionType == null || !(styleItemDef is HairDef))
                    return; // Already allowed, Fortified not installed, or not a hairstyle - nothing to change.

                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid, or not mechanical - not ours to touch.

                if (pawn.def.modExtensions == null)
                    return;

                bool hasExtension = false;
                foreach (DefModExtension ext in pawn.def.modExtensions)
                {
                    if (ExtensionType.IsInstanceOfType(ext))
                    {
                        hasExtension = true;
                        break;
                    }
                }
                if (!hasExtension)
                    return; // Not a Fortified custom-head mech - whatever's restricting its hair isn't this mechanic.

                __result = true;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] HumanlikeMech hairstyle-eligibility patch failed: " + e, 91274531);
            }
        }
    }
}
