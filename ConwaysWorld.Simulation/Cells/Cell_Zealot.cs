namespace ConwaysWorld.Simulation;

/// <summary>
/// A Zealot is created when a <see cref="Cell_Savior"/> dies and converts its
/// <see cref="Cell_Follower"/>s.  It attacks any adjacent living cell regardless of nation —
/// even cells of its own nationality.  Otherwise it behaves like a <see cref="Cell_Soldier"/>:
/// kills adjacent targets first, then advances toward the nearest living cell in range.
/// Standard Conway survival rules apply between steps.
/// </summary>
public class Cell_Zealot : Cell
{
	private bool _specialPerformed = false;

	public Cell_Zealot(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Zealot;
		Conditions = new HashSet<string>();
	}

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

		// Kill any adjacent living cell.
		foreach (var nb in CellNeighborhood.NeighborhoodDict.Values)
		{
			if (nb != this && nb.IsAlive)
			{
				nb.Die();
				nb.Conditions.Add("cleanup");
				return;
			}
		}

		// No adjacent target — move toward the nearest living cell within range 5.
		var target = SelectNearbyCellByRule(cellGrid, c => c != this && c.IsAlive, 6);
		if (target == null)
			return;

		var next = FindNeighborInDirOfCell(cellGrid, target);
		if (next == this)
			return;

		moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, next.Column, next.Row, (int)CellType, Nationality));
		SwapCells(this, next, cellGrid);
	}
}
