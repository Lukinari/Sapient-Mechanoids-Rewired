using System;
using System.Collections;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Mechanoid Upgrades' own comp (MU.CompProperties_UpgradableMechanoid) is whitelisted
    /// (see HumanlikeAnimalSettings_MechQueens.xml), so it survives the sapience conversion
    /// as a comp TYPE - but comps aren't state-copied across RaceMorpher.SwapAnimalToSapient
    /// Version (same gap already found for [AV] Mechanoid Skins' comp), so a freshly
    /// instantiated CompUpgradableMechanoid always starts with an empty upgrades list
    /// (confirmed by decompile: Initialize() sets upgrades = new List&lt;MechUpgrade&gt;()
    /// whenever it's null). Without this, a mech's installed upgrades were silently erased
    /// the moment it became sapient - no compensation, no drop, just gone.
    ///
    /// SwapAnimalToSapientVersion(this Pawn aniPawn) is the right hook - decompiled in full:
    /// it builds a genuinely NEW Pawn (PawnGenerator.GeneratePawn), converts THAT pawn's
    /// ThingDef via SwapThingDef, then calls aniPawn.Destroy() itself before returning the
    /// new pawn - confirmed by decompile, the Destroy() call is the second-to-last line in
    /// the method, well before this Postfix ever runs. So aniPawn is already a destroyed,
    /// otherwise-unreferenced Thing by the time this patch touches it - reading its comps is
    /// still safe (Destroy() doesn't null out a Thing's own comp list), but there's no
    /// window, ever, where a save could catch both the old and new pawn holding the same
    /// upgrade, since destruction already happened before this method even returns. That's
    /// also why the old comp's upgrades list is never touched here at all (earlier versions
    /// of this patch tried to detach the reference from it - unnecessary, since nothing will
    /// ever read that list again).
    ///
    /// Only acts once __result is confirmed non-null (the swap actually succeeded) - if it's
    /// null (e.g. no humanlike counterpart registered), aniPawn was never destroyed either
    /// (see the method's own early-return paths), so nothing here needs to run.
    ///
    /// Reuses CompUpgradableMechanoid's own public AddUpgrade(MechUpgrade) rather than
    /// touching the new comp's list directly - handles the full lifecycle (OnAdded, which
    /// re-points MechUpgrade.holder and grants any ability the upgrade provides, plus cache
    /// invalidation via DirtyUpgrades()) the same way the mod's own UI does when a player
    /// installs an upgrade normally. Deliberately does NOT call the matching RemoveUpgrade
    /// anywhere - confirmed by a real crash log that calling both OnAdded and OnRemoved on
    /// the same shared MechUpgrade object breaks it: MechUpgrade.OnRemoved unconditionally
    /// calls ChangeHolder(null) (decompile-confirmed, no check for whether the given pawn is
    /// even still the current holder), which stomped the holder reference OnAdded had just
    /// set, crashing UpgradeComp_Shield.ShieldState mid-render and taking the whole map's
    /// rendering down with it.
    ///
    /// Mechanoid Upgrades is an optional dependency - resolved by name at runtime, entirely
    /// inert if that mod isn't installed, same pattern as this mod's other optional-mod
    /// integrations.
    /// </summary>
    [HarmonyPatch(typeof(RaceMorpher), nameof(RaceMorpher.SwapAnimalToSapientVersion))]
    public static class RaceMorpher_SwapAnimalToSapientVersion_MechUpgrades_Patch
    {
        private static readonly Type CompType = AccessTools.TypeByName("MU.CompUpgradableMechanoid");
        private static readonly Type UpgradeType = AccessTools.TypeByName("MU.MechUpgrade");

        private static readonly FieldInfo UpgradesField = CompType == null
            ? null
            : AccessTools.Field(CompType, "upgrades");

        private static readonly MethodInfo AddUpgradeMethod = CompType == null || UpgradeType == null
            ? null
            : AccessTools.Method(CompType, "AddUpgrade", new[] { UpgradeType });

        private static bool IsAvailable => CompType != null && UpgradesField != null && AddUpgradeMethod != null;

        private static object GetComp(Pawn pawn)
        {
            if (pawn?.AllComps == null)
                return null;

            foreach (ThingComp comp in pawn.AllComps)
            {
                if (CompType.IsInstanceOfType(comp))
                    return comp;
            }
            return null;
        }

        public static void Postfix(Pawn aniPawn, Pawn __result)
        {
            if (!IsAvailable || __result == null)
                return; // Swap didn't happen (or Mechanoid Upgrades isn't installed) - nothing to carry over.

            try
            {
                object oldComp = GetComp(aniPawn);
                if (oldComp == null)
                    return; // No upgrades comp on the original animal at all - the common case.

                if (!(UpgradesField.GetValue(oldComp) is IEnumerable upgrades))
                    return;

                object newComp = GetComp(__result);
                if (newComp == null)
                    return; // This particular sapient mech kind isn't covered by Mechanoid Upgrades.

                foreach (object upgrade in upgrades)
                {
                    // Each upgrade gets its own try/catch so one bad upgrade can't stop the
                    // rest of the batch from carrying over.
                    try
                    {
                        AddUpgradeMethod.Invoke(newComp, new[] { upgrade });
                    }
                    catch (Exception e)
                    {
                        Log.ErrorOnce("[SapientMechanoidFix] Carrying one mech upgrade over during sapience conversion failed: " + e, 91274566);
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Carrying mech upgrades over during sapience conversion failed: " + e, 91274563);
            }
        }
    }
}
