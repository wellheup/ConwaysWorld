namespace ConwaysWorld.Simulation;

/// <summary>
/// The standard Conway's Game of Life cell. Follows the classic survival rules:
/// survives with 2–3 living neighbours, is born into empty cells with exactly 2 neighbours,
/// and dies from under- or over-population otherwise.
/// <para>
/// Spawn-time variants (applied by <see cref="Cell_Generator.CreateBasic"/>):
/// <list type="bullet">
///   <item><term>immune (25 % chance)</term><description>All disease conditions (<c>d_</c>/<c>p_</c>) are stripped each step — the cell can never be infected.</description></item>
///   <item><term>immaculate (1 % chance)</term><description>Triggers <see cref="Cell.Immaculate"/> once: the cell and two axis-aligned neighbours are forced alive regardless of Conway rules.</description></item>
/// </list>
/// Basic cells can be promoted to <see cref="CellType.Warrior"/> by a neighbouring King,
/// and are used as the replacement type when Warriors or Hunters demote after 3 idle steps.
/// </para>
/// </summary>
public class Cell_Basic : Cell
{
	/// <summary>
	/// Creates a Basic cell at the specified position.
	/// All conditions and the disease-immune flag are managed externally by <see cref="Cell_Generator"/>.
	/// </summary>
	public Cell_Basic(int column, int row, bool isAlive)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		CellType = CellType.Basic;
		Conditions = new HashSet<string>();
	}
}
