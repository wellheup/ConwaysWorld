namespace ConwaysWorld.Simulation;

/// <summary>Records a single cell movement during <see cref="Model.UpdateSpecialActions"/>.</summary>
public record MoveRecord(int FromCol, int FromRow, int ToCol, int ToRow, int CellType, int Nationality);

/// <summary>
/// The central orchestrator for one Conway's World simulation run.
/// Owns the cell grid, the alive-next-generation scratch grid, and the nation registry.
/// <para>
/// The main entry point is <see cref="Step"/>, which executes the following pipeline each generation:
/// <list type="number">
///   <item><see cref="UpdateNeighborhoodsGrid"/> — rebuild each cell's Moore neighbourhood.</item>
///   <item><see cref="UpdateAliveNextGenGrid"/> — ask every cell whether it survives.</item>
///   <item><see cref="UpdateCellLives"/> — apply live/die decisions and update population count.</item>
///   <item><see cref="UpdateCellConditions"/> — process condition tags (disease, breeding, promotion, grid resize).</item>
///   <item><see cref="UpdateNeighborhoodsGrid"/> — rebuild again after condition changes.</item>
///   <item><see cref="UpdateSpecialActions"/> — movement, combat, disease spread, etc.</item>
///   <item><see cref="AddRandomLife"/> — inject new cells if population drops below the configured floor.</item>
///   <item><see cref="UpdateNations"/> — run census, elect Diplomats, crown Kings.</item>
/// </list>
/// </para>
/// </summary>
public partial class Model
{
	// ── Public grid state ─────────────────────────────────────────────────────────

	/// <summary>The live simulation grid.  Dimensions: [Columns, Rows].</summary>
	public Cell[,] CellGrid = null!;

	/// <summary>Scratch grid storing each cell's survival vote for the current step.</summary>
	public bool[,] AliveNextGenGrid = null!;

	/// <summary>All active nations keyed by their nation index.</summary>
	public Dictionary<int, Cell_Nation> Nations = new();

	// ── Event log ─────────────────────────────────────────────────────────────────

	/// <summary>
	/// Events generated during the most recent <see cref="Step"/> call.
	/// Cleared at the start of each step.  Consumers should read this list immediately
	/// after <see cref="Step"/> returns and before the next call.
	/// </summary>
	public List<string> PendingEvents { get; } = new();

	/// <summary>
	/// Cell movement deltas recorded during the most recent <see cref="Step"/> call.
	/// Cleared at the start of each step.  Used by the renderer to animate moving cells.
	/// </summary>
	public List<MoveRecord> PendingMoves { get; } = new();

	/// <summary>
	/// Column of the cell currently selected by the user, or -1 for none.
	/// Set by the Blazor layer; the simulation emits <c>selected_cell:</c> events
	/// describing what happens to this cell each step.
	/// </summary>
	public int SelectedCol { get; set; } = -1;

	/// <summary>Row of the currently selected cell, or -1 for none.</summary>
	public int SelectedRow { get; set; } = -1;

	// ── Private state ─────────────────────────────────────────────────────────────

	private readonly SimulationSettings _settings;
	private Cell_Generator _generator;
	private int _currentPopulation;
	private int _columns;
	private int _rows;
	private HashSet<(int, int)> _prevSoldierPairs = new();
	private int _lastDoctorBonus = -1;

	/// <summary>The one Savior currently alive on the grid, or null.</summary>
	public Cell_Savior? ActiveSavior { get; private set; } = null;

	/// <summary>All Necromancer cells currently alive on the grid.</summary>
	private readonly List<Cell_Necromancer> _activeNecromancers = new();

	/// <summary>
	/// True once any Zombie has ever been created this run.
	/// Guards the O(N) zombie-slot cleanup scan in UpdateNecromancers so it only runs
	/// when zombies could plausibly exist.
	/// </summary>
	private bool _zombiesEverExisted = false;

	/// <summary>Reusable list for nation-join candidate collection; avoids per-cell allocation.</summary>
	private readonly List<int> _nationJoinScratch = new(8);

	// ── Failure detection state ───────────────────────────────────────────────────

	/// <summary>Highest population ever seen this run; used for failure check #2 (drop-after-growth).</summary>
	private int _peakPopulation = 0;

	/// <summary>True once population has exceeded <see cref="SimulationSettings.FailurePopAfterGrowthThreshold"/> at least once.</summary>
	private bool _grewPastAfterGrowthThreshold = false;

	/// <summary>Population value recorded at the previous step; used for stagnation detection.</summary>
	private int _prevStepPopulation = -1;

	/// <summary>How many consecutive steps population has been unchanged.</summary>
	private int _stagnationCount = 0;

	/// <summary>
	/// Set to a non-null failure reason string after <see cref="Step"/> detects a failure.
	/// The Blazor layer reads this each tick and shows the failure popup when it is non-null.
	/// </summary>
	public string? FailureReason { get; private set; } = null;

	// ── Public read-only accessors ────────────────────────────────────────────────

	/// <summary>Current grid width in cells.</summary>
	public int Columns => _columns;

	/// <summary>Current grid height in cells.</summary>
	public int Rows => _rows;

	/// <summary>Number of living cells counted at the most recent step.</summary>
	public int CurrentPopulation => _currentPopulation;

	/// <summary>Number of steps completed since the last <see cref="Restart"/> or construction.</summary>
	public int Generation { get; private set; }

	// ── Construction and reset ────────────────────────────────────────────────────

	/// <summary>
	/// Constructs the model with the supplied settings and immediately runs <see cref="PopulateGrid"/>.
	/// </summary>
	public Model(SimulationSettings settings)
	{
		_settings = settings;
		_generator = new Cell_Generator(settings);
		_generator.CanSpawnSavior = CanSpawnSaviorNow;
		_columns = settings.StartColumns;
		_rows = settings.StartRows;
		FailureReason = null;
		_peakPopulation = 0;
		_grewPastAfterGrowthThreshold = false;
		_prevStepPopulation = -1;
		_stagnationCount = 0;
		PopulateGrid();
	}

	/// <summary>
	/// Resets generation, population, and grid dimensions to their initial values,
	/// rebuilds the generator, and repopulates the grid.
	/// </summary>
	public void Restart()
	{
		_generator = new Cell_Generator(_settings);
		_generator.CanSpawnSavior = CanSpawnSaviorNow;
		_lastDoctorBonus = -1;
		ActiveSavior = null;
		_activeNecromancers.Clear();
		_zombiesEverExisted = false;
		_columns = _settings.StartColumns;
		_rows = _settings.StartRows;
		Generation = 0;
		_currentPopulation = 0;
		_famineActive = false;
		_famineDurationCount = 0;
		_stepsSinceLastFamineEnd = 0;
		_floodActive = false;
		_floodCooldownCount = 0;
		_floodTriggerAt = 100 + SimRandom.Range(50, 101);
		FailureReason = null;
		_peakPopulation = 0;
		_grewPastAfterGrowthThreshold = false;
		_prevStepPopulation = -1;
		_stagnationCount = 0;
		PopulateGrid();
	}

	// ── Step pipeline ─────────────────────────────────────────────────────────────

	/// <summary>
	/// Advances the simulation by one generation, executing the full 8-phase pipeline.
	/// </summary>
	/// <returns>The living cell count after the step.</returns>
	public int Step()
	{
		PendingEvents.Clear();
		PendingMoves.Clear();
		if (!_settings.PureConwayMode)
		{
			UpdateFamine();
			UpdateFlood();
		}
		ApplyCellNeighborRules();
		UpdateNeighborhoodsGrid();
		EmitSelectedCellSnapshot();
		UpdateAliveNextGenGrid();
		UpdateCellLives();
		UpdateCellConditions();
		if (!_settings.PureConwayMode)
		{
			UpdateNeighborhoodsGrid();
			UpdateSpecialActions();
			TrackActiveSavior();
			UpdateNecromancers();
			CheckSoldierAttackOutcomes();
			if (_settings.RandomLifeEnabled)
			{
				if (_settings.ReactiveDoctor)
				{
					int diseasedCount = 0;
					for (int c2 = 0; c2 < _columns; c2++)
						for (int r2 = 0; r2 < _rows; r2++)
							if (CellGrid[c2, r2].IsAlive &&
									(CellGrid[c2, r2].CellType == CellType.Diseased || CellGrid[c2, r2].CellType == CellType.Plague))
								diseasedCount++;
					int bonus = diseasedCount / 30;
					if (bonus != _lastDoctorBonus)
					{
						_lastDoctorBonus = bonus;
						_generator.RebuildWithDoctorBonus(bonus);
					}
				}
				AddRandomLife();
			}
			UpdateNations();
		}
		CheckFailureConditions();
		EmitSelectedCellOutcome();
		Generation++;
		return _currentPopulation;
	}

	/// <summary>
	/// Rebuilds the <see cref="Cell_Neighborhood"/> for every cell in the grid.
	/// Must be called after any cell movement or replacement operation.
	/// </summary>
	public void UpdateNeighborhoodsGrid()
	{
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
				CellGrid[c, r].CellNeighborhood = new Cell_Neighborhood(CellGrid, c, r);
	}

	/// <summary>
	/// Populates <see cref="AliveNextGenGrid"/> by asking each cell <see cref="Cell.CalcCellAliveNextGen"/>.
	/// Results are stored separately so all cells see the same snapshot of neighbour states.
	/// </summary>
	public void UpdateAliveNextGenGrid()
	{
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
				AliveNextGenGrid[c, r] = CellGrid[c, r].CalcCellAliveNextGen();
	}

	/// <summary>
	/// Applies the survival/birth decisions from <see cref="AliveNextGenGrid"/>,
	/// calls <see cref="Cell.Live"/> or <see cref="Cell.Die"/> on each cell,
	/// and rebuilds the partial citizen lists used during nation census.
	/// </summary>
	/// <returns>The living cell count after applying all decisions.</returns>
	public int UpdateCellLives()
	{
		_currentPopulation = 0;
		foreach (var nation in Nations.Values)
			nation.CitizensList.Clear();

		for (int c = 0; c < _columns; c++)
		{
			for (int r = 0; r < _rows; r++)
			{
				bool wasAlive = CellGrid[c, r].IsAlive;
				bool willLive = AliveNextGenGrid[c, r];

				if (wasAlive)
				{
					if (willLive)
					{
						CellGrid[c, r].Live();
						// Irradiated, Zombie, and Necromancer cells do NOT count toward the
						// living population (so a grid of only such cells reads as "all dead").
						var ct = CellGrid[c, r].CellType;
						if (ct != CellType.Irradiated && ct != CellType.Zombie && ct != CellType.Necromancer)
							_currentPopulation++;
						var nat = CellGrid[c, r].Nationality;
						if (nat >= 0 && Nations.ContainsKey(nat))
							Nations[nat].CitizensList.Add(CellGrid[c, r]);
					}
					else
					{
						CellGrid[c, r].Die();
					}
				}
				else
				{
					if (willLive)
					{
						// Conway birth: if any Moore neighbour is a living Islander,
						// the newborn cell is also an Islander (nationless).
						if (HasIslanderNeighbour(c, r))
						{
							CellGrid[c, r] = Cell.ReplaceCell(CellGrid[c, r], CellType.Islander, true);
							CellGrid[c, r].Nationality = -1;
						}
						else
						{
							CellGrid[c, r].Live();
						}
						// Newly born cells count toward the living population.
						var ct = CellGrid[c, r].CellType;
						if (ct != CellType.Irradiated && ct != CellType.Zombie && ct != CellType.Necromancer)
							_currentPopulation++;
					}
				}
			}
		}
		return _currentPopulation;
	}

	// ── Cell neighbor rule application ────────────────────────────────────────────

	/// <summary>
	/// Applies the base Conway survival constraints (<see cref="SimulationSettings.MinLivingNeighbors"/>
	/// and <see cref="SimulationSettings.MaxLivingNeighbors"/>) to every cell.
	/// Must be called before <see cref="UpdateAliveNextGenGrid"/>.
	/// </summary>
	private void ApplyCellNeighborRules()
	{
		int baseMin = _settings.MinLivingNeighbors;
		int baseMax = _settings.MaxLivingNeighbors;
		int baseBirth = _settings.BirthNeighborCount;
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				CellGrid[c, r].MinLivingNeighbors = baseMin;
				CellGrid[c, r].MaxLivingNeighbors = baseMax;
				CellGrid[c, r].BirthNeighborCount = baseBirth;
			}
	}

	// ── Private helpers ───────────────────────────────────────────────────────────

	/// <summary>
	/// Returns <c>true</c> if any of the eight Moore-neighbourhood cells around
	/// (<paramref name="col"/>, <paramref name="row"/>) is currently alive.
	/// Uses direct grid bounds checks — safe to call before neighbourhoods are rebuilt.
	/// </summary>
	private bool HasLivingNeighbor(int col, int row)
	{
		for (int dc = -1; dc <= 1; dc++)
			for (int dr = -1; dr <= 1; dr++)
			{
				if (dc == 0 && dr == 0)
					continue;
				int nc = col + dc, nr = row + dr;
				if (nc >= 0 && nc < _columns && nr >= 0 && nr < _rows && CellGrid[nc, nr].IsAlive)
					return true;
			}
		return false;
	}

	/// <summary>
	/// Counts all living cells in the grid and stores the result in <see cref="_currentPopulation"/>.
	/// Called once during <see cref="PopulateGrid"/> before stepping begins.
	/// </summary>
	private void CountInitialPopulation()
	{
		_currentPopulation = 0;
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
				if (CellGrid[c, r].IsAlive)
					_currentPopulation++;
	}
}
