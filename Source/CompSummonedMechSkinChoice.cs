using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    public class CompProperties_SummonedMechSkinChoice : CompProperties
    {
        public CompProperties_SummonedMechSkinChoice()
        {
            compClass = typeof(CompSummonedMechSkinChoice);
        }
    }

    /// <summary>
    /// Lets the player pick an [AV] Mechanoid Skins design once on a mech that summons
    /// other mechanoids (War Queen, War Empress - both use vanilla's own
    /// RimWorld.CompMechCarrier), so every future summon gets that same design instead of
    /// AV Mechanoid Skins' own random/rule-based pick. Purely a preference holder - the
    /// actual application happens in CompMechCarrier_TrySpawnPawns_Patch.cs, right after
    /// TrySpawnPawns() generates the new pawns.
    ///
    /// Attached via Patches/MechanoidSkins_SummonerChoice.xml, gated on
    /// Veltaris.MechanoidSkins - without that mod installed the comp is never added to
    /// either ThingDef in the first place, so this class (and the gizmo below) is
    /// harmless dead weight, never constructed. Whitelisted in
    /// HumanlikeAnimalSettings_MechQueens.xml so it survives onto the sapient clone too;
    /// works identically for a real or sapient summoner, since it only stores a
    /// preference and reacts to a vanilla method neither sapience nor this mod's other
    /// fixes touch.
    /// </summary>
    public class CompSummonedMechSkinChoice : ThingComp
    {
        public Def chosenSkin;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref chosenSkin, "chosenSkin");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            if (!SummonedMechSkinChoiceSupport.IsAvailable)
                yield break;

            if (SapientMechanoidFixMod.Settings?.enableSummonedMechSkinChoice != true)
                yield break;

            if (!(parent is Pawn pawn) || pawn.Faction != Faction.OfPlayer)
                yield break;

            CompMechCarrier carrier = parent.GetComp<CompMechCarrier>();
            PawnKindDef urchinKind = carrier?.Props?.spawnPawnKind;
            if (urchinKind == null)
                yield break;

            string currentLabel = chosenSkin != null ? chosenSkin.LabelCap : "Default (AV Mechanoid Skins' own pick)";
            yield return new Command_Action
            {
                defaultLabel = "Choose urchin design",
                defaultDesc = $"Pick which [AV] Mechanoid Skins design future {urchinKind.LabelCap} summoned by this mech should use. Current: {currentLabel}.",
                icon = TexButton.Rename,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_ChooseSummonedMechSkin(this, urchinKind));
                }
            };
        }
    }
}
