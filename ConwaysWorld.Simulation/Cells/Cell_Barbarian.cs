namespace ConwaysWorld.Simulation;

/// <summary>
/// A Barbarian is a nationless aggressor that emerges when a nation cell touches an Islander.
/// Each step it:
/// <list type="number">
///   <item>Converts every <see cref="CellType.Islander"/> within 3 tiles into a Barbarian.</item>
///   <item>Kills the nearest cell within 3 tiles that is neither an Islander nor a Barbarian.</item>
///   <item>If no such target exists, reverts to an <see cref="CellType.Islander"/>.</item>
/// </list>
/// Between special actions, Barbarians obey standard Conway survival rules and never join
/// a nation.
/// </summary>
public class Cell_Barbarian : Cell_Combatant
{
	/// <summary>Creates a Barbarian cell at the given position.</summary>
	public Cell_Barbarian(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Barbarian;
		Conditions = new HashSet<string>();
		Nationality = -1;
	}

	/// <summary>
	/// Skips <see cref="Cell.ChooseNation"/> so Barbarians remain permanently nationless.
	/// </summary>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (Age > MatureAge)
			Conditions.Add("mature");
		_specialPerformed = false;
	}

	// Die() inherited from Cell_Combatant.

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;

		// Step 1 — convert every Islander within 3 tiles into a Barbarian.
		var islanders = GetAllCellsInRangeByRule(
										cellGrid,
										c => c.IsAlive && c.CellType == CellType.Islander,
										3);
		foreach (var isle in islanders)
		{
			var barb = ReplaceCell(isle, CellType.Barbarian, true);
			barb.Nationality = -1;
			cellGrid[isle.Column, isle.Row] = barb;
		}

		// Step 2 — kill the nearest non-Islander, non-Barbarian within 2 tiles.
		var target = SelectNearbyCellByRule(
										cellGrid,
										c => c.IsAlive &&
														 c.CellType != CellType.Islander &&
														 c.CellType != CellType.Barbarian,
										3); // maxRange is exclusive; 3 covers Chebyshev range 2

		if (target == null)
		{
			// No nation targets in range — revert to Islander.
			var isle = ReplaceCell(this, CellType.Islander, true);
			isle.Nationality = -1;
			cellGrid[Column, Row] = isle;
			return;
		}

		SwapCells(this, target, cellGrid);
		target.Die();
		target.Conditions.Add("cleanup");
	}
}
