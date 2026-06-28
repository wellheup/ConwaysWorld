namespace ConwaysWorld.Simulation;

/// <summary>
/// A PlagueRat spawns at the emptiest region of the grid (like a Wayfinder), then travels
/// in a single random direction for up to 1/3 of the available grid space in that axis.
/// <para>
/// Movement: swaps with the next cell in its chosen direction each step.
/// It infects only the cell it physically swaps with (not all 8 neighbours).
/// If it cannot swap (blocked by an Irradiated cell or another immovable), it dies immediately.
/// If it exhausts its movement budget, it dies immediately.
/// </para>
/// <para>
/// Disease: uses a unique <c>r_</c> plague strain (see <see cref="Cell_Spreader.StrainId"/>).
/// Infection is applied to the swapped cell only, with 100% transmission (no roll) —
/// it directly adds the strain tag.  The <see cref="Cell_Spreader.CanInfect"/> guard
/// (alive, not Immortal, not Irradiated, not immune, not vaccinated) is checked before tagging.
/// </para>
/// <para>
/// Nationless, cannot be converted to another type, and is a valid target for any
/// cell that kills others (Warriors, Hunters, Bombers, Soldiers, Barbarians, Zealots).
/// </para>
/// </summary>
public class Cell_PlagueRat : Cell_Spreader
{
	/// <summary>Direction column component (-1, 0, or 1).</summary>
	private int _dirC;

	/// <summary>Direction row component (-1, 0, or 1).</summary>
	private int _dirR;

	/// <summary>Remaining movement steps before the rat stops and dies.</summary>
	private int _movesLeft;

	/// <summary>Whether the rat has reached its spawn destination and should start moving.</summary>
	private bool _hasArrived;

	/// <summary>Whether SpecialActions has already run this step.</summary>
	private bool _specialPerformed;

	private int _targetCol = -1;
	private int _targetRow = -1;

	/// <summary>
	/// Creates a PlagueRat cell.  Passes <c>'r'</c> to <see cref="Cell_Spreader"/> which
	/// generates the unique <see cref="Cell_Spreader.StrainId"/> tag automatically.
	/// </summary>
	public Cell_PlagueRat(int column, int row, bool isAlive)
		: base(column, row, isAlive, 'r')
	{
		CellType = CellType.PlagueRat;
		Nationality = -1;
		_hasArrived = false;
		_specialPerformed = false;
		_dirC = 0;
		_dirR = 0;
		_movesLeft = 0;
	}

	/// <inheritdoc/>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		CellType = CellType.PlagueRat;
		Nationality = -1;
		_specialPerformed = false;
	}

	/// <inheritdoc/>
	public override void Die()
	{
		IsAlive = false;
		Nationality = -1;
		_specialPerformed = true;
	}

	/// <summary>PlagueRat ignores Conway rules — alive until it stops or is killed.</summary>
	public override bool CalcCellAliveNextGen() => IsAlive;

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;

		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		// Phase 1: travel to emptiest region (like Wayfinder), no infection yet.
		if (!_hasArrived)
		{
			if (_targetCol < 0)
				SelectTarget(cellGrid);

			if (_targetCol < 0)
			{
				// No target found — arrive in place and start moving.
				ArriveAndChooseDirection(cols, rows);
				return;
			}

			int dc = _targetCol - Column;
			if (Math.Abs(dc) > cols / 2)
				dc = -Math.Sign(dc) * (cols - Math.Abs(dc));
			int dr = _targetRow - Row;
			if (Math.Abs(dr) > rows / 2)
				dr = -Math.Sign(dr) * (rows - Math.Abs(dr));

			int chebyDist = Math.Max(Math.Abs(dc), Math.Abs(dr));
			if (chebyDist <= 1)
			{
				ArriveAndChooseDirection(cols, rows);
				return;
			}

			// Move up to 2 cells toward target (no infection during transit).
			int dirC = Math.Sign(dc);
			int dirR = Math.Sign(dr);

			int col2 = (Column + dirC * 2 + cols) % cols;
			int row2 = (Row + dirR * 2 + rows) % rows;
			int col1 = (Column + dirC + cols) % cols;
			int row1 = (Row + dirR + rows) % rows;

			var dest2 = cellGrid[col2, row2];
			var dest1 = cellGrid[col1, row1];

			if (dest2.CellType != CellType.Irradiated && !dest2.IsAlive && dest2 != this)
			{
				moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, col2, row2, (int)CellType, Nationality));
				SwapCells(this, dest2, cellGrid);
			}
			else if (dest1.CellType != CellType.Irradiated && !dest1.IsAlive && dest1 != this)
			{
				moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, col1, row1, (int)CellType, Nationality));
				SwapCells(this, dest1, cellGrid);
			}
			return;
		}

		// Phase 2: directional run — infect the cell we swap with, die when done.
		if (_movesLeft <= 0)
		{
			Die();
			cellGrid[Column, Row].Die();
			return;
		}

		int nc = (Column + _dirC + cols) % cols;
		int nr = (Row + _dirR + rows) % rows;
		var next = cellGrid[nc, nr];

		// Blocked by Irradiated — die immediately.
		if (next.CellType == CellType.Irradiated)
		{
			Die();
			cellGrid[Column, Row].Die();
			return;
		}

		// Infect the cell we are about to swap with (using Cell_Spreader.CanInfect guard).
		if (CanInfect(next))
			cellGrid[nc, nr].Conditions.Add(StrainId);

		moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, nc, nr, (int)CellType, Nationality));
		SwapCells(this, next, cellGrid);
		_movesLeft--;
	}

	// ── Private helpers ───────────────────────────────────────────────────────────

	/// <summary>
	/// Scans the grid (sampled) for the cell with fewest living neighbours within radius 5.
	/// </summary>
	private void SelectTarget(Cell[,] cellGrid)
	{
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		int step = Math.Max(1, Math.Min(cols, rows) / 15);

		int bestCount = int.MaxValue;
		int bestCol = -1, bestRow = -1;

		for (int c = 0; c < cols; c += step)
			for (int r = 0; r < rows; r += step)
			{
				if (cellGrid[c, r].IsAlive)
					continue;
				int count = CountLivingInRadius(cellGrid, c, r, 5);
				if (count < bestCount)
				{
					bestCount = count;
					bestCol = c;
					bestRow = r;
				}
			}

		_targetCol = bestCol;
		_targetRow = bestRow;
	}

	private static int CountLivingInRadius(Cell[,] cellGrid, int c, int r, int radius)
	{
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		int count = 0;
		for (int dc = -radius; dc <= radius; dc++)
			for (int dr = -radius; dr <= radius; dr++)
			{
				int nc = (c + dc + cols) % cols;
				int nr = (r + dr + rows) % rows;
				if (cellGrid[nc, nr].IsAlive)
					count++;
			}
		return count;
	}

	/// <summary>
	/// Called on arrival at the target. Picks a random cardinal or diagonal direction
	/// and sets the movement budget to 1/3 of the grid span in that direction.
	/// </summary>
	private void ArriveAndChooseDirection(int cols, int rows)
	{
		_hasArrived = true;

		// Pick a random direction (8 possible, no center).
		string dir = "center";
		while (dir == "center")
			dir = Cell_Neighborhood.NeighborHoodKeys[SimRandom.Range(0, Cell_Neighborhood.NeighborHoodKeys.Length)];

		(_dirC, _dirR) = dir switch
		{
			"north" => (0, -1),
			"south" => (0, 1),
			"west" => (-1, 0),
			"east" => (1, 0),
			"northwest" => (-1, -1),
			"northeast" => (1, -1),
			"southwest" => (-1, 1),
			"southeast" => (1, 1),
			_ => (1, 0),
		};

		// Budget = 1/3 of the dominant axis size.
		int span = Math.Abs(_dirC) > 0 && Math.Abs(_dirR) > 0
						? Math.Min(cols, rows)
						: (Math.Abs(_dirC) > 0 ? cols : rows);
		_movesLeft = Math.Max(1, span / 3);
	}
}
