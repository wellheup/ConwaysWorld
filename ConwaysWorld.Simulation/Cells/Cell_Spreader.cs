namespace ConwaysWorld.Simulation;

/// <summary>
/// Abstract base for cells that carry a unique strain identifier and stamp
/// infection conditions onto neighbouring cells each step.
/// <para>
/// Direct subclasses: <see cref="Cell_Diseased"/> (and transitively
/// <see cref="Cell_Plague"/>), <see cref="Cell_PlagueRat"/>.
/// </para>
/// <para>
/// Each concrete spreader passes its strain prefix character to the
/// <see cref="Cell_Spreader(int,int,bool,char)"/> constructor, which generates
/// a unique <see cref="StrainId"/> tag via <see cref="Cell.RandomCondition"/>.
/// Subclasses may override <see cref="StrainId"/> after construction to change
/// the active strain (e.g. <see cref="Cell_Plague"/> switches from <c>d_</c> to <c>p_</c>).
/// </para>
/// </summary>
public abstract class Cell_Spreader : Cell
{
	/// <summary>
	/// Unique strain tag generated at spawn from a prefix character
	/// (e.g. <c>"d_38291047"</c> for Diseased, <c>"r_12840193"</c> for PlagueRat).
	/// Written to a target cell's <see cref="Cell.Conditions"/> to schedule conversion.
	/// </summary>
	public string StrainId { get; protected set; } = string.Empty;

	/// <summary>
	/// Initialises position and liveness, then generates a unique <see cref="StrainId"/>
	/// from <paramref name="strainPrefix"/> via <see cref="Cell.RandomCondition"/>.
	/// </summary>
	/// <param name="column">Grid column.</param>
	/// <param name="row">Grid row.</param>
	/// <param name="isAlive">Initial alive state.</param>
	/// <param name="strainPrefix">
	/// Single character identifying the strain family
	/// (<c>'d'</c> for Diseased, <c>'p'</c> for Plague, <c>'r'</c> for PlagueRat).
	/// </param>
	protected Cell_Spreader(int column, int row, bool isAlive, char strainPrefix)
	{
		Column = column;
		Row = row;
		IsAlive = isAlive;
		Conditions = new HashSet<string>();
		StrainId = RandomCondition(strainPrefix);
	}

	/// <summary>
	/// Returns <c>true</c> if <paramref name="target"/> can receive this spreader's
	/// strain condition: must be alive, not <see cref="CellType.Immortal"/>, not
	/// <see cref="CellType.Irradiated"/>, not generally immune, and not already
	/// vaccinated against this specific strain.
	/// </summary>
	protected virtual bool CanInfect(Cell target) =>
		target.IsAlive &&
		target.CellType != CellType.Immortal &&
		target.CellType != CellType.Irradiated &&
		!target.Conditions.Contains("immune") &&
		!target.Conditions.Contains("vax_" + StrainId);
}
