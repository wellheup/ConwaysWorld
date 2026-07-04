namespace ConwaysWorld.Simulation;

/// <summary>
/// Captures a single cell movement that occurred during a simulation step.
/// Used by <see cref="Cell.SpecialActions"/> implementations to report movement
/// to callers (e.g. the canvas renderer for animation).
/// </summary>
/// <param name="FromCol">Column the cell moved from.</param>
/// <param name="FromRow">Row the cell moved from.</param>
/// <param name="ToCol">Column the cell moved to.</param>
/// <param name="ToRow">Row the cell moved to.</param>
/// <param name="CellType">Integer value of the moving cell's <see cref="CellType"/>.</param>
/// <param name="Nationality">Nationality of the moving cell.</param>
public record MoveRecord(int FromCol, int FromRow, int ToCol, int ToRow, int CellType, int Nationality);
