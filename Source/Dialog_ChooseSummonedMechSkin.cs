using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// A deliberately simple picker - not a reimplementation of [AV] Mechanoid Skins' own
    /// Dialog_ShowSkinChangeWindow (that one is built entirely around recoloring/reskinning
    /// one specific already-existing pawn right now: faction/ideology/favorite-color rules,
    /// density sliders, a full RGB wheel). This dialog answers a different question -
    /// "which design should pawns that don't exist yet use" - so it's just a scrollable
    /// list of designs, plus a "default" option to fall back to AV Mechanoid Skins' own
    /// automatic pick.
    ///
    /// Defaults to the designs AV Mechanoid Skins curated for the urchin's own pawn kind
    /// (matchingMechs/useableForAll), with a toggle to show every valid design instead -
    /// every SkinDef is one universal texture set regardless of which mech it was authored
    /// for, so nothing about rendering actually requires a "matching" mech, only AV
    /// Mechanoid Skins' own curation does.
    /// </summary>
    public class Dialog_ChooseSummonedMechSkin : Window
    {
        private readonly CompSummonedMechSkinChoice comp;
        private readonly List<Def> recommendedOptions;
        private readonly List<Def> allOptions;
        private bool showAllSkins = false;
        private Vector2 scrollPosition;

        private List<Def> CurrentOptions => showAllSkins ? allOptions : recommendedOptions;

        public override Vector2 InitialSize => new Vector2(640f, 620f);

        public Dialog_ChooseSummonedMechSkin(CompSummonedMechSkinChoice comp, PawnKindDef urchinKind)
        {
            this.comp = comp;
            recommendedOptions = SummonedMechSkinChoiceSupport.GetSkinsForKind(urchinKind);
            allOptions = SummonedMechSkinChoiceSupport.GetAllSkins();
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("Choose urchin design");
            Text.Font = GameFont.Small;
            listing.Gap();

            if (listing.RadioButton("Default (AV Mechanoid Skins' own pick)", comp.chosenSkin == null))
            {
                comp.chosenSkin = null;
                Close();
            }
            listing.Gap();

            bool showAllBefore = showAllSkins;
            listing.CheckboxLabeled("Show all designs, not just ones matching this urchin", ref showAllSkins,
                "Every design is one universal texture set regardless of which mech it was made for - this just widens the list below to every design AV Mechanoid Skins knows about, curated or not.");
            if (showAllSkins != showAllBefore)
                scrollPosition = Vector2.zero;

            listing.GapLine();

            float listTop = listing.CurHeight;
            listing.End();

            var outerRect = new Rect(inRect.x, inRect.y + listTop, inRect.width, inRect.height - listTop - CloseButSize.y - 10f);
            List<Def> options = CurrentOptions;

            if (options.Count == 0)
            {
                Widgets.Label(outerRect, "No designs found.");
                return;
            }

            var viewRect = new Rect(0f, 0f, outerRect.width - 16f, options.Count * RowHeight);
            Widgets.BeginScrollView(outerRect, ref scrollPosition, viewRect);

            float curY = 0f;
            foreach (Def option in options)
            {
                var rowRect = new Rect(0f, curY, viewRect.width, RowHeight);
                DrawSkinRow(rowRect, option);
                curY += RowHeight;
            }

            Widgets.EndScrollView();
        }

        private const float RowHeight = 100f;
        private const float IconSize = 84f;
        private const float IconGap = 4f;

        // SummonedMechSkinChoiceSupport.GetSkinIcons returns [South, West, East, North] -
        // West is skipped here since a lot of skins don't bother defining a distinct west
        // texture (many mechs are symmetric enough that it'd just duplicate east anyway).
        private static readonly int[] DirectionIndices = { 0, 2, 3 };
        private static readonly string[] DirectionLabels = { "S", "E", "N" };

        private void DrawSkinRow(Rect rowRect, Def option)
        {
            bool selected = comp.chosenSkin == option;
            if (selected)
                Widgets.DrawHighlightSelected(rowRect);
            else
                Widgets.DrawHighlightIfMouseover(rowRect);

            Texture2D[] icons = SummonedMechSkinChoiceSupport.GetSkinIcons(option);
            float iconsTop = rowRect.y + (RowHeight - IconSize) / 2f;
            float x = rowRect.x + 6f;
            for (int i = 0; i < DirectionIndices.Length; i++)
            {
                Texture2D icon = icons[DirectionIndices[i]];
                var iconRect = new Rect(x, iconsTop, IconSize, IconSize);
                if (icon != null)
                    Widgets.DrawTextureFitted(iconRect, icon, 1f);
                else
                    Widgets.DrawBoxSolid(iconRect, new Color(0f, 0f, 0f, 0.15f));

                var captionRect = new Rect(iconRect.x, iconRect.yMax - 16f, iconRect.width, 16f);
                Color colorBefore = GUI.color;
                GameFont fontBefore = Text.Font;
                TextAnchor anchorBefore2 = Text.Anchor;
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.LowerCenter;
                Widgets.Label(captionRect, DirectionLabels[i]);
                Text.Anchor = anchorBefore2;
                Text.Font = fontBefore;
                GUI.color = colorBefore;

                x += IconSize + IconGap;
            }

            var labelRect = new Rect(x + 6f, rowRect.y, rowRect.width - (x - rowRect.x) - 6f, RowHeight);
            var anchorBefore = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, option.LabelCap);
            Text.Anchor = anchorBefore;

            if (Widgets.ButtonInvisible(rowRect))
            {
                comp.chosenSkin = option;
                Close();
            }
        }
    }
}
