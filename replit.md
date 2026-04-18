# Conway's World

## Overview

A web-based JavaScript port of Conway's World, a sophisticated implementation of Conway's Game of Life originally built in Unity/C#. The game features extended cellular automata rules with specialized "Nations" and many unique cell types.

## Architecture

This is a **single-page static web app** served by a minimal Node.js HTTP server.

- **`index.html`** — The entire game: HTML, CSS, and JavaScript simulation logic all in one file.
- **`server.js`** — A minimal Node.js HTTP server serving static files on port 5000.
- **`Assets/`** — Original Unity C# source code (reference only; not executed).

## Running the App

```
node server.js
```

Serves the game at `http://0.0.0.0:5000`.

## Game Features

### Cell Types (ported from C# source)
- **Basic** — Standard Conway's Game of Life cells
- **Immortal** — Lives unless isolated
- **Diseased** — Spreads disease to neighbors (25% transmission rate, 3-turn countdown)
- **Plague** — More aggressive disease (50% transmission, faster spread)
- **Traveler** — Moves in a random direction each turn
- **Explorer** — Like Traveler, can trigger grid expansion
- **Doctor** — Cures diseased neighbors, immune to infection
- **Warrior** — Hunts diseased cells from other nations
- **Hunter** — Seeks and kills Immortals and Kings
- **Bomber** — Explodes after 2 turns, killing all cells in range
- **Diplomat** — Spawned by large nations to convert enemy cells
- **King** — Crowns itself from large nations, turns neighbors into Warriors

### Nations System
- Up to 20 color-coded nations
- Cells inherit nearby nation affiliations
- Nations elect Diplomats and crown Kings when they grow large enough
- Cell type colors shown inside nation-color background squares

### Controls
- **Space** — Play/Pause
- **R** — Restart simulation
- **Speed slider** — Adjust simulation speed
- **Mouse hover** — Tooltip showing cell type, nation, age, conditions

## Key Technical Notes

- Grid wraps around edges (toroidal topology)
- Neighborhoods are computed fresh each step before any updates
- Special actions (movement, combat, disease spread) run after life/death updates
- The `updateConditions` pass handles disease infection conversion and "toWar" recruitment
- Nations take a census each generation to track citizens, elect diplomats, and crown kings

## Deployment

Configured for **autoscale** deployment via `node server.js` on port 5000.
