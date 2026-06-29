namespace ConwaysWorld.Simulation;

public partial class Model
{
	/// <summary>
	/// Creates a living cell of the requested type at the given position with an optional nationality.
	/// Falls back to a live Basic cell for types that require special construction (e.g. Zombie).
	/// </summary>
	public Cell CreateCellOfType(CellType type, int col, int row, int nationality = -1)
	{
		Cell cell = type switch
		{
			CellType.Basic => new Cell_Basic(col, row, true),
			CellType.Immortal => new Cell_Immortal(col, row, true),
			CellType.Diseased => new Cell_Diseased(col, row, true),
			CellType.Plague => new Cell_Plague(col, row, true),
			CellType.Traveler => new Cell_Traveler(col, row, true),
			CellType.Explorer => new Cell_Explorer(col, row, true),
			CellType.Doctor => new Cell_Doctor(col, row, true),
			CellType.Warrior => new Cell_Warrior(col, row, true),
			CellType.Hunter => new Cell_Hunter(col, row, true),
			CellType.Bomber => new Cell_Bomber(col, row, true),
			CellType.Diplomat => new Cell_Diplomat(col, row, true),
			CellType.King => new Cell_King(col, row, true),
			CellType.Rebel => new Cell_Rebel(col, row, true),
			CellType.Revolutionary => new Cell_Revolutionary(col, row, true),
			CellType.Voyager => new Cell_Voyager(col, row, true),
			CellType.Wayfinder => new Cell_Wayfinder(col, row, true),
			CellType.Islander => new Cell_Islander(col, row, true),
			CellType.Barbarian => new Cell_Barbarian(col, row, true),
			CellType.Spy => new Cell_Spy(col, row, true),
			CellType.Soldier => new Cell_Soldier(col, row, true),
			CellType.Conquistador => new Cell_Conquistador(col, row, true),
			CellType.Savior => new Cell_Savior(col, row, true),
			CellType.Follower => new Cell_Follower(col, row, true),
			CellType.Zealot => new Cell_Zealot(col, row, true),
			CellType.Irradiated => new Cell_Irradiated(col, row, true),
			CellType.PlagueRat => new Cell_PlagueRat(col, row, true),
			CellType.Necromancer => new Cell_Necromancer(col, row, true),
			CellType.Mutant => new Cell_Mutant(col, row, true),
			_ => new Cell_Basic(col, row, true),
		};
		if (nationality >= 0)
			cell.Nationality = nationality;
		return cell;
	}

	/// <summary>Places a living cell of the given type at (col, row), overwriting whatever was there.</summary>
	public void PlaceCell(int col, int row, CellType type, int nationality)
	{
		if (col < 0 || col >= _columns || row < 0 || row >= _rows)
			return;
		CellGrid[col, row] = CreateCellOfType(type, col, row, nationality);
	}

	/// <summary>Replaces the cell at (col, row) with a dead Basic cell.</summary>
	public void RemoveCell(int col, int row)
	{
		if (col < 0 || col >= _columns || row < 0 || row >= _rows)
			return;
		CellGrid[col, row] = new Cell_Basic(col, row, false);
	}

	/// <summary>
	/// Moves the cell at (fromCol, fromRow) to (toCol, toRow).
	/// The source slot is cleared; the destination gets a fresh cell of the same type and nationality.
	/// </summary>
	public void MoveCell(int fromCol, int fromRow, int toCol, int toRow)
	{
		if (fromCol < 0 || fromCol >= _columns || fromRow < 0 || fromRow >= _rows)
			return;
		if (toCol < 0 || toCol >= _columns || toRow < 0 || toRow >= _rows)
			return;
		var src = CellGrid[fromCol, fromRow];
		CellGrid[fromCol, fromRow] = new Cell_Basic(fromCol, fromRow, false);
		CellGrid[toCol, toRow] = CreateCellOfType(src.CellType, toCol, toRow, src.Nationality);
	}

	/// <summary>
	/// Kills every living cell on the grid (replaces each with a dead Basic cell).
	/// Returns the list of positions that were alive, for undo purposes.
	/// </summary>
	public List<(int Col, int Row, CellType Type, int Nat)> ClearAllCells()
	{
		var cleared = new List<(int, int, CellType, int)>();
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (cell.IsAlive)
				{
					cleared.Add((c, r, cell.CellType, cell.Nationality));
					CellGrid[c, r] = new Cell_Basic(c, r, false);
				}
			}
		return cleared;
	}

	/// <summary>Alias for <see cref="ClearAllCells"/>.</summary>
	public List<(int Col, int Row, CellType Type, int Nat)> ClearGrid() => ClearAllCells();

	/// <summary>Restores a single cell to a snapshot state (used for undo and redo).</summary>
	public void RestoreCell(int col, int row, bool alive, CellType type, int nat)
	{
		if (col < 0 || col >= _columns || row < 0 || row >= _rows)
			return;
		if (!alive || type == CellType.Dead)
			CellGrid[col, row] = new Cell_Basic(col, row, false);
		else
			CellGrid[col, row] = CreateCellOfType(type, col, row, nat);
	}
}
