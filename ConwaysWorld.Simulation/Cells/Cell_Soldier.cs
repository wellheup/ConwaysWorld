namespace ConwaysWorld.Simulation;

/// <summary>
/// A Soldier is a combat cell created by <see cref="Cell_Spy"/> and <see cref="Cell_Conquistador"/>.
/// Each step it:
/// <list type="number">
///   <item>Kills any adjacent living enemy cell (Nationality ≠ own) immediately.</item>
///   <item>If no adjacent enemy exists, moves one step toward the nearest enemy within range 5.</item>
/// </list>
/// Between special actions Soldiers obey standard Conway survival rules.
/// When the last Soldier from a given (attacker, defender) nation pair dies the <see cref="Model"/>
/// checks whether the defender should be absorbed into the attacker.
/// </summary>
public class Cell_Soldier : Cell
{
	/// <summary>
	/// The nation this Soldier was dispatched to fight.
	/// Read by <see cref="Model"/> to track attack waves.
	/// A value of -1 means the target nation is unspecified (Soldier will attack any enemy).
	/// </summary>
	public int TargetNation { get; set; } = -1;

	private bool _specialPerformed = false;

	/// <summary>Creates a Soldier cell at the given position.</summary>
	public Cell_Soldier(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Soldier;
		Conditions = new HashSet<string>();
	}

	/// <summary>Standard Live — Soldiers inherit nations normally and track maturity.</summary>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (Age > MatureAge)
			Conditions.Add("mature");
		ChooseNation();
		_specialPerformed = false;
	}

	/// <inheritdoc/>
	public override void Die()
	{
		base.Die();
		_specialPerformed = true;
	}

	// Standard Conway survival rules (CalcCellAliveNextGen inherited from Cell_Basic logic via base).

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;

		// Step 1 — kill an adjacent enemy.
		Cell? adjacentEnemy = null;
		foreach (var nb in CellNeighborhood.NeighborhoodDict.Values)
		{
			if (nb != this && nb.IsAlive && nb.Nationality >= 0 && nb.Nationality != Nationality)
			{
				adjacentEnemy = nb;
				break;
			}
		}

		if (adjacentEnemy != null)
		{
			adjacentEnemy.Die();
			adjacentEnemy.Conditions.Add("cleanup");
			return;
		}

		// Step 2 — advance toward the nearest enemy within range 5.
		var target = SelectNearbyCellByRule(
				cellGrid,
				c => c.IsAlive && c.Nationality >= 0 && c.Nationality != Nationality,
				6);

		if (target == null)
			return;

		var next = FindNeighborInDirOfCell(cellGrid, target);
		if (next == this)
			return;

		moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, next.Column, next.Row, (int)CellType, Nationality));
		SwapCells(this, next, cellGrid);
	}
}
