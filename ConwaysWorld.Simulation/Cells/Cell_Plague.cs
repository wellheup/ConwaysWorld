namespace ConwaysWorld.Simulation;

/// <summary>
/// A more virulent variant of <see cref="Cell_Diseased"/> carrying a <c>p_</c> strain.
/// Transmission rate is 40 % higher than the base Diseased rate (10 × 1.4 = 14 per 100 rolls).
/// <para>
/// Plague behaves identically to Diseased in all other respects:
/// 3-step countdown to death, spread via <see cref="Cell_Diseased.SpreadDisease"/>,
/// blocked by <c>immune</c>, Immortal, and <c>vax_</c> conditions.
/// </para>
/// </summary>
public class Cell_Plague : Cell_Diseased
{
	/// <summary>
	/// Creates a Plague cell and sets its transmission rate to 14 % (10 × 1.4, rounded).
	/// Generates a unique <c>p_</c> strain tag distinct from <c>d_</c> Diseased strains.
	/// </summary>
	public Cell_Plague(int column, int row, bool isAlive)
					: base(column, row, isAlive)
	{
		TransmissionRate = (int)Math.Round(10 * 1.4);
		CellType = CellType.Plague;
		StrainId = RandomCondition('p');
	}

	/// <inheritdoc/>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (Age > MatureAge)
			Conditions.Add("mature");
		CellType = CellType.Plague;
		ChooseNation();
	}

	/// <summary>Removes the plague strain and resets nationality on death.</summary>
	public override void Die()
	{
		IsAlive = false;
		Conditions.Remove(StrainId);
		Nationality = -1;
	}

	/// <inheritdoc/>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null) => SpreadDisease(cellGrid);
}
