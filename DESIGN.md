# Conway's World — Design Document

> Authored from the original Unity source (`Assets/Scripts/ConwaysWorld.cs`, `Assets/Scripts/Model.cs`,
> and the individual cell scripts) and the current Blazor WebAssembly port.
> The web port lives in `ConwaysWorld.Simulation/` (pure C#) and `ConwaysWorld.Blazor/` (Blazor WASM UI).

---

## Table of Contents

1. [Simulation Overview](#1-simulation-overview)
2. [Step Pipeline](#2-step-pipeline)
3. [Grid & Toroidal Addressing](#3-grid--toroidal-addressing)
4. [Cell Types](#4-cell-types)
5. [Condition / Tag System](#5-condition--tag-system)
6. [Nations System](#6-nations-system)
7. [Disease & Immunity](#7-disease--immunity)
8. [Random Life Injection](#8-random-life-injection)
9. [Grid Expansion (Explorer)](#9-grid-expansion-explorer)
10. [Spawn Weights & Settings](#10-spawn-weights--settings)
11. [Original TODO Comments](#11-original-todo-comments)
12. [Future Cell Type Ideas](#12-future-cell-type-ideas)
13. [Architecture Notes](#13-architecture-notes)

---

## 1. Simulation Overview

Conway's World is an extension of Conway's Game of Life. The standard two-state (alive/dead) grid
is augmented with **13 distinct cell types**, each overriding survival, movement, or interaction rules.
Cells form **nations** that compete, trade influence, and wage war.

The simulation is written as a pure C# library (`ConwaysWorld.Simulation`) with no Unity, Blazor,
or ASP.NET dependencies, making it directly portable to Unity or any other C# host.

---

## 2. Step Pipeline

Each generation runs the following eight phases in order:

| Phase | Method | What it does |
|-------|---------|-------------|
| 1 | `UpdateNeighborhoodsGrid` | Rebuilds the Moore neighbourhood for every cell (toroidal wrap). |
| 2 | `UpdateAliveNextGenGrid` | Asks each cell `CalcCellAliveNextGen()` and stores the answer in a scratch grid. |
| 3 | `UpdateCellLives` | Applies live/die decisions; calls `Cell.Live()` or `Cell.Die()`. |
| 4 | `UpdateCellConditions` | Processes condition tags: disease infection, breeding, `toWar` promotion, Hunter/Warrior demotion, Explorer resize trigger. |
| 5 | `UpdateNeighborhoodsGrid` | Rebuilds again because conditions may have replaced cells. |
| 6 | `UpdateSpecialActions` | Runs `Cell.SpecialActions()` — movement, combat, disease spread. |
| 7 | `AddRandomLife` | If population density drops below `MinLifePercent`, injects a new batch. |
| 8 | `UpdateNations` | Runs census; elects Diplomats; crowns Kings; creates new nation slots if needed. |

---

## 3. Grid & Toroidal Addressing

- The grid is a 2-D `Cell[columns, rows]` array.
- All neighbour lookups use modular arithmetic (`(x + offset + size) % size`) so the grid wraps
  seamlessly — cells on the left edge are neighbours of cells on the right edge, and likewise
  vertically.
- The 8-cell Moore neighbourhood is stored in `Cell_Neighborhood` with named direction keys:
  `northWest`, `north`, `northEast`, `west`, `east`, `southWest`, `south`, `southEast`, `center`.
- Neighbourhoods are rebuilt from scratch each step (phases 1 and 5) because moving cells
  invalidate the cached references.

---

## 4. Cell Types

### Basic
The standard Conway cell. Survives with 2–3 living neighbours; born into empty cells with exactly
2 neighbours; dies from under- or over-population.

Spawn-time traits (applied by `Cell_Generator`):
- **25 % immune** — all `d_`/`p_` disease tags are stripped from this cell every step.
- **1 % immaculate** — triggers a one-time forced-birth of two axis-aligned neighbours.

Can be promoted to **Warrior** by a King's `MakeArmy` action.

---

### Immortal
Ignores Conway population rules entirely — always votes to survive. The only way an Immortal
dies is isolation: if it has zero living neighbours for more than **8 consecutive steps** it dies.

- Unconditionally immune to disease (cannot be infected or converted).
- Targeted by Hunter as a priority prey type.

---

### Diseased
Carries a unique `d_XXXXXXXX` strain tag. Behaviour:
- Dies after a **3-step countdown** regardless of Conway rules (though it can die earlier from under-population).
- Each step rolls a **10 % per-neighbour** infection chance; on success, the strain tag is added to
  the neighbour's conditions. The Model converts the neighbour to a Diseased cell on the next conditions pass.
- Blocked by: `immune` condition, Immortal type, existing `vax_<strain>` vaccination.

---

### Plague
Identical to Diseased but:
- Carries a `p_XXXXXXXX` strain prefix.
- Transmission rate is **40 % higher** — `round(10 × 1.4) = 14` per 100 rolls.

---

### Traveler
Swaps position with a random neighbour every step.

Survival rules (overrides Conway):
- Always votes to survive.
- Dies if isolated (zero neighbours) for more than **3 consecutive steps**.
- Dies if fully surrounded (8 neighbours) for more than **3 consecutive steps** (crush countdown).

Base class for Explorer, Hunter, and Warrior.

---

### Explorer
A Traveler that triggers **grid expansion** when it reaches a border cell.

- When an Explorer occupies a slot at the grid edge (column 0, column max, row 0, row max),
  `UpdateCellConditions` schedules `ResizeCellGrid`, which grows the grid by one dead-Basic
  border on all four sides — provided the grid has not yet reached `MaxGridSize`.
- Isolation tolerance is **4 steps** instead of 3.

---

### Doctor
Cures disease in adjacent cells and stamps vaccination markers.

Each step:
1. **Cure**: removes known `d_`/`p_` conditions from living neighbours; stamps `vax_<strain>`;
   converts full Diseased/Plague cells back to Basic.
2. **Seek**: scans neighbours for new disease tags to add to its known-strains set.

Survival: survives unconditionally if it performed **at least one new vaccination** this step;
otherwise falls back to standard Conway rules. This causes Doctors to die out naturally when
there is no disease to cure.

---

### Hunter
Pursues and kills Immortals and Kings.

- Scans up to **range 5** for a prey cell.
- Steps one cell toward it each turn via `FindNeighborInDirOfCell` (shortest toroidal path).
- Kills prey on contact by calling `Die()` and marking it `"cleanup"`.
- If no prey is visible, moves randomly like a Traveler.
- **Idle demotion**: demotes to Basic after **3 consecutive steps** without a kill.

---

### Warrior
Extends Hunter with a combat system targeting foreign Diseased/Plague cells.

- Prey: alive cells of a **different nation** with type Diseased or Plague (range 2).
- Combat vs. another Warrior: **strength comparison** decides the winner.
  - Strength = count of same-nation neighbours (+1 per Warrior, +2 per King among them).
  - Older combatant gets +1 power. Ties are coin-flipped.
  - Loser is killed and marked `"cleanup"`.
- Warriors are only created when a King marks a neighbouring Basic cell with `"toWar"`.
- **Idle demotion**: demotes to Basic after **3 consecutive steps** without a fight.

---

### Bomber
A suicidal cell that detonates after reaching **age 2**.

- Votes to survive unconditionally until detonation.
- On detonation: calls `Die()` on every living cell within a **2-cell Chebyshev radius** (a 5×5 area), then dies itself.

---

### Diplomat
Elected by its nation to travel toward foreign nations and convert their cells.

Each step:
1. **Convert**: for each adjacent foreign living cell, **25 % chance** of converting its nationality.
2. **Move**: steps one cell toward the nearest foreign living cell within **range 8**.

Election criteria (run by `Cell_Nation.ElectDiplomat`):
- Nation must have ≥ 10 citizens.
- Diplomat count must be < 5 % of citizen count.

---

### King
The highest-ranked cell in its nation. Each step marks every adjacent living Basic cell with
the `"toWar"` condition, converting them to Warriors on the next conditions pass.

Crowning criteria (run by `Cell_Nation.CrownKing`):
- Nation must have ≥ 5 citizens and more citizens than Diplomats.
- Only one King can exist per nation; a new one is crowned when the old one dies.

Kings are targeted by Hunter as priority prey.

---

## 5. Condition / Tag System

Conditions are string tags in `Cell.Conditions` (`HashSet<string>`).
They are processed during `UpdateCellConditions` each step.

| Tag | Applied by | Effect when processed |
|-----|-----------|----------------------|
| `immune` | `Cell_Generator` (25 % on Basic) | Strips all `d_`/`p_` tags; cell can never be infected. |
| `immaculate` | `Cell_Generator` (1 % on Basic) | One-time: forces this cell + two axis-aligned neighbours alive (if they have zero neighbours). |
| `mature` | `Cell.Live()` when age > `MatureAge` | Triggers `Cell.Breed()` — spawns one offspring of same type into a random empty neighbour. |
| `d_XXXXXXXX` | `Cell_Diseased.SpreadDisease` | Converts the cell to a Diseased type via `Cell_Diseased.Infect`. |
| `p_XXXXXXXX` | `Cell_Plague.SpreadDisease` | Converts the cell to a Plague type. |
| `vax_<strain>` | `Cell_Doctor.CureDisease` | Prevents re-infection by that specific strain. |
| `toWar` | `Cell_King.MakeArmy` | Converts a Basic cell to a Warrior. |
| `cleanup` | Various (warriors, Kings on death, cells killed in combat) | Replaces the entire cell slot with a dead Basic cell. |
| `exploring` | `Cell_Explorer.Live()` | Informational — set when the Explorer's direction crosses the grid edge. Cleared in `SpecialActions`. |

---

## 6. Nations System

Nations group living cells by a shared integer index stored in `Cell.Nationality` (-1 = none).

**Nation assignment**: each step, a living cell with `Nationality == -1` inherits the nationality
of a random living neighbour that already has one (`Cell.ChooseNation`). After age 1, any still-
unaffiliated cell gets a random nation index from the existing pool.

**Nation creation**: the number of nation slots is derived from the living population:

```
numNations = floor(BasePercentLiving × columns × rows / MinCellsPerNation)
```

capped at `MaxNations` (default 20) and `Cell_Nation.NationColors.Count` (20).

**Nation census** (runs once per step per nation):
1. Scans the full grid to rebuild `CitizensList` and `DiplomatsList`.
2. `ElectDiplomat`: if the nation has ≥ 10 citizens and fewer Diplomats than 5 % of citizens,
   promotes a random non-King citizen to Diplomat.
3. `CrownKing`: if the nation has ≥ 5 citizens, more citizens than Diplomats, and no living King,
   promotes a random citizen to King.

**Nation colours**: each nation index maps to a fixed hex colour from `Cell_Nation.NationColors`
(20 entries). The JS canvas renderer uses these to tint living cells by nation.

---

## 7. Disease & Immunity

- Each Diseased/Plague cell generates a **unique strain** at spawn (`d_XXXXXXXX` / `p_XXXXXXXX`).
- Spread is tag-based: the strain tag is added to a neighbour's `Conditions`; on the next step
  the Model converts the neighbour's cell type.
- **Immune cells** (`immune` condition) have all disease tags stripped each step — they can be
  tagged but never actually converted.
- **Immortal cells** are skipped entirely by `SpreadDisease` and by `Cell_Diseased.Infect`.
- **Vaccinated cells** (`vax_<strain>`) are immune to that specific strain only.
- **Doctors** can accumulate immunity to multiple strains over time as they encounter new diseases.

---

## 8. Random Life Injection

If `currentPopulation / totalCells < MinLifePercent` (default 5 %) at the end of a step,
`AddRandomLife` injects a batch of new cells:

- Batch size = `PopValue` cells (count mode) or `totalCells × PopValue / 100` (percent mode).
- Cells are placed at random empty slots (up to 10× the batch size in attempts).
- Uses the same `Cell_Generator` as grid initialisation, so spawn weights apply.

---

## 9. Grid Expansion (Explorer)

When an Explorer reaches a cell at the grid boundary, `UpdateCellConditions` sets a `needResize`
flag. After processing all cells, if the flag is set and the grid is not already at `MaxGridSize`,
`ResizeCellGrid` runs:

1. Allocates a new grid `(columns + 2) × (rows + 2)`.
2. Fills the new outer border with dead Basic cells.
3. Copies all existing cells inward, updating their `Column`/`Row` fields.
4. Rebuilds all neighbourhoods.

This means a single Explorer can incrementally grow the grid from `StartColumns × StartRows`
up to `MaxGridSize × MaxGridSize` over time.

---

## 10. Spawn Weights & Settings

Default spawn weights (relative, enabled types only):

| Cell Type | Weight | Notes |
|-----------|--------|-------|
| Basic | 50 | 25 % immune, 1 % immaculate at spawn |
| Diseased | 15 | 20 % chance produces Plague instead |
| Bomber | 8 | — |
| Traveler | 6 | 40 % chance produces Explorer instead |
| Doctor | 5 | — |
| Hunter | 5 | — |
| Explorer | 3 | — |
| Plague | 3 | — |
| Immortal | 2 | — |

Warriors, Diplomats, and Kings are **never spawned** — they are promoted/elected in-game.

Key settings and their defaults:

| Setting | Default | Description |
|---------|---------|-------------|
| `StartColumns` / `StartRows` | 40 × 40 | Initial grid dimensions. |
| `MaxGridSize` | 120 | Max column or row count before Explorer expansion stops. |
| `UserCellSize` | 14 px | Canvas cell rendering size. |
| `PopMode` | Percent | How `PopValue` is interpreted. |
| `PopValue` | 10 | 10 % of grid = ~160 cells on a 40×40 grid. |
| `MinCellsPerNation` | 3 | Controls how many nations can exist. |
| `MaxNations` | 20 | Hard cap on concurrent nations. |
| `MinLifePercent` | 5 % | Population floor below which random life is injected. |

---

## 11. Original TODO Comments

The following are preserved from the original Unity source (`Assets/Scripts/ConwaysWorld.cs`
and `Assets/Scripts/Model.cs`) exactly as written. They represent open design questions and
improvement ideas from the original author.

### Mechanics

> `TODO: maybe cells should be more likely to survive death for every neighbor of the same nation?`

Possible implementation: in `Cell.LiveBasic()`, add a leniency bonus based on the fraction of
neighbours sharing `Nationality`. E.g., survival threshold expands from 2–3 to 2–4 when all
living neighbours are of the same nation.

---

> `TODO: grid size limits don't seem to work...`

The current port calculates `IsMaxGrid()` from `MaxGridSize` compared against both `_columns`
and `_rows`. If the original Unity version compared against total cell count instead, the
semantics differ. Worth testing with a live Explorer run.

---

> `TODO: the currentPopulation count is off because it gets updated by special moves...`

Traveler/Explorer swaps, Doctor conversions, and Warrior kills can change whether a slot is
alive mid-step. `_currentPopulation` is counted during `UpdateCellLives` (phase 3) before
`SpecialActions` (phase 6). A true end-of-step count would require a second full-grid scan.

---

> `TODO: make MinLivingNeighbors and MaxLivingNeighbors for cells accessible...`

These are currently public fields on `Cell`. Exposing them via `SimulationSettings` would let
the player tune survival rules per cell type at runtime without recompiling.

---

> `TODO: add class and method descriptions using /// notation`

**Done** — all classes and public methods in `ConwaysWorld.Simulation` now carry XML doc comments.

---

> `TODO: add a Cell_Grid type to contain all grid-based functions`

The grid-management logic currently lives in `Model.cs` (`UpdateNeighborhoodsGrid`,
`ResizeCellGrid`, `UpdateCellConditions`, etc.). Extracting these into a dedicated `Cell_Grid`
class would improve separation of concerns and make the Model a pure orchestrator.

---

> `TODO: make minimum allowable grid size 5x5`

Neither the original nor the port enforces a lower bound on `StartColumns`/`StartRows`.
A guard in `Model.Restart()` / `Model.PopulateGrid()` would prevent degenerate grids.

---

> `TODO: utilize a Number of Islands and a Max/Min size of an island algorithm...`

The original idea was to use a flood-fill island-detection pass at grid generation to ensure
initial cells form coherent groups rather than random scatter. This would make early nation
formation more geographically meaningful.

---

> `TODO: move nation/cell colors to View`

Currently `Cell_Nation.NationColors` is a static list in the simulation library. Moving it to
the Blazor UI layer (`ConwaysWorld.Blazor`) would keep the pure-C# library truly dependency-free
and let different hosts (Unity, web, console) define their own colour palettes.

---

> `TODO: change sprites to sprite atlas`

The current web UI loads individual `.jpg` files per cell type from `wwwroot/Assets/Sprites/`.
A sprite atlas would reduce HTTP requests and allow GPU-efficient batched rendering on the canvas.

---

> `TODO: animate changes between updates`

Currently each step is rendered as an instant frame swap. Interpolating cell births, deaths, and
movements over the inter-step interval would make the simulation visually smoother.

---

> `TODO: visualize per cell`

Possibility: clicking a cell opens a detail panel showing its full condition set, age,
nation membership, current prey (for Hunters), disease strain, etc.

---

### Model

> `TODO (Model.cs): make Census() a static function that returns a new nations dictionary`

`Cell_Nation.Census()` currently mutates the nation's own `CitizensList`/`DiplomatsList`.
Making it static and returning a value type would simplify state management and make Census
thread-safe, which matters if the simulation is ever parallelised.

---

### Hunter / Warrior

> `TODO (Cell_Hunter.cs / Cell_Warrior.cs): is this valid? or should I mark the cell for death and let the model kill it somehow?`

In both types, `Die()` is called directly on prey cells from within `SpecialActions`.
The alternative — marking with `"cleanup"` and deferring to `UpdateCellConditions` — would
guarantee that all live/die decisions happen in phase 3 rather than being spread across phases,
making the simulation more deterministic and easier to reason about.

---

## 12. Future Cell Type Ideas

The following new cell types were noted in the original `ConwaysWorld.cs` (lines 137–148).
None are currently implemented.

| Name | Concept |
|------|---------|
| **Voyager** | Explorer variant that targets foreign nations; upon arrival spawns Diplomats or spreads Plague. |
| **Necromancer** | Revives recently dead neighbours; dead cells it revives become Zombies. |
| **Zombie** | Linked to Necromancer; survives over-population that would kill a Basic cell. |
| **Mutant / Mutator** | Randomly alters the type of surrounding cells each step. |
| **Islander** | Prefers empty space; dies if it has too many living neighbours (inverse of standard Conway). |
| **Savior** | A moving cell that other cells "follow" — neighbours shift toward it each step. |
| **Conqueror** | Moves in a fixed direction until it hits a foreign-nation cell, then attempts to convert the entire surrounding area. |
| **Teacher / Elder** | Promotes adjacent Basic cells to more complex types over time. |
| **Irradiated** | Kills any cell that enters its immediate neighbourhood; causes permanent death (no random-life reinsertion for killed slots). |
| **Spy** | Can move through living neighbours (swaps without triggering combat or disease). |
| **God** | Has a global effect on every cell on the board each step (exact mechanic TBD). |

---

## 13. Architecture Notes

### Simulation library (`ConwaysWorld.Simulation`)
- Pure C# — no Unity, Blazor, or ASP.NET dependencies.
- Uses only `System.*` namespaces.
- Randomness is centralised in `SimRandom` (wraps `System.Random`) for easy swapping with `UnityEngine.Random`.
- Tab-indented throughout; XML doc comments on all public members.

### Unity portability
1. Copy `ConwaysWorld.Simulation/` into `Assets/Scripts/` in Unity.
2. Replace `SimRandom.Range/Value/CoinFlip` calls with `UnityEngine.Random` equivalents.
3. Wire `Model.Step()` into a `MonoBehaviour.Update()` or `InvokeRepeating`.
4. Replace the JS canvas renderer with a Unity `Tilemap` or `SpriteRenderer` grid.

### Web front-end (`ConwaysWorld.Blazor`)
- Blazor WebAssembly; single-page app served on port 5000.
- Simulation state lives in the C# layer; rendering is delegated to a JavaScript canvas interop (`wwwroot/canvas-interop.js`).
- Cell sprites: `wwwroot/Assets/Sprites/Cell_<Type>.jpg` (one per type, including future types with placeholder art).
- Nation colours applied as tinted overlays on the JS canvas.
