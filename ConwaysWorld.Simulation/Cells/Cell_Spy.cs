namespace ConwaysWorld.Simulation;

/// <summary>
/// A Spy spawns in enemy territory and secretly belongs to a minority nation.
/// <list type="bullet">
///   <item><term>Nationality assignment</term><description>On the first <see cref="SpecialActions"/>
///         call the Spy examines its Moore neighbours, identifies the majority nation as the
///         <c>_enemyNation</c> it is infiltrating, then assigns itself to one of the other
///         nations present (coin-flip if tied).  If no minority nation is found anywhere on
///         the grid it converts to <see cref="Cell_Basic"/>.</description></item>
///   <item><term>Movement</term><description>Each step it finds the enemy King and moves one
///         cell toward it by swapping with the adjacent cell in that direction.</description></item>
///   <item><term>Conversion</term><description>When it swaps into a slot occupied by a living
///         cell, that displaced cell is converted into a <see cref="Cell_Soldier"/> for the
///         Spy's own nation.</description></item>
/// </list>
/// Spies are hunted by Warriors and Hunters of any nation.
/// </summary>
public class Cell_Spy : Cell
{
	private int _enemyNation = -1;
	private bool _nationAssigned = false;
	private Cell? _kingTarget = null;
	private bool _specialPerformed = false;

	/// <summary>Creates a Spy cell at the given position.</summary>
	public Cell_Spy(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Spy;
		Conditions = new HashSet<string>();
		Nationality = -1;
	}

	/// <summary>Standard Live cycle but skips ChooseNation — Spy assigns itself in SpecialActions.</summary>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (Age > MatureAge)
			Conditions.Add("mature");
		_specialPerformed = false;
	}

	/// <inheritdoc/>
	public override void Die()
	{
		base.Die();
		_specialPerformed = true;
	}

	// ── Helpers ───────────────────────────────────────────────────────────────────

	/// <summary>
	/// Analyses the Moore neighbourhood, finds the majority nation (random tie-break),
	/// then assigns <see cref="Cell.Nationality"/> to one of the other nations present.
	/// If no minority nation exists locally, widens the search to the full grid.
	/// Converts self to <see cref="Cell_Basic"/> if no minority nation can be found at all.
	/// </summary>
	private void AssignNation(Cell[,] cellGrid)
	{
		// Count neighbours by nation.
		var nationCounts = new Dictionary<int, int>();
		foreach (var nb in CellNeighborhood.NeighborhoodDict.Values)
		{
			if (nb == this || !nb.IsAlive || nb.Nationality < 0)
				continue;
			nationCounts.TryGetValue(nb.Nationality, out int existing);
			nationCounts[nb.Nationality] = existing + 1;
		}

		if (nationCounts.Count == 0)
		{
			// No neighbours with nations — convert to Basic immediately.
			BecomeBasic(cellGrid);
			return;
		}

		// Majority nation (random among ties).
		int maxCount = 0;
		foreach (var v in nationCounts.Values)
			if (v > maxCount)
				maxCount = v;
		var majorityList = new List<int>();
		foreach (var kv in nationCounts)
			if (kv.Value == maxCount)
				majorityList.Add(kv.Key);
		_enemyNation = majorityList[SimRandom.Range(0, majorityList.Count)];

		// Other nations in the immediate neighbourhood.
		var otherLocal = new List<int>();
		foreach (var k in nationCounts.Keys)
			if (k != _enemyNation)
				otherLocal.Add(k);

		if (otherLocal.Count > 0)
		{
			Nationality = otherLocal[SimRandom.Range(0, otherLocal.Count)];
			_nationAssigned = true;
			return;
		}

		// No minority local — scan grid for any other nation.
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		var otherGlobal = new HashSet<int>();
		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (cell.IsAlive && cell.Nationality >= 0 && cell.Nationality != _enemyNation)
					otherGlobal.Add(cell.Nationality);
			}

		if (otherGlobal.Count == 0)
		{
			BecomeBasic(cellGrid);
			return;
		}

		var otherGlobalList = new List<int>(otherGlobal);
		Nationality = otherGlobalList[SimRandom.Range(0, otherGlobalList.Count)];
		_nationAssigned = true;
	}

	/// <summary>Replaces this cell with a dead Basic in the grid (used when no infiltration is possible).</summary>
	private void BecomeBasic(Cell[,] cellGrid)
	{
		var basic = ReplaceCell(this, CellType.Basic, IsAlive);
		cellGrid[Column, Row] = basic;
	}

	/// <summary>Locates the living King of <see cref="_enemyNation"/> nearest to this cell.</summary>
	private void RefreshKingTarget(Cell[,] cellGrid)
	{
		if (_kingTarget != null && _kingTarget.IsAlive &&
			_kingTarget.CellType == CellType.King && _kingTarget.Nationality == _enemyNation)
			return;

		_kingTarget = null;
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		int bestDist = int.MaxValue;

		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (!cell.IsAlive || cell.CellType != CellType.King || cell.Nationality != _enemyNation)
					continue;
				int dc = Math.Abs(c - Column);
				int dr = Math.Abs(r - Row);
				int dist = Math.Min(dc, cols - dc) + Math.Min(dr, rows - dr);
				if (dist < bestDist)
				{ bestDist = dist; _kingTarget = cell; }
			}
	}

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;

		if (!_nationAssigned)
		{
			AssignNation(cellGrid);
			if (!_nationAssigned)
				return; // became Basic or stayed dead
		}

		RefreshKingTarget(cellGrid);

		if (_kingTarget == null)
			return; // Nothing to move toward — wait.

		// Step one cell toward the king.
		var dest = FindNeighborInDirOfCell(cellGrid, _kingTarget);
		if (dest == this)
			return;

		bool destWasAlive = dest.IsAlive;
		int destCol = dest.Column;
		int destRow = dest.Row;

		moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, destCol, destRow, (int)CellType, Nationality));
		SwapCells(this, dest, cellGrid);

		// After swap: `dest` is now at the Spy's old position.
		// If it was alive, convert it to a Soldier for the Spy's nation.
		if (destWasAlive)
		{
			var soldier = ReplaceCell(dest, CellType.Soldier, true);
			soldier.Nationality = Nationality;
			if (soldier is Cell_Soldier s)
				s.TargetNation = _enemyNation;
			cellGrid[dest.Column, dest.Row] = soldier;
		}
	}
}
