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
///   <item>If the next step is the prey cell, swaps and kills it; prey is marked <c>"cleanup"</c>.</item>
///   <item>If no prey is visible, moves randomly like a Traveler.</item>
/// </list>
/// </para>
/// <para>
/// Idle demotion: if the Hunter makes no kill for 3 consecutive steps it demotes itself
/// to a Basic cell (handled by <see cref="Model.UpdateCellConditions"/>).
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
		PreyTypes = new List<CellType> { CellType.Immortal, CellType.King };
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

	/// <summary>Dies only from isolation; overcrowding does not kill a Hunter.</summary>
	public override bool CalcCellAliveNextGen()
	{
		if (DeathCountDown > MaxAloneTime) return false;
		return true;
	}

	/// <summary>Returns <c>true</c> if <paramref name="c"/> is an alive prey type.</summary>
	private bool IsPrey(Cell c) => PreyTypes.Contains(c.CellType) && c.IsAlive;

	/// <summary>
	/// Locates the nearest prey, steps one cell toward it, and kills it if adjacent.
	/// If no prey is found, moves randomly.
	/// </summary>
	/// <returns><c>true</c> if a prey cell was killed this step.</returns>
	protected bool Hunt(Cell[,] cellGrid)
	{
		if (CurrentPrey == null || !CurrentPrey.IsAlive)
			CurrentPrey = SelectNearbyCellByRule(cellGrid, IsPrey, 5);

		Cell cellToSwap;
		if (CurrentPrey != null)
			cellToSwap = FindNeighborInDirOfCell(cellGrid, CurrentPrey);
		else
			cellToSwap = CellNeighborhood.NeighborhoodDict[ChooseTravelDirection()];

		if (CurrentPrey != null && cellToSwap == CurrentPrey && cellToSwap.IsAlive)
		{
			SwapCells(this, cellToSwap, cellGrid);
			cellToSwap.Die();
			CurrentPrey = null;
			return true;
		}
		else if (IsPrey(cellToSwap))
		{
			SwapCells(this, cellToSwap, cellGrid);
			cellToSwap.Die();
			return true;
		}
		else
		{
			SwapCells(this, cellToSwap, cellGrid);
			return false;
		}
	}

	/// <summary>
	/// Runs <see cref="Hunt"/> and increments or resets <see cref="Cell.IdleTurns"/> accordingly.
	/// The Model demotes the cell to Basic when <c>IdleTurns ≥ 3</c>.
	/// </summary>
	public override void SpecialActions(Cell[,] cellGrid)
	{
		if (!IsAlive) return;
		bool didKill = Hunt(cellGrid);
		if (didKill)
			IdleTurns = 0;
		else
			IdleTurns++;
	}
}
