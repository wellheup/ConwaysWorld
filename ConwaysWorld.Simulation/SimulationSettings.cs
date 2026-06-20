namespace ConwaysWorld.Simulation;

/// <summary>Determines how <see cref="SimulationSettings.PopValue"/> is interpreted.</summary>
public enum PopMode
{
	/// <summary><see cref="SimulationSettings.PopValue"/> is a percentage (0–100) of the grid.</summary>
	Percent,
	/// <summary><see cref="SimulationSettings.PopValue"/> is an absolute cell count.</summary>
	Count,
}

/// <summary>
/// All user-configurable parameters for a simulation run.
/// An instance is passed to <see cref="Model"/> on construction; changing values after
/// construction has no effect on the running simulation — call <see cref="Model.Restart"/> instead.
/// </summary>
public class SimulationSettings
{
	// ── Grid dimensions ──────────────────────────────────────────────────────────

	/// <summary>Number of columns when the grid is first created.</summary>
	public int StartColumns { get; set; } = 40;

	/// <summary>Number of rows when the grid is first created.</summary>
	public int StartRows { get; set; } = 40;

	/// <summary>
	/// Maximum column/row count the grid may grow to via Explorer expansion.
	/// Set to 0 to allow unlimited growth.
	/// </summary>
	public int MaxGridSize { get; set; } = 120;

	// ── Initial population ───────────────────────────────────────────────────────

	/// <summary>Whether <see cref="PopValue"/> is a percentage or an absolute count.</summary>
	public PopMode PopMode { get; set; } = PopMode.Percent;

	/// <summary>
	/// The seed population value interpreted according to <see cref="PopMode"/>.
	/// At 10 % on a 40×40 grid this yields roughly 160 starting cells.
	/// </summary>
	public int PopValue { get; set; } = 10;

	// ── Nations ──────────────────────────────────────────────────────────────────

	/// <summary>Hard cap on the number of simultaneous nations (maximum 20, one per colour slot).</summary>
	public int MaxNations { get; set; } = 20;

	/// <summary>
	/// Number of cell clusters to seed at grid initialisation.
	/// Each cluster grows outward from a random point with at most 75% fill per ring.
	/// Defaults to MaxNations / 4 (minimum 1).
	/// </summary>
	public int StartClusters { get; set; } = 2;

	/// <summary>
	/// Minimum number of connected nationless cells required to spontaneously form a nation.
	/// Connectivity is Chebyshev-3 (each cell must be within 3 tiles of at least one other in the group).
	/// </summary>
	public int NationFormThreshold { get; set; } = 5;

	// ── Conway survival rules ────────────────────────────────────────────────────

	/// <summary>
	/// Minimum living neighbours required for a living cell to survive (also the birth threshold).
	/// Standard Conway value is 2.
	/// </summary>
	public int MinLivingNeighbors { get; set; } = 2;

	/// <summary>
	/// Maximum living neighbours before a cell dies of overcrowding.
	/// Standard Conway value is 3.
	/// </summary>
	public int MaxLivingNeighbors { get; set; } = 3;

	// ── World events ─────────────────────────────────────────────────────────────

	/// <summary>Whether the Famine world event can trigger during a simulation run.</summary>
	public bool FamineEnabled { get; set; } = true;

	/// <summary>Whether the Flood world event can trigger during a simulation run.</summary>
	public bool FloodEnabled { get; set; } = true;

	/// <summary>
	/// When <c>true</c>, <see cref="Model.AddRandomLife"/> injects new cells whenever
	/// the living population drops below <see cref="MinLifePercent"/> of the grid.
	/// Defaults to <c>false</c>.
	/// </summary>
	public bool RandomLifeEnabled { get; set; } = false;

	/// <summary>
	/// Minimum number of steps that must pass after a famine ends before a new one can start.
	/// Default is 15.
	/// </summary>
	public int FamineCooldown { get; set; } = 15;

	/// <summary>How many steps a famine lasts once it triggers. Default is 10.</summary>
	public int FamineDuration { get; set; } = 10;

	// ── Life floor ───────────────────────────────────────────────────────────────

	/// <summary>
	/// If population density drops below this fraction of the grid, <see cref="Model.AddRandomLife"/>
	/// injects a new batch of cells to prevent total extinction.
	/// </summary>
	public float MinLifePercent { get; set; } = 0.05f;

	// ── Spawn weights ────────────────────────────────────────────────────────────

	/// <summary>
	/// Relative spawn frequency for each cell type at grid initialisation and random-life injection.
	/// Higher weight = greater share of the living budget.  A weight of 0 disables spawning for that type.
	/// Warriors, Diplomats, and Kings are intentionally absent — they are only promoted from existing cells.
	/// </summary>
	public Dictionary<CellType, int> SpawnWeights { get; set; } = new()
																																																																{
																																																																																																																																{ CellType.Basic,    50 },
																																																																																																																																{ CellType.Immortal,  2 },
																																																																																																																																{ CellType.Diseased, 15 },
																																																																																																																																{ CellType.Plague,    3 },
																																																																																																																																{ CellType.Traveler,  6 },
																																																																																																																																{ CellType.Explorer,  3 },
																																																																																																																																{ CellType.Doctor,    5 },
																																																																																																																																{ CellType.Hunter,    5 },
																																																																																																																																{ CellType.Bomber,    8 },
																																																																};

	/// <summary>
	/// The set of types that are eligible for spawning.
	/// Only types present in both this set and <see cref="SpawnWeights"/> are considered.
	/// </summary>
	public HashSet<CellType> SpawnEnabled { get; set; } = new()
																																																																{
																																																																																																																																CellType.Basic,
																																																																																																																																CellType.Immortal,
																																																																																																																																CellType.Diseased,
																																																																																																																																CellType.Plague,
																																																																																																																																CellType.Traveler,
																																																																																																																																CellType.Explorer,
																																																																																																																																CellType.Doctor,
																																																																																																																																CellType.Hunter,
																																																																																																																																CellType.Bomber,
																																																																};

	// ── Derived ──────────────────────────────────────────────────────────────────

	/// <summary>
	/// Fraction of the grid (0–1) that should be occupied at initialisation,
	/// derived from <see cref="PopMode"/> and <see cref="PopValue"/>.
	/// </summary>
	public float BasePercentLiving
	{
		get
		{
			if (PopMode == PopMode.Percent)
				return PopValue / 100f;
			int totalCells = StartColumns * StartRows;
			return totalCells > 0 ? (float)PopValue / totalCells : 0.1f;
		}
	}
}
