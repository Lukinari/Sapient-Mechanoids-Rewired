using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Without this, a sapient mech at full health (no injuries, no missing weapon)
    /// never gets assigned a repair job at all - see WorkGiver_RepairMech_HasJobOnThing_Patch,
    /// which calls this same vanilla method - so once its reactive-armor charges run
    /// out, they'd never recharge. Mirrors Glitterworld Destroyer 5's own
    /// MechRepairUtility.CanRepair prefix: keep the mechanitor topping it up purely to
    /// refill charges, same as vanilla already does for actual injuries.
    /// </summary>
    [HarmonyPatch(typeof(MechRepairUtility), nameof(MechRepairUtility.CanRepair))]
    public static class MechRepairUtility_CanRepair_Patch
    {
        public static bool Prefix(Pawn mech, ref bool __result)
        {
            try
            {
                CompSapientMechHitArmor comp = mech?.GetComp<CompSapientMechHitArmor>();
                if (comp != null && comp.CanRepair)
                {
                    __result = true;
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Reactive armor CanRepair patch failed, falling back to vanilla: " + e, 91274448);
            }

            return true;
        }
    }
}
