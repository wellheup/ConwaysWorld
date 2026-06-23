namespace ConwaysWorld.Simulation;

/// <summary>
/// A Necromancer spawns randomly (never elected) and keeps itself alive as long as it has
/// at least 2 active zombie cells. It can only die if explicitly killed by another cell.
/// <para>
/// On spawn it immediately resurrects the 3 nearest dead cells that have a <see cref="Cell.LastType"/>
/// as zombies belonging to itself. Each step thereafter it resurrects one more such cell.
/// </para>
/// <para>
/// Zombies treat the Necromancer as their King and each other as their nation-mates.
/// If the Necromancer dies, all of its zombies are killed.
/// </para>
/// </summary>
public class Cell_Necromancer : Cell
{
	/// <summary>All zombie cells currently belonging to this Necromancer.</summary>
	public List<Cell_Zombie> Zombies { get; } = new();

	/// <summary>Whether the initial burst of 3 resurrections has been performed.</summary>
	private bool _initialResurrectionDone = false;

	/// <summary>Prevents SpecialActions from running twice in one step.</summary>
	private bool _specialPerformed = false;

	/// <summary>Creates a Necromancer cell at the given position.</summary>
	public Cell_Necromancer(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Necromancer;
		Conditions = new HashSet<string>();
		Nationality = -1;
	}

	/// <inheritdoc/>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		CellType = CellType.Necromancer;
		Nationality = -1;
		_specialPerformed = false;
	}

	/// <summary>
	/// Records LastType before dying, then kills all zombies.
	/// (Zombie cleanup happens in Model.UpdateNecromancers — Die() just flags death.)
	/// </summary>
	public override void Die()
	{
		if (IsAlive && CellType != CellType.Dead)
			LastType = CellType;
		IsAlive = false;
		Age = 0;
		Nationality = -1;
	}

	/// <summary>
	/// The Necromancer only dies if it has fewer than 2 living zombies.
	/// It ignores Conway rules entirely.
	/// </summary>
	public override bool CalcCellAliveNextGen()
	{
		if (!IsAlive)
			return false;
		// Remove dead zombies from the list first.
		Zombies.RemoveAll(z => !z.IsAlive);
		return Zombies.Count >= 2;
	}

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;

		// Remove dead zombies.
		Zombies.RemoveAll(z => !z.IsAlive);

		if (!_initialResurrectionDone)
		{
			_initialResurrectionDone = true;
			// Resurrect the 3 nearest eligible dead cells.
			var targets = FindNearestDeadWithLastType(cellGrid, 3);
			foreach (var target in targets)
				Resurrect(target, cellGrid);
		}
		else
		{
			// Resurrect 1 eligible dead cell per step.
			var targets = FindNearestDeadWithLastType(cellGrid, 1);
			foreach (var target in targets)
				Resurrect(target, cellGrid);
		}
	}

	// ── Public helper called by Model ─────────────────────────────────────────────

	/// <summary>
	/// Kills all zombies belonging to this Necromancer, wiping their last-type so they
	/// cannot be re-resurrected. Called by Model when the Necromancer dies.
	/// </summary>
	public void KillAllZombies(Cell[,] cellGrid)
	{
		foreach (var zombie in Zombies)
		{
			if (!zombie.IsAlive)
				continue;
			// Replace with a dead Basic that has no last-type (permanently destroyed).
			var dead = ReplaceCell(zombie, CellType.Basic, false);
			dead.LastType = null;
			cellGrid[zombie.Column, zombie.Row] = dead;
		}
		Zombies.Clear();
	}

	// ── Private helpers ───────────────────────────────────────────────────────────

	/// <summary>
	/// Scans the entire grid for dead cells that have a <see cref="Cell.LastType"/>,
	/// sorted by Chebyshev distance from this Necromancer. Returns up to
	/// <paramref name="count"/> candidates.
	/// </summary>
	private List<Cell> FindNearestDeadWithLastType(Cell[,] cellGrid, int count)
	{
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		var candidates = new List<(int dist, Cell cell)>();
		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (cell.IsAlive)
					continue;
				if (cell.LastType == null)
					continue;
				if (cell.CellType == CellType.Irradiated)
					continue;
				int dc = Math.Abs(c - Column);
				int dr = Math.Abs(r - Row);
				int dist = Math.Max(Math.Min(dc, cols - dc), Math.Min(dr, rows - dr));
				candidates.Add((dist, cell));
			}

		candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
		var result = new List<Cell>();
		for (int i = 0; i < Math.Min(count, candidates.Count); i++)
			result.Add(candidates[i].cell);
		return result;
	}

	/// <summary>
	/// Converts <paramref name="deadCell"/> into a Zombie of its <see cref="Cell.LastType"/>,
	/// registers it with this Necromancer, and places it into <paramref name="cellGrid"/>.
	/// </summary>
	private void Resurrect(Cell deadCell, Cell[,] cellGrid)
	{
		var zombie = new Cell_Zombie(deadCell.Column, deadCell.Row, true, this, deadCell.LastType!.Value);
		zombie.LastType = deadCell.LastType;
		zombie.CellNeighborhood = deadCell.CellNeighborhood;
		cellGrid[deadCell.Column, deadCell.Row] = zombie;
		Zombies.Add(zombie);
	}
}
