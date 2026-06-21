namespace ConwaysWorld.Simulation;

/// <summary>
/// A Hunter actively pursues and kills specific prey cell types.
/// <para>
/// Default prey: <see cref="CellType.Immortal"/> and <see cref="CellType.King"/>.
/// <see cref="Cell_Warrior"/> overrides the prey list to target Diseased and Plague enemies instead.
/// </para>
/// <para>
/// Movement and hunting logic (runs in <see cref="SpecialActions"/> each step):
/// <list type="number">
///   <item>Locates a prey target within range 5 using <see cref="Cell.SelectNearbyCellByRule"/>.</item>
///   <item>Steps one cell toward it via <see cref="Cell.FindNeighborInDirOfCell"/>.</item>
///   <item>If the next step is the prey cell, swaps and kills it.</item>
///   <item>If no prey is visible, moves randomly like a Traveler.</item>
/// </list>
/// </para>
/// <para>
/// Age economy: each successful kill reduces <see cref="Cell.Age"/> by 1, rewarding active hunters.
/// The Hunter dies naturally once its age reaches 12.
/// </para>
/// <para>
/// Idle demotion: <see cref="Cell.IdleTurns"/> only increments when the Hunter finds <b>no target at all</b>
/// in a given step. Moving toward a known target or making a kill both reset the counter.
/// After 8 consecutive targetless steps the Model demotes it to Basic.
/// </para>
/// </summary>
public class Cell_Hunter : Cell_Traveler
{
	/// <summary>The cell types this Hunter will pursue.  Overridden by <see cref="Cell_Warrior"/>.</summary>
	protected List<CellType> PreyTypes;

	/// <summary>The prey cell currently being tracked.  Cleared when prey is killed or becomes invalid.</summary>
	protected Cell? CurrentPrey;

	/// <summary>Creates a Hunter targeting Immortals and Kings.</summary>
	public Cell_Hunter(int column, int row, bool isAlive)
																	: base(column, row, isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Hunter;
		Conditions = new HashSet<string>();
		PreyTypes = new List<CellType> { CellType.Immortal, CellType.King, CellType.Rebel, CellType.Revolutionary, CellType.Spy };
		CurrentPrey = null;
	}

	/// <summary>Updates isolation counter and nation assignment without crush-death logic.</summary>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (CellNeighborhood.NumNeighbors == 0)
			DeathCountDown++;
		else
			DeathCountDown = 0;
		ChooseNation();
	}

	/// <summary>
	/// Dies from isolation, or once age reaches 12 (natural lifespan).
	/// Overcrowding does not kill a Hunter.
	/// </summary>
	public override bool CalcCellAliveNextGen()
	{
		if (DeathCountDown > MaxAloneTime)
			return false;
		if (Age >= 12)
			return false;
		return true;
	}

	/// <summary>Returns <c>true</c> if <paramref name="c"/> is an alive prey type.</summary>
	private bool IsPrey(Cell c) => PreyTypes.Contains(c.CellType) && c.IsAlive;

	/// <summary>
	/// Locates the nearest prey, steps one cell toward it, and kills it if adjacent.
	/// If no prey is found, moves randomly.
	/// </summary>
	/// <param name="cellGrid">The live simulation grid.</param>
	/// <param name="foundTarget">
	/// Set to <c>true</c> if a prey target was located this step (whether or not a kill occurred).
	/// Set to <c>false</c> if no prey exists within range — the only case that counts as idle.
	/// </param>
	/// <returns><c>true</c> if a prey cell was killed this step.</returns>
	protected bool Hunt(Cell[,] cellGrid, out bool foundTarget, List<MoveRecord>? moves = null)
	{
		if (CurrentPrey == null || !CurrentPrey.IsAlive)
			CurrentPrey = SelectNearbyCellByRule(cellGrid, IsPrey, 5);

		foundTarget = CurrentPrey != null;

		Cell cellToSwap;
		if (CurrentPrey != null)
			cellToSwap = FindNeighborInDirOfCell(cellGrid, CurrentPrey);
		else
			cellToSwap = CellNeighborhood.NeighborhoodDict[ChooseTravelDirection()];

		if (CurrentPrey != null && cellToSwap == CurrentPrey && cellToSwap.IsAlive)
		{
			if (cellToSwap != this)
				moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, cellToSwap.Column, cellToSwap.Row, (int)CellType, Nationality));
			SwapCells(this, cellToSwap, cellGrid);
			cellToSwap.Die();
			CurrentPrey = null;
			return true;
		}
		else if (IsPrey(cellToSwap))
		{
			if (cellToSwap != this)
				moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, cellToSwap.Column, cellToSwap.Row, (int)CellType, Nationality));
			SwapCells(this, cellToSwap, cellGrid);
			cellToSwap.Die();
			return true;
		}
		else
		{
			if (cellToSwap != this)
				moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, cellToSwap.Column, cellToSwap.Row, (int)CellType, Nationality));
			SwapCells(this, cellToSwap, cellGrid);
			return false;
		}
	}

	/// <summary>
	/// Runs <see cref="Hunt"/> and updates <see cref="Cell.IdleTurns"/> and <see cref="Cell.Age"/>:
	/// <list type="bullet">
	///   <item>Kill → reset idle counter; age reduced by 1 (min 0).</item>
	///   <item>Target found but not yet killed → reset idle counter; no age change.</item>
	///   <item>No target found → increment idle counter.</item>
	/// </list>
	/// The Model demotes the Hunter to Basic when <c>IdleTurns ≥ 8</c>.
	/// </summary>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive)
			return;
		bool didKill = Hunt(cellGrid, out bool foundTarget, moves);
		if (didKill)
		{
			IdleTurns = 0;
			Age = Math.Max(0, Age - 1);
		}
		else if (foundTarget)
		{
			IdleTurns = 0;
		}
		else
		{
			IdleTurns++;
		}
	}
}
