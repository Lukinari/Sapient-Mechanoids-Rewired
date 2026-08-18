using System;
using System.Runtime.CompilerServices;
using BigAndSmall;
using UnityEngine;
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
    /// its confirmed check. Off by default, since it's a correctness tradeoff rather than a
    /// free win.
    ///
    /// The very first check for any given pawn is never eligible for that freeze, even with
    /// the setting on - a freshly-converted (or freshly-loaded, since a save load
    /// deserializes a brand new Pawn instance with no existing cache entry) sapient
    /// mechanoid's first-ever check landing on `false` gets one guaranteed recheck before
    /// it can stick permanently.
    ///
    /// IsMechanical() can also throw outright rather than returning a value: observed in a
    /// player log as a NullReferenceException raised inside BigAndSmall.ModExtHelper.
    /// ExtensionsOnDef, reached through GetAllPawnExtensions, for one particular pawn. The
    /// throw is caught here rather than left to each calling patch's own guard, because the
    /// original shape of this method only wrote a cache entry once IsMechanical() had
    /// returned - so a pawn that made it throw was never cached at all, and every subsequent
    /// call re-entered Big and Small and threw again. Since the callers include members read
    /// for every pawn every frame, that meant an exception throw plus a full Harmony stack
    /// walk per call, indefinitely, while the mod's own Log.ErrorOnce reported it a single
    /// time and hid how often it was really firing.
    ///
    /// A throw is therefore treated as "not mechanical" and cached like any other result, so
    /// the cost is bounded to one throw per refresh window instead of one per call. The entry
    /// is deliberately left unconfirmed: a failure is not evidence about the pawn, so it must
    /// never become eligible for the freezeNonMechanicalCache shortcut, or a transient fault
    /// inside another mod would permanently mark a genuinely mechanical pawn as organic.
    ///
    /// The refresh window is measured against Time.realtimeSinceStartup (real wall-clock
    /// seconds), not Find.TickManager.TicksGame - confirmed via a real bug: TicksGame
    /// freezes entirely while the game is paused, so a wrong `false` cached the moment
    /// before a pause (e.g. a sapient War Queen checked a tick before Big and Small finished
    /// attaching its mechanical-marker hediff) could never self-correct for as long as the
    /// player stayed paused, no matter how long that was in real time - and inspecting a
    /// pawn's gizmos to see whether a fix worked is exactly when a player is most likely to
    /// be paused. The cache's entire purpose is bounding real per-frame CPU cost, not
    /// simulated game time, so real time is the correct thing to gate on regardless.
    /// </summary>
    public static class IsMechanicalCache
    {
        private const int DefaultRefreshIntervalTicks = 250;

        private const float TicksPerSecondAt1x = 60f;

        private static float RefreshIntervalSeconds => (SapientMechanoidFixMod.Settings?.isMechanicalCacheRefreshTicks ?? DefaultRefreshIntervalTicks) / TicksPerSecondAt1x;

        private static bool FreezeNonMechanicalCache => SapientMechanoidFixMod.Settings?.freezeNonMechanicalCache ?? false;

        private sealed class Entry
        {
            public bool Value;
            public float ComputedRealTime;
            public bool Confirmed; // Survived at least one refresh cycle - only then is a `false` freeze-eligible.
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

                if (FreezeNonMechanicalCache && entry.Confirmed)
                    return false;

                float now = Time.realtimeSinceStartup;
                if (now - entry.ComputedRealTime < RefreshIntervalSeconds)
                    return false; // Not due for a recheck yet - including the grace period before a first `false` can freeze.

                bool evaluated = TryEvaluate(pawn, out bool refreshed);

                entry.Value = refreshed;

                // Stamped even when the evaluation threw - that's what bounds a throwing pawn
                // to one exception per refresh window rather than one per call.
                entry.ComputedRealTime = now;

                // Only a real answer confirms the entry. A throw leaves it unconfirmed so it
                // can never be frozen by freezeNonMechanicalCache.
                if (evaluated)
                    entry.Confirmed = true;

                return entry.Value;
            }

            TryEvaluate(pawn, out bool result);
            Cache.Add(pawn, new Entry { Value = result, ComputedRealTime = Time.realtimeSinceStartup, Confirmed = false });
            return result;
        }

        /// <summary>
        /// Calls Big and Small's IsMechanical, reporting whether it produced an answer at all.
        /// Returns false with <paramref name="result"/> set to false when it throws, so the
        /// caller can cache the fallback without ever treating it as a confirmed result.
        /// </summary>
        private static bool TryEvaluate(Pawn pawn, out bool result)
        {
            try
            {
                result = pawn.IsMechanical();
                return true;
            }
            catch (Exception e)
            {
                result = false;
                Log.ErrorOnce("[SapientMechanoidFix] Big and Small's IsMechanical threw for " +
                              (pawn.def?.defName ?? "an unknown pawn") + " - treating it as non-mechanical " +
                              "and caching that, so the check is not retried on every call: " + e, 91274567);
                return false;
            }
        }
    }
}
