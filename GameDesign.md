# Dungeon — Game Design Document

## Overview

**Working Title:** Dungeon  
**Genre:** 2D top-down management / dungeon-keeper hybrid (Stardew Valley pacing, Graveyard Keeper art angle)  
**Engine:** Unity 6000.4.7f1  
**Platform:** PC (primary)  
**Perspective:** 2D top-down with slight isometric-style angle; one elevation layer visible at a time

---

## Core Concept

Two brothers share a single fate. One died, one lived. The dead brother inhabits his underground crypt and cannot leave it — he must feed on life energy to survive, which he harvests from adventurers who enter. The living brother runs a shop in the village above, acquiring adventurers' leftover gear and funneling both money and materials back into the dungeon. Both halves must thrive together or neither will.

---

## World Structure

### Spatial Layout
- The world is an infinite XZ plane divided into a grid of 1m × 1m cells.
- Elevation layers sit on the Y axis: `Y=0` is the surface, `Y=-1` is the first underground layer, `Y=-2` the second, etc.
- Only one elevation layer is rendered at a time. Transitioning between layers is handled by portals/stairs that teleport the player (and camera) to the matching position on another layer.
- The world is chunk-streamed and treated as infinite; practical map size is expected to be in the hundreds to low thousands of cells per axis.

### Surface Layer (Y=0) — The Village
- The living brother's domain.
- Contains the village, NPC homes, the shop, roads, forests, farms, etc.
- Adventurers spawn or arrive here, browse the shop, pick up quests, then head toward the dungeon entrance.

### Underground Layers (Y=-1, Y=-2, …) — The Crypt & Dungeon
- The dead brother's domain.
- Starts as a small sealed crypt; expands by mining into the rock.
- Multiple floors accessible via stairs/portals placed by the dead brother.
- Contains traps, treasure vaults, monster dens, and eventually the Tomb (lose condition trigger point).

---

## Characters

All characters in the world share the same underlying `WorldObject` entity and the same feature set. Only the player's control binding differs.

### The Dead Brother (player-controlled, underground)
- Cannot leave crypt bounds (enforced as a gameplay constraint, not a hard wall).
- Needs **Life Energy** to exist; energy drains over time and is replenished by killing/consuming adventurers.
- Actions:
  - **Mine:** Dig cells to expand the dungeon footprint; yields raw materials.
  - **Place objects:** Lay traps, treasure chests, decorations, waypoints using mined materials.
  - **Set AI routes & behaviour:** Define patrol routes and response rules for monsters (player cannot directly control monsters, only his own movement).
  - **Use shared chests:** Deposit/withdraw items to/from chests accessible by both brothers.
  - **Fight:** Can directly fight adventurers but is weak; intended as a last resort.
- Lose condition: Adventurers reach and loot/destroy his Tomb, or energy reaches zero.

### The Living Brother (player-controlled, surface)
- Runs a shop in the village, selling recovered loot and buying supplies.
- Actions:
  - **Sell items:** Price and sell adventurer loot to village NPCs or passing adventurers.
  - **Buy materials:** Purchase supplies for dungeon construction and village upkeep.
  - **Use shared chests:** Transfer materials and loot between brothers via shared chest objects.
  - **Talk to NPCs/adventurers:** Trigger scheduled dialogue lines; no formal quest system yet.
  - **Fight:** Same combat system as all characters; not combat-focused.
- Penalty on death: Loses some money, dead brother energy drops.

### Adventurers (AI-controlled NPCs)
- Arrive at the village on a fixed schedule (time-of-day or day-cycle trigger).
- Each adventurer has programmed goals and a behaviour script (explore dungeon, loot, fight, flee when low health).
- Own inventory, stats, and equipment; buy from the shop before descending if applicable.
- Dead adventurers drop loot in the dungeon for the dead brother to collect and send up via shared chest.
- No reputation tracking — adventurer flow is purely schedule-driven.

### Monsters (AI-controlled)
- Monsters are ordinary WorldObjects with the same feature set as any other character.
- Distinguished only by their `AIBrain` configuration and `Faction`.
- Follow waypoint routes; react to other factions based on configured behaviour (patrol, attack-on-sight, flee).
- Not directly controlled; the dead brother sets routes and behaviour, then they act autonomously.

### Villagers (AI-controlled)
- One villager in the MVP; has a daily schedule (home → wander → home).
- A few fixed speech lines triggered on proximity interaction.
- No quest system, no reputation tracking.

---

## Gameplay Loops

### Primary Loop
```
Adventurers arrive on schedule
  → Living brother sells gear from shop stock
  → Adventurers enter dungeon (Y=-1 and below)
  → Dead brother's traps/monsters fight them
  → Some die: loot drops, dead brother gains energy
  → Survivors flee back to surface
  → Dead brother collects loot, deposits in shared chest
  → Living brother withdraws loot, sells it, buys materials
  → Materials deposited in shared chest → dead brother withdraws, expands dungeon
  → Repeat
```

### Dead Brother Sub-loop
1. Energy low → mine aggressively, hunt adventurers directly.
2. Energy stable → expand dungeon with mined materials, place traps and treasure.
3. Configure monster waypoints and behaviour to form effective kill corridors.

### Living Brother Sub-loop
1. Withdraw loot from shared chest; price and sell from shop.
2. Buy materials and supplies; deposit in shared chest for dead brother.
3. Talk to villagers and adventurers (scheduled dialogue).
4. Manage shop stock levels.

---

## Combat

- **Direct combat:** Stardew-valley style. Player controls character movement + a single attack action. Timing and positioning matter.
- Both brothers can fight but are not combat-focused; they lose to groups.
- Adventurers and monsters fight automatically (AI-driven) when in range.
- The dead brother's primary combat power is environmental (traps, choke points, monster placement).

---

## Progression & Story

- Story-driven with NPC dialogue and player choices (no branching skill tree or hard progression gates initially).
- No explicit "win" — the story concludes after enough narrative milestones.
- No explicit "lose" — dying incurs penalties (energy drain, loot theft, money loss) but is recoverable.
- Narrative milestones (examples; to be fleshed out in a separate story doc):
  - First adventurer killed.
  - First monster hired.
  - Village reaches a population threshold.
  - Dead brother's Tomb threatened for the first time.
  - The brothers communicate / learn the truth of their situation.

---

## Level Design & Editor

- The **Level Editor** and **Play Scene** are the same Unity scene. Editor tools are an additional visual overlay toggled on/off.
- In editor mode the designer can:
  - Paint terrain tiles per cell per elevation layer.
  - Place WorldObjects (props, spawn markers, interactive objects) on specific cells.
  - Set elevation layer, switch visible layer.
  - Select / move / delete placed objects.
  - Save the authored layout to a JSON level file.
  - Load a JSON level file back into the editor.
- In play mode the same scene loads the JSON file into the logic layer and instantiates visuals from it.

---

## MVP Starting Layout

The first playable build ships with a minimal but complete world slice.

### Surface (Y=0)
| Building / Object | Count | Notes |
|-------------------|-------|-------|
| Villager house | 1 | One villager NPC with daily schedule |
| Shop (living brother) | 1 | Living brother's home and shop counter |
| Cemetery | 1 area | Decorative crosses, static props |
| Crypt entrance | 1 | Contains stairs/portal down to Y=-1 |
| Shared chest | 1 | Placed near crypt entrance; both brothers can access |

### Underground Floor 1 (Y=-1)
| Content | Notes |
|---------|-------|
| Small starting crypt room | Pre-authored in level file; sealed rock walls on all sides |
| Stairs up | Matching portal back to Y=0 crypt entrance |
| Dead brother spawn point | Starting position |
| All surrounding cells | Solid rock (mineable) — nothing placed yet |

### Characters in MVP
| Character | Faction | Controlled by |
|-----------|---------|---------------|
| Dead Brother | Crypt | Player (switchable) |
| Living Brother | Village | Player (switchable) |
| Villager | Village | AI (schedule) |
| Adventurer (1 type) | Neutral | AI (schedule + programmed behaviour) |

---

## Use Cases

| ID | Actor | Goal | Steps |
|----|-------|------|-------|
| UC-01 | Player | Switch active brother | Click portrait → camera and input transfer to selected brother |
| UC-02 | Dead Brother | Mine a cell | Move adjacent to wall → use mine action → cell tile type changes to floor, materials added to inventory |
| UC-03 | Dead Brother | Place a trap | Open placement palette → select trap → click target cell → trap WorldObject spawned, materials consumed |
| UC-04 | Dead Brother | Set monster route | Select monster → draw waypoints on grid → monster AI uses route |
| UC-05 | Dead Brother | Deposit loot in shared chest | Move to chest → open inventory → transfer items → chest inventory updated |
| UC-06 | Living Brother | Withdraw from shared chest | Move to chest → open inventory → take items → chest inventory updated |
| UC-07 | Living Brother | Sell item | Move to shop counter → interact → select item → sell; item removed from inventory |
| UC-08 | Living Brother | Buy materials | Move to shop counter → interact → select supply item → buy; item added to inventory |
| UC-09 | Any character | Descend to Y=-1 | Move to stairs/portal → interact → elevation switches, camera teleports, new layer rendered |
| UC-10 | Adventurer AI | Enter dungeon | Arrives on schedule → pathfinds to crypt entrance → descends stairs → explores and fights |
| UC-11 | Adventurer AI | Flee dungeon | Health threshold triggered → pathfinds to exit → ascends stairs → returns to surface |
| UC-12 | Villager AI | Follow schedule | Time advances → villager moves to next waypoint on daily route → triggers speech on player proximity |
| UC-13 | Level Designer | Author terrain | Toggle editor mode → select elevation layer → paint cells with tile palette |
| UC-14 | Level Designer | Place object | Select object type → click cell → object appears, saved in level data |
| UC-15 | Level Designer | Save level | Ctrl+S → serialise grid + objects to JSON → write to file |
| UC-16 | Level Designer | Load level | Open file picker → deserialise JSON → populate logic grid + spawn visuals |
| UC-17 | System | Stream chunks | Camera moves → ChunkLoader generates new chunks around view → WorldRenderer renders new chunks |
