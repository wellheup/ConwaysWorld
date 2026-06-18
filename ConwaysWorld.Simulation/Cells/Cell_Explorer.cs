namespace ConwaysWorld.Simulation;

/// <summary>
/// An Explorer is a <see cref="Cell_Traveler"/> that triggers grid expansion when it
/// reaches a border cell.
/// <para>
/// When an Explorer occupies a slot at the edge of the grid (column 0, column max, row 0, or row max)
/// <see cref="Model.UpdateCellConditions"/> sets a flag that causes <see cref="Model.ResizeCellGrid"/>
/// to run at the end of the step, growing the grid by one dead-Basic border on all four sides —
/// as long as the grid has not yet reached <see cref="SimulationSettings.MaxGridSize"/>.
/// </para>
/// <para>
/// Differences from Traveler:
/// <list type="bullet">
///   <item><see cref="MaxAloneTime"/> is 4 instead of 3 (slightly more tolerant of isolation).</item>
///   <item>Sets the <c>"exploring"</c> condition when its travel direction crosses a grid edge.</item>
/// </list>
/// </para>
/// </summary>
public class Cell_Explorer : Cell_Traveler
{
	/// <summary>Prevents the grid from expanding more than once per Explorer lifetime.</summary>
	private bool _hasExplored = false;

	/// <summary>
	/// Creates an Explorer, inheriting Traveler state and overriding the isolation tolerance.
	/// </summary>
	public Cell_Explorer(int column, int row, bool isAlive)
		: base(column, row, isAlive)
	{
		CellType = CellType.Explorer;
		MaxAloneTime = 4;
		Conditions = new HashSet<string>();
		ChooseNation();
	}

	/// <summary>
	/// Updates counters and sets the <c>"exploring"</c> condition if the cell's travel
	/// direction would cross the grid boundary (detected by a coordinate distance &gt; 1,
	/// which only occurs with toroidal wrap-around at the edges).
	/// </summary>
	public override void Live()
	{
		IsAlive = true;
		Age++;

		if (CellNeighborhood.NumNeighbors == 0)
			DeathCountDown++;
		else
			DeathCountDown = 0;

		if (CellNeighborhood.NumNeighbors == 8)
			CrushCountDown++;
		else
			CrushCountDown = 0;

		SpecialPerformed = false;

		if (!_hasExplored && IsNeighborOverEdge(CellNeighborhood.NeighborhoodDict[Direction]))
			Conditions.Add("exploring");
	}

	/// <summary>
	/// Returns <c>true</c> if <paramref name="neighbor"/> is more than 1 cell away in any axis,
	/// which only happens via toroidal wrapping — i.e. the neighbour is on the opposite edge.
	/// </summary>
	private bool IsNeighborOverEdge(Cell neighbor) =>
		Math.Abs(Column - neighbor.Column) > 1 || Math.Abs(Row - neighbor.Row) > 1;

	/// <summary>
	/// Picks a new travel direction and swaps into that slot.
	/// The <c>"exploring"</c> tag is cleared here (the actual resize is handled by the Model).
	/// </summary>
	public override void SpecialActions(Cell[,] cellGrid)
	{
		if (IsAlive && !SpecialPerformed)
		{
			Conditions.Remove("exploring");
			Direction = ChooseTravelDirection();
			SwapCells(this, CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
			SpecialPerformed = true;
		}
	}
}
