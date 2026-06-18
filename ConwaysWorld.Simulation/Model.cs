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
///   <item><see cref="UpdateCellConditions"/> — process condition tags (disease infection, breeding, promotion, demotion, grid resize).</item>
///   <item><see cref="UpdateNeighborhoodsGrid"/> — rebuild again after condition changes.</item>
///   <item><see cref="UpdateSpecialActions"/> — movement, combat, disease spread, etc.</item>
///   <item><see cref="AddRandomLife"/> — inject new cells if population drops below the configured floor.</item>
///   <item><see cref="UpdateNations"/> — run census, elect Diplomats, crown Kings.</item>
/// </list>
/// </para>
/// </summary>
public class Model
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

	// ── Private state ─────────────────────────────────────────────────────────────

	private readonly SimulationSettings _settings;
	private Cell_Generator _generator;
	private int _currentPopulation;
	private int _columns;
	private int _rows;

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
		_columns = settings.StartColumns;
		_rows = settings.StartRows;
		PopulateGrid();
	}

	/// <summary>
	/// Resets generation, population, and grid dimensions to their initial values,
	/// rebuilds the generator, and repopulates the grid.
	/// </summary>
	public void Restart()
	{
		_generator = new Cell_Generator(_settings);
		_columns = _settings.StartColumns;
		_rows = _settings.StartRows;
		Generation = 0;
		_currentPopulation = 0;
		PopulateGrid();
	}

	/// <summary>
	/// Allocates fresh grids, fills them with randomly generated cells,
	/// then sets up initial neighbourhoods, nations, and population count.
	/// </summary>
	public void PopulateGrid()
	{
		CellGrid = new Cell[_columns, _rows];
		AliveNextGenGrid = new bool[_columns, _rows];

		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
				CellGrid[c, r] = _generator.InitializeRandomCell(c, r);

		InitializeNations();
		UpdateNeighborhoodsGrid();
		CountInitialPopulation();
		UpdateNations();
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
		UpdateNeighborhoodsGrid();
		UpdateAliveNextGenGrid();
		UpdateCellLives();
		UpdateCellConditions();
		UpdateNeighborhoodsGrid();
		UpdateSpecialActions();
		AddRandomLife();
		UpdateNations();
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
						var nat = CellGrid[c, r].Nationality;
						if (nat >= 0 && Nations.ContainsKey(nat))
							Nations[nat].CitizensList.Add(CellGrid[c, r]);
					}
					else
					{
						CellGrid[c, r].Die();
					}
					_currentPopulation++;
				}
				else
				{
					if (willLive)
						CellGrid[c, r].Live();
				}
			}
		}
		return _currentPopulation;
	}

	/// <summary>
	/// Processes all condition tags on every cell in a single pass:
	/// <list type="bullet">
	///   <item><c>"cleanup"</c> — replaces the slot with a dead Basic cell.</item>
	///   <item><c>"immune"</c> — strips all active disease tags.</item>
	///   <item><c>"d_"</c> / <c>"p_"</c> tags — converts the cell into Diseased/Plague via <see cref="Cell_Diseased.Infect"/>.</item>
	///   <item><c>"mature"</c> — triggers <see cref="Cell.Breed"/>.</item>
	///   <item><c>"immaculate"</c> — triggers <see cref="Cell.Immaculate"/>.</item>
	///   <item>Explorer at grid edge — schedules a <see cref="ResizeCellGrid"/> call.</item>
	///   <item>Unaffiliated living cell (age ≥ 1) — assigns a random nation.</item>
	///   <item><c>"toWar"</c> on a Basic cell — promotes it to Warrior.</item>
	///   <item>Hunter/Warrior with <c>IdleTurns ≥ 3</c> — demotes to Basic.</item>
	/// </list>
	/// </summary>
	public void UpdateCellConditions()
	{
		bool needResize = false;

		for (int c = 0; c < _columns; c++)
		{
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];

				if (cell.Conditions.Contains("cleanup"))
				{
					CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Basic, false);
					continue;
				}

				if (cell.Conditions.Contains("immune"))
					cell.Conditions.RemoveWhere(s => s.StartsWith("d_") || s.StartsWith("p_"));

				if (cell.CellType != CellType.Doctor && cell.CellType != CellType.Immortal)
				{
					string? diseaseFound = null;
					string? plagueFound = null;
					foreach (var cond in cell.Conditions)
					{
						if (cond.StartsWith("d_"))
						{ diseaseFound = cond; break; }
						if (cond.StartsWith("p_"))
						{ plagueFound = cond; break; }
					}
					if (plagueFound != null)
						CellGrid[c, r] = Cell_Diseased.Infect(CellGrid[c, r], plagueFound, CellType.Plague);
					else if (diseaseFound != null)
						CellGrid[c, r] = Cell_Diseased.Infect(CellGrid[c, r], diseaseFound, CellType.Diseased);
				}

				if (cell.Conditions.Contains("mature"))
					cell.Breed(CellGrid);

				if (cell.Conditions.Contains("immaculate"))
					cell.Immaculate(CellGrid);

				if (cell.IsAlive && cell.CellType == CellType.Explorer &&
						(c == 0 || c == _columns - 1 || r == 0 || r == _rows - 1))
					needResize = true;

				if (cell.IsAlive && cell.Age >= 1 && cell.Nationality < 0)
					cell.Nationality = SimRandom.Range(0, Nations.Count > 0 ? Nations.Count : 1);

				if (cell.IsAlive && cell.CellType == CellType.Basic && cell.Conditions.Contains("toWar"))
				{
					CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Warrior, true);
					CellGrid[c, r].Conditions.Remove("toWar");
				}

				if (cell.CellType == CellType.Warrior && cell.IdleTurns >= 3)
					CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Basic, cell.IsAlive);

				if (cell.CellType == CellType.Hunter && cell.IdleTurns >= 8)
					CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Basic, cell.IsAlive);
			}
		}

		if (needResize && !IsMaxGrid())
			ResizeCellGrid();
	}

	/// <summary>
	/// Calls <see cref="Cell.SpecialActions"/> on every cell.
	/// This is where movement, combat, and disease spread happen.
	/// Neighbourhoods must have been rebuilt immediately before this call.
	/// </summary>
	public void UpdateSpecialActions()
	{
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
				CellGrid[c, r].SpecialActions(CellGrid, PendingMoves);
	}

	/// <summary>
	/// If the population density has fallen below <see cref="SimulationSettings.MinLifePercent"/>,
	/// injects a batch of new randomly generated cells into empty, not-yet-alive slots.
	/// The batch size matches the configured <see cref="SimulationSettings.PopValue"/> (or the
	/// equivalent cell count for percent mode).
	/// </summary>
	public void AddRandomLife()
	{
		float totalCells = _columns * _rows;
		if (totalCells == 0)
			return;
		if (_currentPopulation / totalCells > _settings.MinLifePercent)
			return;

		int numNew;
		if (_settings.PopMode == PopMode.Percent)
			numNew = (int)(totalCells * _settings.PopValue / 100f);
		else
			numNew = _settings.PopValue;

		int added = 0;
		int attempts = 0;
		int maxAttempts = numNew * 10;

		while (added < numNew && attempts < maxAttempts)
		{
			int rc = SimRandom.Range(0, _columns);
			int rr = SimRandom.Range(0, _rows);
			if (!CellGrid[rc, rr].IsAlive && !AliveNextGenGrid[rc, rr])
			{
				CellGrid[rc, rr] = _generator.InitializeRandomCell(rc, rr);
				added++;
			}
			attempts++;
		}
	}

	/// <summary>
	/// Runs <see cref="Cell_Nation.Census"/> for each existing nation, fires
	/// king-crowned and king-fallen events into <see cref="PendingEvents"/>,
	/// then creates new nation slots if the current population supports more
	/// nations than currently exist (up to <see cref="SimulationSettings.MaxNations"/>).
	/// </summary>
	public void UpdateNations()
	{
		// Snapshot king references before census so we can detect changes.
		// By this point UpdateCellLives() has already run, so King.IsAlive
		// reflects whether the king survived this step.
		var prevKings = Nations.ToDictionary(kv => kv.Key, kv => kv.Value.King);
		var prevCounts = Nations.ToDictionary(kv => kv.Key, kv => kv.Value.CitizensList.Count);

		foreach (var nation in Nations.Values)
			nation.Census(CellGrid);

		foreach (var kv in Nations)
		{
			prevKings.TryGetValue(kv.Key, out var oldKing);
			prevCounts.TryGetValue(kv.Key, out var oldCount);
			var newKing = kv.Value.King;

			if (oldKing != null && !oldKing.IsAlive)
				PendingEvents.Add($"king_fallen:Nation {kv.Key}: The King has fallen!");

			if (newKing != null && newKing != oldKing)
				PendingEvents.Add($"king_crowned:Nation {kv.Key}: A new King is crowned!");

			if (kv.Value.CitizensList.Count == 0 && oldCount > 0)
				PendingEvents.Add($"kingdom_destroyed:Nation {kv.Key}: Kingdom destroyed!");
		}

		float basePct = _settings.BasePercentLiving;
		float numNations = basePct * _columns * _rows / _settings.MinCellsPerNation;
		int cap = Math.Min(_settings.MaxNations, Cell_Nation.NationColors.Count);
		int target = (int)Math.Min(numNations, cap);
		for (int i = Nations.Count; i < target; i++)
			Nations[i] = new Cell_Nation(i);
	}

	// ── Private helpers ───────────────────────────────────────────────────────────

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

	/// <summary>
	/// Derives the initial number of nation slots from the living-cell budget and
	/// <see cref="SimulationSettings.MinCellsPerNation"/>, capped at <see cref="SimulationSettings.MaxNations"/>.
	/// </summary>
	private void InitializeNations()
	{
		Nations = new Dictionary<int, Cell_Nation>();
		float basePct = _settings.BasePercentLiving;
		float numNations = basePct * _columns * _rows / _settings.MinCellsPerNation;
		int cap = Math.Min(_settings.MaxNations, Cell_Nation.NationColors.Count);
		int count = (int)Math.Min(numNations, cap);
		for (int i = 0; i < count; i++)
			Nations[i] = new Cell_Nation(i);
	}

	/// <summary>
	/// Grows the grid by one cell on all four sides, filling the new border with dead Basic cells
	/// and shifting all existing cells inward by one position.
	/// Rebuilds all neighbourhoods and the alive-next-gen scratch grid after resizing.
	/// Called when an Explorer reaches a grid edge and the grid has not yet reached maximum size.
	/// </summary>
	private void ResizeCellGrid()
	{
		var old = CellGrid;
		int newCols = _columns + 2;
		int newRows = _rows + 2;
		var newGrid = new Cell[newCols, newRows];

		for (int c = 0; c < newCols; c++)
		{
			for (int r = 0; r < newRows; r++)
			{
				if (c == 0 || c == newCols - 1 || r == 0 || r == newRows - 1)
				{
					newGrid[c, r] = new Cell_Basic(c, r, false);
				}
				else
				{
					newGrid[c, r] = old[c - 1, r - 1];
					newGrid[c, r].Column = c;
					newGrid[c, r].Row = r;
				}
			}
		}

		_columns = newCols;
		_rows = newRows;
		CellGrid = newGrid;
		AliveNextGenGrid = new bool[_columns, _rows];
		UpdateNeighborhoodsGrid();
	}

	/// <summary>
	/// Returns <c>true</c> if either grid dimension has reached or exceeded
	/// <see cref="SimulationSettings.MaxGridSize"/>.  A MaxGridSize of 0 means unlimited.
	/// </summary>
	private bool IsMaxGrid()
	{
		int limit = _settings.MaxGridSize;
		if (limit <= 0)
			return false;
		return _columns >= limit || _rows >= limit;
	}
}
