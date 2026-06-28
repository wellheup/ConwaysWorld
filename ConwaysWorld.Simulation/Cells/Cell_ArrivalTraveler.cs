namespace ConwaysWorld.Simulation;

/// <summary>
/// Abstract base for cells that travel to a destination and trigger an arrival effect
/// when they reach it, bypassing standard Conway survival rules during transit.
/// <para>
/// Direct subclasses: <see cref="Cell_NationTraveler"/> (nation-seeking travel used by
/// <see cref="Cell_Voyager"/> and <see cref="Cell_Conquistador"/>),
/// <see cref="Cell_Wayfinder"/> (coordinate-based travel to the emptiest region).
/// </para>
/// <para>
/// Provides the <see cref="_specialPerformed"/> guard, standard <see cref="Live"/> /
/// <see cref="Die"/> overrides, transit survival (<see cref="CalcCellAliveNextGen"/>
/// returns <c>true</c> while alive), and the shared <see cref="NearestVacant"/> utility.
/// </para>
/// </summary>
public abstract class Cell_ArrivalTraveler : Cell
{
	/// <summary>
	/// Prevents <see cref="Cell.SpecialActions"/> from running twice in one generation
	/// when the grid iterator reaches this cell's new position after it moves.
	/// Reset to <c>false</c> in <see cref="Live"/>; set to <c>true</c> in <see cref="Die"/>
	/// and at the start of <see cref="Cell.SpecialActions"/>.
	/// </summary>
	protected bool _specialPerformed = false;

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
	/// Arrival travelers bypass Conway survival rules during transit — they always
	/// survive until they trigger their <see cref="Cell.SpecialActions"/> arrival effect.
	/// </summary>
	public override bool CalcCellAliveNextGen() => IsAlive;

	/// <summary>
	/// Collects up to <paramref name="needed"/> vacant (dead) cells sorted outward
	/// from (<paramref name="col"/>, <paramref name="row"/>) by expanding Chebyshev ring.
	/// Used by subclass arrival effects to find spawn slots near the landing site.
	/// </summary>
	/// <param name="cellGrid">The live simulation grid.</param>
	/// <param name="col">Centre column of the search.</param>
	/// <param name="row">Centre row of the search.</param>
	/// <param name="needed">Maximum number of vacant cells to collect.</param>
	/// <returns>Up to <paramref name="needed"/> vacant cells ordered nearest-first.</returns>
	protected static List<Cell> NearestVacant(Cell[,] cellGrid, int col, int row, int needed)
	{
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);
		var vacant = new List<Cell>();
		for (int range = 1; vacant.Count < needed && range < Math.Max(cols, rows); range++)
			for (int co = -range; co <= range; co++)
				for (int ro = -range; ro <= range; ro++)
				{
					if (Math.Abs(co) != range && Math.Abs(ro) != range)
						continue;
					int nc = (col + co + cols) % cols;
					int nr = (row + ro + rows) % rows;
					var c = cellGrid[nc, nr];
					if (!c.IsAlive && !vacant.Contains(c))
						vacant.Add(c);
				}
		return vacant;
	}
}
