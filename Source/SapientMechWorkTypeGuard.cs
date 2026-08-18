using System.Collections.Generic;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Vanilla's Pawn.GetDisabledWorkTypes treats IsColonyMech==true as "restrict this
    /// pawn to RaceProps.mechEnabledWorkTypes, disable every other WorkTypeDef" -
    /// correct for a real, purpose-built mechanoid, but wrong for a sapient one, which
    /// is meant to work like any other colonist (skills/backstory/traits), not a
    /// narrow-purpose drone. That restriction is a single self-contained block inside
    /// a much larger method (backstory/trait/health disables all happen in the same
    /// method too) - rather than duplicating all of that just to omit one block, or
    /// trying to subtract its specific contribution from the result afterward (which
    /// would also strip out any legitimate backstory/trait disable that happens to
    /// land on the same WorkTypeDef), this lets Pawn_GetDisabledWorkTypes_Patch mark a
    /// pawn as "report IsColonyMech's real, unforced value for the duration of this one
    /// call" - which Pawn_IsColonyMech_Patch checks before granting its usual override,
    /// so vanilla's own mech-restriction condition naturally evaluates false and skips
    /// itself, using vanilla's own logic rather than a reimplementation of it.
    /// </summary>
    internal static class SapientMechWorkTypeGuard
    {
        private static readonly HashSet<Pawn> suppressed = new HashSet<Pawn>();

        public static bool IsSuppressed(Pawn pawn) => pawn != null && suppressed.Contains(pawn);

        public static void Suppress(Pawn pawn)
        {
            if (pawn != null)
                suppressed.Add(pawn);
        }

        public static void Unsuppress(Pawn pawn)
        {
            if (pawn != null)
                suppressed.Remove(pawn);
        }

        /// <summary>
        /// Suppresses <paramref name="pawn"/> only if it is a Big and Small sapient mechanoid
        /// currently being reported as a colony mech, returning whether it did - so a caller can
        /// unsuppress exactly what it suppressed and nothing else. A real mechanoid is never
        /// touched: its mech-only restrictions are correct and must stay.
        ///
        /// Safe to nest. The IsColonyMech read below goes through Pawn_IsColonyMech_Patch, which
        /// already honours suppression - so if an outer patch has this pawn suppressed, that read
        /// returns the real (false) value and this bails out without suppressing or, more
        /// importantly, without reporting a suppression the caller would later undo out from
        /// under the outer one.
        /// </summary>
        public static bool TrySuppress(Pawn pawn)
        {
            if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                return false;

            if (!pawn.IsColonyMech)
                return false;

            Suppress(pawn);
            return true;
        }
    }
}
