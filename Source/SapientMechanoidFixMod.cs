using UnityEngine;
using Verse;

namespace SapientMechanoidFix
{
    public class SapientMechanoidFixMod : Mod
    {
        public static SapientMechanoidFixSettings Settings;

        public SapientMechanoidFixMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<SapientMechanoidFixSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                "Sapient mechs feel no pain",
                ref Settings.painImmunity,
                "Sapient mechanoids never enter pain shock or take pain-related capacity penalties, the same way a real mechanoid never does - becoming sentient doesn't grow them organic nerves. Off by default.");
            listing.CheckboxLabeled(
                "Allow resurrecting sapient mechs",
                ref Settings.allowMechResurrection,
                "Mechanoid-resurrection abilities (the vanilla Apocriton's \"Resurrect Mechs\", Alpha Mechs' \"Resurrect Mech Minor\") can target and revive a dead sapient mech's corpse, the same as a real one. Turn this off if you'd rather a sapient mech's death be permanent, like an ordinary colonist's. On by default.");
            listing.CheckboxLabeled(
                "Fix sapient mech size and weight class",
                ref Settings.fixMechSizeAndWeightClass,
                "Restores a sapient mechanoid's real body size, health scale, and weight class, which Big and Small otherwise quietly resets to plain human values. Mostly invisible day-to-day, but anything that checks a mech's size or weight class directly - like Mechanoid Upgrades' size-restricted upgrades - needs this to judge a sapient mech correctly. On by default. Takes effect the next time a save is loaded or a new game is started, not immediately.");
            listing.CheckboxLabeled(
                "Freeze Ascension Megacorp mechs' Readiness need",
                ref Settings.freezeAscensionMegacorpReadiness,
                "Ascension Megacorp mechs have their own \"Readiness\" need, refilled with Component Industrial, alongside their real vanilla energy bar (which a sapient mech never gets back, regardless of this setting, same as any other sapient mechanoid). Turning this on stops a sapient mech's Readiness from draining at all - the bar stays on its need list, but it never demands a component-resupply chore. Off by default, so it behaves like a normal need unless you opt out. Takes effect immediately, even on an existing save.");
            listing.Gap();
            GameFont fontBefore = Text.Font;
            listing.CheckboxLabeled(
                "Never re-check pawns already confirmed non-mechanical",
                ref Settings.freezeNonMechanicalCache,
                "Once this mod checks a pawn and finds it's not mechanical, it normally still re-checks periodically (see the interval below), just in case some other mod later turns that organic pawn mechanical - a gene, hediff, or piece of apparel added mid-game. If nothing in your mod list ever does that, those re-checks are pure waste, since the vast majority of any colony is ordinary human colonists. Turning this on skips all future re-checks for a pawn once it's confirmed non-mechanical - safe if you don't run anything that converts an organic pawn into a mechanical one, but if you do, this mod may permanently fail to notice and that pawn won't get this mod's fixes applied. Off by default.");
            if (Settings.freezeNonMechanicalCache)
            {
                Text.Font = GameFont.Tiny;
                listing.Label("Refresh interval below doesn't apply while this is on - a non-mechanical pawn is never re-checked at all, so there's nothing left to time.");
                Text.Font = fontBefore;
            }
            else
            {
                listing.Label($"IsMechanical cache refresh interval: {Settings.isMechanicalCacheRefreshTicks} ticks");
                Settings.isMechanicalCacheRefreshTicks = Mathf.RoundToInt(listing.Slider(Settings.isMechanicalCacheRefreshTicks, 30f, 5000f));
                Text.Font = GameFont.Tiny;
                listing.Label("Most of this mod's patches ask \"is this pawn one of ours\" very often - the answer is cached per pawn for this many ticks before being re-checked, rather than recomputed every time. 250 (default) is a good balance for most colonies. If you're running an unusually mechanoid-heavy colony and still seeing stutter, try raising this - the tradeoff is this mod taking slightly longer to notice if a pawn's mechanical status ever genuinely changes, which in practice is rare. A confirmed mechanical pawn is never re-checked regardless of this setting, since that status doesn't revert - this only affects how often an ordinary colonist gets re-checked.");
                Text.Font = fontBefore;
            }
            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Sapient Mechanoids Rewired";
        }
    }
}
