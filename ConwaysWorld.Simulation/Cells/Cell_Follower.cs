namespace ConwaysWorld.Simulation;

/// <summary>
/// A Follower is created by a <see cref="Cell_Savior"/> passing adjacent to a Basic cell (50 % chance).
/// <list type="bullet">
///   <item>Waits <see cref="WaitSteps"/> steps before it starts moving.</item>
///   <item>Moves 1 cell/step in the last direction broadcast by its Savior
///         (<see cref="LastSaviorDirC"/> / <see cref="LastSaviorDirR"/>).</item>
///   <item>Cannot move through Kings or Revolutionaries or other Followers.</item>
///   <item>Prefers swapping with a living cell over an empty slot.</item>
///   <item>Immune to Conway crowding / isolation (always survives).</item>
///   <item>After 4 consecutive blocked steps reverts to Basic.</item>
///   <item>Targeted by Warriors and Hunters of the Savior's birth nation.</item>
/// </list>
/// </summary>
public class Cell_Follower : Cell
{
	public const int WaitSteps = 3;
	private static readonly HashSet<CellType> _blockedTypes = new()
				{ CellType.King, CellType.Revolutionary, CellType.Follower };

	/// <summary>Column direction last broadcast by the Savior (-1, 0, or 1).</summary>
	public int LastSaviorDirC = 0;
	/// <summary>Row direction last broadcast by the Savior (-1, 0, or 1).</summary>
	public int LastSaviorDirR = 0;

	/// <summary>The nation the Savior originated from — Hunters/Warriors of this nation target followers.</summary>
	public int SaviorBirthNation = -1;

	private int _waitRemaining;
	private int _blockedStreak = 0;
	private bool _specialPerformed = false;

	public Cell_Follower(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Follower;
		Conditions = new HashSet<string>();
		_waitRemaining = WaitSteps;
	}

	/// <summary>Followers ignore Conway rules — they always survive while alive.</summary>
	public override bool CalcCellAliveNextGen() => IsAlive;

	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (Age > MatureAge)
			Conditions.Add("mature");
		ChooseNation();
		_specialPerformed = false;
	}

	public override void Die()
	{
		base.Die();
		_specialPerformed = true;
	}

	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;

		if (_waitRemaining > 0)
		{
			_waitRemaining--;
			return;
		}

		// No direction yet — stay put.
		if (LastSaviorDirC == 0 && LastSaviorDirR == 0)
			return;

		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		int tc = (Column + LastSaviorDirC + cols) % cols;
		int tr = (Row + LastSaviorDirR + rows) % rows;

		var dest = cellGrid[tc, tr];

		// Blocked by king, revolutionary, or fellow follower.
		if (dest == this || _blockedTypes.Contains(dest.CellType))
		{
			_blockedStreak++;
			if (_blockedStreak >= 4)
			{
				// Revert to Basic in place.
				var basic = ReplaceCell(this, CellType.Basic, true);
				basic.Nationality = Nationality;
				cellGrid[Column, Row] = basic;
			}
			return;
		}

		// Prefer a living dest over an empty one if the living slot is in the same direction.
		// (The computed dest IS the direction slot — just move there.)
		_blockedStreak = 0;
		moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, tc, tr, (int)CellType, Nationality));
		SwapCells(this, dest, cellGrid);
	}
}
