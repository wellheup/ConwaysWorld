namespace ConwaysWorld.Simulation;

/// <summary>
/// A Mutant cell marks each of its 8 immediate neighbours with a <c>mutate_</c> condition
/// every step.  On the next <see cref="Model.UpdateCellConditions"/> pass those marks cause
/// the target cell to be replaced with a randomly chosen living <see cref="CellType"/>.
/// Mutation supersedes all other scheduled changes (disease, promotion, demotion, etc.)
/// for that cell during the same step.
/// <para>
/// The Mutant itself obeys a 5-step countdown to death like <see cref="Cell_Diseased"/>,
/// spreading mutations until it expires.  It inherits nationality normally.
/// </para>
/// </summary>
public class Cell_Mutant : Cell
{
	/// <summary>All living cell types the mutant can pick from, excluding Dead, Irradiated, Zombie.</summary>
	private static readonly CellType[] MutantTargetTypes =
	{
		CellType.Basic, CellType.Immortal, CellType.Diseased, CellType.Plague,
		CellType.Traveler, CellType.Explorer, CellType.Doctor,
		CellType.Warrior, CellType.Hunter, CellType.Bomber,
		CellType.Diplomat, CellType.King, CellType.Rebel, CellType.Revolutionary,
		CellType.Voyager, CellType.Wayfinder, CellType.Islander, CellType.Barbarian,
		CellType.Spy, CellType.Soldier, CellType.Conquistador,
		CellType.Savior, CellType.Follower, CellType.Zealot,
		CellType.PlagueRat, CellType.Necromancer, CellType.Mutant,
	};

	private int _countDown = 5;

	/// <summary>Creates a Mutant cell at the given position.</summary>
	public Cell_Mutant(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Mutant;
		Conditions = new HashSet<string>();
	}

	/// <inheritdoc/>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (Age > MatureAge)
			Conditions.Add("mature");
		CellType = CellType.Mutant;
		ChooseNation();
	}

	/// <inheritdoc/>
	public override bool CalcCellAliveNextGen()
	{
		_countDown--;
		if (_countDown <= 0)
			return false;
		return LiveBasic();
	}

	/// <summary>
	/// Stamps a <c>mutate_</c> condition on every live neighbour each step.
	/// The actual replacement happens in <see cref="Model.UpdateCellConditions"/>.
	/// </summary>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive)
			return;

		foreach (var key in Cell_Neighborhood.NeighborHoodKeys)
		{
			if (key == "center")
				continue;
			var neighbor = CellNeighborhood.NeighborhoodDict[key];
			if (!neighbor.IsAlive)
				continue;
			if (neighbor.CellType == CellType.Irradiated)
				continue;
			// Pick target type now and encode it in the condition tag.
			var target = MutantTargetTypes[SimRandom.Range(0, MutantTargetTypes.Length)];
			neighbor.Conditions.Add("mutate_" + (int)target);
		}
	}

	/// <summary>Returns a random cell type from the mutant target pool.</summary>
	public static CellType RandomMutantTarget()
		=> MutantTargetTypes[SimRandom.Range(0, MutantTargetTypes.Length)];
}
