namespace ConwaysWorld.Simulation;

/// <summary>
/// A Warrior is a combat-oriented cell that targets foreign Diseased and Plague cells.
/// It extends <see cref="Cell_Hunter"/> but replaces the movement/hunt loop with a
/// strength-based combat system.
/// <para>
/// Combat rules (runs in <see cref="Fight"/>):
/// <list type="bullet">
///   <item>Scans up to range 2 for an enemy (alive, different nation, Diseased or Plague).</item>
///   <item>Moves into the enemy's slot via <see cref="Cell.SwapCells"/>.</item>
///   <item>If the target is also a Warrior, a strength comparison decides the winner:
///         each cell's power = number of same-nation neighbours, +1 per Warrior neighbour,
///         +2 per King neighbour.  Older cell gets +1.  Ties are coin-flipped.</item>
///   <item>The loser is killed and marked <c>"cleanup"</c>.</item>
/// </list>
/// </para>
/// <para>
/// Warriors are never spawned directly; they are promoted from Basic cells that have the
/// <c>"toWar"</c> condition set by a neighbouring <see cref="Cell_King"/>.
/// Like Hunters, Warriors demote back to Basic after 3 idle steps (no fight found).
/// </para>
/// </summary>
public class Cell_Warrior : Cell_Hunter
{
	/// <summary>Cell types this Warrior targets (Diseased and Plague of foreign nations).</summary>
	private readonly List<CellType> _warriorPreyTypes;

	/// <summary>Creates a Warrior, overriding Hunter's prey list.</summary>
	public Cell_Warrior(int column, int row, bool isAlive)
									: base(column, row, isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Warrior;
		Conditions = new HashSet<string>();
		_warriorPreyTypes = new List<CellType> { CellType.Diseased, CellType.Plague, CellType.King, CellType.Rebel, CellType.Revolutionary, CellType.Spy };
	}

	/// <summary>Increments age and assigns nationality; does not track isolation like a Traveler.</summary>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		ChooseNation();
	}

	/// <summary>
	/// Returns <c>true</c> if <paramref name="c"/> is alive, belongs to a different nation,
	/// and is a type in <see cref="_warriorPreyTypes"/>.
	/// </summary>
	private bool IsEnemy(Cell c) =>
									c.IsAlive &&
									c.Nationality != Nationality &&
									_warriorPreyTypes.Contains(c.CellType);

	/// <summary>
	/// Compares the combat strength of this cell against <paramref name="target"/>.
	/// Strength = count of same-nation neighbours, weighted by type (+1 Warrior, +2 King).
	/// The older combatant receives +1 power.  Ties are resolved by coin-flip.
	/// </summary>
	private bool IsCombatWinner(Cell target)
	{
		int myPower = GetStrength(this);
		int theirPower = GetStrength(target);
		if (Age > target.Age)
			myPower++;
		else
			theirPower++;
		if (myPower == theirPower)
			return SimRandom.CoinFlip();
		return myPower > theirPower;
	}

	/// <summary>
	/// Sums the combat power contributed by <paramref name="cell"/>'s living neighbours
	/// of the same nation.  Each same-nation neighbour adds 1; Warriors add 2 total; Kings add 3 total.
	/// </summary>
	private static int GetStrength(Cell cell)
	{
		int power = 0;
		foreach (var neighbor in cell.CellNeighborhood.NeighborsDict.Values)
		{
			if (neighbor.Nationality == cell.Nationality)
			{
				power++;
				if (neighbor.CellType == CellType.Warrior)
					power++;
				if (neighbor.CellType == CellType.King)
					power += 2;
			}
		}
		return power;
	}

	/// <summary>
	/// Finds the nearest enemy, swaps into its slot, and kills it.
	/// If the target is a Warrior and this cell loses the combat check, this cell dies instead.
	/// </summary>
	/// <returns><c>true</c> if this Warrior successfully fought (won or initiated).</returns>
	protected bool Fight(Cell[,] cellGrid)
	{
		var target = SelectNearbyCellByRule(cellGrid, IsEnemy, 2);
		if (target == null)
			return false;

		if (target.CellType == CellType.Warrior && !IsCombatWinner(target))
		{
			Die();
			Conditions.Add("cleanup");
			return false;
		}
		else
		{
			SwapCells(this, target, cellGrid);
			target.Die();
			target.Conditions.Add("cleanup");
			return true;
		}
	}

	/// <summary>
	/// Runs <see cref="Fight"/> and tracks idle turns for demotion back to Basic.
	/// </summary>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive)
			return;
		bool didFight = Fight(cellGrid);
		if (didFight)
			IdleTurns = 0;
		else
			IdleTurns++;
	}
}
