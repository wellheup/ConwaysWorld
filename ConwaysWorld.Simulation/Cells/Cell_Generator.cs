namespace ConwaysWorld.Simulation;

/// <summary>
/// Randomly creates cell instances at grid initialisation and during random-life injection,
/// respecting the spawn weights and enabled types in <see cref="SimulationSettings"/>.
/// <para>
/// The <see cref="SpawnFrequency"/> table is built once from the settings and maps each
/// enabled type to its fractional share of the configured living-cell budget.
/// Dead cells fill the remainder of probability space to reach 1.0.
/// </para>
/// <para>
/// Some types produce mixed variants when chosen:
/// <list type="bullet">
///   <item><see cref="CellType.Diseased"/> → 80 % Diseased, 20 % Plague.</item>
///   <item><see cref="CellType.Traveler"/> → 60 % Traveler, 40 % Explorer.</item>
/// </list>
/// </para>
/// </summary>
public class Cell_Generator
{
	/// <summary>Maps a cell type to its probability share of the living-cell budget.</summary>
	private readonly struct SpawnFrequency
	{
		public CellType Type { get; init; }
		public float Freq { get; init; }
	}

	/// <summary>Full probability table including Dead for <see cref="InitializeRandomCell"/>.</summary>
	private readonly List<SpawnFrequency> _frequencies = new();

	/// <summary>
	/// Living-only probability table (normalised to 1.0) for <see cref="InitializeLivingCell"/>.
	/// Keeps spawn weights accurate regardless of how sparse the overall population is.
	/// </summary>
	private readonly List<SpawnFrequency> _livingFrequencies = new();

	/// <summary>
	/// Constructs a generator and builds the frequency table from <paramref name="settings"/>.
	/// </summary>
	public Cell_Generator(SimulationSettings settings)
	{
		BuildFrequencies(settings);
	}

	/// <summary>
	/// Computes each enabled type's share of <see cref="SimulationSettings.BasePercentLiving"/>
	/// proportional to its weight, then appends a Dead entry for the dead-cell remainder.
	/// Also builds a living-only table (normalised to 1.0) used by
	/// <see cref="InitializeLivingCell"/> so spawn weights are honoured at any population density.
	/// </summary>
	private void BuildFrequencies(SimulationSettings settings)
	{
		_frequencies.Clear();
		_livingFrequencies.Clear();

		float basePct = settings.BasePercentLiving;
		int totalWeight = 0;
		foreach (var kv in settings.SpawnWeights)
			if (settings.SpawnEnabled.Contains(kv.Key))
				totalWeight += kv.Value;

		float livingBudget = basePct;
		foreach (var kv in settings.SpawnWeights)
		{
			if (!settings.SpawnEnabled.Contains(kv.Key))
				continue;
			float share = totalWeight > 0 ? (float)kv.Value / totalWeight : 0f;
			_frequencies.Add(new SpawnFrequency { Type = kv.Key, Freq = livingBudget * share });
			// Living table: normalised directly by weight share (sums to 1.0).
			_livingFrequencies.Add(new SpawnFrequency { Type = kv.Key, Freq = share });
		}

		float deadPct = 1f - basePct;
		_frequencies.Add(new SpawnFrequency { Type = CellType.Dead, Freq = deadPct });
	}

	/// <summary>
	/// Draws a random cell type from the full frequency table (includes Dead).
	/// </summary>
	private CellType GetRandomCellType()
	{
		float roll = SimRandom.Value;
		float cumulative = 0f;
		foreach (var entry in _frequencies)
		{
			cumulative += entry.Freq;
			if (roll < cumulative)
				return entry.Type;
		}
		return CellType.Dead;
	}

	/// <summary>
	/// Draws a random cell type from the living-only table (never returns Dead).
	/// Used for guaranteed-living spawns so spawn weights are applied correctly
	/// regardless of how sparse the population density is.
	/// </summary>
	private CellType GetRandomLivingCellType()
	{
		if (_livingFrequencies.Count == 0)
			return CellType.Basic;
		float roll = SimRandom.Value;
		float cumulative = 0f;
		foreach (var entry in _livingFrequencies)
		{
			cumulative += entry.Freq;
			if (roll < cumulative)
				return entry.Type;
		}
		return _livingFrequencies[_livingFrequencies.Count - 1].Type;
	}

	/// <summary>
	/// Creates and returns a guaranteed living cell at (<paramref name="column"/>, <paramref name="row"/>).
	/// Type is drawn from the living-only normalised table so spawn weights are always
	/// honoured, even at very low population densities.
	/// </summary>
	public Cell InitializeLivingCell(int column, int row)
	{
		CellType type = GetRandomLivingCellType();

		float variant = SimRandom.Value;
		return type switch
		{
			CellType.Basic => CreateBasic(column, row),
			CellType.Immortal => new Cell_Immortal(column, row, true),
			CellType.Diseased => variant > 0.2f
																																																																			? new Cell_Diseased(column, row, true)
																																																																			: (Cell)new Cell_Plague(column, row, true),
			CellType.Plague => new Cell_Plague(column, row, true),
			CellType.Traveler => variant > 0.4f
																																																																			? new Cell_Traveler(column, row, true)
																																																																			: (Cell)new Cell_Explorer(column, row, true),
			CellType.Explorer => new Cell_Explorer(column, row, true),
			CellType.Doctor => new Cell_Doctor(column, row, true),
			CellType.Hunter => new Cell_Hunter(column, row, true),
			CellType.Bomber => new Cell_Bomber(column, row, true),
			CellType.Mutant => new Cell_Mutant(column, row, true),
			_ => new Cell_Basic(column, row, true),
		};
	}

	/// <summary>
	/// Creates and returns a randomly typed cell at (<paramref name="column"/>, <paramref name="row"/>).
	/// Dead rolls produce a dead Basic cell.  Some types produce mixed-variant results
	/// (see class summary).
	/// </summary>
	/// <summary>Callback set by Model to gate Savior spawning (at most one per grid, requires ≥2 nations).</summary>
	public Func<bool>? CanSpawnSavior { get; set; }

	public Cell InitializeRandomCell(int column, int row)
	{
		float variant = SimRandom.Value;
		var type = GetRandomCellType();
		if (type == CellType.Savior && (CanSpawnSavior == null || !CanSpawnSavior()))
			type = CellType.Basic;
		return type switch
		{
			CellType.Basic => CreateBasic(column, row),
			CellType.Immortal => new Cell_Immortal(column, row, true),
			CellType.Diseased => variant > 0.2f
																																																																			? new Cell_Diseased(column, row, true)
																																																																			: (Cell)new Cell_Plague(column, row, true),
			CellType.Plague => new Cell_Plague(column, row, true),
			CellType.Traveler => variant > 0.4f
																																																																			? new Cell_Traveler(column, row, true)
																																																																			: (Cell)new Cell_Explorer(column, row, true),
			CellType.Explorer => new Cell_Explorer(column, row, true),
			CellType.Doctor => new Cell_Doctor(column, row, true),
			CellType.Hunter => new Cell_Hunter(column, row, true),
			CellType.Bomber => new Cell_Bomber(column, row, true),
			CellType.Savior => new Cell_Savior(column, row, true),
			CellType.Irradiated => new Cell_Irradiated(column, row, true),
			CellType.PlagueRat => new Cell_PlagueRat(column, row, true),
			CellType.Necromancer => new Cell_Necromancer(column, row, true),
			CellType.Mutant => new Cell_Mutant(column, row, true),
			_ => new Cell_Basic(column, row, false),
		};
	}

	/// <summary>
	/// Creates a Basic cell with optional spawn-time conditions:
	/// <list type="bullet">
	///   <item>25 % chance of <c>"immune"</c> — permanently strips disease tags each step.</item>
	///   <item>1 % chance of <c>"immaculate"</c> — triggers a one-time forced-birth of axis-aligned neighbours.</item>
	/// </list>
	/// </summary>
	private static Cell CreateBasic(int column, int row)
	{
		var cell = new Cell_Basic(column, row, true);
		if (SimRandom.Range(1, 5) == 1)
			cell.Conditions.Add("immune");
		if (SimRandom.Range(1, 101) == 1)
			cell.Conditions.Add("immaculate");
		return cell;
	}
}
