# Conway's World

A browser-based simulation built on Conway's Game of Life, extended with 13 specialised cell types, a Nations system, diplomacy, combat, and animated rendering. The simulation logic is written in pure C# so it can be dropped directly into a Unity project.

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 7.0 | Builds and runs the Blazor WebAssembly app |
| [Node.js](https://nodejs.org/) | 18 or later | Compiles TypeScript and formats source files |

No other global tools are required — `tsc` and `prettier` are installed locally via npm as part of the build.

---

## Fresh Install & Running Locally

```bash
# 1. Clone the repo
git clone <repo-url>
cd ConwaysWorld

# 2. Start the application
bash run.sh
```

`run.sh` does three things in order:

1. Runs `dotnet format` on both C# projects to normalise code style.
2. Runs `dotnet run` on the Blazor project, which triggers an MSBuild target (`CompileTypeScript`) that:
   - Runs `npm install` to pull in TypeScript and Prettier.
   - Runs `prettier --write` to format all `.ts` source files.
   - Runs `tsc` to compile TypeScript → JavaScript.
3. Starts the Blazor dev server on **http://0.0.0.0:5000**.

Open your browser to `http://localhost:5000` to use the application.

---

## Controls

| Input | Action |
|-------|--------|
| **Space** | Play / Pause |
| **R** | Restart with the same settings |
| **Scroll wheel** | Zoom in / out (scrolling fully out resets to centred view) |
| **Right-click drag** | Pan the camera |
| **Double-click** | Reset zoom and pan to default |
| **Left-click** | Select a cell (highlights it) |
| **Hover** | Tooltip showing type, nation, age, and conditions |

The **Settings** panel on the right lets you adjust grid size, step speed, spawn weights, and animation toggle before clicking **Apply & Restart**.

---

## Cell Types

| Type | Colour | Behaviour |
|------|--------|-----------|
| **Basic** | Light grey | Standard Conway rules. 25 % immune chance; 1 % chance of spawning immaculate (immune to all disease). |
| **Immortal** | Gold | Lives forever unless isolated for more than 8 consecutive steps. Immune to disease. |
| **Diseased** | Dark red | Spreads a unique `d_` strain to neighbours; dies after a 3-step countdown. |
| **Plague** | Bright red | Like Diseased but with 40 % higher transmission rate and a `p_` strain. |
| **Traveler** | Blue | Swaps with a random neighbour each step. Dies if alone or fully surrounded for more than 3 steps. |
| **Explorer** | Cyan | Like Traveler, but triggers a grid expansion (+1 cell on each side) when it reaches an edge. |
| **Doctor** | Pink | Cures adjacent Diseased/Plague cells and stamps `vax_<strain>` immunity markers on recovered cells. |
| **Warrior** | Orange | Attacks foreign Diseased/Plague cells within range 2. Demotes to Basic after 3 idle steps. |
| **Hunter** | Red-grey | Hunts Immortals and Kings within range 5. Demotes to Basic after 8 idle steps. |
| **Bomber** | Amber | Detonates at age 2, destroying all living cells within a 2-cell radius, then dies. |
| **Diplomat** | Purple | Elected from large nations; travels toward foreign nations and converts adjacent cells to its own nation. |
| **King** | Yellow | Crowned from a nation with ≥ 5 citizens. Turns neighbouring Basic cells into Warriors each step. |

### Nations

Cells belong to numbered nations (distinguished by background colour). A nation large enough will:
- **Elect a Diplomat** to expand its influence into neighbouring nations.
- **Crown a King** to defend its territory by raising Warriors.

Kings and Immortals have special death animations (grow + spin). Newly crowned Kings play a bounce animation.

---

## Project Structure

```
ConwaysWorld/
├── ConwaysWorld.Simulation/        Pure C# class library — no framework dependencies
│   ├── Cells/
│   │   ├── Cell.cs                 Base class with shared state (type, nation, age, conditions)
│   │   ├── CellType.cs             Enum of all 13 cell types
│   │   ├── Cell_Basic.cs           Standard Conway cell
│   │   ├── Cell_Immortal.cs        Persistent cell, isolated-death rule
│   │   ├── Cell_Diseased.cs        Infection + countdown death; base for Plague
│   │   ├── Cell_Plague.cs          High-transmission variant of Diseased
│   │   ├── Cell_Traveler.cs        Moving cell with isolation/crush death
│   │   ├── Cell_Explorer.cs        Traveler variant that expands the grid
│   │   ├── Cell_Doctor.cs          Healer with vaccination markers
│   │   ├── Cell_Warrior.cs         Melee fighter, idle demotion
│   │   ├── Cell_Hunter.cs          Long-range predator of Immortals/Kings
│   │   ├── Cell_Bomber.cs          Suicide detonator
│   │   ├── Cell_Diplomat.cs        Nation spreader
│   │   ├── Cell_King.cs            Nation leader, Warrior recruiter
│   │   ├── Cell_Nation.cs          Census, Diplomat election, King crowning
│   │   ├── Cell_Neighborhood.cs    Pre-computed neighbour references per cell
│   │   └── Cell_Generator.cs       Weighted random cell spawner
│   ├── Model.cs                    Main simulation loop (Step, Restart, grid resize)
│   └── SimulationSettings.cs       All tunable parameters (grid size, speeds, weights)
│
├── ConwaysWorld.Blazor/            Blazor WebAssembly frontend
│   ├── ts/
│   │   └── canvas-interop.ts       TypeScript source — canvas rendering, zoom/pan,
│   │                               all animations (moves, births, deaths, coronations)
│   ├── wwwroot/
│   │   ├── canvas-interop.js       Generated by tsc — do not edit directly
│   │   ├── css/app.css             Application styles
│   │   ├── Assets/Sprites/         Cell sprite images (Cell_Basic.jpg, etc.)
│   │   └── index.html              Blazor WASM host page
│   ├── Pages/Index.razor           Main page: simulation loop, settings panel,
│   │                               event log, JS interop calls
│   ├── Shared/                     Shared Blazor layout components
│   ├── tsconfig.json               TypeScript compiler config (strict, ES6, ts/ → wwwroot/)
│   ├── .prettierrc                 Prettier config (4-space indent, single quotes)
│   ├── package.json                Node devDependencies: typescript, prettier
│   └── ConwaysWorld.Blazor.csproj  Includes MSBuild target to format + compile TS on build
│
├── Assets/Scripts/                 Original Unity C# source (reference only, not compiled)
├── index.html                      Original plain-JS implementation (preserved as reference)
├── server.js                       Original Node.js server (preserved as reference)
├── run.sh                          Startup script: format C# → dotnet run
└── README.md                       This file
```

---

## TypeScript Workflow

The JavaScript served to the browser is compiled from TypeScript automatically — you never need to run `tsc` manually.

```
Edit:    ConwaysWorld.Blazor/ts/canvas-interop.ts
Format:  npx prettier --write "./ts/**/*.ts"   (runs automatically on build)
Compile: npx tsc --project tsconfig.json       (runs automatically on build)
Output:  ConwaysWorld.Blazor/wwwroot/canvas-interop.js  (gitignored)
```

To format manually without rebuilding:
```bash
cd ConwaysWorld.Blazor
npm run format
```

To check formatting without modifying files:
```bash
cd ConwaysWorld.Blazor
npm run check
```

---

## Unity Portability

`ConwaysWorld.Simulation` uses only `System.*` namespaces — no Unity, Blazor, or ASP.NET dependencies. To use it in a Unity project:

1. Copy `ConwaysWorld.Simulation/` into your Unity project's `Assets/Scripts/`.
2. Replace `SimRandom.*` calls with `UnityEngine.Random.*` equivalents.
3. Wire `Model.Step()` into `MonoBehaviour.Update()` or a coroutine/`InvokeRepeating`.
4. Read `Model.PendingMoves` each frame to drive GameObject animations.

---

## Deployment

The app is configured for **autoscale** deployment. To publish:

```bash
dotnet publish ConwaysWorld.Blazor/ConwaysWorld.Blazor.csproj -c Release
```

Or use the Replit **Publish** button — the app will be served from a `.replit.app` domain with HTTPS handled automatically.
