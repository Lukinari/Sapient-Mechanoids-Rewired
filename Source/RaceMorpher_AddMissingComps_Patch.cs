using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BigAndSmall;
using HarmonyLib;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Whitelisting AV Framework's/vanilla's mech-queen comps (see
    /// HumanlikeAnimalSettings_MechQueens.xml) gets them back into
    /// def.comps for the generated sapient ThingDef, and Big and Small's own
    /// RaceMorpher.AddMissingComps (private, called from SwapThingDef) does correctly
    /// notice they're missing from the pawn and instantiates them - but only via
    /// Activator.CreateInstance + ThingComp.Initialize(props). It never calls
    /// ThingComp.PostPostMake().
    ///
    /// PostPostMake is where CompMechCarrier (and AV Framework's equivalent) actually
    /// construct their internal resource ThingOwner - see vanilla's
    /// ThingWithComps.PostMake(), which is the ONLY place that calls PostPostMake, and
    /// only once, when a Thing is first created via ThingMaker.MakeThing. A comp added
    /// to an already-existing pawn later - whether by AddMissingComps here, or by
    /// vanilla's own InitializeComps() rebuilding comps from scratch when a save
    /// containing an older, comp-less version of this pawn loads - never goes through
    /// PostMake again, so that container stays permanently null. The comp's own gizmo
    /// then throws the moment it reads from that container to show a resource count,
    /// which - since that throw happens inside RimWorld's own gizmo-grid draw loop -
    /// takes the whole gizmo bar down with it, not just that one gizmo (see
    /// GizmoGridDrawer.DrawGizmoGrid, which has no per-gizmo try/catch of its own).
    ///
    /// This postfixes AddMissingComps, diffs the pawn's AllComps before and after, and
    /// calls PostPostMake on whatever's newly there - exactly what would have happened
    /// had the pawn been created with this comp from the start.
    ///
    /// That's still only half of a normal Thing's lifecycle. The other half -
    /// PostSpawnSetup(respawningAfterLoad: false), normally called once by
    /// GenSpawn.Spawn/Thing.SpawnSetup right after a freshly-made Thing is placed on the
    /// map - also never reruns for a comp added to an already-spawned pawn. Some comps
    /// rely on THAT method, not PostPostMake, to finish their own setup: e.g. AV
    /// Framework's CompMechReloadableResourceHolder only builds its internal
    /// "innerContainer" IThingHolder container there, and CompMechCarrierChoice only
    /// fills its per-spawner-def "AreaList" there (FillAreaList()). A null container
    /// doesn't necessarily crash (some comps null-check it) but does make
    /// ThingOwnerUtility.TryGetInnerInteractableThingOwner skip the comp when resolving
    /// where a hauled item should go, silently falling back to the pawn's own inventory
    /// - and an empty AreaList throws ArgumentOutOfRangeException the moment its
    /// release-urchins gizmo action is clicked (AreaList[SpawnDefNumber(spawnerdef)]).
    /// So after PostPostMake, this also calls PostSpawnSetup(false) on every newly-added
    /// comp - exactly the second half of what would have happened had the pawn been
    /// created with this comp from the start - plus a reflection-based backstop for any
    /// IThingHolder comp whose "innerContainer" is still null after that, in case some
    /// comp's own PostSpawnSetup doesn't cover it.
    /// </summary>
    [HarmonyPatch]
    public static class RaceMorpher_AddMissingComps_Patch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(RaceMorpher), "AddMissingComps");
        }

        static void Prefix(Pawn pawn, out List<ThingComp> __state)
        {
            __state = pawn?.AllComps?.ToList();
        }

        static void Postfix(Pawn pawn, List<ThingComp> __state)
        {
            if (pawn?.AllComps == null || __state == null)
                return;

            foreach (ThingComp comp in pawn.AllComps)
            {
                if (comp == null || __state.Contains(comp))
                    continue; // Already existed before this call - not ours to touch.

                try
                {
                    comp.PostPostMake();
                }
                catch (Exception e)
                {
                    Log.Error($"[SapientMechanoidFix] Failed to PostPostMake newly-added comp {comp.GetType()} on {pawn}: {e}");
                }

                try
                {
                    comp.PostSpawnSetup(respawningAfterLoad: false);
                }
                catch (Exception e)
                {
                    Log.Error($"[SapientMechanoidFix] Failed to PostSpawnSetup newly-added comp {comp.GetType()} on {pawn}: {e}");
                }

                BackstopInnerContainer(comp);
            }
        }

        private static void BackstopInnerContainer(ThingComp comp)
        {
            if (!(comp is IThingHolder holder))
                return;

            try
            {
                Traverse containerField = Traverse.Create(comp).Field("innerContainer");
                if (containerField.GetValue<ThingOwner>() != null)
                    return;

                containerField.SetValue(new ThingOwner<Thing>(holder, oneStackOnly: false));
            }
            catch (Exception e)
            {
                Log.Error($"[SapientMechanoidFix] Failed to backstop innerContainer for newly-added comp {comp.GetType()}: {e}");
            }
        }
    }
}
