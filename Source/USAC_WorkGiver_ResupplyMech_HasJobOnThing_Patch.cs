using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Ascension Megacorp's mechs run on their own "readiness" need (Need_Readiness,
    /// backed by CompMechReadiness/CompProperties_MechReadiness, refilled with industrial
    /// components) instead of vanilla's energy bar. USAC.WorkGiver_ResupplyMech -
    /// the colonist-side work that keeps a mech topped up - rejects any target where
    /// !RaceProps.IsMechanoid, same root cause and same shape as vanilla's own
    /// WorkGiver_RepairMech (see WorkGiver_RepairMech_HasJobOnThing_Patch.cs): a sapient
    /// mech is never auto-resupplied because it no longer satisfies that check, even
    /// though CompMechReadiness/Need_Readiness themselves have no mechanoid-specific
    /// gating anywhere in their own logic (confirmed by full source read - both key off
    /// the comp's presence and the need's own CurLevel, nothing else).
    ///
    /// This replicates HasJobOnThing's remaining checks without the IsMechanoid gate, only
    /// for pawns Big and Small itself still considers mechanical - a genuine mechanoid, or
    /// anything not mechanical at all, falls straight through to the original unmodified.
    /// FindSupply is private on the original WorkGiver instance, so it's invoked
    /// reflectively rather than reimplemented.
    ///
    /// Ascension Megacorp is an optional dependency - WorkGiver_ResupplyMech/
    /// CompMechReadiness/Need_Readiness are all resolved by name at runtime and only ever
    /// invoked through cached MethodInfo or vanilla ThingComp/Need base-class members,
    /// never referenced directly in this patch's own signature (only vanilla
    /// WorkGiver_Scanner/Pawn/Thing types), so this class is entirely inert if that mod
    /// isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class USAC_WorkGiver_ResupplyMech_HasJobOnThing_Patch
    {
        private static readonly Type WorkGiverType = AccessTools.TypeByName("USAC.WorkGiver_ResupplyMech");
        private static readonly Type ReadinessCompType = AccessTools.TypeByName("USAC.CompMechReadiness");
        private static readonly Type ReadinessCompPropsType = AccessTools.TypeByName("USAC.CompProperties_MechReadiness");
        private static readonly Type ReadinessNeedType = AccessTools.TypeByName("USAC.Need_Readiness");
        private static readonly MethodInfo FindSupplyMethod = WorkGiverType == null ? null : AccessTools.Method(WorkGiverType, "FindSupply", new[] { typeof(Pawn), ReadinessCompType });
        private static readonly FieldInfo AutoResupplyField = ReadinessCompType == null ? null : AccessTools.Field(ReadinessCompType, "autoResupply");
        private static readonly FieldInfo CapacityField = ReadinessCompPropsType == null ? null : AccessTools.Field(ReadinessCompPropsType, "capacity");

        static MethodBase TargetMethod()
        {
            return WorkGiverType == null ? null : AccessTools.Method(WorkGiverType, "HasJobOnThing");
        }

        public static bool Prefix(object __instance, Pawn pawn, Thing t, bool forced, ref bool __result)
        {
            try
            {
                if (t is not Pawn mech || mech.RaceProps.IsMechanoid || !IsMechanicalCache.Get(mech))
                    return true; // Genuine mechanoid, or not mechanical by Big and Small's own reckoning either - original handles it correctly as-is.

                if (!ModLister.CheckBiotech("Repair mech") || mech.Faction != pawn.Faction)
                {
                    __result = false;
                    return false;
                }

                ThingComp readinessComp = mech.AllComps.Find(c => ReadinessCompType.IsInstanceOfType(c));
                Need readinessNeed = mech.needs?.AllNeeds.Find(n => ReadinessNeedType.IsInstanceOfType(n));
                if (readinessComp == null || readinessNeed == null || readinessNeed.CurLevelPercentage >= 1f)
                {
                    __result = false;
                    return false;
                }

                if (mech.InAggroMentalState || mech.HostileTo(pawn) || mech.IsBurning() || mech.IsAttacking()
                    || !pawn.CanReserve(t, 1, -1, null, forced))
                {
                    __result = false;
                    return false;
                }

                float capacity = (float)(CapacityField?.GetValue(readinessComp.props) ?? 100f);
                if (!forced && readinessNeed.CurLevel > capacity * 0.75f)
                {
                    __result = false;
                    return false;
                }

                bool autoResupply = (bool)(AutoResupplyField?.GetValue(readinessComp) ?? true);
                if (!forced && !autoResupply)
                {
                    __result = false;
                    return false;
                }

                object supply = FindSupplyMethod?.Invoke(__instance, new object[] { pawn, readinessComp });
                __result = supply != null;
                return false;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] USAC WorkGiver_ResupplyMech patch failed, falling back to the original: " + e, 91274514);
                return true;
            }
        }
    }
}
