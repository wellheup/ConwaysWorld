namespace ConwaysWorld.Simulation;

/// <summary>
/// The Savior is a unique cell — at most one may exist on the grid at a time.
/// It flees its birth nation toward a randomly chosen foreign nation, converting
/// adjacent Basic cells into <see cref="Cell_Follower"/>s along the way.
/// <para>
/// Movement rules:
/// <list type="bullet">
///   <item>Moves 1 cell/step toward the target nation.</item>
///   <item>Prefers a living destination over an empty slot.</item>
///   <item>Cannot move through Kings or Revolutionaries — tries adjacent alternatives.</item>
///   <item>Immune to Conway crowding/isolation (always survives).</item>
/// </list>
/// </para>
/// <para>
/// On reaching adjacency to the target King: 50 % assimilation (becomes Immortal in
/// target nation, all Followers become Basic in target nation) or 50 % death (all
/// Followers become Zealots).
/// </para>
/// Warriors and Hunters of the Savior's birth nation treat it (and its Followers) as prey.
/// </summary>
public class Cell_Savior : Cell_Converter
{
	private static readonly HashSet<CellType> _blockedTypes = new()
					{ CellType.King, CellType.Revolutionary };

	/// <summary>The nation this Savior was born into.</summary>
	public int BirthNation { get; private set; } = -1;

	/// <summary>The nation this Savior is heading toward.</summary>
	public int TargetNation { get; private set; } = -1;

	/// <summary>All Followers this Savior has created (tracked so we can convert them on death/assimilation).</summary>
	public List<Cell_Follower> Followers { get; } = new();

	/// <summary>Last step's movement direction — broadcast to new Followers each step.</summary>
	public int LastDirC { get; private set; } = 0;
	public int LastDirR { get; private set; } = 0;

	private bool _initialized = false;
	private Cell? _targetKing = null;

	// Die() inherited from Cell_Converter.

	public Cell_Savior(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Savior;
		Conditions = new HashSet<string>();
	}

	/// <summary>Saviors ignore Conway rules.</summary>
	public override bool CalcCellAliveNextGen() => IsAlive;

	/// <summary>
	/// Skips <see cref="Cell.ChooseNation"/> — the Savior manages its own nation
	/// context via <see cref="BirthNation"/> and <see cref="TargetNation"/>.
	/// </summary>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (Age > MatureAge)
			Conditions.Add("mature");
		_specialPerformed = false;
	}

	// ── Helpers ───────────────────────────────────────────────────────────────────

	/// <summary>
	/// First-step initialisation: records birth nation, picks a random other nation as target.
	/// Returns false if fewer than 2 nations are detectable (Savior should become Basic).
	/// </summary>
	private bool Initialize(Cell[,] cellGrid)
	{
		BirthNation = Nationality;

		var foreign = new HashSet<int>();
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (cell.IsAlive && cell.Nationality >= 0 && cell.Nationality != BirthNation)
					foreign.Add(cell.Nationality);
			}

		if (foreign.Count == 0)
			return false;

		var list = new List<int>(foreign);
		TargetNation = list[SimRandom.Range(0, list.Count)];
		_initialized = true;
		return true;
	}

	/// <summary>Finds the living King of the target nation.</summary>
	private void RefreshTargetKing(Cell[,] cellGrid)
	{
		if (_targetKing != null && _targetKing.IsAlive &&
			_targetKing.CellType == CellType.King && _targetKing.Nationality == TargetNation)
			return;

		_targetKing = null;
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		int bestDist = int.MaxValue;

		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (!cell.IsAlive || cell.CellType != CellType.King || cell.Nationality != TargetNation)
					continue;
				int dist = Math.Abs(c - Column) + Math.Abs(r - Row);
				if (dist < bestDist)
				{ bestDist = dist; _targetKing = cell; }
			}
	}

	/// <summary>
	/// Tries to move one step toward <paramref name="goal"/>, avoiding blocked types.
	/// Prefers living destinations. Returns true if a move was made.
	/// </summary>
	private bool MoveToward(Cell goal, Cell[,] cellGrid, List<MoveRecord>? moves)
	{
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		int dc = goal.Column - Column;
		int dr = goal.Row - Row;
		if (Math.Abs(dc) > cols / 2)
			dc = -Math.Sign(dc) * (cols - Math.Abs(dc));
		if (Math.Abs(dr) > rows / 2)
			dr = -Math.Sign(dr) * (rows - Math.Abs(dr));

		int dirC = Math.Sign(dc);
		int dirR = Math.Sign(dr);

		// Build candidate moves: primary diagonal, then axis-aligned alternatives.
		var candidates = new List<(int dc, int dr)>();
		if (dirC != 0 && dirR != 0)
		{
			candidates.Add((dirC, dirR));
			candidates.Add((dirC, 0));
			candidates.Add((0, dirR));
		}
		else if (dirC != 0)
		{
			candidates.Add((dirC, 0));
			candidates.Add((dirC, 1));
			candidates.Add((dirC, -1));
		}
		else if (dirR != 0)
		{
			candidates.Add((0, dirR));
			candidates.Add((1, dirR));
			candidates.Add((-1, dirR));
		}

		// Among unblocked candidates, prefer ones with a living dest.
		Cell? bestDest = null;
		int bestDc = 0, bestDr = 0;
		bool bestIsAlive = false;

		foreach (var (cdc, cdr) in candidates)
		{
			int nc = (Column + cdc + cols) % cols;
			int nr = (Row + cdr + rows) % rows;
			var dest = cellGrid[nc, nr];
			if (dest == this)
				continue;
			if (_blockedTypes.Contains(dest.CellType))
				continue;

			bool alive = dest.IsAlive;
			if (bestDest == null || (alive && !bestIsAlive))
			{
				bestDest = dest;
				bestDc = cdc;
				bestDr = cdr;
				bestIsAlive = alive;
			}
		}

		if (bestDest == null)
			return false;

		LastDirC = bestDc;
		LastDirR = bestDr;
		moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, bestDest.Column, bestDest.Row, (int)CellType, Nationality));
		SwapCells(this, bestDest, cellGrid);
		return true;
	}

	/// <summary>
	/// Converts all tracked Followers to Zealots (called on Savior death).
	/// </summary>
	public void ConvertFollowersToZealots(Cell[,] cellGrid)
	{
		foreach (var follower in Followers)
		{
			if (!follower.IsAlive)
				continue;
			var zealot = ReplaceCell(follower, CellType.Zealot, true);
			zealot.Nationality = BirthNation;
			cellGrid[follower.Column, follower.Row] = zealot;
		}
		Followers.Clear();
	}

	/// <summary>
	/// Converts all tracked Followers to Basic cells in the target nation (called on assimilation).
	/// </summary>
	public void AssimilateFollowers(Cell[,] cellGrid)
	{
		foreach (var follower in Followers)
		{
			if (!follower.IsAlive)
				continue;
			var basic = ReplaceCell(follower, CellType.Basic, true);
			basic.Nationality = TargetNation;
			cellGrid[follower.Column, follower.Row] = basic;
		}
		Followers.Clear();
	}

	/// <summary>
	/// Tries to convert adjacent Basic cells to Followers (50 % chance each).
	/// Updates direction broadcast on all existing live Followers.
	/// </summary>
	private void UpdateFollowers(Cell[,] cellGrid)
	{
		// Broadcast latest direction to all live followers.
		for (int i = Followers.Count - 1; i >= 0; i--)
		{
			var f = Followers[i];
			if (!f.IsAlive)
			{ Followers.RemoveAt(i); continue; }
			f.LastSaviorDirC = LastDirC;
			f.LastSaviorDirR = LastDirR;
		}

		// Try converting adjacent Basic cells.
		foreach (var nb in CellNeighborhood.NeighborhoodDict.Values)
		{
			if (nb == this || !nb.IsAlive || nb.CellType != CellType.Basic)
				continue;
			if (!SimRandom.CoinFlip())
				continue;

			var follower = (Cell_Follower)ReplaceCell(nb, CellType.Follower, true);
			follower.Nationality = Nationality;
			follower.SaviorBirthNation = BirthNation;
			follower.LastSaviorDirC = LastDirC;
			follower.LastSaviorDirR = LastDirR;
			cellGrid[nb.Column, nb.Row] = follower;
			Followers.Add(follower);
		}
	}

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;

		if (!_initialized)
		{
			if (!Initialize(cellGrid))
			{
				// No foreign nation available — become Basic.
				var basic = ReplaceCell(this, CellType.Basic, true);
				cellGrid[Column, Row] = basic;
				return;
			}
		}

		RefreshTargetKing(cellGrid);

		// Check adjacency to target king — resolve outcome.
		if (_targetKing != null && _targetKing.IsAlive)
		{
			bool adjacent = false;
			foreach (var nb in CellNeighborhood.NeighborhoodDict.Values)
				if (nb == _targetKing)
				{ adjacent = true; break; }

			if (adjacent)
			{
				if (SimRandom.CoinFlip())
				{
					// Assimilate: become Immortal in target nation.
					AssimilateFollowers(cellGrid);
					var immortal = ReplaceCell(this, CellType.Immortal, true);
					immortal.Nationality = TargetNation;
					cellGrid[Column, Row] = immortal;
				}
				else
				{
					// Killed.
					ConvertFollowersToZealots(cellGrid);
					Die();
					Conditions.Add("cleanup");
				}
				return;
			}
		}

		// Move toward target king or (if none yet) any target-nation cell.
		Cell? goal = _targetKing;
		if (goal == null)
		{
			// Head toward any cell in the target nation.
			goal = SelectNearbyCellByRule(cellGrid,
							c => c.IsAlive && c.Nationality == TargetNation,
							int.MaxValue);
		}

		if (goal != null)
			MoveToward(goal, cellGrid, moves);

		// After moving, rebuild neighbourhood and try to convert adjacent Basics.
		UpdateFollowers(cellGrid);
	}
}
