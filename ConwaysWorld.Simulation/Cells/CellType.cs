namespace ConwaysWorld.Simulation;

/// <summary>
/// Identifies the type of a cell in the simulation grid.
/// <para>
/// <b>Dead</b> is the only non-living state. All other values represent living cell types
/// that can be spawned at initialisation or promoted/converted during gameplay.
/// Warriors and Kings are never spawned directly — they are promoted from existing cells
/// (Basic→Warrior via the "toWar" condition; any citizen→King via <see cref="Cell_Nation.CrownKing"/>).
/// </para>
/// </summary>
public enum CellType
{
	/// <summary>An empty, non-living grid slot.</summary>
	Dead,

	/// <summary>Standard Conway cell. 25 % immune chance; 1 % immaculate chance at spawn.</summary>
	Basic,

	/// <summary>Persists indefinitely unless isolated for more than 8 consecutive steps.</summary>
	Immortal,

	/// <summary>Spreads a unique <c>d_</c> strain to neighbours; dies after a 3-step countdown.</summary>
	Diseased,

	/// <summary>Like Diseased but with 40 % higher transmission rate and a <c>p_</c> strain.</summary>
	Plague,

	/// <summary>Swaps with a random neighbour every step; dies if alone &gt; 3 steps or crushed &gt; 3 steps.</summary>
	Traveler,

	/// <summary>Traveler variant that triggers grid expansion when it reaches an edge cell.</summary>
	Explorer,

	/// <summary>Cures neighbouring Diseased/Plague cells and stamps <c>vax_</c> immunity markers.</summary>
	Doctor,

	/// <summary>Targets foreign Diseased/Plague cells within range 2; demotes to Basic after 3 idle steps.</summary>
	Warrior,

	/// <summary>Hunts Immortals and Kings within range 5; demotes to Basic after 3 idle steps.</summary>
	Hunter,

	/// <summary>Detonates at age 2, killing all living cells within a 2-cell radius, then dies itself.</summary>
	Bomber,

	/// <summary>Elected from large nations; travels toward foreign nations and converts adjacent cells.</summary>
	Diplomat,

	/// <summary>Crowned from a nation with ≥ 5 citizens; marks neighbouring Basic cells with "toWar".</summary>
	King,
}
