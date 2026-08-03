using System;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SapientMechanoidFix
{
    public class CompProperties_SapientMechReactiveArmor : CompProperties
    {
        public CompProperties_SapientMechReactiveArmor()
        {
            compClass = typeof(CompSapientMechReactiveArmor);
        }
    }

    /// <summary>
    /// Ports "Mechanoids: Total Warfare"'s steel-scaled reactive armor
    /// (CompFuelBasedDamageReduction, in that mod's NCLvsTW.dll) for our sapient War
    /// Queen and Work Queen: damage taken is reduced to 50% at a full steel reserve,
    /// rising to 150% when the reserve is empty, linearly interpolated in between -
    /// exactly the formula and thresholds that mod uses.
    ///
    /// Attached (see Patches/ReactiveArmor.xml) to the same original ThingDefs already
    /// whitelisted for sapient conversion - Mech_Warqueen and AV_Mech_Workerqueen - so
    /// it flows through the same clone/AddMissingComps pipeline as the steel reserve
    /// comps themselves. Deliberately scoped to sapient mechs only via IsActive: a
    /// non-sapient War Queen or Work Queen carries this comp but it's always inert,
    /// since real mechanoids already have their own vanilla/AV Framework behavior and
    /// this is purely a sapient-mech feature addition, not a regression fix.
    /// </summary>
    public class CompSapientMechReactiveArmor : ThingComp
    {
        private bool IsActive
        {
            get
            {
                Pawn pawn = parent as Pawn;
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                    return false;
                if (pawn.RaceProps.IsMechanoid || !pawn.IsMechanical())
                    return false; // Real mechanoid, or not mechanical at all - not ours to touch.

                return GetPercentageFull() != null;
            }
        }

        private float? GetPercentageFull()
        {
            try
            {
                CompMechCarrier vanillaCarrier = parent.GetComp<CompMechCarrier>();
                if (vanillaCarrier != null)
                    return vanillaCarrier.PercentageFull;

                foreach (ThingComp comp in parent.AllComps)
                {
                    if (comp?.GetType().FullName != "AV_Framework.CompMechReloadableResourceHolder")
                        continue;

                    return Traverse.Create(comp).Property("PercentageFull").GetValue<float>();
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Failed to read steel reserve percentage for reactive armor: " + e, 91274443);
            }

            return null;
        }

        private float CurrentDamageFactor
        {
            get
            {
                float? percentageFull = GetPercentageFull();
                if (percentageFull == null)
                    return 1f;

                float fuelPercentage = Mathf.Clamp01(percentageFull.Value);
                if (fuelPercentage >= 0.5f)
                    return Mathf.Lerp(1f, 0.5f, (fuelPercentage - 0.5f) * 2f);
                return Mathf.Lerp(1.5f, 1f, fuelPercentage * 2f);
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(ref dinfo, out absorbed);
            try
            {
                if (absorbed || !IsActive)
                    return;

                dinfo.SetAmount(dinfo.Amount * CurrentDamageFactor);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Reactive armor damage scaling failed: " + e, 91274444);
            }
        }
    }
}
