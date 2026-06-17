---
name: Conway simulation step order
description: Correct step sequence for the C# simulation — deviating causes stale-neighborhood bugs in movement cells
---

## Correct order in Model.Step()
1. UpdateNeighborhoodsGrid — snapshot all neighbor counts before any changes
2. UpdateAliveNextGenGrid — compute life/death for all cells
3. UpdateCellLives — apply live/die, call Live()/Die()
4. UpdateCellConditions — process disease, mature/breed, immaculate, toWar, idle demotion
5. **UpdateNeighborhoodsGrid again** — refresh after condition changes (cells may have been replaced)
6. UpdateSpecialActions — movement, combat, disease spread, bombing
7. AddRandomLife — repopulate if population below threshold
8. UpdateNations — census, elect diplomats, crown kings

**Why:** Traveler/Hunter/Warrior swap cells in SpecialActions. If neighborhoods aren't refreshed after UpdateCellConditions (where cells get replaced), movers use neighborhood refs pointing to old (replaced) cell objects.
