using System;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("lukinari.sapientmechanoidfix");

            // Nearly every optional-dependency patch in this mod resolves its target via a
            // TargetMethod() that returns null when the corresponding mod isn't installed -
            // by design, so that one patch is inert rather than crashing. harmony.PatchAll()
            // doesn't tolerate that the way its per-class try/catch documentation implies:
            // PatchClassProcessor.Patch() throws when TargetMethod() returns null, and
            // PatchAll's own internal loop has no per-class try/catch of its own, so ONE
            // missing optional mod aborts the scan partway through, silently skipping every
            // remaining [HarmonyPatch] class - confirmed via a real bug where a missing
            // Alpha Mechs install broke the War Queen's (entirely unrelated) steel-reserve/
            // urchin-release fix, since OverseerGizmoSuppressionPatches.Apply() ran after
            // the point PatchAll() aborted. Patching each class individually, each in its
            // own try/catch, keeps one missing optional dependency from taking out anything
            // else.
            int patched = 0;
            int failed = 0;
            foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(HarmonyInit).Assembly))
            {
                if (!type.HasHarmonyAttribute())
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    patched++;
                }
                catch (Exception e)
                {
                    failed++;
                    Log.Error($"[SapientMechanoidFix] Failed to apply patch class {type.FullName}: {e}");
                }
            }

            try
            {
                OverseerGizmoSuppressionPatches.Apply(harmony);
                AbilityOverseerGizmoPatches.Apply(harmony);
            }
            catch (Exception e)
            {
                Log.Error("[SapientMechanoidFix] Failed to apply overseer-gizmo patches: " + e);
            }

            Log.Message($"[SapientMechanoidFix] Patch applied ({patched} classes patched, {failed} failed).");
        }
    }
}
