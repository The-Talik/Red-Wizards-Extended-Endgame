# RWEE — Red Wizards Extended Endgame (Star Valor)

Red Wizard's Extended Endgame is an **end-game overhaul** mod for **Star Valor**.  It attempt at extending the gameplay in a number of ways intended to feel balanced with the core game.  Most features of this mod do not come into play until the player or sectors are L50+.  Increase player level cap to 100, sector level cap to 200, adds endgame upgrades, improves enemy AI, core gameplay improvements, and a bunch of other tweaks.

- Player level cap → **100**  
- Sector level cap → **200**  
- New endgame tiers, upgrades, drops, AI tweaks, and QoL improvements  
- Safe to add/remove (with a few expected caveats—see below)

---

## Downloads
- Nexus Mods: https://www.nexusmods.com/starvalor/mods/55  
- GitHub Releases: https://github.com/The-Talik/Red-Wizards-Extended-Endgame/releases  

---

## Requirements
RWEE requires **BepInEx**:
- Install guide: https://steamcommunity.com/sharedfiles/filedetails/?id=3365166065

As of **RWEE v1.1.3**, this mod requires **RWMM (Red Wizards Mod Manager)**.
- RWMM is included in the RWEE download for convenience.
- RWMM docs (for modders): [RWMM/README.md](RWMM/README.md)

---

## Installation (Windows)
1. Install BepInEx (link above).
2. Copy all `*.plugin` folders into:
   `Star Valor\BepInEx\plugins\`
3. Copy all `*.patcher` files into:
   `Star Valor\BepInEx\patchers\`
4. Launch the game.

---

## Adding / Removing RWEE (Save Safety)
RWEE is designed to be **safe to add to an existing save**.
- On first load, RWEE will update existing sectors to support the higher limits.

RWEE can also be **removed safely**, with these expected behaviors:
- Sectors retain their current levels, but ships in those sectors won’t spawn beyond the vanilla cap.
- Mythic-tier items will become non-upgradable Tier 1 items.
- You may see errors on first load after removal due to item IDs no longer existing (expected).

---

## What RWEE Changes

### Sectors
- Sector level cap increased to **200** (with appropriate spawn scaling).
- Sector leveling can cause **lower-level neighboring sectors** to level up.

### Enemies
- Enemy stats are buffed after **L50**, scaling **exponentially** beyond normal progression. Good luck!
- Higher-level Marauder Hideouts can spawn **elite ships**  
  - Reason: Marauder quests are extremely profitable due to repeatability.  Let's make them harder!
- Hideouts can **occasionally respawn** their marauders.
- Enemies over **L50** adjust tactics more intelligently:
  - Flee when health is low
  - Adjust tactics based on target (e.g., switch to hit-and-run if more maneuverable)
  - Ravagers and Marauders attack from further away to reduce long-range cheese
- Loot tuning:
  - Enemy drops adjusted to give endgame equipment, even when enemy levels exceed vanilla formula assumptions.

### Players
- Player level cap increased to **100**
- XP gain slows progressively after **level 50**
- A new skill reset is added whenever you complete a quest **and you’re out of skill resets**
  - Reason: encourages flexibility without allowing spam.
- Tech + Construction caps increased to **200** (to match sector limits)  
  - Other knowledges remain capped at **50** for balance.
- Space Pilot bonuses are applied when no fleet members are present (even if you control a fleet)

### Items / Loot
- Added endgame tiers:
  - **Mythic (Tier 6)** and **Ascended (Tier 7)**
  - Droppable only by very high-level enemies (starting at **L50** and **L100**, respectively)
- Scrapping mechanics:
  - Scrapping **Legendary or Mythic equipment** (not weapons, not Ascended) can drop **cores** (~10%)
  - Cores combine into the familiar upgrade item for that equipment type
  - Results are seeded (sorry savescummers 😉)
- Added: **Pirate Capital Booster**
- Legendary+ weapons buffed roughly **1.5×** to compete better with endgame crafted weapons.

### Crew
- Escape Pod crew always start at **level 1**
  - High-level sectors instead have a chance to spawn **higher tier crew** (not higher level)
  - Reason: high-level found crew is often *worse* due to reduced evolution runway.
- Unique crew spawn rate increased (may be adjusted later).
- Sam can be found in an escape pod after unlocking the scoundrel path and sending him away.

### Stations
- Completing missions for a station can level it up
  - Leveled stations can also increase the sector level
  - The higher your level compared to the station, the higher the chance
- Stations above **L50** have:
  - More missions
  - Higher chance of requesting gold hunter missions (lower chance of regular ones)
- Hunter quests tend to send you slightly outward into tougher space when you outlevel a station
  - Reason: keeps progression moving into harder sectors even if you “follow quests”.

### Bugfixes / Core Tweaks / QOL
- Fleet ships properly use scanner power when looking for asteroids to mine
- Ancient artifacts are purple (instead of blue)
- Too many installed equipments will disable them instead of removing them (useful for fleets)
- Inventory shows quantity needed for quests (ie. "(1/3) Iron")
- Hephaestus set to Epic
- Number of large asteroids in a sector shown on sector map

---

## Compatibility
- No hard incompatibilities known yet.
- Recommended: This mod does a lot.  Read through the features to ensure no overlap with other mods you have installed.

---

## Changelog

### 1.1.4
- Number of large asteroids in a sector shown on sector map
- Space Pilot bonuses are applied when no fleet members are present (even if you control a fleet)
- Fixed a bug where disabled equipment would poof out of existence.

### 1.1.3
- Too many installed equipments will disable them rather than remove them (buggy)
- Inventory shows quantity needed for quests
- Split project into RWEE + RWMM (resource injection framework)
- Hephaestus set to Epic
- Scrapping Legendary+ items now drops cores used to create upgrade items

### 1.1.2
- Fixes save load issue from 1.1.1
- Auto-check prepatcher and plugin versions to prevent mismatches
- “Too many equipments installed disables them” work in progress

### 1.1.1
- Reworked L55+ enemy drops so all endgame loot has a chance to drop
- Fixed Tier 7 relics not dropping
- Standardized IDs and refNames for items from this mod

### 1.1.0
- Item ID inconsistency (internal change)
- ID fix for Pirate Capital Booster
- Minor balance tweaks

### 1.0.5b
- Fix for techLevel capping at 100

### 1.0.5
- Legendary+ weapons buffed ~1.5×
- Fixed tech leveling past 100
- Fixed Mythic Relics upgrading past Tier 5
- Renamed relics and tiers from this mod
- Added Pirate Capital Booster

### 1.0.4b
- Hotfix for version popup message

### 1.0.4
- Improved high-level enemy drop distribution across equipment types
- Fixed enemy stat scaling bug from last build

### 1.0.3
- Fixed tech leveling past 50
- Sector leveling can push nearby lower-level sectors up
- Hideouts can respawn marauders
- Smarter enemy tactics over L50

### 1.0.2
- Added Tier 6/7 rarity levels + Tier 6 upgrade items from scrapping
- Existing sectors leveled up on first install

### 1.0.1
- Fixed a bug with Mythic Relics

---

## Roadmap

### 1) Near-term
- Crew allowed to evolve up to tier 7.
- Ravager re-spawning
- Swap/Launch systems:
  - Swap with fleet captain
  - Swap with a derelict (leaving your ship derelict or FO-piloted)
  - Launch from inventory into a stored ship
- Unlock Lacewing (possibly as a derelict) after completing the appropriate perk
- Late-game questline to obtain **Thoth**
- Higher MK levels of QoL items (scanners/warp/collector beams/etc.)
  - More advanced sensors (detect stations in sector before visiting)
  - Higher-level Battle Computer (more opponent info)
- Background perk changes (no lockouts; faction diplomacy rework)
- Turret stat scaling in high-level sectors

### Long-term ideas (This section is basically my scratchpad for long-term ideas.  Expect duplicates and half-formed ideas.)
- Higher mk levels of other items.  (This requires more balance consideration, so is listed separately from above)
  - More faction specific items.  Especially new chains that don't have faction specific versions, like Gyroscopes, etc.
  - Item that increases turret rotation speed.
- new bases spawn in sectors that had a base defeated.  IE, if you help PCM destroy a Pirate base, the sector should spawn a PCM base sometime later.
- base spawning for factions with low base-counts.
- Add some high level perks to challenge people.  ie, defeat a L200 hunter crew, ravager, etc.
- Attacking a fleet member should make the whole fleet hostile.
- Attacking/destroying a turret should make the whole sector hostile.
- Faction launches hunter fleet if you threaten a station.
- Equipment that adds Bounty (You will more often be attacked by...)
- Auto updater for mod framework.
- Equipment that converts equipment/cargo/hanger/crew/etc between each other.
  - Add crew slot.  Limit 1 per type, so this only helps ships that don't already have that crew type.
- Way to make smaller ships more competitive late-game.
  - Maybe big ships are vulnerable from behind?  Causing extra damage, and debufs?
- Way to make ramming builds more viable late-game
- Maybe a separate higher-level version of Space Pilot and Fleet Commander that only starts when the base skills are maxed out.  This would allow the game to keep the standard L55-60 skills as hard to get, but introduce a new set of skill upgrades.
- Allow higher versions of items (mk, etc.) to be intentionally turned down to mimic their weaker versions.
- When enemies run from you due to low health, they should flee to their own bases, turret clusteres if possible.
- Add Bounty to end-game ships.
- Balance for ultra-late-game sectors L150+
  - Tiers 8 (Celestial) and 9(Transcendent)
  - allow very high level enemies to drop ship blueprints, possibly including unique faction ships
- Make Hephaestus harder to get, but better.
- UI improvements:
  - Controls active with map open.
  - Speed boosters as a toggle, rather than hold.
  - Map UI updates (Maybe a separate mod?)
  - Crafting UI updates (maybe a separate mod?)
  - Button to hide/forget a recipe from the crafting list.
  - name of item spotted on minimap
- Autopilot
  - Maybe as an equipment module.
  - Auto fly to area highlighted on local map.

---
