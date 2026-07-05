namespace ConwaysWorld.Simulation;

public partial class Model
{
	// ── Grid population ───────────────────────────────────────────────────────────

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

		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
				CellGrid[c, r] = new Cell_Basic(c, r, false);

		int totalCells = _columns * _rows;
		int livingBudget = (int)(totalCells * _settings.BasePercentLiving);

		if (livingBudget < 1)
			return;

		int clusterCount = _settings.StartClusters > 0
			? Math.Clamp(_settings.StartClusters, 1, livingBudget)
			: Math.Max(1, _settings.MaxNations / 4);

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

		int perCluster = livingBudget / seeds.Count;
		int remainder = livingBudget - perCluster * seeds.Count;

		for (int si = 0; si < seeds.Count; si++)
		{
			int budget = perCluster + (si < remainder ? 1 : 0);
			SpawnCluster(seeds[si].c, seeds[si].r, budget, nationNum: _settings.NationsEnabled ? si : -1);
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
	private void SpawnCluster(int seedCol, int seedRow, int budget, int nationNum = -1)
	{
		if (budget <= 0)
			return;

		int placed = 0;

		if (!CellGrid[seedCol, seedRow].IsAlive)
		{
			CellGrid[seedCol, seedRow] = _generator.InitializeLivingCell(seedCol, seedRow);
			if (nationNum >= 0)
				CellGrid[seedCol, seedRow].Nationality = nationNum;
			placed++;
		}

		for (int radius = 1; placed < budget && radius < Math.Max(_columns, _rows); radius++)
		{
			var ring = new List<(int c, int r)>();
			for (int dc = -radius; dc <= radius; dc++)
			{
				for (int dr = -radius; dr <= radius; dr++)
				{
					if (Math.Abs(dc) != radius && Math.Abs(dr) != radius)
						continue;
					int tc = (seedCol + dc + _columns) % _columns;
					int tr = (seedRow + dr + _rows) % _rows;
					if (!CellGrid[tc, tr].IsAlive)
						ring.Add((tc, tr));
				}
			}

			if (ring.Count == 0)
				continue;

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
				if (nationNum >= 0)
					CellGrid[tc, tr].Nationality = nationNum;
				placed++;
			}
		}
	}

	// ── Random life injection ─────────────────────────────────────────────────────

	/// <summary>
	/// If the population density has fallen below <see cref="SimulationSettings.MinLifePercent"/>,
	/// injects a batch of new randomly generated cells into empty slots.
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

		int added = 0, attempts = 0, maxAttempts = numNew * 10;
		while (added < numNew && attempts < maxAttempts)
		{
			int rc = SimRandom.Range(0, _columns);
			int rr = SimRandom.Range(0, _rows);
			if (!CellGrid[rc, rr].IsAlive && !AliveNextGenGrid[rc, rr])
			{
				CellGrid[rc, rr] = _generator.InitializeRandomCell(rc, rr);
				if (CellGrid[rc, rr].CellType == CellType.Bomber && !HasLivingNeighbor(rc, rr))
					CellGrid[rc, rr] = new Cell_Basic(rc, rr, true);
				added++;
			}
			attempts++;
		}
	}

	// ── Grid resize ───────────────────────────────────────────────────────────────

	/// <summary>
	/// Resets the nation registry to empty.  Nations form organically during
	/// <see cref="UpdateNations"/> via <see cref="FormNationsFromNationlessClusters"/>.
	/// </summary>
	private void InitializeNations()
	{
		Nations = new Dictionary<int, Cell_Nation>();
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (cell.IsAlive && cell.Nationality >= 0 && !Nations.ContainsKey(cell.Nationality))
					Nations[cell.Nationality] = new Cell_Nation(cell.Nationality);
			}
	}

	/// <summary>
	/// Grows the grid by one cell on all four sides, filling the new border with dead Basic cells
	/// and shifting all existing cells inward by one position.
	/// Rebuilds all neighbourhoods and the alive-next-gen scratch grid after resizing.
	/// </summary>
	private void ResizeCellGrid()
	{
		var old = CellGrid;
		int newCols = _columns + 2;
		int newRows = _rows + 2;
		var newGrid = new Cell[newCols, newRows];

		for (int c = 0; c < newCols; c++)
			for (int r = 0; r < newRows; r++)
			{
				if (c == 0 || c == newCols - 1 || r == 0 || r == newRows - 1)
					newGrid[c, r] = new Cell_Basic(c, r, false);
				else
				{
					newGrid[c, r] = old[c - 1, r - 1];
					newGrid[c, r].Column = c;
					newGrid[c, r].Row = r;
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

	// ── Failure detection ─────────────────────────────────────────────────────────

	/// <summary>
	/// Checks all configured failure conditions after each step and sets
	/// <see cref="FailureReason"/> if any are triggered.
	/// </summary>
	private void CheckFailureConditions()
	{
		if (FailureReason != null)
			return;

		if (_currentPopulation > _peakPopulation)
			_peakPopulation = _currentPopulation;

		int growThresh = _settings.FailurePopAfterGrowthThreshold;
		if (growThresh > 0 && _currentPopulation > growThresh)
			_grewPastAfterGrowthThreshold = true;

		if (_currentPopulation == 0)
		{
			FailureReason = "extinction:All cells have died — the simulation has gone extinct.";
			PendingEvents.Add(FailureReason);
			return;
		}

		int rawThresh = _settings.FailurePopThreshold;
		if (rawThresh > 0 && _currentPopulation <= rawThresh)
		{
			FailureReason = $"failure_pop:Population ({_currentPopulation}) fell to or below the failure threshold ({rawThresh}).";
			PendingEvents.Add(FailureReason);
			return;
		}

		if (growThresh > 0 && _grewPastAfterGrowthThreshold && _currentPopulation <= growThresh)
		{
			FailureReason = $"failure_pop_growth:Population ({_currentPopulation}) collapsed back below the growth threshold ({growThresh}).";
			PendingEvents.Add(FailureReason);
			return;
		}

		int stagnSteps = _settings.StagnationSteps;
		if (stagnSteps > 0)
		{
			if (_prevStepPopulation == _currentPopulation)
			{
				_stagnationCount++;
				if (_stagnationCount >= stagnSteps)
				{
					FailureReason = $"failure_stagnation:Population has been stuck at {_currentPopulation} for {_stagnationCount} consecutive steps.";
					PendingEvents.Add(FailureReason);
					return;
				}
			}
			else
			{
				_stagnationCount = 0;
			}
		}
		_prevStepPopulation = _currentPopulation;
	}
}
