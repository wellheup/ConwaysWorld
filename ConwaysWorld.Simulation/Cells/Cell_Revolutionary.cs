namespace ConwaysWorld.Simulation;

/// <summary>
/// A Revolutionary defects from an over-dominant nation, founds a rival nation, then
/// continuously recruits cells from its former homeland as Warriors and Rebels.
/// <para>
/// Promotion trigger: when any nation holds at least twice as many citizens as the
/// second-largest nation, <see cref="Model.CheckRevolution"/> promotes a random non-King
/// citizen of the dominant nation to Revolutionary.
/// </para>
/// <para>
/// Behaviour:
/// <list type="number">
///   <item>Starts a brand-new nation (if below the cap) and becomes its first citizen,
///         or defects to the second-largest nation as a <see cref="Cell_Warrior"/> instead.</item>
///   <item>On its first active step: recruits the 3 nearest old-nation cells —
///         the closest becomes a Warrior, the next two become Rebels.</item>
///   <item>Each subsequent step: recruits 1 Warrior and 1 Rebel from the old nation.</item>
///   <item>Also marks adjacent same-nation Basic cells with <c>"toWar"</c> each step,
///         mirroring <see cref="Cell_King"/> army-building behaviour.</item>
///   <item>Is promoted to King automatically once its nation hits the crowning threshold.</item>
/// </list>
/// </para>
/// <para>
/// Revolutionaries are hunted by <see cref="Cell_Warrior"/> and <see cref="Cell_Hunter"/>.
/// </para>
/// </summary>
public class Cell_Revolutionary : Cell_Converter
{
	/// <summary>The nation this Revolutionary defected from; used to identify recruit targets.</summary>
	public int OldNationality { get; set; } = -1;

	// Live() and Die() inherited from Cell_Converter.

	/// <summary>Creates a Revolutionary cell at the given position.</summary>
	public Cell_Revolutionary(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Revolutionary;
		Conditions = new HashSet<string>();
	}

	/// <summary>
	/// Marks every adjacent living Basic cell of the same nation with <c>"toWar"</c>,
	/// mirroring <see cref="Cell_King"/> army-building behaviour.
	/// </summary>
	private void MakeArmy()
	{
		foreach (var neighbor in CellNeighborhood.NeighborhoodDict.Values)
		{
			if (neighbor.IsAlive && neighbor != this &&
											neighbor.CellType == CellType.Basic &&
											neighbor.Nationality == Nationality)
				neighbor.Conditions.Add("toWar");
		}
	}

	/// <summary>
	/// Converts the nearest cells from <see cref="OldNationality"/> into Warriors and Rebels,
	/// switching their nationality to the Revolutionary's before promotion.
	/// First step: 1 Warrior + 2 Rebels.  Subsequent steps: 1 Warrior + 1 Rebel.
	/// </summary>
	private void Recruit(Cell[,] cellGrid)
	{
		if (OldNationality < 0)
			return;

		int wantWarriors = 1;
		int wantRebels = 2;

		var candidates = GetAllCellsInRangeByRule(cellGrid,
										c => c.IsAlive &&
																		 c.Nationality == OldNationality &&
																		 c.CellType != CellType.King &&
																		 c.CellType != CellType.Revolutionary,
										8);

		candidates.Sort((a, b) =>
		{
			int da = Math.Abs(a.Column - Column) + Math.Abs(a.Row - Row);
			int db = Math.Abs(b.Column - Column) + Math.Abs(b.Row - Row);
			return da.CompareTo(db);
		});

		int idx = 0;
		while (wantWarriors > 0 && idx < candidates.Count)
		{
			var cell = candidates[idx++];
			cell.Nationality = Nationality;
			cell.Conditions.Add("toWar");
			wantWarriors--;
		}
		while (wantRebels > 0 && idx < candidates.Count)
		{
			var cell = candidates[idx++];
			cell.Nationality = Nationality;
			cell.Conditions.Add("toRebel");
			wantRebels--;
		}

	}

	/// <summary>Builds its army and recruits defectors from the old nation each step.</summary>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;
		MakeArmy();
		Recruit(cellGrid);
	}
}
