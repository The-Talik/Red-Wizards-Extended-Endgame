What is it?
 Red Wizards Extended Endgame is an endgame overhaul that attempts to extend the gameplay in a way that feels balanced, rewarding, and challenging all at the same time. Increase player level cap to 100, sector level cap to 200, adds endgame upgrades, and a bunch of other changes.

Highlights:
 - Player levels are capped at 100.
 - Sector levels are capped at 200, with high level enemies offering a significant challenge.
 - Scrapping legendary equipment can return legendary upgrade items.
 - Stations improve.

Installation Instructions:
 1. Follow this guide to install the modding engine, BepInEx: https://steamcommunity.com/sharedfiles/filedetails/?id=3365166065
 2. Place the RedWizardsExtendedEndgame.plugin folder in BepInEx\plugins
 3. Place the RedWizardsExtendedEndgame.Patcher.dll file in PebInEx\patchers
 4. Profit!

Current Version: 1.0.5
Download: https://www.nexusmods.com/starvalor/mods/55
Alternate: https://github.com/The-Talik/Red-Wizards-Extended-Endgame/releases

Adding/Removing this mod:
 It can now be fully safely added to a game.  Your first load will update existing sectors to increase their limit.
 Mod can be safely removed from a game as well.  Sectors will retain their existing levels, but ships within that sector will not spawn beyond the regular level cap.  Mythic tier items will turn into non-upgradable tier 1 items.

Incompatible mods:
 No hard incompatibilities that I am aware of (yet), but it is recommended you avoid mods that increase skills per level, since the level cap is increased.

Changes from base game:
 Sectors:
 - Sector level cap increased to 200 (along with all appropriate spawns in that sector)
 - Sectors leveling up can cause lower level neighboring sectors to level up
 Enemies:
 - Enemy stats buffed after L50, exponentially beyond normal level scaling.  Good luck!
 - Higher level Marauder Hideouts spawn with elite ships
  - (Reason: Marauder quests are really profitable, since they are repeatable once you clear the hideout)
 - Hideouts will occasionally respawn their marauders
 - Enemies over L50 will adjust their tactics more intelligently
  - Enemies will flee when health gets low
  - Enemies adjust their tactics based on who they are attacking (ie, they will switch to hit and run if they are more maneuverable than their target)
  - Ravagers and Marauders will attack you from further away preventing long range attack spamming
 Players:
 - Player level cap increased to 100
 - XP gain slowed down progressively after level 50
 - A new skill reset is added whenever you complete a quest and are out of skill resets
  - (Reason: This allows skills to be reset more freely (even after hitting max level) but not spammed)
 - Tech and Construction Level cap is increased to 200 to match sector limits.  Rest of the knowledges are still capped at 50 for balance purposes
 Items:
 - Added tier Mythic (Tier 6) and Ascended (Tier 7) weapons and equipment, droppable only by very high level enemies (starting at L50 and L100, respectively)
 - Scrapping Legendary or Mythic equipment (Not weapons or Ascended) will occasionally drop an upgrade item for that item type (roughly 10% of the time).
  - Example: scrapping a legendary battery can drop a Legendary Battery Catalyst that can upgrade batteries to Legendary.  Sorry savescummers, the results are seeded. ;-)
 - Added Pirate Capital Booster
 - Legendary and higher weapons are buffed roughly 1.5x to be more competitive with endgame player-made ones.
 Crew:
 - Escape Pod crew always start as level 1
  - Higher level sectors have a chance of spawning higher tier crew, instead of higher level
  - (Reason: Higher level crew is actually bad because they have less time to evolve, making found crew basically useless in normal late-game.  This makes found crew generally better than bought crew, especially in higher level sectors.)
 - Unique crew spawn rate increased.  (Maybe too much, might dial this back after another playthrough)
 - Sam is available to be found in an escape pod after you have unlocked the scoundrel path, and sent him away
 Stations:
 - Completing missions for a station has a chance to level it up
  - A leveled up station also has a chance to increase the level of the sector
  - The higher your level compared to the station, the higher the chance
 - Stations above L50 have more missions and a higher chance of requesting gold hunter missions with a lesser chance of regular ones
 - If you are a higher level than the station, hunter quests tend to look for slightly higher level sectors to send you to
  - (Reason: This keeps you slightly expanding towards harder sectors even if you just follow quests.)
 Bugfixes/core game tweaks:
 - Fleet ships properly use their scanner power when looking for asteroids to mine
 - Ancient artifacts are purple, instead of blue

Changelog:
1.0.5
 - Legendary and higher weapons are buffed roughly 1.5x to be more competitive with endgame player-made ones.
 - Fixed a bug prevent tech from leveling past 100
 - Fixed a bug that allowed Mythic Relics to upgrade items past Tier 5.
 - Renamed relics and tiers from this mod.
 - Added Pirate Capital Booster.
1.0.4b
 - hotfix for version popup message.
1.0.4
 - Adjusts item drop for high level enemies to give a more even spread of equipment types, and ensure reasonable weapons are always returned even if the item level is too high. for the default formula.
 - fix a bug with enemy stat scaling from the last build.
1.0.3
 - Fixed a bug stopping tech from leveling past 50
 - Sector level up can now cause nearby, lower level sectors to level up.
 - Hideouts will occasionally respawn their marauders
 - Enemies over L50 adjust their tactics more intelligently.
1.0.2
 - Added tier 6 and 7 rarity levels with Tier 6 upgrade items from scrapping.
 - Existing sectors now leveled up on first install.
1.0.1
 - Fixed a bug with Mythic Relics.

Roadmap:
 - Higher mk levels of QOL items, scanners, warp, collector beams, etc.
  - Higher level sensors with more features (detect which stations are in a sector before visiting, etc)
  - Higher level Battle Computer which shows more info about your opponent.
 - Allow Crew to evolve beyond legendary, which will mostly only be possible with crew from escape pods found in high level sectors.
 - Ravager re-spawning
 - Disable extra equipment/weapons when owning a fleet or resetting skills, rather than uninstalling it.
 - Get Space Pilot benefit when no fleet members are present (even if you control a fleet).
 - swap/launch
  - Swap with captain of a fleet ship.
  - Swap with a derelict (leaving your ship a derelict, or maybe piloted by your first officer)
  - Launch from inventory.  Pilot a ship from your inventory, leaving your ship as derelict, or piloted by your FO.
 - Require merging mythic relics with ancient relic before it can be used.  (Is this just going to be annoying?)
 - Make background perks not lock you out of other background perks and remove the part preventing you from allying.
  - Maybe also make being friendly with a faction add automatic displeasure to their enemy factions so you can't ally everyone at the same time.

Looser down-the-road ideas:
 _ Tiers 8 (Celestial) and 9(Transcendent)
 - Higher mk levels of other items.  (This requires more balance consideration, so is listed separately from above)
 - new bases spawn in sectors that had a base defeated.  IE, if you help PCM destroy a Pirate base, the sector should spawn a PCM base sometime later.
 - base spawning for factions with low base-counts.
 - Add some high level perks to challenge people.  ie, defeat a L200 hunter crew, ravager, etc.
 - Button to hide a recipe from the crafting list.
 - Attacking a fleet member should make the whole fleet hostile.
 - Attacking/destroying a turret should make the whole sector hostile.
 - Faction launches hunter fleet if you threaten a station.
 - Items that add Bounty (You will more often be attacked by blah blah blah...)
 - Double Star Enemies.
  - High level hunter quests.
  - High Level Ravagers.
  - (Reason: In late game, hunter quests and ravagers feel really week, compared to hunter fleets)
 - Auto updater. (Maybe a separate mod-manager mod?)
 - tweak pirate speed boosters
  - Probably add a space 6 Pirate Capital Booster. (Maybe)
 - Way to make smaller ships more competitive late-game.
  - Maybe big ships are vulnerable from behind.  Causing extra damage, and debufs.
  - Equipment that ads extra crew seats.  Maybe the equipment can only be used if the ship does not already have a crew seat for that type making it useless on big ships.
 - Some form of endgame state that requires you to piss off all factions but one, in order to gain some super reward.
 - Maybe a separate higher-level version of Space Pilot and Fleet Commander that only starts when the base skills are maxed out.  This would allow the game to keep the standard L55-60 skills as hard to get, but introduce a new set of skill upgrades.
 - allow very high level enemies to drop ship blueprints
 - UI improvements:
  - Controls active with map open.
  - Map UI updates (Maybe a separate mod?)
  - Crafting UI updates (maybe a separate mod?)
  - allow very high level enemies to drop ship blueprints