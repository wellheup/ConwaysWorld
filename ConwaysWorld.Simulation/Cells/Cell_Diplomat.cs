namespace ConwaysWorld.Simulation;

/// <summary>
/// A Diplomat is elected by its nation to travel toward foreign nations and convert their cells.
/// <para>
/// Behaviour each step (runs in <see cref="SpecialActions"/>):
/// <list type="number">
///   <item><c>Convert</c>: for each immediately adjacent foreign living cell, 25 % chance of
///         changing that cell's <see cref="Cell.Nationality"/> to match the Diplomat's.
///         A successful conversion resets the idle counter.</item>
///   <item><c>MoveTowardForeignNation</c>: seeks the nearest foreign living cell within range 8
///         and steps one slot toward it each turn.  If no target exists the idle counter increments.</item>
/// </list>
/// </para>
/// <para>
/// Diplomats are never spawned at grid initialisation.  They are elected by <see cref="Cell_Nation.ElectDiplomat"/>
/// when a nation reaches at least 10 citizens and has fewer Diplomats than 5 % of its population.
/// </para>
/// </summary>
public class Cell_Diplomat : Cell
{
	/// <summary>The foreign cell currently being pursued.  Refreshed when it becomes invalid.</summary>
	private Cell? _target;

	/// <summary>Consecutive steps without a successful conversion.  Currently tracked but not yet used for demotion.</summary>
	private int _idleTurns = 0;

	/// <summary>Prevents <see cref="SpecialActions"/> from running twice in one step when the Diplomat moves to a later grid position.</summary>
	private bool _specialPerformed = false;

	/// <summary>Creates a Diplomat cell at the given position.</summary>
	public Cell_Diplomat(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Diplomat;
		Conditions = new HashSet<string>();
		_target = null;
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

	/// <summary>
	/// Returns <c>true</c> if <paramref name="c"/> is alive, belongs to a different nation,
	/// and has a valid nation index.
	/// </summary>
	private bool IsForeignAlive(Cell c) =>
									c.IsAlive && c.Nationality != Nationality && c.Nationality >= 0;

	/// <summary>
	/// For each immediately adjacent foreign cell, rolls a 1-in-4 chance of converting
	/// it to the Diplomat's nation.
	/// </summary>
	private void Convert(Cell[,] cellGrid)
	{
		foreach (var neighbor in CellNeighborhood.NeighborsDict.Values)
		{
			var target = cellGrid[neighbor.Column, neighbor.Row];
			if (target.IsAlive && target.Nationality != Nationality && target.Nationality >= 0)
			{
				if (SimRandom.Range(0, 4) == 0)
				{
					target.Nationality = Nationality;
					_idleTurns = 0;
				}
			}
		}
	}

	/// <summary>
	/// Refreshes the target if it has died or switched nations, then steps one cell
	/// toward the target.  Increments <see cref="_idleTurns"/> when no target can be found.
	/// </summary>
	private void MoveTowardForeignNation(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (_target == null || !_target.IsAlive || _target.Nationality == Nationality)
			_target = SelectNearbyCellByRule(cellGrid, IsForeignAlive, 8);

		if (_target != null)
		{
			var step = FindNeighborInDirOfCell(cellGrid, _target);
			if (step != this)
			{
				moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, step.Column, step.Row, (int)CellType, Nationality));
				SwapCells(this, step, cellGrid);
			}
		}
		else
		{
			_idleTurns++;
		}
	}

	/// <summary>Converts adjacent foreign cells, then moves toward the nearest foreign nation.</summary>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;
		Convert(cellGrid);
		MoveTowardForeignNation(cellGrid, moves);
	}
}
