namespace ConwaysWorld.Simulation;

/// <summary>
/// A Voyager seeks out a foreign nation that shares no border with its own nation, travels
/// to it across open space (ignoring normal Conway survival requirements), and upon arrival
/// resolves a 50/50 outcome:
/// <list type="bullet">
///   <item>Peaceful landing: becomes an <see cref="Cell_Explorer"/> and plants 2 Diplomats + 2 Warriors
///         in the nearest vacant slots, all bearing its own nation.</item>
///   <item>Hostile landing: becomes a <see cref="Cell_Plague"/> cell and seeds 4 additional Plague cells
///         in the nearest vacant slots.</item>
/// </list>
/// <para>
/// Movement: moves 2 cells per step toward the target (jumping over the intermediate slot),
/// falling back to 1 cell if the 2-cell destination is occupied.  Does not move if both
/// the 2- and 1-cell destinations are blocked.
/// </para>
/// <para>
/// Survival: bypasses standard Conway rules — the Voyager never dies from isolation or
/// overcrowding during transit.
/// </para>
/// </summary>
public class Cell_Voyager : Cell
{
	private Cell? _target;
	private int _targetNation = -1;
	private bool _specialPerformed = false;

	/// <summary>Creates a Voyager cell at the given position.</summary>
	public Cell_Voyager(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Voyager;
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

	/// <summary>Voyagers ignore Conway survival rules during transit — they always survive.</summary>
	public override bool CalcCellAliveNextGen() => IsAlive;

	// ── Private helpers ───────────────────────────────────────────────────────────

	private bool IsTargetValid() =>
			_target != null && _target.IsAlive &&
			_targetNation >= 0 && _targetNation != Nationality &&
			_target.Nationality == _targetNation;

	/// <summary>
	/// Scans the entire grid to find the nearest foreign nation that has no cells
	/// adjacent (Moore neighbourhood) to any cell of the Voyager's own nation.
	/// Sets <see cref="_target"/> and <see cref="_targetNation"/>; leaves both at their
	/// null/−1 defaults if no disconnected foreign nation exists.
	/// </summary>
	private void SelectTarget(Cell[,] cellGrid)
	{
		_target = null;
		_targetNation = -1;

		if (Nationality < 0)
			return;

		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		// Build the set of all grid positions adjacent (Moore) to any own-nation cell.
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

		// Separate foreign nations into those touching our border vs. those fully disconnected.
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

		// Find the nearest cell from any disconnected nation.
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

	/// <summary>
	/// Steps toward <see cref="_target"/> by up to 2 cells this turn.
	/// Tries the 2-cell jump first; falls back to 1 cell if blocked; stays if both are blocked.
	/// </summary>
	private void MoveTowardTarget(Cell[,] cellGrid, List<MoveRecord>? moves)
	{
		if (_target == null)
			return;

		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		// Direction toward target using shortest toroidal path.
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
		// Both blocked — stay put this step.
	}

	/// <summary>
	/// Collects up to <paramref name="needed"/> vacant (dead) cells sorted outward from
	/// (<paramref name="col"/>, <paramref name="row"/>) by Chebyshev ring.
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
	/// Resolves the arrival: 50 % chance peaceful (Explorer + 2 Diplomats + 2 Warriors),
	/// 50 % chance hostile (Plague self + 4 Plague seeds).
	/// </summary>
	private void Arrive(Cell[,] cellGrid)
	{
		int myNation = Nationality;
		int col = Column, row = Row;
		var vacant = NearestVacant(cellGrid, col, row, 4);

		if (SimRandom.Range(0, 2) == 0)
		{
			// Peaceful: become Explorer, plant 2 Diplomats + 2 Warriors.
			var explorer = ReplaceCell(this, CellType.Explorer, true);
			explorer.Nationality = myNation;
			cellGrid[col, row] = explorer;

			for (int i = 0; i < Math.Min(2, vacant.Count); i++)
			{
				var slot = vacant[i];
				var dip = ReplaceCell(slot, CellType.Diplomat, true);
				dip.Nationality = myNation;
				cellGrid[slot.Column, slot.Row] = dip;
			}
			for (int i = 2; i < Math.Min(4, vacant.Count); i++)
			{
				var slot = vacant[i];
				var war = ReplaceCell(slot, CellType.Warrior, true);
				war.Nationality = myNation;
				cellGrid[slot.Column, slot.Row] = war;
			}
		}
		else
		{
			// Hostile: become Plague, seed 4 additional Plague cells.
			var plague = ReplaceCell(this, CellType.Plague, true);
			plague.Nationality = myNation;
			cellGrid[col, row] = plague;

			for (int i = 0; i < Math.Min(4, vacant.Count); i++)
			{
				var slot = vacant[i];
				var p = ReplaceCell(slot, CellType.Plague, true);
				p.Nationality = myNation;
				cellGrid[slot.Column, slot.Row] = p;
			}
		}
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
			return;

		// Check whether we are now adjacent to any cell of the target nation.
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
