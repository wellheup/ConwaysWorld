namespace ConwaysWorld.Simulation;

/// <summary>
/// A Conquistador works like a <see cref="Cell_Voyager"/> — it seeks a disconnected foreign
/// nation and travels to it (2 cells/step, bypassing Conway rules) — but its arrival is
/// always hostile:
/// <list type="number">
///   <item>It finds up to 10 living cells from its home nation anywhere on the grid, sorted
///         by proximity to the landing site.</item>
///   <item>It finds up to 10 vacant slots near its landing site in expanding rings.</item>
///   <item>For each (source, destination) pair it kills the source cell and places a
///         <see cref="Cell_Soldier"/> at the destination, set to the Conquistador's nation
///         and targeting the enemy nation.</item>
///   <item>The Conquistador itself becomes a <see cref="Cell_Soldier"/> at its own position.</item>
/// </list>
/// <para>
/// Extends <see cref="Cell_NationTraveler"/> which provides target selection, toroidal
/// 2-cell movement, and the full <see cref="Cell.SpecialActions"/> state machine.
/// The only things unique to Conquistador are <see cref="FindNearestNationCells"/> and
/// the <see cref="Arrive"/> effect.
/// </para>
/// If there are no disconnected nations for 2 consecutive steps the Conquistador becomes
/// a <see cref="Cell_Traveler"/>.
/// </summary>
public class Cell_Conquistador : Cell_NationTraveler
{
	/// <summary>Creates a Conquistador cell at the given position.</summary>
	public Cell_Conquistador(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Conquistador;
		Conditions = new HashSet<string>();
	}

	/// <summary>
	/// Finds up to <paramref name="count"/> living cells from this cell's nation,
	/// ordered by ascending distance to this cell's current position.
	/// </summary>
	private List<Cell> FindNearestNationCells(Cell[,] cellGrid, int count)
	{
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		var results = new List<(Cell cell, int dist)>();

		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (cell == this || !cell.IsAlive || cell.Nationality != Nationality)
					continue;
				int distC = Math.Abs(c - Column);
				int distR = Math.Abs(r - Row);
				int dist = Math.Min(distC, cols - distC) + Math.Min(distR, rows - distR);
				results.Add((cell, dist));
			}

		results.Sort((a, b) => a.dist.CompareTo(b.dist));
		var list = new List<Cell>();
		for (int i = 0; i < Math.Min(count, results.Count); i++)
			list.Add(results[i].cell);
		return list;
	}

	/// <summary>
	/// Arrival: teleport nearest 10 home-nation cells to vacant slots here, convert them
	/// (and self) to <see cref="Cell_Soldier"/> targeting the enemy nation.
	/// Uses <see cref="Cell_ArrivalTraveler.NearestVacant"/> for slot discovery.
	/// </summary>
	protected override void Arrive(Cell[,] cellGrid)
	{
		int myNation = Nationality;
		int enemyNat = _targetNation;
		int col = Column, row = Row;

		var vacantSlots = NearestVacant(cellGrid, col, row, 10);
		var sourceCells = FindNearestNationCells(cellGrid, 10);

		int pairs = Math.Min(vacantSlots.Count, sourceCells.Count);
		for (int i = 0; i < pairs; i++)
		{
			var src = sourceCells[i];
			var dst = vacantSlots[i];

			// Kill the source cell (cleanup replaces it with dead Basic next condition pass).
			src.Die();
			src.Conditions.Add("cleanup");

			// Place a Soldier at the destination slot.
			var soldier = ReplaceCell(dst, CellType.Soldier, true);
			soldier.Nationality = myNation;
			if (soldier is Cell_Soldier s)
				s.TargetNation = enemyNat;
			cellGrid[dst.Column, dst.Row] = soldier;
		}

		// The Conquistador itself becomes a Soldier at its current position.
		var selfSoldier = ReplaceCell(this, CellType.Soldier, true);
		selfSoldier.Nationality = myNation;
		if (selfSoldier is Cell_Soldier ss)
			ss.TargetNation = enemyNat;
		cellGrid[col, row] = selfSoldier;
	}
}
