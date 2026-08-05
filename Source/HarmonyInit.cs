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
            try
            {
                harmony.PatchAll();
                OverseerGizmoSuppressionPatches.Apply(harmony);
                AbilityOverseerGizmoPatches.Apply(harmony);
                Log.Message("[SapientMechanoidFix] Patch applied.");
            }
            catch (Exception e)
            {
                Log.Error("[SapientMechanoidFix] Failed to apply patch: " + e);
            }
        }
    }
}
