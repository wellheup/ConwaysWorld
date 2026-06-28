namespace ConwaysWorld.Simulation;

/// <summary>
/// A Voyager seeks out a foreign nation that shares no border with its own nation, travels
/// to it across open space (ignoring normal Conway survival requirements), and upon arrival
/// resolves a 50/50 outcome:
/// <list type="bullet">
///   <item>Peaceful landing: becomes an <see cref="Cell_Explorer"/> and plants 2 Diplomats + 2 Warriors
///         in the nearest vacant slots, all bearing its own nation.</item>
///   <item>Hostile landing: becomes a <see cref="Cell_Plague"/> cell and seeds 4 additional Plague cells
///         in the nearest vacant slots.</item>
/// </list>
/// <para>
/// Extends <see cref="Cell_NationTraveler"/> which provides target selection, toroidal
/// 2-cell movement, and the full <see cref="Cell.SpecialActions"/> state machine.
/// The only thing unique to Voyager is the <see cref="Arrive"/> effect.
/// </para>
/// </summary>
public class Cell_Voyager : Cell_NationTraveler
{
	/// <summary>Creates a Voyager cell at the given position.</summary>
	public Cell_Voyager(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Voyager;
		Conditions = new HashSet<string>();
	}

	/// <summary>
	/// Resolves the arrival: 50 % chance peaceful (Explorer + 2 Diplomats + 2 Warriors),
	/// 50 % chance hostile (Plague self + 4 Plague seeds).
	/// </summary>
	protected override void Arrive(Cell[,] cellGrid)
	{
		int myNation = Nationality;
		int col = Column, row = Row;
		var vacant = NearestVacant(cellGrid, col, row, 4);

		if (SimRandom.Range(0, 2) == 0)
		{
			// Peaceful: become Explorer, plant 2 Diplomats + 2 Warriors.
			var explorer = ReplaceCell(this, CellType.Explorer, true);
			explorer.Nationality = myNation;
			cellGrid[col, row] = explorer;

			for (int i = 0; i < Math.Min(2, vacant.Count); i++)
			{
				var slot = vacant[i];
				var dip = ReplaceCell(slot, CellType.Diplomat, true);
				dip.Nationality = myNation;
				cellGrid[slot.Column, slot.Row] = dip;
			}
			for (int i = 2; i < Math.Min(4, vacant.Count); i++)
			{
				var slot = vacant[i];
				var war = ReplaceCell(slot, CellType.Warrior, true);
				war.Nationality = myNation;
				cellGrid[slot.Column, slot.Row] = war;
			}
		}
		else
		{
			// Hostile: become Plague, seed 4 additional Plague cells.
			var plague = ReplaceCell(this, CellType.Plague, true);
			plague.Nationality = myNation;
			cellGrid[col, row] = plague;

			for (int i = 0; i < Math.Min(4, vacant.Count); i++)
			{
				var slot = vacant[i];
				var p = ReplaceCell(slot, CellType.Plague, true);
				p.Nationality = myNation;
				cellGrid[slot.Column, slot.Row] = p;
			}
		}
	}
}
