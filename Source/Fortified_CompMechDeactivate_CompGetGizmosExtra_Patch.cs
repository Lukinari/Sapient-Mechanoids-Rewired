using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Fortified Feature Framework's CompMechDeactivate ("shut this mech down into a
    /// mech capsule building" gizmo) gates on pawn.RaceProps.IsMechanoid directly,
    /// alongside requiring ModExtension_MechCapsule on the pawn's def/kindDef - always
    /// false for a sapient pawn (Big and Small clears FleshType away from Mechanoid).
    ///
    /// Currently unwired: no Dead Man's Switch ThingDef uses ModExtension_MechCapsule,
    /// so this never actually triggers today - fixed preemptively in case another
    /// Fortified-consuming mod does. Fortified is an optional dependency - resolved by
    /// name at runtime, never referenced directly in this Postfix's own signature
    /// (only vanilla ThingComp/Gizmo types plus reflection for the two Fortified-only
    /// pieces: the DeactivateMech method call and the ModExtension_MechCapsule type
    /// check), so this class is entirely inert if that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class Fortified_CompMechDeactivate_CompGetGizmosExtra_Patch
    {
        private static readonly MethodInfo DeactivateMechMethod = ResolveDeactivateMechMethod();

        private static MethodInfo ResolveDeactivateMechMethod()
        {
            Type type = AccessTools.TypeByName("Fortified.MechCapsuleUtility");
            return type == null ? null : AccessTools.Method(type, "DeactivateMech", new[] { typeof(Pawn) });
        }

        static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("Fortified.CompMechDeactivate");
            return type == null ? null : AccessTools.Method(type, "CompGetGizmosExtra");
        }

        public static void Postfix(ThingComp __instance, ref IEnumerable<Gizmo> __result)
        {
            try
            {
                Pawn pawn = __instance.parent as Pawn;
                if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Faction != Faction.OfPlayer)
                    return; // Matches the original's other gates - not our concern regardless of mechanoid status.

                if (pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid (original already handles it correctly), or not mechanical at all.

                if (DeactivateMechMethod == null || !(HasMechCapsuleExtension(pawn.def) || HasMechCapsuleExtension(pawn.kindDef)))
                    return; // Doesn't qualify for the gizmo even under the original's other rules.

                __result = __result.Concat(new[] { BuildGizmo(pawn) });
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Fortified CompMechDeactivate gizmo postfix failed: " + e, 91274450);
            }
        }

        private static bool HasMechCapsuleExtension(Def def)
        {
            return def?.modExtensions != null && def.modExtensions.Any(e => e.GetType().FullName == "Fortified.ModExtension_MechCapsule");
        }

        private static Gizmo BuildGizmo(Pawn pawn)
        {
            return new Command_Action
            {
                defaultLabel = "FFF.DeactivateMech".Translate(),
                defaultDesc = "FFF.DeactivateMechDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject", true),
                action = delegate
                {
                    try
                    {
                        object result = DeactivateMechMethod.Invoke(null, new object[] { pawn });
                        if (result is Thing capsule)
                            Messages.Message("FFF.MechDeactivated".Translate(pawn.LabelCap), capsule, MessageTypeDefOf.NeutralEvent);
                    }
                    catch (Exception e)
                    {
                        Log.ErrorOnce("[SapientMechanoidFix] Fortified DeactivateMech action failed: " + e, 91274451);
                    }
                }
            };
        }
    }
}
