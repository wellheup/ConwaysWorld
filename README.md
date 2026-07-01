# Conway's World

A browser-based simulation built on Conway's Game of Life, extended with **28 specialised cell types**, a full Nations system, diplomacy, combat, disease, and animated rendering. The simulation logic is written in pure C# (Blazor WebAssembly) and is directly portable to Unity.

🌐 **[Play it on GitHub Pages](https://wellheup.github.io/ConwaysWorld/)**

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0 | Builds and runs the Blazor WebAssembly app |
| [Node.js](https://nodejs.org/) | 18 or later | Compiles TypeScript |

---

## Running Locally

```bash
git clone https://github.com/wellheup/ConwaysWorld.git
cd ConwaysWorld
bash run.sh
```

`run.sh` formats the C# source, compiles TypeScript, and starts the Blazor dev server on **http://localhost:5000**.

---

## Controls

| Input | Action |
|-------|--------|
| **Space** | Play / Pause |
| **R** | Restart |
| **E** | Toggle Edit Mode (auto-pauses simulation) |
| **Escape** | Exit Edit Mode / close modal |
| **Scroll wheel** | Zoom in / out |
| **Right-click drag** | Pan |
| **Double-click** | Reset zoom and pan |
| **Left-click** | Select cell (or paint in Edit Mode) |
| **Hover** | Tooltip: type, nation, age, conditions |

The toolbar and sidebar are collapsible overlays — on mobile both auto-collapse to give the grid the full screen.

---

## Edit Mode

Press **E** or click **✏ Edit** in the toolbar to enter Edit Mode (simulation auto-pauses).

- **Paint** — click or drag to stamp any cell type from the palette
- **Erase** — select the Eraser and paint to remove cells
- **Nation** — choose a nation from the dropdown (Nationless or Nation 1–N)
- **Move** — toggle Move Mode and drag cells between tiles
- **Undo / Redo** — stroke-level history (up to 200 entries)
- **Clear All** — removes every living cell (undoable)

---

## Cell Types

| # | Type | Colour | Behaviour |
|---|------|--------|-----------|
| 1 | Basic | purple | Standard Conway rules; 25% immune chance; 1% immaculate at spawn |
| 2 | Immortal | yellow | Lives forever unless isolated >8 steps; immune to disease |
| 3 | Diseased | green | Spreads `d_` strain to neighbours; dies after 3-step countdown |
| 4 | Plague | dark green | Like Diseased but 40% higher transmission rate (`p_` strain) |
| 5 | Traveler | cyan | Moves each step; dies if isolated >3 steps or surrounded >3 steps |
| 6 | Explorer | light cyan | Like Traveler; triggers grid expansion at edges |
| 7 | Doctor | pink | Cures nearby disease; stamps `vax_` immunity markers; survives while active |
| 8 | Warrior | red | Fights foreign Diseased/Plague within range 2; hunts Saviors/Followers of any nation; demotes to Basic after 3 idle steps |
| 9 | Hunter | orange | Hunts Immortals and Kings within range 5; hunts Saviors/Followers of any nation; demotes to Basic after 3 idle steps |
| 10 | Bomber | dark red | Detonates at age 2, killing all cells within a 2-cell radius |
| 11 | Diplomat | blue | Elected from large nations; travels to foreign nations and converts adjacent cells |
| 12 | King | gold | Crowned from nations with ≥5 citizens; marks nearby Basic cells with `toWar`; death triggers neutralisation cooldown for distant cells |
| 13 | Rebel | light red | Short-lived diplomat variant with 3× conversion rate; created by Revolutionaries; hunted by Warriors and Hunters |
| 14 | Revolutionary | dark purple | Defects from a dominant nation, founds a rival nation, recruits Warriors and Rebels from the old homeland |
| 15 | Voyager | teal | Travels to a disconnected foreign nation; on arrival spawns Diplomats and Warriors, or seeds 4 Plague cells |
| 16 | Wayfinder | light green | Finds the emptiest grid region and travels there; on arrival spawns 5 Islander cells |
| 17 | Islander | sand | Nationless; lives by Conway rules but dies from overcrowding (20+ cells within 5 tiles); converts to Barbarian when touched by a nation cell |
| 18 | Barbarian | brown | Nationless aggressor; converts adjacent Islanders and kills nearby nation cells; reverts to Islander when no targets remain |
| 19 | Spy | grey-blue | Infiltrates enemy territory; seeks the enemy King by swapping through living cells, converting each displaced cell into a Soldier |
| 20 | Soldier | steel blue | Combat cell created by Spies and Conquistadors; kills adjacent enemies; triggers a nation-merge check when the last of its wave dies |
| 21 | Conquistador | dark orange | Like Voyager but teleports the nearest 10 home-nation cells to the landing zone and converts them into Soldiers |
| 22 | Savior | white | At most one per grid (requires ≥2 nations). Flees toward a random foreign nation, converting Basic cells into Followers. On reaching the target King: 50% assimilates or 50% dies (spawning Zealots). Immune to Conway rules; hunted by all nations |
| 23 | Follower | light blue | Created by a Savior; follows the Savior's direction after a 3-step delay; immune to Conway rules; hunted by all nations |
| 24 | Zealot | red-orange | Created when a Savior dies; attacks any adjacent living cell regardless of nation |
| 25 | Irradiated | bright green | Permanent hazard tile; kills any cell that moves onto it; not counted as living |
| 26 | PlagueRat | dark red | Nationless roamer; spreads `r_` plague strain; hunted by Warriors and Hunters |
| 27 | Zombie | near-black | Resurrected by a Necromancer; retains original appearance; immune to Conway rules, disease, and old age; invisible to other cells' Conway counts; dies when its Necromancer dies; permanently destroyed when killed by a Doctor/Warrior/Hunter |
| 28 | Necromancer | near-black | Spawns randomly; resurrects the nearest 3 dead cells as zombies on spawn, then 1 more each step; survives while ≥2 zombies are alive |

---

## Nation System

- **Census** — every step, cells are counted per nationality; Kings and Diplomats are elected from nations above population thresholds
- **King crowning** — a nation with ≥5 citizens may crown a King, which marks nearby Basic cells with `toWar` to promote them to Warriors
- **King-distance neutralisation** — Basic cells further than `(columns + rows) / 3` from their King lose their nationality and gain a 3-step cooldown
- **Diplomat election** — large nations elect a Diplomat that travels to the nearest foreign nation and converts adjacent cells
- **Revolutionary defection** — when a nation becomes too dominant, a member may defect, splitting off a rival nation

---

## Project Structure

```
ConwaysWorld.Simulation/        Pure C# library — no framework dependencies
  Cells/                        All 28 cell type implementations
  Model.cs                      Simulation step orchestration
  Cell_Nation.cs                Nations: census, diplomat election, king crowning
  SimulationSettings.cs         All configurable parameters

ConwaysWorld.Blazor/            Blazor WebAssembly frontend
  Pages/Index.razor             Main page: canvas, sidebar, toolbar, settings
  ts/canvas-interop.ts          TypeScript: canvas rendering, zoom/pan, keyboard shortcuts
  wwwroot/canvas-interop.js     Compiled JS (always edit the .ts, then run npx tsc)
  wwwroot/css/app.css           All styles

Assets/Scripts/                 Original Unity C# source (reference only, not compiled)
index.html                      Original plain-JS implementation (reference)
server.js                       Original Node.js server (reference)
run.sh                          Startup script: format → compile TS → dotnet run
```

---

## TypeScript

The canvas renderer is TypeScript — always edit the `.ts` source and recompile:

```bash
cd ConwaysWorld.Blazor && npx tsc
```

The compiled `wwwroot/canvas-interop.js` is what the browser loads. Never edit the `.js` directly.

---

## Unity Portability

`ConwaysWorld.Simulation` uses only `System.*` — no Unity, Blazor, or ASP.NET dependencies.

1. Copy `ConwaysWorld.Simulation/` into your Unity project's `Assets/Scripts/`
2. Replace `SimRandom.*` calls with `UnityEngine.Random.*` equivalents
3. Wire `Model.Step()` into `MonoBehaviour.Update()` or `InvokeRepeating`

---

## Deployment

### GitHub Pages
The repository includes a GitHub Actions workflow (`.github/workflows/deploy.yml`) that automatically publishes to GitHub Pages on every push to `main`.

### Replit
The app runs on Replit via the **Start application** workflow (`bash run.sh`) and can be published via the Replit **Deploy** button.
