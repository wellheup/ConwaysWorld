namespace ConwaysWorld.Simulation;

/// <summary>
/// A Diseased cell carries a unique strain identifier (prefix <c>d_</c>) and dies after
/// a 3-step countdown regardless of Conway rules.  Each step it attempts to spread its
/// strain to each living neighbour with a 10 % per-neighbour transmission rate.
/// <para>
/// Infection mechanism (shared with <see cref="Cell_Plague"/> via <see cref="SpreadDisease"/>):
/// <list type="bullet">
///   <item>The disease strain tag (<c>d_XXXXXXXX</c>) is added to an eligible neighbour's <see cref="Cell.Conditions"/>.</item>
///   <item>On the next <see cref="Model.UpdateCellConditions"/> pass the tag triggers <see cref="Cell_Diseased.Infect"/>, converting the neighbour into a full Diseased cell.</item>
///   <item>Immune cells (<c>immune</c> condition) strip disease tags every step and are never converted.</item>
///   <item>Immortal cells are unconditionally skipped.</item>
///   <item>Vaccinated cells (<c>vax_&lt;strain&gt;</c>) are skipped for that specific strain.</item>
/// </list>
/// </para>
/// </summary>
public class Cell_Diseased : Cell
{
	/// <summary>Steps remaining before this cell dies. Decremented in <see cref="CalcCellAliveNextGen"/>.</summary>
	protected int CountDown = 3;

	/// <summary>Per-neighbour infection probability out of 100 (10 % for Diseased, 14 % for Plague).</summary>
	protected int TransmissionRate = 10;

	/// <summary>Unique strain identifier generated at spawn (e.g. <c>d_38291047</c>).</summary>
	public string Disease;

	/// <summary>
	/// Creates a Diseased cell and generates its unique strain tag.
	/// The tag is added to the cell's own conditions to mark it as the carrier.
	/// </summary>
	public Cell_Diseased(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Diseased;
		Conditions = new HashSet<string>();
		Disease = RandomCondition('d');
	}

	/// <inheritdoc/>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (Age > MatureAge)
			Conditions.Add("mature");
		CellType = CellType.Diseased;
		ChooseNation();
	}

	/// <summary>Removes the strain condition from this cell when it dies.</summary>
	public override void Die()
	{
		IsAlive = false;
		Conditions.Remove(Disease);
		base.Die();
	}

	/// <summary>
	/// Decrements <see cref="CountDown"/> and dies when it reaches zero.
	/// While alive still obeys Conway rules (a Diseased cell in a dead zone dies sooner).
	/// </summary>
	public override bool CalcCellAliveNextGen()
	{
		CountDown--;
		if (CountDown <= 0)
			return false;
		return LiveBasic();
	}

	/// <summary>
	/// Converts <paramref name="cell"/> into the given <paramref name="cellType"/> (Diseased or Plague)
	/// if it is alive, not already that type, not Immortal, and not vaccinated against <paramref name="disease"/>.
	/// The strain tag is added to the new cell's conditions.
	/// </summary>
	/// <returns>The converted cell, or the original cell unchanged if conversion was blocked.</returns>
	public static Cell Infect(Cell cell, string disease, CellType cellType)
	{
		if (!cell.IsAlive)
			return cell;
		if (cell.CellType == cellType)
			return cell;
		if (cell.CellType == CellType.Immortal)
			return cell;
		var vaxKey = "vax_" + disease;
		if (cell.Conditions.Contains(vaxKey))
			return cell;

		var temp = ReplaceCell(cell, cellType, true);
		temp.Conditions.Add(disease);
		return temp;
	}

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null) => SpreadDisease(cellGrid);

	/// <summary>
	/// For each of the 8 neighbouring slots, rolls against <see cref="TransmissionRate"/> and,
	/// on success, appends the strain tag to the neighbour's conditions.
	/// Immune, Immortal, and already-vaccinated neighbours are skipped.
	/// The actual cell-type conversion happens later in <see cref="Model.UpdateCellConditions"/>.
	/// </summary>
	protected void SpreadDisease(Cell[,] cellGrid)
	{
		if (!IsAlive)
			return;

		foreach (var key in Cell_Neighborhood.NeighborHoodKeys)
		{
			if (key == "center")
				continue;
			if (SimRandom.Range(1, 101) > TransmissionRate)
				continue;

			var neighbor = CellNeighborhood.NeighborhoodDict[key];
			int nc = neighbor.Column;
			int nr = neighbor.Row;
			var target = cellGrid[nc, nr];

			if (target.CellType == CellType.Immortal)
				continue;
			if (target.Conditions.Contains("immune"))
				continue;
			var vaxKey = "vax_" + Disease;
			if (target.Conditions.Contains(vaxKey))
				continue;

			cellGrid[nc, nr].Conditions.Add(Disease);
		}
	}
}
