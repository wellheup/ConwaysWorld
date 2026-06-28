namespace ConwaysWorld.Simulation;

/// <summary>
/// Abstract base for combat-oriented cells that scan for targets, move toward them,
/// and kill them on contact.
/// <para>
/// Direct subclasses: <see cref="Cell_Soldier"/>, <see cref="Cell_Barbarian"/>,
/// <see cref="Cell_Zealot"/>.
/// </para>
/// <para>
/// <see cref="Cell_Hunter"/> (and its subclass <see cref="Cell_Warrior"/>) form a
/// separate combatant branch via <see cref="Cell_Traveler"/> and do not extend this class.
/// </para>
/// <para>
/// Provides the <see cref="_specialPerformed"/> guard so that
/// <see cref="Cell.SpecialActions"/> cannot fire twice in the same generation —
/// the grid iterator may reach a cell's new position after it moves.
/// </para>
/// </summary>
public abstract class Cell_Combatant : Cell
{
	/// <summary>
	/// Set to <c>true</c> at the start of <see cref="Cell.SpecialActions"/> and reset
	/// to <c>false</c> each step in <see cref="Live"/>.  Prevents double-execution when
	/// the grid iterator reaches a cell that already moved this generation.
	/// </summary>
	protected bool _specialPerformed = false;

	/// <summary>
	/// Standard combatant live step: increments age, marks maturity, assigns nationality,
	/// and resets the <see cref="_specialPerformed"/> guard.
	/// <para>
	/// Subclasses that must skip <see cref="Cell.ChooseNation"/> (e.g. nationless
	/// combatants like <see cref="Cell_Barbarian"/>) should override this method.
	/// </para>
	/// </summary>
	public override void Live()
	{
		IsAlive = true;
		Age++;
		if (Age > MatureAge)
			Conditions.Add("mature");
		ChooseNation();
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
