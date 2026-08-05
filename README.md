# Sapient Mechanoids Rewired

Version 1.0.0

[Big and Small - Sapient Animals](https://steamcommunity.com/sharedfiles/filedetails/?id=2925432336) can turn a mechanoid into a "sapient" pawn - basically a mechanoid that thinks and lives like one of your colonists, with its own skills, needs, and personality. Great idea, but along the way it quietly loses a bunch of things that made it a mechanoid in the first place. This mod puts those back.

Does nothing unless Big and Small is active.

## What this fixes

For any sapient mechanoid, from any mod:

- **Right-click Repair works again.** Mechanitors used to flat-out refuse to repair a sapient mech.
- **Built-in personal energy shields work again.** Any mechanoid that has its own shield (not a shield belt it's wearing) used to lose it entirely once made sapient.
- **Mech-only items and abilities work again.** Everything a regular mech has, its sapient mech now has as well. Reactive armor, steel supplies, urchin controls, abilities, the works. Anything that's supposed to target mechs (but not regular colonists) can target a sapient mech again. 
- **Mechanoid-resurrection abilities work on a sapient mech's corpse too** (the vanilla Apocriton's "Resurrect Mechs", Alpha Mechs' "Resurrect Mech Minor") - on by default, can be turned off in the mod settings if you'd rather a sapient mech's death stay permanent.
- **Custom mechanoid heads render again.** Some mods build their mechanoid's head as its own separate piece instead of using a stock one - Big and Small itself was dropping that piece entirely, leaving the mechanoid with no head at all once made sapient. Confirmed fixed on Dead Man's Switch's Lady and Ascension Megacorp's Rocky. Can still occasionally reappear missing within the same session a mech was made sapient - see Known Issues.
- **Dead Man's Switch's Lady and Ascension Megacorp's Rocky can pick a hairstyle at the styling station now**, not just Bald. Their custom-head system defaults hair off (correctly, for the real, non-sapient mech), and the styling station respected that even once they became a full colonist - only their current hairstyle (Bald, by default) was ever offered as an option. They get the same hairstyle freedom as any other sapient mech now.
- **A sapient mech's real size and weight class are no longer forgotten.** Big and Small itself was quietly resetting every sapient mechanoid to plain human size and no weight class at all - invisible day-to-day since the game's own size-dependent behavior (health, damage, etc.) was compensating for it separately, but anything that reads a mech's size or weight class directly (like Mechanoid Upgrades' size-restricted upgrades, see below) was judging every sapient mech as human-sized and class-less, regardless of whether it used to be a tiny drone or a towering war machine.
- Anything else that checks whether a pawn still counts as a mechanoid under mechanitor control gets fixed the same way - in any mod, not just a hardcoded list.
- **Mounted turret guns work again**, along with their fire-at-will toggle. A mechanoid with a built-in turret weapon (like the vanilla War Queen's) used to lose it entirely once made sapient - confirmed fixed generically, not just for the War Queen.
- **No performance impact.** Measured performance with this mod installed is the same as without it. This mod's own checks (asking "is this pawn one of ours" for every patch) are cached per pawn, and a confirmed answer is never recomputed again - the added cost approaches zero the longer a save runs.

For the War Queen, Work Queen and similar mechanoids specifically:

- **Steel storage and releasing urchins work again.**

## Optional: Mechanoids: Total Warfare

If you also run *Mechanoids: Total Warfare*, a sapient War Queen or Work Queen gets its steel-charged armor plating back - tougher the fuller its steel reserve is, weaker when it runs dry. The Apocriton's stealth camouflage ability works too, cloaking it from hostiles just like a real one.

## Optional: Glitterworld Destroyer 5

If you also run *Glitterworld Destroyer 5 - Mechanoid Addon*:

- A sapient War Queen or Work Queen also gets GD5's own War Queen upgrades back: the auto-release-urchins toggle, the "kill all urchins" button, and its own damage-resistance charge mechanic - including its charges actually recharging via repair, which turned out to be silently broken in GD5 itself (for real mechanoids too, not just sapient ones) due to a stale method reference from a RimWorld version change; fixed here rather than reimplemented, so a sapient mech gets GD5's own mechanic working correctly instead of a separate parallel one.
- GD5's own mechs work normally once made sapient: Observer, Cataphract Centipede, Centipede Swordsman, Black Scyther, Recon Scyther, Black Lancer, Marine Lancer, Black Tesseron, Black Legionary, Black Militor, Firefly, Black Apocriton, and the Archo Hunter drone. The Annihilator isn't supported yet - see Known Issues.
- GD5 also bolts extra abilities onto a couple of vanilla mechs, and those work too now: the Centurion's shield ability and the Mosquito's Rocket Attack. The Mosquito's Air Raid ability doesn't - see Known Issues.

## Optional: The Dead Man's Switch

If you also run *The Dead Man's Switch*, its craftable upgrade parts (Ceramic Plates, Reinforced Frame, Synthetic Tendon, Nuclear Battery, and the rest) can be installed on a sapient automatroid just like a real one. The Nuclear Battery also cuts a sapient mech's hunger instead of its energy use, since it doesn't run on energy anymore - so it's not a wasted upgrade on them.

## Optional: Reinforced Mechanoid 2

If you also run *Reinforced Mechanoid 2*, its mechs keep working once made sapient: Caretaker, Gremlin, Harpy, Zealot, Wraith, Locust, Behemoth, Falcon, Marshal, Sentinel, Vulture, Ranger, and Matriarch (and their VFE-branded counterparts). Caretaker and Marshal both have their own personal shield - Marshal's is newly fixed, Caretaker shield ability currently doesn't work correctly unlike its active shield, see Known Issues. The Buffer and Spartan droids aren't supported yet - also see Known Issues.

## Optional: Alpha Mechs

If you also run *Alpha Mechs*, its mechs keep working once made sapient, including the Aura, Daggersnout, Demolisher, Fireworm, Goliath, Phalanx, Siegebreaker, Guttersnipe, Infernus, Legate, Optio, Apoptosis, Bellicor, Artilleron, Blitzkrieg, MasterChef, Munifex, Polychoron, PristineAssembler, PristineSlurrypede, PristineStrider, Sagittarius, Siegemelter, Starfire, TurboCleaner, and WarEmpress. Its own "Resurrect Mech Minor" ability is covered by the resurrection setting above too.

## Optional: [AV] Mechtech

If you also run *[AV] Mechtech*, its mechs keep working once made sapient: the Fluoid, Scrapper, Tarantula, and Companoid sphere. The Reshaper isn't supported - see Known Issues.

## Optional: Ascension Megacorp

If you also run *Ascension Megacorp*, its mechs keep working once made sapient: Cobalt, Gonk, Omaha, Paraman, and Rocky. Resupplying them with components works the same as auto-repair does for any other sapient mechanoid.

## Optional: Mechanoid Upgrades

If you also run *Mechanoid Upgrades*, you can now walk a sapient mech into a Mech Upgrader building and install upgrades on it, the same as a real mechanoid - previously the option to do so didn't even show up. A sapient mech also can't be starved of a mechanitor's supervision and go feral the way a real uncontrolled mech would - it doesn't need one in the first place. Every individual upgrade type has now been checked - shielding, reactive armor, laser defence, the "spawn helper mech" upgrade, cosmetic add-ons, all work correctly on a sapient mech. Upgrades restricted to certain mech sizes or weight classes now correctly check the mech's real size and weight class too, rather than treating every sapient mech as plain human-sized. The one exception is aura-style upgrades that buff or heal nearby mechs - see Known issues.

## Known issues

- **Mechanoids: Total Warfare** - Shell Fortification's "Turn into Building" ability doesn't work on a sapient mech. Cause not found yet.
- **Mechanoids: Total Warfare** - the Drone loses its "Boom!" and "Destroy" buttons. Cause is known.
- **Glitterworld Destroyer 5** - the Annihilator isn't supported. Its turret ignores the fire-at-will toggle and a few other checks, and its jump/teleport abilities and AI need a much bigger rework to function at all on a sapient version.
- **Glitterworld Destroyer 5** - the Mosquito's Air Raid ability is deliberately left disabled rather than fixed - actually enabling it as-is would crash the game.
- **Reinforced Mechanoid 2** - the Buffer and Spartan droids aren't supported. They run on a separate "power" need that a sapient version might keep with no way to refill it, which could risk unexpectedly killing the pawn - held back until that's confirmed safe.
- **Reinforced Mechanoid 2** - the Caretaker's bubble shield ability doesn't work, but its regular shield does. Not actively being investigated right now as the diagnostic code was causing stutter, but it's been removed.
- **Glitterworld Destroyer 5** - the Observer's target-designation ability works, but the Observer doesn't follow its target while it's active the way it's probably meant to. Low priority.
- **[AV] Mechtech** - the Reshaper isn't supported. Its whole gimmick depends on it staying recognizable as the specific mechanoid it started as, and sapience breaks that in more than one place at once.
- **Mechanoid Upgrades** - upgrades that buff or heal nearby *mechs* don't recognize a nearby sapient mech as one, so a sapient mech won't benefit from an ally's aura-style upgrade (though it can still use one installed on itself).
- **Only Mechanitors can currently repair a sapient mech, and only manually** - requires even more extensive investigation and testing, may be added as toggleable in the near future.
- **A custom mechanoid head (see "What this fixes" above) can render missing again**, even on a mech it was previously confirmed working on, if the head-rendering fix runs before something else it depends on has finished setting up in that same play session - reloading the save fixes it every time, so this is a one-time-per-session cosmetic glitch, not a lasting break. Root cause (the exact ordering issue) not pinned down yet.

## Planned features

- **MAP Mechanoid Commander** - not looked at yet.
- **Alpha Mechs' Lux** - its weapon's "warmup" toggle doesn't seem to do anything once sapient. Investigated and found nothing that looks sapience-specific about it, so the cause is still unclear - needs more testing to pin down.
- **Ascension Megacorp's Deactivate button and paint job** - confirmed not working on a sapient mech. Not investigated yet.
- **[AV] Mechanoid Skins** - adds cosmetic customization/skins for mechanoids, including [AV] Mechtech's. Not looked at yet.
- **Ascension Megacorp** - Gonk, Omaha, and Paraman work but also lose their custom appearance once sapient, regardless of this mod, looking to fix. Cobalt and Rocky are unaffected.

## Mod settings

- **Sapient mechs feel no pain** (off by default) - never enter pain shock or take pain-related penalties, the same as a real mechanoid.
- **Allow resurrecting sapient mechs** (on by default) - lets mechanoid-resurrection abilities bring a dead sapient mech back, same as a real one. Turn off if you'd rather their death be permanent, like an ordinary colonist's.
- **Fix sapient mech size and weight class** (on by default) - restores a sapient mech's real body size, health scale, and weight class instead of treating it as plain human-sized. Only matters to other mods that check a mech's size or weight class directly (like Mechanoid Upgrades' size-restricted upgrades) - takes effect on the next save load or new game, not immediately.
- **Freeze Ascension Megacorp mechs' Readiness need** (off by default) - a real Ascension Megacorp mech has both a vanilla energy bar and its own component-refilled Readiness need; a sapient one never gets the energy bar back either way, same as any other sapient mechanoid, but this setting controls whether its Readiness need keeps draining. Turn on and a sapient mech's Readiness bar stops moving entirely - it stays on the need list but never demands a component-resupply chore. Takes effect immediately, even on an existing save with mechs that already have the need.
- **Never re-check pawns already confirmed non-mechanical** (off by default) - skips future re-checks for a pawn once it's confirmed not mechanical, instead of periodically re-checking it in case some other mod later converts it (a gene, hediff, or apparel added mid-game). Safe to turn on if nothing in your mod list ever turns an organic pawn into a mechanical one - most of a colony is ordinary colonists, so this avoids rescanning them forever after the first check. Leave off if you're not sure, since a pawn converted after being cached as non-mechanical would permanently miss this mod's fixes. Every pawn's first-ever check is always given one refresh cycle before it's eligible to freeze, so a newly-converted or freshly-loaded mech that happens to fail its very first check still gets a chance to self-correct instead of getting stuck non-mechanical forever.
- **IsMechanical cache refresh interval** (250 ticks by default) - most of this mod's patches ask "is this pawn one of ours" very often; the answer is cached per pawn for roughly this many ticks' worth of real time rather than recomputed every time - measured by the clock, not the game's simulated ticks, so it still elapses on schedule even while the game is paused. Raise this if you're running an unusually mechanoid-heavy colony and still seeing stutter - the tradeoff is this mod taking slightly longer to notice if a pawn's mechanical status ever genuinely changes, which in practice is rare. A pawn already confirmed mechanical is never re-checked regardless of this setting, since that status doesn't revert. With the setting above on, this still controls one thing: how long each pawn's first check is given to self-correct before it can freeze.

## Requirements

- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- [Big and Small - Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2925432336)

## Installation

Drop this folder into your RimWorld `Mods` directory and enable it below Big and Small in the mod list (load order otherwise doesn't matter much, though loading after Biotech/AV Framework/Glitterworld Destroyer 5/Mechanoids: Total Warfare if present is recommended).

## Tested on

Every mechanoid in each of these rosters has been individually made sapient and tested, not just spot-checked:

- Every vanilla Biotech mechanoid, including the War Queen
- Work Queen ([AV] Framework)
- The Dead Man's Switch's mech roster
- Mechanoids: Total Warfare's mech roster
- Glitterworld Destroyer 5's mech roster
- Reinforced Mechanoid 2's mech roster
- Alpha Mechs' mech roster
- [AV] Mechtech's mech roster
- Ascension Megacorp's mech roster

The general fixes should work on any other sapient mechanoid, from any mod, since they check behavior rather than a specific race - the above are just the ones actually tested. The extra gizmos (steel reserve, urchin release, mounted gun) are, for now, only built for the War Queen and Work Queen specifically.

## Compatibility notes

This mod only ever touches a pawn after Big and Small has converted it to sapient - real, non-sapient mechanoids are left completely alone. It shouldn't conflict with anything else unless that mod also touches sapient mechs.

## Credits

- **RimWorld** - Ludeon Studios. This mod is a compatibility patch for their game and wouldn't exist without it.
- **Harmony** - Andreas Pardeike. The patching library this mod (and most of the RimWorld modding ecosystem) runs on.
- **Big and Small** - RedMattis. The sapient-animal conversion framework this mod fixes compatibility for.
- **[AV] Framework** and **[AV] Work Queen** - Veltaris. The Work Queen and the comps behind its steel reserve and urchin release.
- **Glitterworld Destroyer 5 - Mechanoid Addon** - Feng Xinzi. Source of the War Queen upgrades restored here, and its own mechanoid roster.
- **Mechanoids: Total Warfare** - Nyarlathotep. Source of the steel-charged-plating mechanic reimplemented here, and its own mechanoid roster.
- **The Dead Man's Switch** - Aoba. Source of the automatroid roster this mod restores compatibility for.
- **Reinforced Mechanoid 2** - Mlie. Source of the mechanoid roster this mod restores compatibility for.
- **Alpha Mechs** - Sarg Bjornson. Source of the mechanoid roster this mod restores compatibility for.
- **[AV] Mechtech** - Veltaris. Source of the mechanoid roster this mod restores compatibility for.
- **Ascension Megacorp** - AobaKuma and contributors. Source of the mechanoid roster this mod restores compatibility for.
- **Mechanoid Upgrades** - GoGaTio. Source of the upgrade framework this mod restores compatibility for.

## Development

Developed with AI assistance (Claude Code). Verified through extensive in-game testing over the course of several days and several nights rather than assumed to work. I'll be using it in my own personal playthrough and I'm incredibly paranoid about crash-prone or save-corrupting mods. It's safe to add from testing and should be safe to remove with nothing but some harmless errors in the log.

## Source code

[github.com/Lukinari/Sapient-Mechanoids-Rewired](https://github.com/Lukinari/Sapient-Mechanoids-Rewired)

Forks and Steam Workshop reuploads are welcome - just link back to this repository so people can find updates and report issues in one place.

## License

[MIT](LICENSE)
