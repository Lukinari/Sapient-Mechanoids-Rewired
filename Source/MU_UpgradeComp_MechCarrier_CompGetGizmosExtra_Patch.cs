using System;
using System.Collections.Generic;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Mechanoid Upgrades' "mech carrier" upgrade (spawns helper drones, the same idea as
    /// vanilla's own War Queen urchins) gates its whole gizmo - the button that actually
    /// spawns them - on `MechanitorUtility.GetOverseer(parent.holder) == null`, same
    /// recurring bug as AV Framework's Work Queen and GD5's War Queen add-ons (see
    /// OverseerGizmoSuppressionPatches.cs), just on the mod's own parallel "UpgradeComp"
    /// class instead of a real Verse.ThingComp - that mechanism's TryPatchComp can't be
    /// reused directly since it binds __instance as ThingComp, so this reimplements the
    /// same suppression-window technique standalone, sharing the same
    /// SapientMechOverseerGizmoGuard the rest of this mod's overseer-gizmo fixes use.
    ///
    /// Mechanoid Upgrades is an optional dependency - UpgradeComp_MechCarrier is resolved
    /// by name at runtime and only ever invoked through cached MethodInfo/reflection,
    /// never referenced directly in this patch's own signature (only vanilla Pawn/Gizmo
    /// types), so this class is entirely inert if that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class MU_UpgradeComp_MechCarrier_CompGetGizmosExtra_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("MU.UpgradeComp_MechCarrier");
        private static readonly FieldInfo ParentField = CompType == null ? null : AccessTools.Field(CompType, "parent");
        private static readonly PropertyInfo HolderProperty = ParentField == null ? null : AccessTools.Property(ParentField.FieldType, "holder");

        static MethodBase TargetMethod()
        {
            return CompType == null ? null : AccessTools.Method(CompType, "CompGetGizmosExtra");
        }

        private static Pawn GetMech(object instance)
        {
            object parent = ParentField?.GetValue(instance);
            return parent == null ? null : HolderProperty?.GetValue(parent) as Pawn;
        }

        public static void Postfix(object __instance, ref IEnumerable<Gizmo> __result)
        {
            try
            {
                Pawn pawn = GetMech(__instance);
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid, or not mechanical - leave GetOverseer's real behavior alone.

                __result = WrapWithOverseerSuppression(__result, pawn);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Mechanoid Upgrades MechCarrier gizmo postfix failed: " + e, 91274522);
            }
        }

        // Same technique as OverseerGizmoSuppressionPatches.WrapWithOverseerSuppression -
        // brackets each pull from the original lazy iterator, since the comp's own
        // GetOverseer check only actually runs once the caller enumerates.
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
                    if (current is Command_Action commandAction && commandAction.action != null)
                    {
                        Action original = commandAction.action;
                        commandAction.action = () => RunSuppressed(pawn, original);
                    }
                    yield return current;
                }
            }
        }

        private static void RunSuppressed(Pawn pawn, Action original)
        {
            SapientMechOverseerGizmoGuard.Suppress(pawn);
            try
            {
                original();
            }
            finally
            {
                SapientMechOverseerGizmoGuard.Unsuppress(pawn);
            }
        }
    }
}
