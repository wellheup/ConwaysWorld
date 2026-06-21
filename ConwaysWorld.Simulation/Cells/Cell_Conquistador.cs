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
/// If there are no disconnected nations for 2 consecutive steps the Conquistador becomes
/// a <see cref="Cell_Traveler"/>.
/// </summary>
public class Cell_Conquistador : Cell
{
	private Cell? _target;
	private int _targetNation = -1;
	private bool _specialPerformed = false;
	private int _noTargetTurns = 0;

	/// <summary>Creates a Conquistador cell at the given position.</summary>
	public Cell_Conquistador(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Conquistador;
		Conditions = new HashSet<string>();
	}

	/// <inheritdoc/>
	public override void Live()
	{
		base.Live();
		_specialPerformed = false;
	}

	/// <inheritdoc/>
	public override void Die()
	{
		base.Die();
		_specialPerformed = true;
	}

	/// <summary>Conquistadors ignore Conway survival rules during transit.</summary>
	public override bool CalcCellAliveNextGen() => IsAlive;

	// ── Private helpers (movement mirrors Cell_Voyager) ───────────────────────────

	private bool IsTargetValid() =>
			_target != null && _target.IsAlive &&
			_targetNation >= 0 && _targetNation != Nationality &&
			_target.Nationality == _targetNation;

	/// <summary>Identical target-selection logic to <see cref="Cell_Voyager"/>.</summary>
	private void SelectTarget(Cell[,] cellGrid)
	{
		_target = null;
		_targetNation = -1;

		if (Nationality < 0)
			return;

		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		var ownAdj = new HashSet<(int, int)>();
		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				if (!cellGrid[c, r].IsAlive || cellGrid[c, r].Nationality != Nationality)
					continue;
				for (int dc = -1; dc <= 1; dc++)
					for (int dr = -1; dr <= 1; dr++)
						if (dc != 0 || dr != 0)
							ownAdj.Add(((c + dc + cols) % cols, (r + dr + rows) % rows));
			}

		var disconnected = new HashSet<int>();
		var touching = new HashSet<int>();
		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (!cell.IsAlive || cell.Nationality == Nationality || cell.Nationality < 0)
					continue;
				if (ownAdj.Contains((c, r)))
					touching.Add(cell.Nationality);
				else
					disconnected.Add(cell.Nationality);
			}

		disconnected.ExceptWith(touching);
		if (disconnected.Count == 0)
			return;

		Cell? best = null;
		int bestDist = int.MaxValue;
		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (!cell.IsAlive || !disconnected.Contains(cell.Nationality))
					continue;
				int distC = Math.Abs(c - Column);
				int distR = Math.Abs(r - Row);
				int dist = Math.Min(distC, cols - distC) + Math.Min(distR, rows - distR);
				if (dist < bestDist)
				{
					bestDist = dist;
					best = cell;
					_targetNation = cell.Nationality;
				}
			}
		_target = best;
	}

	/// <summary>Move up to 2 cells per step toward the target (falls back to 1 if blocked).</summary>
	private void MoveTowardTarget(Cell[,] cellGrid, List<MoveRecord>? moves)
	{
		if (_target == null)
			return;

		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		int dc = _target.Column - Column;
		if (Math.Abs(dc) > cols / 2)
			dc = -Math.Sign(dc) * (cols - Math.Abs(dc));
		int dr = _target.Row - Row;
		if (Math.Abs(dr) > rows / 2)
			dr = -Math.Sign(dr) * (rows - Math.Abs(dr));

		int dirC = Math.Sign(dc);
		int dirR = Math.Sign(dr);
		if (dirC == 0 && dirR == 0)
			return;

		int col2 = (Column + dirC * 2 + cols) % cols;
		int row2 = (Row + dirR * 2 + rows) % rows;
		int col1 = (Column + dirC + cols) % cols;
		int row1 = (Row + dirR + rows) % rows;

		var dest2 = cellGrid[col2, row2];
		var dest1 = cellGrid[col1, row1];

		if (!dest2.IsAlive && dest2 != this)
		{
			moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, col2, row2, (int)CellType, Nationality));
			SwapCells(this, dest2, cellGrid);
		}
		else if (!dest1.IsAlive && dest1 != this)
		{
			moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, col1, row1, (int)CellType, Nationality));
			SwapCells(this, dest1, cellGrid);
		}
	}

	/// <summary>
	/// Finds vacant slots in expanding Chebyshev rings around (<paramref name="col"/>, <paramref name="row"/>).
	/// </summary>
	private static List<Cell> NearestVacant(Cell[,] cellGrid, int col, int row, int needed)
	{
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		var vacant = new List<Cell>();
		for (int range = 1; vacant.Count < needed && range < Math.Max(cols, rows); range++)
			for (int co = -range; co <= range; co++)
				for (int ro = -range; ro <= range; ro++)
				{
					if (Math.Abs(co) != range && Math.Abs(ro) != range)
						continue;
					int nc = (col + co + cols) % cols;
					int nr = (row + ro + rows) % rows;
					var c = cellGrid[nc, nr];
					if (!c.IsAlive && !vacant.Contains(c))
						vacant.Add(c);
				}
		return vacant;
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
	/// </summary>
	private void Arrive(Cell[,] cellGrid)
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

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;

		if (!IsTargetValid())
			SelectTarget(cellGrid);

		if (_target == null)
		{
			if (_noTargetTurns >= 1)
			{
				var traveler = ReplaceCell(this, CellType.Traveler, true);
				traveler.Nationality = Nationality;
				cellGrid[Column, Row] = traveler;
				return;
			}
			_noTargetTurns++;
			return;
		}
		_noTargetTurns = 0;

		// Check adjacency — have we arrived at the target nation?
		foreach (var nb in CellNeighborhood.NeighborhoodDict.Values)
		{
			if (nb != this && nb.IsAlive && nb.Nationality == _targetNation)
			{
				Arrive(cellGrid);
				return;
			}
		}

		MoveTowardTarget(cellGrid, moves);
	}
}
