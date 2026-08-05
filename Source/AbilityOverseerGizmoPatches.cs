using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// The ability-gizmo counterpart to OverseerGizmoSuppressionPatches: several GD5
    /// CompAbilityEffect classes disable their own gizmo directly via
    /// MechanitorUtility.GetOverseer(pawn) == null (same recurring pattern as the comp
    /// gizmos that class already fixes), but abilities aren't ThingComps - their gizmos
    /// come from GizmoDisabled(out reason), a completely different code path that the
    /// comp-focused suppression mechanism never touches.
    ///
    /// Unlike the blind "clear the whole disable" fix used for CompAbilityEffect_PlaceShield
    /// (safe there because its only other disable reasons are Downed/low-energy, both
    /// already meaningless for a sapient mech), some abilities have a real, unrelated
    /// disable condition alongside the overseer check - e.g. the Mosquito's Rocket
    /// Attack also requires the pawn to be Flying, which has nothing to do with sapient
    /// status and must stay respected. Rather than duplicate each ability's own logic to
    /// tell which reason fired, this opens the exact same GetOverseer suppression window
    /// SapientMechOverseerGizmoGuard already provides for comps, just around the
    /// GizmoDisabled call instead of a gizmo enumeration - so the original method's own
    /// downed/flying/etc. checks still evaluate for real, only GetOverseer itself is
    /// transparently satisfied.
    /// </summary>
    public static class AbilityOverseerGizmoPatches
    {
        public static void Apply(Harmony harmony)
        {
            // GD3.CompAbilityEffect_AirRaid deliberately NOT included here - its Apply()
            // casts parent.pawn to the custom MechMosquito subclass and dereferences it
            // unconditionally, which would throw for a sapient Mosquito regardless of
            // whether the gizmo itself is enabled. Fixing only the gizmo would turn a
            // safely-disabled ability into a crash-on-click one.
            TryPatchAbility(harmony, "GD3.CompAbilityEffect_RocketAttack");
        }

        public static void TryPatchAbility(Harmony harmony, string typeName)
        {
            try
            {
                Type compType = AccessTools.TypeByName(typeName);
                if (compType == null)
                    return; // Owning mod not installed (or type renamed) - nothing to patch.

                MethodInfo method = AccessTools.Method(compType, "GizmoDisabled");
                if (method != null)
                {
                    harmony.Patch(method,
                        prefix: new HarmonyMethod(typeof(AbilityOverseerGizmoPatches), nameof(Prefix)),
                        postfix: new HarmonyMethod(typeof(AbilityOverseerGizmoPatches), nameof(Postfix)));
                }
                else
                {
                    Log.Warning($"[SapientMechanoidFix] {typeName}.GizmoDisabled not found; ability overseer fix not applied for it.");
                }
            }
            catch (Exception e)
            {
                Log.Error($"[SapientMechanoidFix] Failed to attach ability gizmo-disabled patch to {typeName}: {e}");
            }
        }

        // CompAbilityEffect is a vanilla base type - safe to reference directly
        // regardless of whether the owning mod is installed.
        public static void Prefix(CompAbilityEffect __instance, out Pawn __state)
        {
            __state = null;
            try
            {
                Pawn pawn = __instance.parent?.pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid - GetOverseer's real behavior matters here, leave it alone.

                SapientMechOverseerGizmoGuard.Suppress(pawn);
                __state = pawn;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Ability gizmo-disabled prefix failed: " + e, 91274506);
            }
        }

        // The unused Exception __exception parameter tells Harmony to run this postfix
        // even if GizmoDisabled's own body throws - without it, a throw would skip this
        // cleanup and leave the pawn permanently stuck in SapientMechOverseerGizmoGuard's
        // suppressed set, silently widening what's meant to be a narrow, temporary
        // suppression window into a permanent one for that pawn (see the guard's own doc
        // comment). Same pattern as Pawn_GetDisabledWorkTypes_Patch's postfix.
        public static void Postfix(Pawn __state, Exception __exception)
        {
            if (__state != null)
                SapientMechOverseerGizmoGuard.Unsuppress(__state);
        }
    }
}
