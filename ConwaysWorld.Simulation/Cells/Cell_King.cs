namespace ConwaysWorld.Simulation;

/// <summary>
/// A King is the highest-ranked cell in its nation and continuously converts neighbouring
/// Basic cells into Warriors.
/// <para>
/// Each step <see cref="MakeArmy"/> stamps the <c>"toWar"</c> condition on every adjacent
/// living Basic cell.  During the subsequent <see cref="Model.UpdateCellConditions"/> pass
/// those cells are replaced with <see cref="Cell_Warrior"/> instances.
/// </para>
/// <para>
/// Kings are never spawned at grid initialisation.  They are crowned by
/// <see cref="Cell_Nation.CrownKing"/> when a nation with at least 5 citizens
/// has no current King.  Only one King can exist per nation at a time.
/// </para>
/// <para>
/// Kings count as prey for <see cref="Cell_Hunter"/>, so they are prioritised targets
/// alongside Immortals.
/// </para>
/// </summary>
public class Cell_King : Cell
{
	/// <summary>Maximum number of steps a King may reign before aging out.</summary>
	public const int MaxAge = 20;

	/// <summary>Creates a King cell at the given position.</summary>
	public Cell_King(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.King;
		Conditions = new HashSet<string>();
	}

	/// <summary>Resets living state, age, and nationality on death (does not call base to avoid double-reset).</summary>
	public override void Die()
	{
		IsAlive = false;
		Age = 0;
		Nationality = -1;
	}

	/// <summary>
	/// Marks every adjacent living Basic cell with the <c>"toWar"</c> condition.
	/// Those cells will be converted to Warriors during the next conditions-update pass.
	/// </summary>
	private void MakeArmy()
	{
		foreach (var neighbor in CellNeighborhood.NeighborhoodDict.Values)
		{
			if (neighbor.IsAlive && neighbor != this && neighbor.CellType == CellType.Basic)
				neighbor.Conditions.Add("toWar");
		}
	}

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null) => MakeArmy();
}
