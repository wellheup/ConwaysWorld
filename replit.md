# Conway's World

## Overview

A web-based implementation of Conway's World — a sophisticated Conway's Game of Life variant with 24 specialized cell types and a Nations system. The simulation logic is written in pure C# (Blazor WebAssembly), making it directly portable back to Unity.

## Architecture

```
ConwaysWorld.Simulation/   ← Pure C# simulation library (no Unity, no Blazor deps)
  Cells/                   ← All 24 cell type implementations
  Model.cs                 ← Simulation step orchestration
  Cell_Nation.cs           ← Nations: census, diplomat election, king crowning
  SimulationSettings.cs    ← All configurable parameters

ConwaysWorld.Blazor/       ← Blazor WebAssembly frontend
  Pages/Index.razor        ← Main page: canvas, sidebar, toolbar, settings
  ts/canvas-interop.ts     ← TypeScript source: canvas rendering, zoom/pan, keyboard shortcuts
  wwwroot/canvas-interop.js ← Compiled JS (always edit the .ts, then run npx tsc)
  wwwroot/css/app.css      ← All styles

Assets/Scripts/            ← Original Unity C# source (reference only, not compiled)
index.html                 ← Original JS implementation (preserved as reference)
server.js                  ← Original Node.js server (preserved as reference)
```

## Running the App

The app is served by the Blazor WASM dev server:

```
dotnet run --project ConwaysWorld.Blazor/ConwaysWorld.Blazor.csproj --urls http://0.0.0.0:5000
```

Or just use the **Start application** workflow in Replit.

## Cell Types

| # | Type | Color | Behavior |
|---|------|-------|----------|
| 1 | Basic | purple | Standard Conway cells; 25% immune chance; 1% immaculate at spawn |
| 2 | Immortal | yellow | Lives forever unless isolated >8 steps; immune to disease |
| 3 | Diseased | green | Spreads `d_` strain to neighbours; dies after 3-step countdown |
| 4 | Plague | dark green | Like Diseased but 40% higher transmission rate (`p_` strain) |
| 5 | Traveler | cyan | Moves each step; dies if isolated >3 steps or surrounded >3 steps |
| 6 | Explorer | light cyan | Like Traveler; triggers grid expansion at edges |
| 7 | Doctor | pink | Cures nearby disease; stamps `vax_` immunity markers; survives while active |
| 8 | Warrior | red | Fights foreign Diseased/Plague within range 2; also hunts Saviors/Followers regardless of nation; demotes to Basic after 3 idle steps |
| 9 | Hunter | orange | Hunts Immortals and Kings within range 5; also hunts Saviors/Followers regardless of nation; demotes to Basic after 3 idle steps |
| 10 | Bomber | dark red | Detonates at age 2, killing all cells within a 2-cell radius |
| 11 | Diplomat | blue | Elected from large nations; travels to foreign nations and converts adjacent cells |
| 12 | King | gold | Crowned from nations with ≥5 citizens; marks neighbouring Basic cells with `toWar`; death triggers Basic-cell neutralisation cooldown for distant cells |
| 13 | Rebel | light red | Short-lived diplomat variant with 3× conversion rate; created by Revolutionaries; hunted by Warriors and Hunters |
| 14 | Revolutionary | dark purple | Defects from a dominant nation, founds a rival nation, recruits Warriors and Rebels from the old homeland |
| 15 | Voyager | teal | Travels to a disconnected foreign nation; on arrival either spawns diplomats and warriors (→ Explorer) or seeds 4 Plague cells |
| 16 | Wayfinder | light green | Finds the emptiest grid region and travels there; on arrival spawns 5 Islander cells |
| 17 | Islander | sand | Nationless; lives by Conway rules but dies from overcrowding (20+ cells within 5 tiles); converts to Barbarian when touched by a nation cell |
| 18 | Barbarian | brown | Nationless aggressor spawned from Islanders; converts adjacent Islanders and kills nearby nation cells; reverts to Islander when no targets remain |
| 19 | Spy | grey-blue | Infiltrates enemy territory from a minority nation; seeks the enemy King by swapping through living cells, converting each displaced cell into a Soldier |
| 20 | Soldier | steel blue | Combat cell created by Spies and Conquistadors; kills adjacent enemies and advances on distant ones; triggers a nation-merge check when the last of its wave dies |
| 21 | Conquistador | dark orange | Like Voyager but on arrival teleports the nearest 10 home-nation cells to the landing zone and converts them (+ itself) into Soldiers |
| 22 | Savior | white | At most one per grid; requires ≥2 nations. Flees its birth nation toward a random foreign nation, converting adjacent Basic cells into Followers (50% chance/step). On reaching the target King: 50% assimilates (→ Immortal, Followers → Basic in target nation) or 50% dies (Followers → Zealots). Immune to Conway rules; hunted by Warriors and Hunters of all nations |
| 23 | Follower | light blue | Created by a Savior. Waits 3 steps then follows the Savior's last broadcast direction (1 cell/step). Blocked by Kings, Revolutionaries, and other Followers; reverts to Basic after 4 consecutive blocked steps. Immune to Conway rules; hunted by Warriors and Hunters of all nations |
| 24 | Zealot | red-orange | Created when a Savior dies. Attacks any adjacent living cell regardless of nation; advances toward the nearest living cell if no adjacent target |
| 25 | Irradiated | bright green | Permanent hazard tile; kills any cell that swaps onto it; not counted as living |
| 26 | PlagueRat | dark red | Nationless roamer; spreads `r_` plague strain; hunted by Warriors & Hunters |
| 27 | Zombie | near-black | Resurrected by a Necromancer; retains visual appearance of original type; immune to Conway rules, disease, old age; invisible to non-zombie cells' Conway counts; dies when its Necromancer dies; permanently destroyed (no revival) when killed by a Doctor/Warrior/Hunter |
| 28 | Necromancer | near-black | Spawns randomly; on spawn resurrects nearest 3 dead cells with prior types as zombies; each step resurrects 1 more; survives while ≥2 zombies alive; death kills all its zombies |

## Nation System

- **Census** — every step, cells are counted per nationality; Kings and Diplomats are elected/crowned from nations above population thresholds.
- **King crowning** — a nation with ≥5 citizens may crown a King, which marks nearby Basic cells with `toWar` to promote them to Warriors.
- **King-distance neutralisation** — Basic cells further than `(columns + rows) / 3` from their King lose their nationality and gain a 3-step `neutral_cooldown` tag. Once the cooldown expires they can naturally re-join a nearby nation via the standard nation-join mechanic.
- **Diplomat election** — large nations elect a Diplomat that travels to the nearest foreign nation and converts adjacent cells.
- **Revolutionary defection** — when a nation becomes too dominant, a member may become a Revolutionary, splitting off a rival nation.

## Key Design Details

- **Warrior/Hunter idle demotion** — demote to Basic after 3 idle steps
- **Warrior/Hunter nation-agnostic prey** — Saviors and Followers are hunted by Warriors and Hunters of *any* nation (not just foreign)
- **Doctor `vax_` vaccination** — `vax_<strain>` markers prevent re-infection; doctor only earns survival credit for new vaccinations
- **Plague transmission** — 40% higher than Diseased (`Math.Round(base * 1.4)`)
- **Immortal disease immunity** — immune to both disease spread and conversion
- **CrushCountDown** — Traveler/Explorer die if fully surrounded for >3 steps
- **Conditions as HashSet** — O(1) lookup vs original List
- **Separate start/max grid size** — `StartColumns/Rows` vs `MaxGridSize`
- **Pop mode** — percent of grid or fixed cell count
- **Single Savior guard** — `CanSpawnSaviorNow()` callback on the generator; returns false if a Savior is already alive or fewer than 2 nations exist

## Controls

| Input | Action |
|-------|--------|
| Space | Play / Pause |
| R | Restart |
| E | Toggle Edit Mode (auto-pauses simulation) |
| Escape | Exit Edit Mode / close modal |
| Scroll wheel | Zoom in/out |
| Right-click drag | Pan |
| Double-click | Reset zoom/pan |
| Left-click | Select cell (or paint in Edit Mode) |
| Hover | Tooltip: type, nation, age, conditions |

## Edit Mode

Press **E** or click **✏ Edit** in the toolbar to enter Edit Mode (simulation auto-pauses).

- **Paint** — click or click-drag to stamp cells. Choose any type from the Edit tab palette.
- **Erase** — select the Eraser from the palette and paint over cells to remove them.
- **Nation** — pick a nation (Nationless or Nation 1–N) from the dropdown; only applies to nation-capable types.
- **Move** — toggle Move Mode, then drag a cell from one tile to another.
- **Undo / Redo** — full stroke-level undo/redo (up to 200 history entries).
- **Clear All** — removes every living cell (undoable).
- Press **Escape** or click **✏ Edit** again to exit Edit Mode.

## Editing the TypeScript Canvas Layer

The canvas renderer is written in TypeScript. Always edit the `.ts` source and recompile:

```
cd ConwaysWorld.Blazor && npx tsc
```

The compiled `wwwroot/canvas-interop.js` is what the app loads — never edit the `.js` directly.

## Unity Portability

`ConwaysWorld.Simulation` uses only `System.*` — no Unity, Blazor, or ASP.NET deps. To use it in Unity:

1. Copy the `ConwaysWorld.Simulation/` folder into your Unity project's `Assets/Scripts/`
2. Replace `SimRandom.*` calls with `UnityEngine.Random.*` equivalents
3. Wire `Model.Step()` into `MonoBehaviour.Update()` or `InvokeRepeating`

## Deployment

Configured for **autoscale** deployment via `dotnet run` on port 5000.

## User Preferences

- Simulation library must remain pure C# with no framework dependencies
