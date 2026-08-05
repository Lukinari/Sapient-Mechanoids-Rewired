using System;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Glitterworld Destroyer 5's Cataphract Centipede draws a battery-level icon while
    /// drafted via CompDrawBattery.PostDraw -> GetTexturePath, which dereferences
    /// pawn.needs.energy.CurLevelPercentage with no null check. A sapient mech has no
    /// mech energy need at all, so this throws a NullReferenceException every single
    /// frame the pawn is drafted - confirmed by reading GD3's own source
    /// (GD3/Mechanoid/CompDrawBattery.cs), not just theorized.
    ///
    /// The icon itself is meaningless without an energy need to report on, so this skips
    /// the whole draw call for a sapient mech rather than trying to patch around the
    /// null - same reasoning as leaving Firefly's invisibility ability free of an energy
    /// cost when it has no energy need to spend.
    ///
    /// Glitterworld Destroyer 5 is an optional dependency - CompDrawBattery is resolved
    /// by name at runtime and only ever invoked through cached MethodInfo, never
    /// referenced directly in this patch's own signature (only vanilla ThingComp/Pawn
    /// types), so this class is entirely inert if that mod isn't installed.
    /// </summary>
    [HarmonyPatch]
    public static class GD3_CompDrawBattery_PostDraw_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("GD3.CompDrawBattery");

        static MethodBase TargetMethod()
        {
            return CompType == null ? null : AccessTools.Method(CompType, "PostDraw");
        }

        public static bool Prefix(ThingComp __instance)
        {
            try
            {
                Pawn pawn = __instance.parent as Pawn;
                if (pawn == null || pawn.RaceProps.IsMechanoid || !IsMechanicalCache.Get(pawn))
                    return true; // Real mechanoid (has needs.energy, draws fine), or not mechanical - run original.

                return pawn.needs.energy != null; // Sapient mech: skip unless it somehow still has the need.
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] GD3 CompDrawBattery guard failed: " + e, 91274495);
                return true;
            }
        }
    }
}
