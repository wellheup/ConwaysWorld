namespace ConwaysWorld.Simulation;

/// <summary>
/// A Zombie is a resurrected cell created by a <see cref="Cell_Necromancer"/>.
/// <para>
/// Key rules:
/// <list type="bullet">
///   <item>Immune to Conway crowding, under-population, disease, plague, and old age.</item>
///   <item>Invisible to non-zombie cells' Conway neighbor counts (see <see cref="Cell_Neighborhood.NumNonZombieNeighbors"/>).</item>
///   <item>Treats its Necromancer as its King and sibling zombies as its nation.</item>
///   <item>Dies if its Necromancer dies (handled by <see cref="Cell_Necromancer.KillAllZombies"/>).</item>
///   <item>Killed by any cell that targets killers (Warriors, Hunters, Soldiers, etc.) — on kill the slot becomes a dead Basic with no last-type (permanently destroyed).</item>
///   <item>Doctor cells treat a Zombie as a diseased cell: curing it kills it and permanently destroys the slot.</item>
///   <item>Does not breed, does not age out, does not gain conditions.</item>
/// </list>
/// </para>
/// </summary>
public class Cell_Zombie : Cell
{
	/// <summary>The Necromancer this zombie belongs to.</summary>
	public Cell_Necromancer Necromancer { get; }

	/// <summary>
	/// The cell type this zombie was before it died.
	/// Drives the zombie's visual appearance (same sprite/colour as its original type).
	/// </summary>
	public CellType OriginalType { get; }

	/// <summary>Creates a Zombie at the given position.</summary>
	public Cell_Zombie(int column, int row, bool isAlive, Cell_Necromancer necromancer, CellType originalType)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Zombie;
		Conditions = new HashSet<string>();
		Nationality = -1;
		Necromancer = necromancer;
		OriginalType = originalType;
	}

	/// <inheritdoc/>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		CellType = CellType.Zombie;
		Nationality = -1;
		// Zombies never gain the "mature" condition and do not breed.
	}

	/// <inheritdoc/>
	public override void Die()
	{
		if (IsAlive && CellType != CellType.Dead)
			LastType = CellType;
		IsAlive = false;
		Age = 0;
		Nationality = -1;
	}

	/// <summary>
	/// Zombies ignore Conway rules entirely — they only die if explicitly killed.
	/// </summary>
	public override bool CalcCellAliveNextGen() => IsAlive;

	/// <summary>
	/// Zombies take no special actions of their own.
	/// (Their original type's behavior is intentionally NOT replicated — zombies are passive.)
	/// </summary>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null) { }
}
