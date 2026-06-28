namespace ConwaysWorld.Simulation;

/// <summary>
/// Abstract base for cells whose primary action is converting adjacent or nearby cells —
/// changing their nationality, cell type, or conditions.
/// <para>
/// Direct subclasses: <see cref="Cell_Diplomat"/> (converts adjacent foreign cells by
/// nationality), <see cref="Cell_Savior"/> (converts adjacent Basic cells to Followers),
/// <see cref="Cell_Revolutionary"/> (recruits Warriors and Rebels from an old nation).
/// <see cref="Cell_Rebel"/> extends <see cref="Cell_Diplomat"/> and is therefore an
/// indirect subclass.
/// </para>
/// <para>
/// Provides the <see cref="_specialPerformed"/> guard and matching <see cref="Live"/> /
/// <see cref="Die"/> overrides, eliminating repeated boilerplate across all converter types.
/// </para>
/// </summary>
public abstract class Cell_Converter : Cell
{
	/// <summary>
	/// Prevents <see cref="Cell.SpecialActions"/> from running twice in one generation
	/// when the grid iterator reaches this cell's new position after it moves.
	/// Reset to <c>false</c> in <see cref="Live"/>; set to <c>true</c> in <see cref="Die"/>
	/// and at the start of <see cref="Cell.SpecialActions"/>.
	/// </summary>
	protected bool _specialPerformed = false;

	/// <summary>
	/// Standard converter live step: calls <see cref="Cell.Live"/> (age, maturity,
	/// <see cref="Cell.ChooseNation"/>) and resets the <see cref="_specialPerformed"/> guard.
	/// <para>
	/// Subclasses that must skip <see cref="Cell.ChooseNation"/> (e.g.
	/// <see cref="Cell_Savior"/>, which tracks its own nation logic) should override
	/// this method without calling <c>base.Live()</c>.
	/// </para>
	/// </summary>
	public override void Live()
	{
		base.Live();
		_specialPerformed = false;
	}

	/// <summary>
	/// Marks the cell dead and sets <see cref="_specialPerformed"/> to prevent any
	/// further special-action processing this generation.
	/// </summary>
	public override void Die()
	{
		base.Die();
		_specialPerformed = true;
	}
}
