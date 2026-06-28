namespace ConwaysWorld.Simulation;

/// <summary>
/// A Bomber is a suicidal cell that detonates after reaching age 2, killing every living
/// cell within a 2-cell Chebyshev radius (a 5×5 area) and then dying itself.
/// <para>
/// Lifecycle:
/// <list type="bullet">
///   <item>Age 0–1: votes to survive unconditionally via <see cref="CalcCellAliveNextGen"/>.</item>
///   <item>Age 2+: <see cref="SpecialActions"/> calls <see cref="Detonate"/>, which kills all
///         living neighbours in range and then calls <see cref="Cell.Die"/> on the Bomber itself.</item>
/// </list>
/// </para>
/// <para>
/// Note: killed cells have <see cref="Cell.Die"/> called directly; they are not marked for
/// cleanup and their slots remain as dead cells of their original type until the next step.
/// </para>
/// </summary>
public class Cell_Bomber : Cell
{
	/// <summary>Creates a Bomber cell at the given position.</summary>
	public Cell_Bomber(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Bomber;
		Conditions = new HashSet<string>();
		Nationality = -1;
	}

	/// <summary>
	/// Votes to stay alive only while already alive (so the Bomber reaches age 2 before detonating).
	/// A dead Bomber slot defers to standard Conway birth rules — this prevents the oscillation
	/// bug where a dead Bomber with ≥3 live neighbours endlessly re-spawns every other step.
	/// Detonation is handled in <see cref="SpecialActions"/>, not here.
	/// </summary>
	public override bool CalcCellAliveNextGen() => IsAlive;

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null) => Detonate(cellGrid);

	/// <summary>
	/// Kills all living cells within range 2 (Chebyshev) and then kills the Bomber itself.
	/// Does nothing if <see cref="Cell.Age"/> is less than 2.
	/// </summary>
	private void Detonate(Cell[,] cellGrid)
	{
		if (Age < 2)
			return;

		var victims = GetAllCellsInRangeByRule(cellGrid, c => c.IsAlive, 2);
		foreach (var v in victims)
			v.Die();
		Die();
	}
}
