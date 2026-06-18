namespace ConwaysWorld.Simulation;

/// <summary>
/// Represents a nation — a group of living cells sharing the same <see cref="Cell.Nationality"/> index.
/// The Model maintains one <see cref="Cell_Nation"/> per active nation in <see cref="Model.Nations"/>.
/// <para>
/// Each step <see cref="Census"/> rebuilds the citizen list, then:
/// <list type="bullet">
///   <item>Elects a new <see cref="CellType.Diplomat"/> if the nation is large enough and under-represented.</item>
///   <item>Crowns a new <see cref="CellType.King"/> if the nation has no current King.</item>
/// </list>
/// </para>
/// </summary>
public class Cell_Nation
{
	/// <summary>The index of this nation; used as the value stored in <see cref="Cell.Nationality"/>.</summary>
	public int NationNum;

	/// <summary>Reference to the current living King cell, or <c>null</c> if the nation has no King.</summary>
	public Cell? King = null;

	/// <summary>All living cells belonging to this nation as of the last <see cref="Census"/> call.</summary>
	public List<Cell> CitizensList = new();

	/// <summary>All living Diplomat cells belonging to this nation as of the last <see cref="Census"/> call.</summary>
	public List<Cell> DiplomatsList = new();

	/// <summary>
	/// The 20 fixed hex colour strings assigned to nations by index.
	/// Nation 0 uses index 0, nation 1 uses index 1, etc.
	/// The count here determines the maximum number of concurrent nations.
	/// </summary>
	public static readonly List<string> NationColors = new()
	{
		"#bc00ff",
		"#471415",
		"#a3181c",
		"#00f542",
		"#617f1c",
		"#f50005",
		"#473d14",
		"#17a33d",
		"#a38517",
		"#299bae",
		"#00dbff",
		"#f5bf00",
		"#232b75",
		"#ff7000",
		"#0019f5",
		"#adff00",
		"#7f4719",
		"#671f80",
		"#1c2bb8",
		"#1c4724",
	};

	/// <summary>Creates a new nation with the given index.</summary>
	public Cell_Nation(int nationNum)
	{
		NationNum = nationNum;
		CitizensList = new List<Cell>();
		DiplomatsList = new List<Cell>();
	}

	/// <summary>
	/// Scans the entire grid to rebuild <see cref="CitizensList"/> and <see cref="DiplomatsList"/>,
	/// then calls <see cref="ElectDiplomat"/> and <see cref="CrownKing"/>.
	/// Called once per step by <see cref="Model.UpdateNations"/>.
	/// </summary>
	public void Census(Cell[,] cellGrid)
	{
		var tempCits = new List<Cell>();
		var tempDips = new List<Cell>();

		for (int x = 0; x < cellGrid.GetLength(0); x++)
		{
			for (int y = 0; y < cellGrid.GetLength(1); y++)
			{
				var cell = cellGrid[x, y];
				if (cell.Nationality == NationNum && cell.IsAlive)
				{
					tempCits.Add(cell);
					if (cell.CellType == CellType.Diplomat)
						tempDips.Add(cell);
				}
			}
		}

		CitizensList = tempCits;
		DiplomatsList = tempDips;

		ElectDiplomat(cellGrid);
		CrownKing(cellGrid);
	}

	/// <summary>
	/// Promotes a random citizen to <see cref="CellType.Diplomat"/> if:
	/// <list type="bullet">
	///   <item>The nation has at least 10 citizens, AND</item>
	///   <item>Diplomats are fewer than 5 % of the citizen count.</item>
	/// </list>
	/// Up to 5 random candidates are tried before giving up; the King and existing Diplomats are excluded.
	/// </summary>
	private void ElectDiplomat(Cell[,] cellGrid)
	{
		if (DiplomatsList.Count >= 0.05f * CitizensList.Count || CitizensList.Count < 10)
			return;

		Cell? elect = null;
		for (int attempt = 0; attempt < 5; attempt++)
		{
			elect = CitizensList[SimRandom.Range(0, CitizensList.Count)];
			if (elect != King && !DiplomatsList.Contains(elect))
				break;
			elect = null;
		}

		if (elect != null && elect != King && elect.IsAlive && !DiplomatsList.Contains(elect))
		{
			var diplomat = Cell.ReplaceCell(elect, CellType.Diplomat, true);
			cellGrid[diplomat.Column, diplomat.Row] = diplomat;
			CitizensList.Add(diplomat);
			DiplomatsList.Add(diplomat);
		}
	}

	/// <summary>
	/// Crowns a random citizen as King if:
	/// <list type="bullet">
	///   <item>The current King is dead (clears the reference and marks it <c>"cleanup"</c>), AND</item>
	///   <item>The nation has at least 5 citizens with more citizens than Diplomats.</item>
	/// </list>
	/// Up to 5 random candidates are tried.  The crowned cell is replaced in the grid with a
	/// <see cref="Cell_King"/> instance sharing the same nationality.
	/// </summary>
	private void CrownKing(Cell[,] cellGrid)
	{
		if (King != null && !King.IsAlive)
		{
			King.Conditions.Add("cleanup");
			King = null;
		}

		if (King != null)
			return;
		if (CitizensList.Count < 5 || CitizensList.Count <= DiplomatsList.Count)
			return;

		Cell? newKing = null;
		for (int attempt = 0; attempt < 5; attempt++)
		{
			newKing = CitizensList[SimRandom.Range(0, CitizensList.Count)];
			if (newKing.IsAlive)
				break;
			newKing = null;
		}

		if (newKing != null && newKing.IsAlive)
		{
			King = Cell.ReplaceCell(newKing, CellType.King, true);
			cellGrid[King.Column, King.Row] = King;
			CitizensList.Add(King);
			DiplomatsList.Remove(King);
		}
	}
}
