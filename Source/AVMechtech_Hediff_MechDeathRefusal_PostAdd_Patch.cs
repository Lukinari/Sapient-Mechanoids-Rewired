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
    /// [AV] Mechtech's Reshaper mech bonds with a "sinistre" that lets it reform after
    /// death - implemented as a hediff, AV_SinistreMechDeathRefusal (class
    /// AV_Mechtech.Hediff_MechDeathRefusal). The same hediff is also what "sinistre
    /// essence" grants to ANY mechanoid the player uses it on (see
    /// CompUsableSinistreEssence.OnUsed), not just the Reshaper - so this isn't just a
    /// Reshaper-specific fix, it's the "grant death-refusal" mechanic for any sapient
    /// mechanoid from any mod.
    ///
    /// Its PostAdd is a straight `if (!ModLister.CheckAnomaly(...) ||
    /// !pawn.RaceProps.IsMechanoid) { pawn.health.RemoveHediff(this); return; }` - on a
    /// sapient mech RaceProps.IsMechanoid is false, so the hediff is silently stripped off
    /// the instant it's added, before it ever does anything.
    ///
    /// PostMake (and therefore InitializeComps/CompPostMake) already ran before PostAdd is
    /// ever called, so `comps` is already populated by this point - the only things the
    /// skipped base-class chain would otherwise do are Hediff.PostAdd's `tickAdded =
    /// Find.TickManager.TicksGame` (the def has no abilities/removeWithTags, so the rest of
    /// that method is a no-op for this specific hediff) and HediffWithComps.PostAdd's
    /// `comps[i].CompPostPostAdd(dinfo)` loop - both reimplemented directly below, using
    /// only vanilla HediffWithComps/HediffComp members. usesLeft/overseer are private to
    /// the optional-mod subclass, so those two are set via cached reflection instead.
    ///
    /// [AV] Mechtech is an optional dependency - Hediff_MechDeathRefusal is resolved by
    /// name at runtime and only ever invoked through cached FieldInfo/reflection, never
    /// referenced directly in this patch's own signature (only vanilla
    /// HediffWithComps/Pawn types), so this class is entirely inert if that mod isn't
    /// installed.
    /// </summary>
    [HarmonyPatch]
    public static class AVMechtech_Hediff_MechDeathRefusal_PostAdd_Patch
    {
        private static readonly Type HediffType = AccessTools.TypeByName("AV_Mechtech.Hediff_MechDeathRefusal");
        private static readonly FieldInfo UsesLeftField = HediffType == null ? null : AccessTools.Field(HediffType, "usesLeft");
        private static readonly FieldInfo OverseerField = HediffType == null ? null : AccessTools.Field(HediffType, "overseer");

        static MethodBase TargetMethod()
        {
            return HediffType == null ? null : AccessTools.Method(HediffType, "PostAdd");
        }

        public static bool Prefix(HediffWithComps __instance, DamageInfo? dinfo)
        {
            try
            {
                Pawn pawn = __instance.pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return true; // Real mechanoid (or something else entirely) - original check is correct, run it.

                if (!ModLister.CheckAnomaly("Death refusal"))
                    return true; // Anomaly not active - original's other removal condition still applies.

                __instance.tickAdded = Find.TickManager.TicksGame;
                List<HediffComp> comps = __instance.comps;
                if (comps != null)
                {
                    foreach (HediffComp comp in comps)
                        comp.CompPostPostAdd(dinfo);
                }

                UsesLeftField?.SetValue(__instance, 1);
                OverseerField?.SetValue(__instance, MechanitorUtility.GetOverseer(pawn));
                pawn.Drawer.renderer.SetAllGraphicsDirty();

                return false; // Skip the original - we ran its "good path" ourselves.
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] AV Mechtech Hediff_MechDeathRefusal.PostAdd patch failed: " + e, 91274512);
                return true;
            }
        }
    }
}
