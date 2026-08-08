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
            listing.CheckboxLabeled(
                "Let the War Queen and War Empress choose urchin designs (NEW FEATURE!)",
                ref Settings.enableSummonedMechSkinChoice,
                "Only does anything if [AV] Mechanoid Skins is also installed. Adds a \"Choose urchin design\" gizmo to the War Queen and Alpha Mechs' War Empress, letting you pick a design once so every urchin that mech summons afterward spawns with it already applied. Works the same whether the mech is sapient or real. On by default. Turning this off hides the gizmo and stops applying any design already chosen.\n\nThis one's newer than the rest of this mod's fixes and hasn't had the same length of testing yet. If anything about it misbehaves, turning this off is safe - it doesn't touch anything else this mod does, and existing urchins already spawned keep whatever design they already have either way.");
            listing.CheckboxLabeled(
                "Let a sapient War Queen use her own [AV] Mechanoid Skins design (WIP - DO NOT TOUCH)",
                ref Settings.enableSapientMechSkinChoice,
                "Only does anything if [AV] Mechanoid Skins is also installed. Lets a sapient War Queen use Mechanoid Skins' own skin-changer gizmo on herself, the same as a real one can - previously impossible, since Big and Small always substitutes its own render tree onto a sapient pawn and Mechanoid Skins' gizmo only shows up on a tree it recognizes. Turning this off (or leaving it off) reverts her to Big and Small's default sapient appearance immediately, even on an existing save, and hides Mechanoid Skins' own skin-changer gizmo on her too.\n\nWork in progress, off by default: the gizmo opens and lets you pick a design, but every option in the list currently shows a blank preview instead of the actual design - not yet fixed, and not yet confirmed whether the applied result itself is affected. Doesn't touch anything else this mod does either way, and doesn't affect the urchin-design setting above.");
            listing.Gap();
            GameFont fontBefore = Text.Font;
            listing.CheckboxLabeled(
                "Never re-check pawns already confirmed non-mechanical",
                ref Settings.freezeNonMechanicalCache,
                "Once this mod checks a pawn and finds it's not mechanical, it normally still re-checks periodically (see the interval below), just in case some other mod later turns that organic pawn mechanical - a gene, hediff, or piece of apparel added mid-game. If nothing in your mod list ever does that, those re-checks are pure waste, since the vast majority of any colony is ordinary human colonists. Turning this on skips future re-checks for a pawn once it's confirmed non-mechanical - safe if you don't run anything that converts an organic pawn into a mechanical one, but if you do, this mod may permanently fail to notice and that pawn won't get this mod's fixes applied. A pawn's very first check is always given one refresh cycle before it's eligible to freeze, so a newly-converted or freshly-loaded mech that happens to fail its first check (a same-tick ordering thing, not a real answer) still gets a chance to self-correct rather than getting stuck. That refresh is measured in real time, not paused game time, so it still happens on schedule even while you're paused inspecting the pawn. Off by default.");
            listing.Label($"IsMechanical cache refresh interval: {Settings.isMechanicalCacheRefreshTicks} ticks");
            Settings.isMechanicalCacheRefreshTicks = Mathf.RoundToInt(listing.Slider(Settings.isMechanicalCacheRefreshTicks, 30f, 5000f));
            Text.Font = GameFont.Tiny;
            if (Settings.freezeNonMechanicalCache)
            {
                listing.Label("With the setting above on, this still controls one thing: how long a pawn's first check is given to self-correct before it can be frozen. A confirmed mechanical pawn is never re-checked regardless of this setting, since that status doesn't revert.");
            }
            else
            {
                listing.Label("Most of this mod's patches ask \"is this pawn one of ours\" very often - the answer is cached per pawn for roughly this many ticks' worth of real time before being re-checked (measured by the clock, not the game's simulated ticks, so it still elapses on schedule even while paused), rather than recomputed every time. 250 (default) is a good balance for most colonies. If you're running an unusually mechanoid-heavy colony and still seeing stutter, try raising this - the tradeoff is this mod taking slightly longer to notice if a pawn's mechanical status ever genuinely changes, which in practice is rare. A confirmed mechanical pawn is never re-checked regardless of this setting, since that status doesn't revert - this only affects how often an ordinary colonist gets re-checked.");
            }
            Text.Font = fontBefore;
            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Sapient Mechanoids Rewired";
        }
    }
}
