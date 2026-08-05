using System;
using BigAndSmall;
using RimWorld;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Vanilla's own Pawn.OverseerSubject getter (Assembly-CSharp, Verse.Pawn) only ever
    /// calls GetComp&lt;CompOverseerSubject&gt;() - and caches the result - when
    /// RaceProps.IsMechanoid is true:
    ///
    ///   if (ModsConfig.BiotechActive &amp;&amp; overseerSubject == null &amp;&amp; RaceProps.IsMechanoid)
    ///       overseerSubject = GetComp&lt;CompOverseerSubject&gt;();
    ///   return overseerSubject;
    ///
    /// A sapient mech's RaceProps.IsMechanoid is false (see Pawn_IsColonyMech_Patch for
    /// why), so this returns null forever even though CompProperties_OverseerSubject is
    /// whitelisted and the comp is genuinely present - CompOverseerSubject_State_Patch's
    /// own Postfix never even runs, because you can't call a getter on a null reference.
    /// This was the missing call site: confirmed via ilspycmd against
    /// RimWorldWin64_Data/Managed/Assembly-CSharp.dll after Mechanoid Upgrades'
    /// Building_MechUpgrader.CanAcceptPawn NRE'd on selPawn.OverseerSubject.State.
    ///
    /// Postfixing the property directly (rather than patching some deeper call site)
    /// fixes every caller at once, including vanilla's own IsColonyMechRequiringMechanitor
    /// logic and any other mod that reads pawn.OverseerSubject. Deliberately doesn't try
    /// to populate vanilla's private overseerSubject cache field - GetComp's own list scan
    /// is cheap and OverseerSubject isn't a hot per-tick path, so a fresh lookup per call
    /// here is simpler and safer than reflection into a private field.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.OverseerSubject), MethodType.Getter)]
    public static class Pawn_OverseerSubject_Patch
    {
        public static void Postfix(Pawn __instance, ref CompOverseerSubject __result)
        {
            try
            {
                if (__result != null || __instance == null)
                    return;

                if (!ModsConfig.BiotechActive || __instance.RaceProps.IsMechanoid || !IsMechanicalCache.Get(__instance))
                    return; // Real mechanoid (vanilla's own result is already correct), or not mechanical at all.

                __result = __instance.GetComp<CompOverseerSubject>();
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Pawn.OverseerSubject patch failed: " + e, 91274530);
            }
        }
    }
}
