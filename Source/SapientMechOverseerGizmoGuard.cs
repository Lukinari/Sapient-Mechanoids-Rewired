using System.Collections.Generic;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Narrow suppression flag, set only around the enumeration of a single sapient
    /// mech's gizmo-producing iterator methods (see OverseerGizmoSuppressionPatches), so
    /// MechanitorUtility_GetOverseer_Patch's fallback only ever fires inside that
    /// exact window - never for any other caller of MechanitorUtility.GetOverseer.
    /// </summary>
    internal static class SapientMechOverseerGizmoGuard
    {
        private static readonly HashSet<Pawn> suppressed = new HashSet<Pawn>();

        public static bool IsSuppressed(Pawn pawn) => pawn != null && suppressed.Contains(pawn);

        public static void Suppress(Pawn pawn)
        {
            if (pawn != null) suppressed.Add(pawn);
        }

        public static void Unsuppress(Pawn pawn)
        {
            if (pawn != null) suppressed.Remove(pawn);
        }
    }
}
