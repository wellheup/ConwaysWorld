# Conway's World

## Overview

A web-based implementation of Conway's World — a sophisticated Conway's Game of Life variant with 13 specialized cell types and a Nations system. The simulation logic is written in pure C# (Blazor WebAssembly), making it directly portable back to Unity.

## Architecture

```
ConwaysWorld.Simulation/   ← Pure C# simulation library (no Unity, no Blazor deps)
  Cells/                   ← All 13 cell type implementations
  Model.cs                 ← Simulation step orchestration
  Cell_Nation.cs           ← Nations: census, diplomat election, king crowning
  SimulationSettings.cs    ← All configurable parameters

ConwaysWorld.Blazor/       ← Blazor WebAssembly frontend
  Pages/Index.razor        ← Main page: canvas, sidebar, toolbar, settings
  wwwroot/canvas-interop.js ← JS: canvas rendering, zoom/pan, keyboard shortcuts
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

| Type | Behavior |
|------|----------|
| Basic | Standard Conway cells; 1/4 chance immune; 1/100 immaculate |
| Immortal | Lives forever unless isolated for >8 steps; immune to disease |
| Diseased | Spreads d_ strain; dies after 3-step countdown |
| Plague | Like Diseased but 40% higher transmission rate |
| Traveler | Moves each step; dies if isolated >3 steps or surrounded >3 steps |
| Explorer | Like Traveler, triggers grid expansion at edges |
| Doctor | Cures nearby disease; stamps vax_ markers; survives while active |
| Warrior | Fights foreign Diseased/Plague; demotes to Basic after 3 idle steps |
| Hunter | Hunts Immortals and Kings; demotes to Basic after 3 idle steps |
| Bomber | Detonates at age 2, killing all cells in 2-cell radius |
| Diplomat | Elected from large nations; travels to foreign nations and converts cells |
| King | Crowned from large nations; turns neighboring Basics into Warriors |

## Key JS Improvements Preserved in C# Port

- **Warrior/Hunter idle demotion** — demote to Basic after 3 idle steps
- **Doctor vax_ vaccination** — `vax_<strain>` markers prevent re-infection; doctor only earns survival credit for new vaccinations
- **Plague transmission** — 40% higher than Diseased (`Math.Round(base * 1.4)`)
- **Immortal disease immunity** — immune to both spread and conversion
- **CrushCountDown** — Traveler/Explorer die if fully surrounded for >3 steps
- **Conditions as HashSet** — O(1) lookup vs original List
- **Separate start/max grid size** — `StartColumns/Rows` vs `MaxGridSize`
- **Pop mode** — percent of grid or fixed cell count

## Controls

| Input | Action |
|-------|--------|
| Space | Play / Pause |
| R | Restart |
| Scroll wheel | Zoom in/out |
| Right-click drag | Pan |
| Double-click | Reset zoom/pan |
| Left-click | Select cell |
| Hover | Tooltip: type, nation, age, conditions |

## Unity Portability

`ConwaysWorld.Simulation` uses only `System.*` — no Unity, Blazor, or ASP.NET deps. To use it in Unity:

1. Copy the `ConwaysWorld.Simulation/` folder into your Unity project's `Assets/Scripts/`
2. Replace `SimRandom.*` calls with `UnityEngine.Random.*` equivalents
3. Wire `Model.Step()` into `MonoBehaviour.Update()` or `InvokeRepeating`

## Deployment

Configured for **autoscale** deployment via `dotnet run` on port 5000.

## User Preferences

- Simulation library must remain pure C# with no framework dependencies
