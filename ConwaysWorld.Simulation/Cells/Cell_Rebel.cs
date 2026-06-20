namespace ConwaysWorld.Simulation;

/// <summary>
/// A Rebel is a short-lived, aggressive cell that converts foreign neighbours at three times
/// the rate of a <see cref="Cell_Diplomat"/>.
/// <para>
/// Key differences from a Diplomat:
/// <list type="bullet">
///   <item>Dies naturally after 10 steps (25 % of the informal Diplomat lifespan ceiling of ~40 steps).</item>
///   <item>Converts adjacent foreign cells with 75 % probability rather than the Diplomat's 25 %.</item>
/// </list>
/// </para>
/// <para>
/// Rebels are never spawned at grid initialisation.  They are created when a
/// <see cref="Cell_Revolutionary"/> recruits cells from its former nation, or when any cell
/// receives the <c>"toRebel"</c> condition processed by <see cref="Model.UpdateCellConditions"/>.
/// </para>
/// <para>
/// Rebels are hunted by <see cref="Cell_Warrior"/> and <see cref="Cell_Hunter"/>.
/// </para>
/// </summary>
public class Cell_Rebel : Cell_Diplomat
{
	/// <summary>Maximum age a Rebel survives before dying naturally.</summary>
	private const int MaxAge = 10;

	/// <summary>Creates a Rebel cell at the given position.</summary>
	public Cell_Rebel(int column, int row, bool isAlive) : base(column, row, isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Rebel;
		Conditions = new HashSet<string>();
	}

	/// <summary>
	/// Adds a hard age cap of <see cref="MaxAge"/> steps on top of the base Conway survival rules.
	/// </summary>
	public override bool CalcCellAliveNextGen()
	{
		if (Age >= MaxAge)
			return false;
		return LiveBasic();
	}

	/// <summary>Converts with 75 % probability — three times the Diplomat's 25 % base rate.</summary>
	protected override bool ShouldConvert() => SimRandom.Range(0, 4) != 0;
}
