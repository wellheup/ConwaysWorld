---
name: Breed method grid update
description: Why Cell.Breed() must accept Cell[,] and write back to the grid
---

## The bug
Original Unity C# Breed() does:
```csharp
cells[randNeighbor] = ReplaceCell(cells[randNeighbor], CellType, true);
```
This updates the local list only — `CellGrid[col, row]` still holds the old cell object.

## The fix
```csharp
public virtual void Breed(Cell[,] cellGrid)
{
    // ... find empty neighbours ...
    var slot = empties[idx];
    var newCell = ReplaceCell(slot, CellType, true);
    cellGrid[slot.Column, slot.Row] = newCell;   // ← must write back
}
```

**Why:** Cell objects are reference types. ReplaceCell() returns a NEW object. Assigning to a local variable doesn't update the grid array. Must write to `cellGrid[col, row]`.

**How to apply:** Any time a cell method needs to replace a cell in the grid (Breed, Doctor curing, Nation electing), pass the grid in and write back directly.
