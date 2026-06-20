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

	private readonly List<SpawnFrequency> _frequencies = new();

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
	/// </summary>
	private void BuildFrequencies(SimulationSettings settings)
	{
		_frequencies.Clear();

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
		}

		float deadPct = 1f - basePct;
		_frequencies.Add(new SpawnFrequency { Type = CellType.Dead, Freq = deadPct });
	}

	/// <summary>
	/// Draws a random cell type from the frequency table using a cumulative probability walk.
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
	/// Creates and returns a guaranteed living cell at (<paramref name="column"/>, <paramref name="row"/>).
	/// The type is drawn from the weighted frequency table, re-rolling any Dead result,
	/// so the returned cell is always alive.  Used during cluster spawning so every placed
	/// cell counts toward the living budget.
	/// </summary>
	public Cell InitializeLivingCell(int column, int row)
	{
		CellType type = GetRandomCellType();
		// Re-roll once if we got Dead — cluster positions should be alive.
		if (type == CellType.Dead)
			type = GetRandomCellType();
		if (type == CellType.Dead)
			type = CellType.Basic;

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
			_ => new Cell_Basic(column, row, true),
		};
	}

	/// <summary>
	/// Creates and returns a randomly typed cell at (<paramref name="column"/>, <paramref name="row"/>).
	/// Dead rolls produce a dead Basic cell.  Some types produce mixed-variant results
	/// (see class summary).
	/// </summary>
	public Cell InitializeRandomCell(int column, int row)
	{
		float variant = SimRandom.Value;
		return GetRandomCellType() switch
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
