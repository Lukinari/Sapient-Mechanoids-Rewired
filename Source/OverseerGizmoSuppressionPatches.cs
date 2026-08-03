using System;
using System.Collections.Generic;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// AV Framework (Work Queen) and Glitterworld Destroyer 5 are both optional
    /// dependencies, so none of their types are referenced at compile time. Everything
    /// here resolves comp types by name at runtime (via AccessTools.TypeByName) and is
    /// only ever attached to Harmony (via manual harmony.Patch calls from HarmonyInit)
    /// after that lookup confirms the owning assembly is actually loaded - so this
    /// class is entirely inert, and never JIT-compiled/executed, for whichever of the
    /// two isn't installed.
    ///
    /// Fixes a recurring pattern across three unrelated comps from two different mods:
    /// each gates its CompGetGizmosExtra directly on
    /// MechanitorUtility.GetOverseer(pawn) == null, bypassing our
    /// IsColonyMechPlayerControlled fix entirely and hiding the gizmo for any sapient
    /// mech with no Overseer relation:
    /// 1. AV_Framework.CompMechReloadableResourceHolder (steel reserve gizmo)
    /// 2. AV_Framework.CompMechCarrierChoice (release-urchins gizmo)
    /// 3. GD3.CompAttackMode (Glitterworld Destroyer 5's auto-release-urchins toggle)
    ///
    /// Also backstops CompMechReloadableResourceHolder's innerContainer, which is left
    /// null after a save load (its own PostSpawnSetup guard is skipped when
    /// respawningAfterLoad is true) - same root cause as vanilla CompMechCarrier's
    /// crash.
    /// </summary>
    internal static class OverseerGizmoSuppressionPatches
    {
        public static void Apply(Harmony harmony)
        {
            TryPatchComp(harmony, "AV_Framework.CompMechReloadableResourceHolder", fixInnerContainer: true);
            TryPatchComp(harmony, "AV_Framework.CompMechCarrierChoice", fixInnerContainer: false);
            TryPatchComp(harmony, "GD3.CompAttackMode", fixInnerContainer: false);
        }

        private static void TryPatchComp(Harmony harmony, string typeName, bool fixInnerContainer)
        {
            try
            {
                Type compType = AccessTools.TypeByName(typeName);
                if (compType == null)
                    return; // Owning mod not installed (or type renamed) - nothing to patch.

                MethodInfo gizmoMethod = AccessTools.Method(compType, "CompGetGizmosExtra");
                if (gizmoMethod != null)
                {
                    harmony.Patch(gizmoMethod, postfix: new HarmonyMethod(typeof(OverseerGizmoSuppressionPatches), nameof(GizmoPostfix)));
                }
                else
                {
                    Log.Warning($"[SapientMechanoidFix] {typeName}.CompGetGizmosExtra not found; overseer-gizmo fix not applied for it.");
                }

                if (fixInnerContainer)
                {
                    MethodInfo exposeMethod = AccessTools.Method(compType, "PostExposeData");
                    if (exposeMethod != null)
                    {
                        harmony.Patch(exposeMethod, postfix: new HarmonyMethod(typeof(OverseerGizmoSuppressionPatches), nameof(PostExposeDataPostfix)));
                    }
                    else
                    {
                        Log.Warning($"[SapientMechanoidFix] {typeName}.PostExposeData not found; innerContainer fix not applied.");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"[SapientMechanoidFix] Failed to attach overseer-gizmo patches to {typeName}: {e}");
            }
        }

        // ThingComp is a vanilla base type - safe to reference directly regardless of
        // whether the owning mod is installed. Harmony binds __instance to the actual
        // runtime comp.
        public static void GizmoPostfix(ThingComp __instance, ref IEnumerable<Gizmo> __result)
        {
            try
            {
                Pawn pawn = __instance.parent as Pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !pawn.IsMechanical())
                    return; // Real mechanoid, or not mechanical - leave GetOverseer's real behavior alone.

                __result = WrapWithOverseerSuppression(__result, pawn);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Overseer gizmo postfix failed: " + e, 91274438);
            }
        }

        // Bracket each pull from the original lazy iterator with the suppression flag,
        // since CompGetGizmosExtra's body (including its GetOverseer check) only
        // actually executes when the caller enumerates - long after this postfix
        // itself returns.
        private static IEnumerable<Gizmo> WrapWithOverseerSuppression(IEnumerable<Gizmo> values, Pawn pawn)
        {
            using (IEnumerator<Gizmo> e = values.GetEnumerator())
            {
                while (true)
                {
                    bool hasNext;
                    Gizmo current = null;
                    SapientMechOverseerGizmoGuard.Suppress(pawn);
                    try
                    {
                        hasNext = e.MoveNext();
                        if (hasNext)
                            current = e.Current;
                    }
                    finally
                    {
                        SapientMechOverseerGizmoGuard.Unsuppress(pawn);
                    }
                    if (!hasNext)
                        yield break;
                    yield return current;
                }
            }
        }

        public static void PostExposeDataPostfix(ThingComp __instance)
        {
            try
            {
                if (Scribe.mode != LoadSaveMode.LoadingVars)
                    return;

                Traverse containerField = Traverse.Create(__instance).Field("innerContainer");
                if (containerField.GetValue<ThingOwner>() != null)
                    return;

                if (!(__instance is IThingHolder holder))
                {
                    Log.ErrorOnce("[SapientMechanoidFix] Comp is not an IThingHolder; cannot rebuild innerContainer.", 91274440);
                    return;
                }

                containerField.SetValue(new ThingOwner<Thing>(holder, oneStackOnly: false));
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] innerContainer backstop failed: " + e, 91274439);
            }
        }
    }
}
