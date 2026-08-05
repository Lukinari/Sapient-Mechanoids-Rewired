using System.Runtime.CompilerServices;
using BigAndSmall;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// BigAndSmall.RaceHelper.IsMechanical(Pawn) - the guard clause at the top of nearly
    /// every patch in this mod - is not a cheap flag read. It calls GetAllPawnExtensions(),
    /// which scans the pawn's hediffs, apparel, active genes, active traits, kindDef,
    /// ThingDef, and royal titles for PawnExtensions, concatenates the results, then
    /// filters by exclusion tags with an OrderBy().ToList() followed by a nested
    /// Where().ToList() inside a loop - real allocations, on every single call, confirmed
    /// by decompiling BigAndSmall.dll.
    ///
    /// Several of this mod's patches sit on vanilla members read constantly for EVERY
    /// pawn in the colony, not just mechanical ones - Pawn.IsColonyMech and
    /// HediffSet.PainTotal in particular. Since RaceProps.IsMechanoid is checked first and
    /// is false for both real humans and sapient mechs alike (that's the whole premise of
    /// the sapience conversion), IsMechanical() ends up evaluated for every ordinary human
    /// colonist too, every time those members are read - paying the full extension-scan
    /// cost for pawns that were never going to be mechanical. Reported as recurring
    /// stutters even after removing two unrelated forgotten diagnostic patches.
    ///
    /// Whether Big and Small considers a given pawn "mechanical" is effectively fixed for
    /// that pawn's whole life once it exists - the flag comes from a PawnExtension on its
    /// kindDef/ThingDef (set once, at generation time), not from anything that toggles
    /// mid-game. Caching per pawn instance is safe; a short refresh window is kept anyway
    /// as a defensive margin against extension sources this mod doesn't control (apparel/
    /// gene/hediff-sourced PawnExtensions) ever changing after the fact. Keyed through a
    /// ConditionalWeakTable so entries for despawned/dead pawns are collected automatically
    /// rather than needing manual cleanup.
    ///
    /// The refresh window itself is a mod setting (SapientMechanoidFixSettings.
    /// isMechanicalCacheRefreshTicks) rather than a fixed constant - with many sapient
    /// mechanoids active in one colony, even this cached path still means more total
    /// re-checks per unit of game time than a colony with just one or two, so a player
    /// running an unusually mechanoid-heavy colony can widen the window further than the
    /// default if they're still seeing stutter, at the cost of taking slightly longer to
    /// notice if a pawn's mechanical status ever genuinely changes.
    ///
    /// A cached `true` result is never re-checked at all, refresh window or not - once Big
    /// and Small considers a pawn mechanical, nothing in this mod's scope ever converts it
    /// back to purely organic, so continuing to re-scan it would just be wasted work.
    ///
    /// A cached `false` result still honors the refresh window by default, since an
    /// ordinary human colonist could in principle pick up a mechanical PawnExtension later
    /// (apparel, a gene, a hediff - some other mod's doing, not this one's) and this mod
    /// has no way to know that happened without re-checking. A player who knows nothing in
    /// their mod list ever converts an organic pawn into a mechanical one can turn on
    /// SapientMechanoidFixSettings.freezeNonMechanicalCache to freeze `false` results too,
    /// permanently - the common case (most of a colony) never gets rescanned again after
    /// its first check. Off by default, since it's a correctness tradeoff rather than a
    /// free win.
    /// </summary>
    public static class IsMechanicalCache
    {
        private const int DefaultRefreshIntervalTicks = 250;

        private static int RefreshIntervalTicks => SapientMechanoidFixMod.Settings?.isMechanicalCacheRefreshTicks ?? DefaultRefreshIntervalTicks;

        private static bool FreezeNonMechanicalCache => SapientMechanoidFixMod.Settings?.freezeNonMechanicalCache ?? false;

        private sealed class Entry
        {
            public bool Value;
            public int ComputedTick;
        }

        private static readonly ConditionalWeakTable<Pawn, Entry> Cache = new ConditionalWeakTable<Pawn, Entry>();

        public static bool Get(Pawn pawn)
        {
            if (pawn == null)
                return false;

            if (Cache.TryGetValue(pawn, out Entry entry))
            {
                if (entry.Value)
                    return true; // Mechanical status doesn't revert - no need to ever re-check a confirmed `true`.

                if (FreezeNonMechanicalCache)
                    return false;

                int currentTick = Find.TickManager?.TicksGame ?? 0;
                if (currentTick - entry.ComputedTick < RefreshIntervalTicks)
                    return false;

                entry.Value = pawn.IsMechanical();
                entry.ComputedTick = currentTick;
                return entry.Value;
            }

            bool result = pawn.IsMechanical();
            Cache.Add(pawn, new Entry { Value = result, ComputedTick = Find.TickManager?.TicksGame ?? 0 });
            return result;
        }
    }
}
