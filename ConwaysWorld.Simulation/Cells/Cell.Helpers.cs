namespace ConwaysWorld.Simulation;

public abstract partial class Cell
{
	// ── Cell replacement and movement ─────────────────────────────────────────────

	/// <summary>
	/// Replaces the cell at <paramref name="oldCell"/>'s position with a new instance of
	/// <paramref name="cellType"/>, preserving conditions, neighbourhood reference, and nationality
	/// (nationality is cleared if <paramref name="isAlive"/> is false).
	/// </summary>
	public static Cell ReplaceCell(Cell oldCell, CellType cellType, bool isAlive)
	{
		int col = oldCell.Column;
		int row = oldCell.Row;
		Cell cell = cellType switch
		{
			CellType.Basic => new Cell_Basic(col, row, isAlive),
			CellType.Immortal => new Cell_Immortal(col, row, isAlive),
			CellType.Diseased => new Cell_Diseased(col, row, isAlive),
			CellType.Plague => new Cell_Plague(col, row, isAlive),
			CellType.Traveler => new Cell_Traveler(col, row, isAlive),
			CellType.Explorer => new Cell_Explorer(col, row, isAlive),
			CellType.Doctor => new Cell_Doctor(col, row, isAlive),
			CellType.Diplomat => new Cell_Diplomat(col, row, isAlive),
			CellType.King => new Cell_King(col, row, isAlive),
			CellType.Hunter => new Cell_Hunter(col, row, isAlive),
			CellType.Bomber => new Cell_Bomber(col, row, isAlive),
			CellType.Warrior => new Cell_Warrior(col, row, isAlive),
			CellType.Rebel => new Cell_Rebel(col, row, isAlive),
			CellType.Revolutionary => new Cell_Revolutionary(col, row, isAlive),
			CellType.Voyager => new Cell_Voyager(col, row, isAlive),
			CellType.Wayfinder => new Cell_Wayfinder(col, row, isAlive),
			CellType.Islander => new Cell_Islander(col, row, isAlive),
			CellType.Barbarian => new Cell_Barbarian(col, row, isAlive),
			CellType.Spy => new Cell_Spy(col, row, isAlive),
			CellType.Soldier => new Cell_Soldier(col, row, isAlive),
			CellType.Conquistador => new Cell_Conquistador(col, row, isAlive),
			CellType.Savior => new Cell_Savior(col, row, isAlive),
			CellType.Follower => new Cell_Follower(col, row, isAlive),
			CellType.Zealot => new Cell_Zealot(col, row, isAlive),
			CellType.Irradiated => new Cell_Irradiated(col, row, isAlive),
			CellType.PlagueRat => new Cell_PlagueRat(col, row, isAlive),
			CellType.Zombie => new Cell_Basic(col, row, isAlive),    // zombies replaced via Necromancer only
			CellType.Necromancer => new Cell_Necromancer(col, row, isAlive),
			CellType.Mutant => new Cell_Mutant(col, row, isAlive),
			_ => new Cell_Basic(col, row, isAlive),
		};
		cell.Conditions = new HashSet<string>(oldCell.Conditions);
		cell.CellNeighborhood = oldCell.CellNeighborhood;
		// Bombers are permanently nationless regardless of the source cell's nationality.
		cell.Nationality = (isAlive && cellType != CellType.Bomber) ? oldCell.Nationality : -1;
		cell.LastType = oldCell.LastType;
		return cell;
	}

	/// <summary>
	/// Exchanges the grid positions of <paramref name="origin"/> and <paramref name="dest"/>,
	/// updating their <see cref="Column"/>/<see cref="Row"/> fields and rebuilding their
	/// <see cref="Cell_Neighborhood"/> references in the live grid.
	/// Used by moving cell types (Traveler, Explorer, Hunter, Diplomat).
	/// </summary>
	public static void SwapCells(Cell origin, Cell dest, Cell[,] cellGrid)
	{
		// Irradiated tiles are impassable — any cell that tries to move onto one dies instantly.
		// The Irradiated cell stays in place and is unaffected.
		if (dest.CellType == CellType.Irradiated)
		{
			origin.Die();
			return;
		}

		int oldCol = origin.Column;
		int oldRow = origin.Row;

		cellGrid[dest.Column, dest.Row] = origin;
		cellGrid[origin.Column, origin.Row] = dest;

		origin.Column = dest.Column;
		origin.Row = dest.Row;
		dest.Column = oldCol;
		dest.Row = oldRow;

		origin.CellNeighborhood = new Cell_Neighborhood(cellGrid, origin.Column, origin.Row);
		dest.CellNeighborhood = new Cell_Neighborhood(cellGrid, dest.Column, dest.Row);
	}

	// ── Special conditions ─────────────────────────────────────────────────────────

	/// <summary>
	/// Triggered once when a Basic cell has the <c>immaculate</c> condition (1 % spawn chance).
	/// Forces this cell alive (if not already) and then spawns two additional axis-aligned
	/// neighbours (either north+south or east+west, chosen randomly), but only if those
	/// slots currently have no living neighbours (to avoid disrupting dense areas).
	/// </summary>
	public virtual void Immaculate(Cell[,] cellGrid)
	{
		Conditions.Remove("immaculate");
		LiveNoNeighbors(cellGrid, this);
		if (!IsAlive)
			return;

		if (SimRandom.Range(1, 3) == 1)
		{
			LiveNoNeighbors(cellGrid, CellNeighborhood.NeighborhoodDict["north"]);
			LiveNoNeighbors(cellGrid, CellNeighborhood.NeighborhoodDict["south"]);
		}
		else
		{
			LiveNoNeighbors(cellGrid, CellNeighborhood.NeighborhoodDict["west"]);
			LiveNoNeighbors(cellGrid, CellNeighborhood.NeighborhoodDict["east"]);
		}
	}

	/// <summary>
	/// Triggered once when a living cell reaches the <c>mature</c> age threshold.
	/// Picks a random empty adjacent slot and places a new cell of the same type there.
	/// The breeding cell's own age is reset to 0 afterwards.
	/// </summary>
	public virtual void Breed(Cell[,] cellGrid)
	{
		Conditions.Remove("mature");
		if (!IsAlive)
			return;

		Age = 0;
		var empties = new List<Cell>();
		foreach (var kv in CellNeighborhood.NeighborhoodDict)
			if (kv.Value != null && !kv.Value.IsAlive)
				empties.Add(kv.Value);

		if (empties.Count == 0)
			return;
		int idx = SimRandom.Range(0, empties.Count);
		var slot = empties[idx];
		var newCell = ReplaceCell(slot, CellType, true);
		cellGrid[slot.Column, slot.Row] = newCell;
	}

	// ── Nation assignment ──────────────────────────────────────────────────────────

	/// <summary>
	/// If this living cell has no nationality yet, inherits the nationality of a random
	/// living neighbour that already has one.  Called from <see cref="Live"/> each step.
	/// </summary>
	public void ChooseNation()
	{
		if (!IsAlive || Nationality >= 0)
			return;

		if (CellNeighborhood != null && CellNeighborhood.NumNeighbors > 0)
		{
			var neighborNations = new List<int>();
			foreach (var neighbor in CellNeighborhood.NeighborhoodDict.Values)
				if (neighbor.IsAlive && neighbor.Nationality >= 0)
					neighborNations.Add(neighbor.Nationality);

			if (neighborNations.Count > 0)
				Nationality = neighborNations[SimRandom.Range(0, neighborNations.Count)];
		}
	}

	// ── Spatial helpers ────────────────────────────────────────────────────────────

	/// <summary>
	/// Generates a random 8-character alphanumeric condition tag with the given prefix character.
	/// Used to create unique disease / plague strain identifiers (e.g. <c>d_3a7f9b2c</c>).
	/// </summary>
	public static string RandomCondition(char prefix)
	{
		const string chars = "0123456789";
		var result = new char[8];
		for (int i = 0; i < result.Length; i++)
			result[i] = chars[SimRandom.Range(0, chars.Length)];
		return prefix + "_" + new string(result);
	}

	/// <summary>
	/// Searches outward ring by ring (Chebyshev distance 1 to <paramref name="maxRange"/>-1)
	/// and returns a random matching cell from the nearest ring that contains at least one match.
	/// Uses toroidal addressing. Returns <c>null</c> if no match is found within range.
	/// </summary>
	/// <param name="rule">Predicate a candidate cell must satisfy.</param>
	/// <param name="maxRange">Exclusive upper bound on search radius.</param>
	protected Cell? SelectNearbyCellByRule(Cell[,] cellGrid, Func<Cell, bool> rule, int maxRange)
	{
		if (maxRange <= 1)
			return null;
		var candidates = new List<Cell>();
		int range = 1;
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		int searchLimit = Math.Min(maxRange, Math.Max(cols, rows) + 1);

		while (candidates.Count == 0 && range < searchLimit)
		{
			for (int x = -range; x <= range; x++)
			{
				int tc = (Column + x + cols) % cols;
				int tr = (Row - range + rows) % rows;
				if (rule(cellGrid[tc, tr]))
					candidates.Add(cellGrid[tc, tr]);
				tr = (Row + range + rows) % rows;
				if (rule(cellGrid[tc, tr]))
					candidates.Add(cellGrid[tc, tr]);
			}
			for (int y = -range + 1; y <= range - 1; y++)
			{
				int tr = (Row + y + rows) % rows;
				int tc = (Column - range + cols) % cols;
				if (rule(cellGrid[tc, tr]))
					candidates.Add(cellGrid[tc, tr]);
				tc = (Column + range + cols) % cols;
				if (rule(cellGrid[tc, tr]))
					candidates.Add(cellGrid[tc, tr]);
			}
			range++;
		}

		return candidates.Count > 0 ? candidates[SimRandom.Range(0, candidates.Count)] : null;
	}

	/// <summary>
	/// Returns all cells within the square [−<paramref name="maxRange"/>, +<paramref name="maxRange"/>]
	/// (Chebyshev) that satisfy <paramref name="rule"/>, excluding this cell itself.
	/// Uses toroidal addressing.
	/// </summary>
	protected List<Cell> GetAllCellsInRangeByRule(Cell[,] cellGrid, Func<Cell, bool> rule, int maxRange)
	{
		var result = new List<Cell>();
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		for (int co = -maxRange; co <= maxRange; co++)
		{
			for (int ro = -maxRange; ro <= maxRange; ro++)
			{
				int nc = (Column + co + cols) % cols;
				int nr = (Row + ro + rows) % rows;
				var c = cellGrid[nc, nr];
				if (c != this && rule(c))
					result.Add(c);
			}
		}
		return result;
	}

	/// <summary>
	/// Returns the immediate neighbour cell (from this cell's neighbourhood) that lies in the
	/// direction of <paramref name="target"/>, taking the shortest toroidal path into account.
	/// Returns <c>this</c> if <paramref name="target"/> is null.
	/// Used by Hunter and Diplomat to step toward a distant cell one slot per turn.
	/// </summary>
	public Cell FindNeighborInDirOfCell(Cell[,] cellGrid, Cell target)
	{
		if (target == null)
			return this;

		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		int innerDistC = Math.Abs(Column - target.Column);
		int outerDistC = Math.Abs(cols - innerDistC);
		int targetDirC = Column == target.Column ? 0 : (Column < target.Column ? 1 : -1);
		int fastestDirC = innerDistC <= outerDistC ? 1 : -1;
		int nearestCol = (Column + targetDirC * fastestDirC + cols) % cols;

		int innerDistR = Math.Abs(Row - target.Row);
		int outerDistR = Math.Abs(rows - innerDistR);
		int targetDirR = Row == target.Row ? 0 : (Row < target.Row ? 1 : -1);
		int fastestDirR = innerDistR <= outerDistR ? 1 : -1;
		int nearestRow = (Row + targetDirR * fastestDirR + rows) % rows;

		return cellGrid[nearestCol, nearestRow];
	}

	// ── Private helpers ────────────────────────────────────────────────────────────

	/// <summary>Forces <paramref name="cell"/> alive only if it currently has zero living neighbours.</summary>
	private void LiveNoNeighbors(Cell[,] cellGrid, Cell cell)
	{
		if (cell.CellNeighborhood.NumNeighbors == 0)
		{
			cell.CellNeighborhood = new Cell_Neighborhood(cellGrid, cell.Column, cell.Row);
			cell.Live();
		}
	}
}
