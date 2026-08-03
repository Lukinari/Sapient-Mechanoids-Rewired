using System;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Vanilla's WorkGiver_RepairMech.HasJobOnThing rejects any target where
    /// !RaceProps.IsMechanoid or needs.energy == null - both of which are true for
    /// every Big and Small sapient mechanoid by design (RaceProps.IsMechanoid is
    /// deliberately cleared for sapience, and a sapient mech runs on ordinary colonist
    /// needs - food etc - not a power bar). MechRepairUtility.RepairTick, the method
    /// that actually performs the repair, never touches needs.energy at all - it just
    /// heals hediffs or regrows a missing weapon - so neither check reflects something
    /// the repair process itself needs, they're just gates written assuming "mechanoid"
    /// always means "has a power bar."
    ///
    /// This replicates HasJobOnThing's remaining checks without those two, but only for
    /// pawns Big and Small itself still considers mechanical (RaceHelper.IsMechanical) -
    /// a genuine mechanoid whose RaceProps.IsMechanoid is still true, or anything not
    /// mechanical at all, falls straight through to vanilla's own unmodified check.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_RepairMech), nameof(WorkGiver_RepairMech.HasJobOnThing))]
    public static class WorkGiver_RepairMech_HasJobOnThing_Patch
    {
        public static bool Prefix(Pawn pawn, Thing t, bool forced, ref bool __result)
        {
            try
            {
                if (t is not Pawn mech || mech.RaceProps.IsMechanoid || !mech.IsMechanical())
                    return true; // Genuine mechanoid, or not mechanical by Big and Small's own reckoning either - vanilla handles it correctly as-is.

                if (!ModLister.CheckBiotech("Repair mech"))
                {
                    __result = false;
                    return false;
                }

                CompMechRepairable compMechRepairable = t.TryGetComp<CompMechRepairable>();
                if (compMechRepairable == null || mech.InAggroMentalState || mech.HostileTo(pawn)
                    || !pawn.CanReserve(t, 1, -1, null, forced) || mech.IsBurning() || mech.IsAttacking()
                    || !MechRepairUtility.CanRepair(mech))
                {
                    __result = false;
                    return false;
                }

                __result = forced || compMechRepairable.autoRepair;
                return false;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] WorkGiver_RepairMech patch failed, falling back to vanilla: " + e, 91274430);
                return true;
            }
        }
    }
}
