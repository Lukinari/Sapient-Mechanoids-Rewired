# Sapient Mechanoid Fix

Fixes what breaks when [Big and Small - Sapient Animals](https://steamcommunity.com/sharedfiles/filedetails/?id=2925432336) turns a War Queen (vanilla Biotech) or Work Queen ([AV] Framework) into one of its "sapient" pawns - a colonist-like mech with its own skills, traits, needs, and personality.

Turning a mechanoid sapient makes it behave like a person, which is the whole point - but along the way it silently loses a bunch of things that made it a *mechanoid* in the first place. This mod puts those back, without undoing the "now it's basically a person" part.

Does nothing unless Big and Small is active.

## What was broken, and what this fixes

- **Steel storage and urchin release** - a sapient War Queen or Work Queen lost its steel reserve gizmo and the button to release war/work urchins entirely. Restored for both.
- **Drafting** - sapient mechs couldn't be drafted, undrafted, or given move/attack orders, and the draft button itself would sometimes disappear. Fixed.
- **Work assignments** - a sapient mech would keep only *one* work type (e.g. a builder-type mech could still construct) and lose access to every other kind of work it should be able to do as a person. Fixed - sapient mechs now keep full access to whatever work they're capable of.
- **"Out of command range"** - drafted sapient mechs would refuse move/attack orders, always reporting they were out of range - even though sapient mechs are supposed to be completely independent and never need a controlling mechanitor. Fixed.
- **Crash on loading a save** - a save containing a sapient War Queen (or, with this mod, a sapient Work Queen) could crash the moment you opened its steel gizmo, with a null-reference error. Fixed.
- **Auto-repair** - mechanitors refused to repair a sapient mech at all, because the game assumed every mechanoid needs to be plugged into a power source, which a sapient mech deliberately doesn't have. Fixed - sapient mechs can now be repaired, and auto-repaired, normally.
- **War Queen's mounted gun** - a sapient War Queen lost its mounted charge blaster turret entirely, leaving it with no ranged attack at all. Restored, along with the fire-at-will toggle.

## Bonus: reactive armor

Two independent "the more you get shot, the more this kicks in" defensive mechanics, built specifically for this mod and active automatically on sapient War Queens/Work Queens - no other mods required, though both were inspired by mechanics seen in other mods:

- **Steel-charged plating** - incoming damage is reduced the fuller the steel reserve is (up to 50% less damage at a full reserve), and increases up to 50% *more* damage when the reserve runs dry.
- **Reactive plating charges** - each hit grants a brief window of full damage immunity, drawn from a limited pool of charges that slowly recharge while the mech is being repaired. Comes with its own gizmo showing charges remaining.

## Optional: Glitterworld Destroyer 5 compatibility

If you also run *Glitterworld Destroyer 5 - Mechanoid Addon*, its War Queen upgrades - the auto-release-urchins toggle and the "kill all urchins" button - are restored for the sapient version too. Not required; everything above works fine without it.

## Requirements

- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- [Big and Small - Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2925432336)

Everything else - the Biotech DLC, [AV] Framework + Work Queen, Glitterworld Destroyer 5 - is optional. The mod only turns on the fixes relevant to whichever of these you actually have installed, and stays completely inert for anything it doesn't recognize.

## Installation

Drop this folder into your RimWorld `Mods` directory and enable it below Big and Small in the mod list (load order otherwise doesn't matter much, though loading after Biotech/AV Framework/Glitterworld Destroyer 5 if present is recommended).

## Compatibility notes

Every fix here only ever affects a pawn that Big and Small has already converted to sapient - real, non-sapient mechanoids are left completely untouched, including real War Queens and Work Queens. Nothing in this mod should conflict with anything that doesn't also touch those two specific mechanoid types.
