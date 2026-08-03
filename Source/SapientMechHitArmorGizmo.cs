using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Ports Glitterworld Destroyer 5's HitArmorGizmo layout, sizing, and per-charge
    /// threshold-tick bar as closely as possible - width formula, padding math, and the
    /// DrawDraggableBarThreshold tick marks are all copied from the decompiled GD3.dll.
    /// Label/tooltip text is hardcoded rather than pulled from GD5's own translation
    /// keys, since this mod doesn't depend on GD5 being installed.
    /// </summary>
    public class SapientMechHitArmorGizmo : Gizmo
    {
        private readonly CompSapientMechHitArmor comp;
        private readonly List<float> bandPercentages;

        private static readonly Texture2D BarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));
        private static readonly Texture2D BarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

        public SapientMechHitArmorGizmo(CompSapientMechHitArmor comp)
        {
            this.comp = comp;
            int limitOfTimes = Props.limitOfTimes;
            bandPercentages = new List<float>();
            for (int i = 0; i <= limitOfTimes; i++)
                bandPercentages.Add(1f / limitOfTimes * i);
        }

        private CompProperties_SapientMechHitArmor Props => (CompProperties_SapientMechHitArmor)comp.props;

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(Props.limitOfTimes * 15f, 270f);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            float barValue = (float)comp.timesLeft / Props.limitOfTimes;

            var overallRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect innerRect = overallRect.ContractedBy(10f);
            Widgets.DrawWindowBackground(overallRect);

            Text.Font = GameFont.Tiny;
            string label = "Reactive Armor";
            float labelHeight = Text.CalcHeight(label, innerRect.width);
            var labelRect = new Rect(innerRect.x, innerRect.y, innerRect.width, labelHeight);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            float availableHeight = innerRect.height - labelRect.height;
            float barHeight = availableHeight - 4f;
            float verticalOffset = (availableHeight - barHeight) / 2f;
            var barRect = new Rect(innerRect.x, labelRect.yMax + verticalOffset, innerRect.width, barHeight);
            DraggableBar(barRect, barValue);

            Text.Anchor = TextAnchor.LowerCenter;
            barRect.y -= 2f;
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(barRect, GetBarTip, Gen.HashCombineInt(comp.GetHashCode(), 91274446));

            return new GizmoResult(GizmoState.Clear);
        }

        private void DraggableBar(Rect barRect, float barValue)
        {
            bool highlighted = Mouse.IsOver(barRect);
            Widgets.FillableBar(barRect, Mathf.Min(barValue, 1f), highlighted ? BarHighlightTex : BarTex, EmptyBarTex, doBorder: true);
            foreach (float bandPercentage in bandPercentages)
                DrawDraggableBarThreshold(barRect, bandPercentage, barValue);
            GUI.color = Color.white;
        }

        private static void DrawDraggableBarThreshold(Rect rect, float percent, float curValue)
        {
            var tickRect = new Rect(
                rect.x + 3f + (rect.width - 8f) * percent,
                rect.y + rect.height - 9f,
                2f,
                6f);
            GUI.DrawTexture(tickRect, curValue < percent ? BaseContent.GreyTex : BaseContent.BlackTex);
        }

        private string GetBarTip()
        {
            return $"Reactive armor charges: {comp.timesLeft} / {Props.limitOfTimes}\nEach charge grants brief full damage immunity when hit. Charges recharge while being repaired.";
        }
    }
}
