using System;
using System.Linq;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Mechanoid Upgrades' whole "walk this mech into the Mech Upgrader building" float
    /// menu option checks `pawn.RaceProps.IsMechanoid` directly to decide whether the
    /// mech is even eligible - false for every sapient mech, so the option to configure
    /// upgrades never appears at all. This is the single entry point for the mod's core
    /// interaction, so without this fix a sapient mech can't be upgraded through it in
    /// any way.
    ///
    /// Mechanoid Upgrades is an optional dependency - FloatMenuOptionProvider_EnterUpgrader
    /// and CompUpgradableMechanoid are resolved by name at runtime and only ever invoked
    /// through cached MethodInfo/Type checks, never referenced directly in this patch's
    /// own signature (only vanilla Pawn/FloatMenuContext types), so this class is entirely
    /// inert if that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class MU_FloatMenuOptionProvider_EnterUpgrader_AppliesInt_Patch
    {
        private static readonly Type ProviderType = AccessTools.TypeByName("MU.FloatMenuOptionProvider_EnterUpgrader");
        private static readonly Type UpgradableCompType = AccessTools.TypeByName("MU.CompUpgradableMechanoid");

        static MethodBase TargetMethod()
        {
            return ProviderType == null ? null : AccessTools.Method(ProviderType, "AppliesInt");
        }

        public static void Postfix(FloatMenuContext context, ref bool __result)
        {
            try
            {
                if (__result || UpgradableCompType == null)
                    return;

                Pawn pawn = context?.FirstSelectedPawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return; // Real mechanoid (original already handles it), or not mechanical at all.

                if (pawn.AllComps.Any(c => UpgradableCompType.IsInstanceOfType(c)))
                    __result = true;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Mechanoid Upgrades EnterUpgrader float menu patch failed: " + e, 91274521);
            }
        }
    }
}
