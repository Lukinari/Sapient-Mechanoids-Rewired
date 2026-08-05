using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Glitterworld Destroyer 5's Firefly cloaks "surrounding mechanoids" by scanning
    /// nearby allies and filtering on x.RaceProps.IsMechanoid directly (see
    /// GD3/Mechanoid/CompPawnInvisibility.cs) - the same recurring pattern as
    /// everywhere else in this mod, just as a LINQ filter inside CompTick rather than a
    /// simple guard clause. A sapient mech standing next to a Firefly never gets cloaked,
    /// because it no longer passes that check. Confirmed by testing: the Firefly's
    /// invisibility toggle does nothing for nearby sapient mechs.
    ///
    /// (Real Fireflies also never cloak themselves via this same loop - it explicitly
    /// excludes its own defName - so that part isn't a sapient-specific bug, just how
    /// the ability has always worked.)
    ///
    /// CompTick's own gating (abilityActivate, cooldown via readyToUseTicks, EMP stun,
    /// low energy) is private/inlined and not worth duplicating - instead this detects
    /// whether the original's block actually ran THIS tick by comparing readyToUseTicks
    /// before and after (only the inner block reassigns it), then does a second, narrower
    /// pass over exactly the pawns the original's IsMechanoid filter would have skipped,
    /// applying the same hediff the same way.
    ///
    /// Glitterworld Destroyer 5 is an optional dependency - CompPawnInvisibility is
    /// resolved by name at runtime and only ever invoked through cached
    /// MethodInfo/FieldInfo, never referenced directly in this patch's own signature
    /// (only vanilla ThingComp/Pawn types), so this class is entirely inert if that mod
    /// isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class GD3_CompPawnInvisibility_CompTick_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("GD3.CompPawnInvisibility");
        private static readonly FieldInfo ReadyToUseTicksField = CompType == null ? null : AccessTools.Field(CompType, "readyToUseTicks");
        private static readonly PropertyInfo PropsProperty = CompType == null ? null : AccessTools.Property(CompType, "Props");

        // Props' declared return type is fixed - resolving these two fields off it once
        // here (rather than off props.GetType() on every Postfix call, as a first version
        // of this patch did) avoids a fresh reflection lookup every time the Firefly's
        // invisibility pulse fires, on top of the per-map-pawn scan already below.
        private static readonly FieldInfo MaxDistanceField = PropsProperty == null ? null : AccessTools.Field(PropsProperty.PropertyType, "maxDistance");
        private static readonly FieldInfo HediffToAddField = PropsProperty == null ? null : AccessTools.Field(PropsProperty.PropertyType, "hediffToAdd");

        static MethodBase TargetMethod()
        {
            return CompType == null ? null : AccessTools.Method(CompType, "CompTick");
        }

        public static void Prefix(object __instance, out int __state)
        {
            __state = -1;
            try
            {
                if (ReadyToUseTicksField != null)
                    __state = (int)ReadyToUseTicksField.GetValue(__instance);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] GD3 CompPawnInvisibility prefix failed: " + e, 91274500);
            }
        }

        public static void Postfix(ThingComp __instance, int __state)
        {
            try
            {
                if (ReadyToUseTicksField == null || PropsProperty == null || __state < 0)
                    return;

                int newReadyToUseTicks = (int)ReadyToUseTicksField.GetValue(__instance);
                if (newReadyToUseTicks == __state)
                    return; // Original's inner block didn't fire this tick - nothing to mirror.

                Pawn caster = __instance.parent as Pawn;
                if (caster == null || caster.Faction == null)
                    return;

                if (MaxDistanceField == null || HediffToAddField == null)
                    return;

                object props = PropsProperty.GetValue(__instance);
                float maxDistance = (float)MaxDistanceField.GetValue(props);
                HediffDef hediffToAdd = (HediffDef)HediffToAddField.GetValue(props);
                if (hediffToAdd == null)
                    return;

                foreach (Pawn other in caster.Map.mapPawns.AllPawns)
                {
                    if (other == caster || other.Faction != caster.Faction)
                        continue;
                    if (other.RaceProps.IsMechanoid || !IsMechanicalCache.Get(other))
                        continue; // Real mechanoid (already handled by the original loop), or not mechanical at all.
                    if (other.Position.DistanceTo(caster.Position) >= maxDistance)
                        continue;

                    Hediff existing = other.health?.hediffSet?.GetFirstHediffOfDef(hediffToAdd);
                    if (existing != null)
                    {
                        HediffComp_Disappears disappears = existing.TryGetComp<HediffComp_Disappears>();
                        if (disappears != null)
                            disappears.ticksToDisappear = 600;
                    }
                    else
                    {
                        Hediff hediff = HediffMaker.MakeHediff(hediffToAdd, other);
                        HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
                        if (disappears != null)
                            disappears.ticksToDisappear = 600;
                        other.health.AddHediff(hediff);
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] GD3 CompPawnInvisibility postfix failed: " + e, 91274501);
            }
        }
    }
}
