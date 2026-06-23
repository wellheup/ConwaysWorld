namespace ConwaysWorld.Simulation;

/// <summary>
/// An Irradiated cell is a permanent hazard tile — it cannot be moved, killed, converted,
/// or given any conditions. It does not count as a living cell for simulation-over checks.
/// <para>
/// Any cell that attempts to move onto an Irradiated tile via the swap mechanic dies
/// instantly on the step it lands. The Irradiated cell itself is unaffected.
/// </para>
/// <para>
/// Irradiated cells have no nationality and never join a nation.
/// </para>
/// </summary>
public class Cell_Irradiated : Cell
{
	/// <summary>Creates an Irradiated cell at the given position.</summary>
	public Cell_Irradiated(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Irradiated;
		Conditions = new HashSet<string>();
		Nationality = -1;
	}

	/// <inheritdoc/>
	/// <remarks>Irradiated cells never take conditions — all condition writes are no-ops via this override.</remarks>
	public override void Live()
	{
		// Do not call base.Live() — we never age, never gain the "mature" condition,
		// and never join a nation. IsAlive stays true from construction.
		IsAlive = true;
		CellType = CellType.Irradiated;
		Nationality = -1;
	}

	/// <summary>Irradiated cells cannot die — this is a no-op.</summary>
	public override void Die() { }

	/// <summary>Irradiated cells are always alive next generation.</summary>
	public override bool CalcCellAliveNextGen() => IsAlive;

	/// <summary>Irradiated cells take no special actions.</summary>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null) { }
}
