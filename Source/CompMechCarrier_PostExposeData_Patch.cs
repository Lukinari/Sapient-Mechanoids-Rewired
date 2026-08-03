using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Supersedes an earlier attempt at this fix that patched ThingWithComps.InitializeComps
    /// instead of here - that ran too early. InitializeComps only constructs the comp via
    /// Activator.CreateInstance + Initialize(); ThingWithComps.ExposeData() then calls
    /// PostExposeData() on every comp immediately afterward in the same pass, and
    /// CompMechCarrier's own PostExposeData does Scribe_Deep.Look(ref innerContainer,
    /// "innerContainer", this) unconditionally. Verse.Scribe_Deep.Look, for a save with no
    /// matching XML node (exactly the case here - this comp never existed in the pawn's
    /// AllComps when the save was written, so it was never serialized), assigns
    /// default(T) - null for a reference type - via ScribeExtractor.SaveableFromNode. That
    /// silently undid the InitializeComps-time fix in the same load pass, which is why the
    /// gizmo was still reading a null container afterward.
    ///
    /// This postfixes PostExposeData itself instead, so it runs strictly after Scribe has
    /// had its say - the true last word for this field on a given load, not an intermediate
    /// step Scribe can still overwrite.
    /// </summary>
    [HarmonyPatch(typeof(CompMechCarrier), nameof(CompMechCarrier.PostExposeData))]
    public static class CompMechCarrier_PostExposeData_Patch
    {
        private static readonly AccessTools.FieldRef<CompMechCarrier, ThingOwner> InnerContainerRef =
            AccessTools.FieldRefAccess<CompMechCarrier, ThingOwner>("innerContainer");

        public static void Postfix(CompMechCarrier __instance)
        {
            try
            {
                if (Scribe.mode != LoadSaveMode.LoadingVars)
                    return; // Only reconciling a load - a Saving pass should never have this field touched.

                if (InnerContainerRef(__instance) != null)
                    return; // Either a real mechanoid with real saved data, or already fixed - nothing to do.

                InnerContainerRef(__instance) = new ThingOwner<Thing>(__instance, oneStackOnly: false);
                if (__instance.maxToFill <= 0)
                    __instance.maxToFill = __instance.Props.startingIngredientCount;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] CompMechCarrier PostExposeData backstop failed: " + e, 55219035);
            }
        }
    }
}
