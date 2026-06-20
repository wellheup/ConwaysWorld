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
		_famineActive = false;
		_famineDurationCount = 0;
		_stepsSinceLastFamineEnd = 0;
		_floodActive = false;
		_floodCooldownCount = 0;
		_floodTriggerAt = 100 + SimRandom.Range(50, 101);
		PopulateGrid();
	}

	/// <summary>
	/// Allocates fresh grids, fills them with dead cells, then places living cells in
	/// clusters.  Each cluster grows outward ring by ring from a random seed point,
	/// filling at most 75 % of each ring's slots.  The total living budget comes from
	/// <see cref="SimulationSettings.BasePercentLiving"/>.
	/// </summary>
	public void PopulateGrid()
	{
		CellGrid = new Cell[_columns, _rows];
		AliveNextGenGrid = new bool[_columns, _rows];

		// Seed every slot with a dead Basic cell first.
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
				CellGrid[c, r] = new Cell_Basic(c, r, false);

		int totalCells = _columns * _rows;
		int livingBudget = (int)(totalCells * _settings.BasePercentLiving);

		if (livingBudget < 1)
			return;   // nothing to spawn — grid too large for this population setting

		int clusterCount = _settings.StartClusters > 0
																		? Math.Clamp(_settings.StartClusters, 1, livingBudget)
																		: Math.Max(1, _settings.MaxNations / 4);

		// Choose cluster seed points (unique grid positions).
		var seeds = new List<(int c, int r)>();
		int seedAttempts = 0;
		while (seeds.Count < clusterCount && seedAttempts < clusterCount * 20)
		{
			seedAttempts++;
			int sc = SimRandom.Range(0, _columns);
			int sr = SimRandom.Range(0, _rows);
			if (!seeds.Contains((sc, sr)))
				seeds.Add((sc, sr));
		}

		// Distribute the living budget evenly across clusters.
		int perCluster = livingBudget / seeds.Count;
		int remainder = livingBudget - perCluster * seeds.Count;

		for (int si = 0; si < seeds.Count; si++)
		{
			int budget = perCluster + (si < remainder ? 1 : 0);
			SpawnCluster(seeds[si].c, seeds[si].r, budget);
		}

		InitializeNations();
		UpdateNeighborhoodsGrid();
		CountInitialPopulation();
		UpdateNations();
	}

	/// <summary>
	/// Places up to <paramref name="budget"/> living cells outward from
	/// (<paramref name="seedCol"/>, <paramref name="seedRow"/>) in concentric
	/// Chebyshev rings.  Each ring is capped at 75 % fill so neighbour clusters
	/// can bleed in without overlap.
	/// </summary>
	private void SpawnCluster(int seedCol, int seedRow, int budget)
	{
		if (budget <= 0)
			return;

		int placed = 0;

		// Seed cell itself.
		if (!CellGrid[seedCol, seedRow].IsAlive)
		{
			CellGrid[seedCol, seedRow] = _generator.InitializeLivingCell(seedCol, seedRow);
			placed++;
		}

		for (int radius = 1; placed < budget && radius < Math.Max(_columns, _rows); radius++)
		{
			// Collect all empty slots in this Chebyshev ring.
			var ring = new List<(int c, int r)>();
			for (int dc = -radius; dc <= radius; dc++)
			{
				for (int dr = -radius; dr <= radius; dr++)
				{
					if (Math.Abs(dc) != radius && Math.Abs(dr) != radius)
						continue; // inner rings already handled
					int tc = seedCol + dc;
					int tr = seedRow + dr;
					if (tc < 0 || tc >= _columns || tr < 0 || tr >= _rows)
						continue;
					if (!CellGrid[tc, tr].IsAlive)
						ring.Add((tc, tr));
				}
			}

			if (ring.Count == 0)
				continue;

			// Shuffle ring so selection is random.
			for (int i = ring.Count - 1; i > 0; i--)
			{
				int j = SimRandom.Range(0, i + 1);
				(ring[i], ring[j]) = (ring[j], ring[i]);
			}

			int ringCap = (int)Math.Ceiling(ring.Count * 0.75);
			int toPlace = Math.Min(ringCap, budget - placed);

			for (int i = 0; i < toPlace; i++)
			{
				var (tc, tr) = ring[i];
				CellGrid[tc, tr] = _generator.InitializeLivingCell(tc, tr);
				placed++;
			}
		}
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
		UpdateFamine();
		UpdateFlood();
		ApplyCellNeighborRules();
		UpdateNeighborhoodsGrid();
		UpdateAliveNextGenGrid();
		UpdateCellLives();
		UpdateCellConditions();
		UpdateNeighborhoodsGrid();
		UpdateSpecialActions();
		if (_settings.RandomLifeEnabled)
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

				// Nation-join: nationless living cell scans within 3 Chebyshev tiles.
				if (cell.IsAlive && cell.Age >= 1 && cell.Nationality < 0)
				{
					var nearbyNations = new List<int>();
					int c3lo = Math.Max(0, c - 3), c3hi = Math.Min(_columns - 1, c + 3);
					int r3lo = Math.Max(0, r - 3), r3hi = Math.Min(_rows - 1, r + 3);
					for (int nc = c3lo; nc <= c3hi; nc++)
						for (int nr = r3lo; nr <= r3hi; nr++)
						{
							if (nc == c && nr == r)
								continue;
							var n = CellGrid[nc, nr];
							if (n.IsAlive && n.Nationality >= 0 && !nearbyNations.Contains(n.Nationality))
								nearbyNations.Add(n.Nationality);
						}
					if (nearbyNations.Count > 0)
						cell.Nationality = nearbyNations[SimRandom.Range(0, nearbyNations.Count)];
				}

				if (cell.IsAlive && cell.CellType == CellType.Basic && cell.Conditions.Contains("toWar"))
				{
					CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Warrior, true);
					CellGrid[c, r].Conditions.Remove("toWar");
				}

				if (cell.IsAlive && cell.Conditions.Contains("toRebel"))
				{
					CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Rebel, true);
					CellGrid[c, r].Conditions.Remove("toRebel");
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
	/// <para>
	/// Before iterating, each cell's <see cref="Cell.StepStartColumn"/> and
	/// <see cref="Cell.StepStartRow"/> are snapshotted from its current grid position
	/// so that move records always reflect where the cell began the step, not where a
	/// prior swap may have displaced it.
	/// </para>
	/// </summary>
	public void UpdateSpecialActions()
	{
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				CellGrid[c, r].StepStartColumn = c;
				CellGrid[c, r].StepStartRow = r;
			}

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
				// Bombers in isolation just alternate spawn/die; require a live neighbour.
				if (CellGrid[rc, rr].CellType == CellType.Bomber && !HasLivingNeighbor(rc, rr))
					CellGrid[rc, rr] = new Cell_Basic(rc, rr, true);
				added++;
			}
			attempts++;
		}
	}

	/// <summary>
	/// Runs <see cref="Cell_Nation.Census"/> for each existing nation, fires
	/// king-crowned and king-fallen events into <see cref="PendingEvents"/>,
	/// then checks for groups of nationless cells large enough to form a new nation
	/// (see <see cref="FormNationsFromNationlessClusters"/>).
	/// </summary>
	public void UpdateNations()
	{
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

		FormNationsFromNationlessClusters();
		CheckRevolution();
	}

	/// <summary>
	/// Scans the grid for connected groups of nationless living cells.
	/// Connectivity is Chebyshev-3: each cell in a group must be within 3 tiles
	/// (Chebyshev) of at least one other cell already in the group.
	/// Any group with at least <see cref="SimulationSettings.NationFormThreshold"/> cells
	/// is assigned a new nation index and an immediate King is elected at random,
	/// provided the nation cap has not been reached.
	/// </summary>
	private void FormNationsFromNationlessClusters()
	{
		int cap = Math.Min(_settings.MaxNations, Cell_Nation.NationColors.Count);
		if (Nations.Count >= cap)
			return;

		// Collect all nationless living cells.
		var unaffiliated = new List<Cell>();
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (cell.IsAlive && cell.Nationality < 0)
					unaffiliated.Add(cell);
			}

		if (unaffiliated.Count == 0)
			return;

		// BFS/union-find: group cells whose Chebyshev-3 neighbourhoods overlap.
		var visited = new HashSet<Cell>();
		var groups = new List<List<Cell>>();

		foreach (var seed in unaffiliated)
		{
			if (visited.Contains(seed))
				continue;

			var group = new List<Cell>();
			var queue = new Queue<Cell>();
			queue.Enqueue(seed);
			visited.Add(seed);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				group.Add(current);

				int clo = Math.Max(0, current.Column - 3);
				int chi = Math.Min(_columns - 1, current.Column + 3);
				int rlo = Math.Max(0, current.Row - 3);
				int rhi = Math.Min(_rows - 1, current.Row + 3);

				for (int nc = clo; nc <= chi; nc++)
					for (int nr = rlo; nr <= rhi; nr++)
					{
						var neighbor = CellGrid[nc, nr];
						if (!visited.Contains(neighbor) &&
														neighbor.IsAlive &&
														neighbor.Nationality < 0)
						{
							visited.Add(neighbor);
							queue.Enqueue(neighbor);
						}
					}
			}

			groups.Add(group);
		}

		// For each qualifying group, assign a new nation and elect a King.
		int threshold = _settings.NationFormThreshold;
		foreach (var group in groups)
		{
			if (group.Count < threshold)
				continue;
			if (Nations.Count >= cap)
				break;

			// Find the next free nation index.
			int newNat = 0;
			while (Nations.ContainsKey(newNat))
				newNat++;

			var nation = new Cell_Nation(newNat);
			Nations[newNat] = nation;

			foreach (var cell in group)
				cell.Nationality = newNat;

			// Run census to populate CitizensList, then crown a King.
			nation.Census(CellGrid);

			PendingEvents.Add($"king_crowned:Nation {newNat} has formed!");
		}
	}

	/// <summary>
	/// Checks whether any nation dominates by holding at least twice the citizens of the
	/// second-largest nation.  If so, a random non-King citizen of the dominant nation is
	/// promoted to <see cref="CellType.Revolutionary"/>:
	/// <list type="bullet">
	///   <item>If a new nation slot is available, the Revolutionary founds a brand-new nation.</item>
	///   <item>Otherwise, it defects to the second-largest nation as a <see cref="CellType.Warrior"/>.</item>
	/// </list>
	/// Called at the end of <see cref="UpdateNations"/> each step.
	/// </summary>
	private void CheckRevolution()
	{
		if (Nations.Count < 2)
			return;

		var sorted = Nations.Values
																																																																		.Where(n => n.CitizensList.Count > 0)
																																																																		.OrderByDescending(n => n.CitizensList.Count)
																																																																		.ToList();

		if (sorted.Count < 2)
			return;

		var dominant = sorted[0];
		var secondLargest = sorted[1];

		if (dominant.CitizensList.Count < secondLargest.CitizensList.Count * 2)
			return;

		if (dominant.CitizensList.Any(c => c.CellType == CellType.Revolutionary))
			return;

		var candidates = dominant.CitizensList
																																																																		.Where(c => c != dominant.King &&
																																																																																																																																																																		c.CellType != CellType.Warrior &&
																																																																																																																																																																		c.CellType != CellType.Diplomat &&
																																																																																																																																																																		c.CellType != CellType.Revolutionary &&
																																																																																																																																																																		c.CellType != CellType.Rebel &&
																																																																																																																																																																		c.IsAlive)
																																																																		.ToList();

		if (candidates.Count == 0)
			return;

		var chosen = candidates[SimRandom.Range(0, candidates.Count)];
		int oldNation = dominant.NationNum;

		int cap = Math.Min(_settings.MaxNations, Cell_Nation.NationColors.Count);
		bool canCreateNation = Nations.Count < cap;

		if (canCreateNation)
		{
			int newNationNum = 0;
			while (Nations.ContainsKey(newNationNum))
				newNationNum++;
			Nations[newNationNum] = new Cell_Nation(newNationNum);

			var rev = (Cell_Revolutionary)Cell.ReplaceCell(chosen, CellType.Revolutionary, true);
			rev.Nationality = newNationNum;
			rev.OldNationality = oldNation;
			CellGrid[rev.Column, rev.Row] = rev;

			PendingEvents.Add($"revolution_start:Nation {oldNation} splinters! A Revolutionary founds Nation {newNationNum}!");
		}
		else
		{
			var warrior = Cell.ReplaceCell(chosen, CellType.Warrior, true);
			warrior.Nationality = secondLargest.NationNum;
			CellGrid[warrior.Column, warrior.Row] = warrior;

			PendingEvents.Add($"revolution_start:Nation {oldNation}: A defector joins Nation {secondLargest.NationNum}!");
		}
	}

	// ── Cell neighbor rule application ───────────────────────────────────────────

	/// <summary>
	/// Applies the base Conway survival constraints (<see cref="SimulationSettings.MinLivingNeighbors"/>
	/// and <see cref="SimulationSettings.MaxLivingNeighbors"/>) to every cell.
	/// Must be called before <see cref="UpdateAliveNextGenGrid"/>.
	/// </summary>
	private void ApplyCellNeighborRules()
	{
		int baseMin = _settings.MinLivingNeighbors;
		int baseMax = _settings.MaxLivingNeighbors;
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				CellGrid[c, r].MinLivingNeighbors = baseMin;
				CellGrid[c, r].MaxLivingNeighbors = baseMax;
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

	/// <summary>
	/// Resets the nation registry to empty.  Nations form organically during
	/// <see cref="UpdateNations"/> via <see cref="FormNationsFromNationlessClusters"/>.
	/// </summary>
	private void InitializeNations()
	{
		Nations = new Dictionary<int, Cell_Nation>();
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
