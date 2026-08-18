using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Stops a sapient mechanoid crewing a gravship from cancelling its own launch.
    ///
    /// The launch ritual sorts every pawn on the map into cargo before it starts. With "board
    /// colony mechanoids" ticked, anything reporting IsColonyMech is added to the engine's
    /// pawnsToBoard set (decompiled, RitualBehaviorWorker_GravshipLaunch.TryExecuteOn). Later,
    /// JobGiver_BoardOrLeaveGravship finds each of those pawns and sends it to a spot on the ship -
    /// and if such a pawn is also in the ritual's Lord, it calls lordJob_Ritual.Cancel() outright.
    ///
    /// Vanilla is right to do that, because it assumes a colony mech is freight and never a
    /// participant, so a pawn that is somehow both must be a mistake worth aborting for. A sapient
    /// mechanoid is genuinely both: mech enough for the boarding sweep to claim it, and a colonist
    /// entitled to hold a ritual role. Assigning one to any slot therefore cancelled the launch the
    /// instant it began - reported as the ritual window refusing to even close.
    ///
    /// The fix is to leave participants out of the cargo manifest. They still board: they walk to
    /// the substructure as part of the ritual, exactly as every human crew member does, and the
    /// end trigger waits for them either way. Only participants are excluded, so a sapient
    /// mechanoid that is not crewing still boards as freight and is not left behind - which is the
    /// one case that already worked and must keep working.
    ///
    /// Suppression rather than a transpiler: SapientMechWorkTypeGuard makes IsColonyMech report its
    /// real, unforced value for the duration of the call, so vanilla's own condition evaluates
    /// false and skips the pawn using vanilla's own logic. The same approach GenConstruct and
    /// GrowerSow use, and it leaves the boarding sweep otherwise untouched.
    /// </summary>
    [HarmonyPatch(typeof(RitualBehaviorWorker_GravshipLaunch),
        nameof(RitualBehaviorWorker_GravshipLaunch.TryExecuteOn))]
    public static class RitualBehaviorWorker_GravshipLaunch_TryExecuteOn_Patch
    {
        public static void Prefix(RitualRoleAssignments assignments, out List<Pawn> __state)
        {
            __state = null;

            try
            {
                if (assignments?.Participants == null)
                    return;

                List<Pawn> suppressed = null;
                foreach (Pawn participant in assignments.Participants)
                {
                    if (!SapientMechWorkTypeGuard.TrySuppress(participant))
                        continue;

                    if (suppressed == null)
                        suppressed = new List<Pawn>();

                    suppressed.Add(participant);
                }

                __state = suppressed;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Gravship launch prefix failed: " + e, 91274570);
            }
        }

        /// <summary>
        /// A finalizer rather than a postfix, so the suppression is lifted even if the original -
        /// or another mod's patch on it - throws. Leaving a pawn suppressed would silently disable
        /// every other fix in this mod for it, for the rest of the session.
        /// </summary>
        public static Exception Finalizer(List<Pawn> __state, Exception __exception)
        {
            if (__state != null)
            {
                for (int i = 0; i < __state.Count; i++)
                    SapientMechWorkTypeGuard.Unsuppress(__state[i]);
            }

            return __exception;
        }
    }
}
