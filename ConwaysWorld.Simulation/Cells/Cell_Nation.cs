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
	/// How many consecutive steps this nation's population has increased.
	/// Reset to 0 whenever population stays flat or shrinks.
	/// </summary>
	public int ConsecutiveGrowthSteps = 0;

	/// <summary>
	/// Column of the King that most recently aged out, used to prefer nearby succession candidates.
	/// Set to -1 when no aged-out king is pending.
	/// </summary>
	public int AgedOutKingColumn = -1;

	/// <summary>
	/// Row of the King that most recently aged out, used to prefer nearby succession candidates.
	/// Set to -1 when no aged-out king is pending.
	/// </summary>
	public int AgedOutKingRow = -1;

	/// <summary>
	/// Snapshot of <see cref="King"/> taken just before <see cref="Census"/> runs.
	/// Used by <see cref="Model.UpdateNations"/> to detect king changes without a temp dict.
	/// </summary>
	public Cell? PreCensusKing = null;

	/// <summary>
	/// Snapshot of <see cref="CitizensList"/>.Count taken just before <see cref="Census"/> runs.
	/// Used by <see cref="Model.UpdateNations"/> to detect nation extinction without a temp dict.
	/// </summary>
	public int PreCensusCount = 0;

	/// <summary>
	/// Number of consecutive steps this nation has had at least one citizen but no King.
	/// Incremented each step after Census when <see cref="King"/> is null and
	/// <see cref="CitizensList"/> is non-empty; reset to 0 when a King is crowned.
	/// Used to give newly-formed nations (e.g. Revolutionary-founded) a grace window
	/// to recruit cells and reach the crowning threshold before being dissolved.
	/// </summary>
	public int StepsKingless = 0;

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
	/// tracks consecutive population growth, then calls <see cref="ElectDiplomat"/> and <see cref="CrownKing"/>.
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

		// Track consecutive population growth before overwriting the list.
		int oldCount = CitizensList.Count;
		int newCount = tempCits.Count;
		if (newCount > oldCount && oldCount > 0)
			ConsecutiveGrowthSteps++;
		else
			ConsecutiveGrowthSteps = 0;

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
		// A nation must have a living King before it can send Diplomats.
		if (King == null || !King.IsAlive)
			return;
		// Threshold: at most 1 Diplomat per 25 citizens; minimum 25 citizens required.
		int maxDiplomats = Math.Max(0, CitizensList.Count / 25);
		if (DiplomatsList.Count >= maxDiplomats || CitizensList.Count < 25)
			return;

		Cell? elect = null;
		for (int attempt = 0; attempt < 5; attempt++)
		{
			elect = CitizensList[SimRandom.Range(0, CitizensList.Count)];
			if (elect != King &&
																																			!DiplomatsList.Contains(elect) &&
																																			elect.CellType != CellType.Warrior &&
																																			elect.CellType != CellType.Rebel &&
																																			elect.CellType != CellType.Revolutionary)
				break;
			elect = null;
		}

		if (elect != null && elect != King && elect.IsAlive && !DiplomatsList.Contains(elect) &&
																																		elect.CellType != CellType.Warrior &&
																																		elect.CellType != CellType.Rebel &&
																																		elect.CellType != CellType.Revolutionary)
		{
			var diplomat = Cell.ReplaceCell(elect, CellType.Diplomat, true);
			cellGrid[diplomat.Column, diplomat.Row] = diplomat;
			int electIdx = CitizensList.IndexOf(elect);
			if (electIdx >= 0)
				CitizensList[electIdx] = diplomat;
			else
				CitizensList.Add(diplomat);
			DiplomatsList.Add(diplomat);
		}
	}

	/// <summary>
	/// Crowns a citizen as King if the nation has no current King and meets the minimum size.
	/// When the previous King aged out (<see cref="AgedOutKingColumn"/> &gt;= 0), prefers
	/// candidates within 5 tiles of the old King's position for a natural succession feel.
	/// Falls back to a random citizen if no nearby candidates qualify.
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

		// If the previous king aged out, prefer nearby cells for succession.
		if (AgedOutKingColumn >= 0)
		{
			var nearby = CitizensList
																																			.Where(c => c.IsAlive &&
																																																																																																																																			c.CellType != CellType.King &&
																																																																																																																																			c.CellType != CellType.Revolutionary &&
																																																																																																																																			c.CellType != CellType.Diplomat &&
																																																																																																																																			Math.Abs(c.Column - AgedOutKingColumn) <= 5 &&
																																																																																																																																			Math.Abs(c.Row - AgedOutKingRow) <= 5)
																																			.ToList();

			if (nearby.Count > 0)
				newKing = nearby[SimRandom.Range(0, nearby.Count)];

			AgedOutKingColumn = -1;
			AgedOutKingRow = -1;
		}

		// Fall back to random citizen selection.
		if (newKing == null)
		{
			for (int attempt = 0; attempt < 5; attempt++)
			{
				newKing = CitizensList[SimRandom.Range(0, CitizensList.Count)];
				if (newKing.IsAlive)
					break;
				newKing = null;
			}
		}

		if (newKing != null && newKing.IsAlive)
		{
			King = Cell.ReplaceCell(newKing, CellType.King, true);
			cellGrid[King.Column, King.Row] = King;
			int kingIdx = CitizensList.IndexOf(newKing);
			if (kingIdx >= 0)
				CitizensList[kingIdx] = King;
			else
				CitizensList.Add(King);
			DiplomatsList.Remove(newKing);
		}
	}
}
