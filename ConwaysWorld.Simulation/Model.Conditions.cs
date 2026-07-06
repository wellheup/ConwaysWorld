namespace ConwaysWorld.Simulation;

public partial class Model
{
	// ── Condition tag processing ───────────────────────────────────────────────────

	/// <summary>
	/// Processes all condition tags on every cell in a single pass:
	/// <list type="bullet">
	///   <item><c>"cleanup"</c> — replaces the slot with a dead Basic cell.</item>
	///   <item><c>"immune"</c> — strips all active disease tags.</item>
	///   <item><c>"d_"</c> / <c>"p_"</c> tags — converts the cell into Diseased/Plague.</item>
	///   <item><c>"mature"</c> — triggers <see cref="Cell.Breed"/>.</item>
	///   <item><c>"immaculate"</c> — triggers <see cref="Cell.Immaculate"/>.</item>
	///   <item>Explorer at grid edge — schedules a <see cref="ResizeCellGrid"/> call.</item>
	///   <item>Unaffiliated living cell (age ≥ 1) — assigns a random nation.</item>
	///   <item><c>"toWar"</c> on a Basic cell — promotes it to Warrior.</item>
	///   <item>Hunter/Warrior with excessive IdleTurns — demotes to Basic.</item>
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

				if (cell.CellType == CellType.Irradiated)
					continue;

				if (cell.Conditions.Contains("cleanup"))
				{
					CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Basic, false);
					continue;
				}

				if (cell.Conditions.Contains("immune"))
					cell.Conditions.RemoveWhere(s => s.StartsWith("d_") || s.StartsWith("p_") || s.StartsWith("r_"));

				// Pure Conway mode: skip all condition processing beyond cleanup and immune clearing.
				// Breeding, disease conversion, immaculate, toWar promotion, nation assignment, and
				// idle-demotion are all suppressed so only the Conway birth/death rules run.
				if (_settings.PureConwayMode)
					continue;

				bool isUndeadType = cell.CellType == CellType.Zombie
						|| cell.CellType == CellType.Necromancer
						|| cell.CellType == CellType.Bomber;

				if (!isUndeadType)
				{
					// mutate_ condition supersedes all other changes — replace cell and skip rest.
					string? mutateTag = null;
					foreach (var cond in cell.Conditions)
					{
						if (cond.StartsWith("mutate_"))
						{ mutateTag = cond; break; }
					}
					if (mutateTag != null && cell.IsAlive && cell.CellType != CellType.Irradiated)
					{
						if (int.TryParse(mutateTag.Substring(7), out int typeInt))
						{
							var targetType = (CellType)typeInt;
							CellGrid[c, r] = Cell.ReplaceCell(cell, targetType, true);
							CellGrid[c, r].Conditions.RemoveWhere(s => s.StartsWith("mutate_"));
						}
						continue;
					}

					if (cell.CellType != CellType.Doctor && cell.CellType != CellType.Immortal)
					{
						string? diseaseFound = null;
						string? plagueFound = null;
						foreach (var cond in cell.Conditions)
						{
							if (cond.StartsWith("d_"))
							{ diseaseFound = cond; break; }
							// p_ = Plague cell strain; r_ = PlagueRat strain (both convert to Plague)
							if (cond.StartsWith("p_") || cond.StartsWith("r_"))
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
				}

				if (cell.IsAlive && cell.CellType == CellType.Explorer &&
						(c == 0 || c == _columns - 1 || r == 0 || r == _rows - 1) &&
						_settings.AllowGridExpansion)
					needResize = true;

				// Basic-cell king-distance neutralisation.
				if (cell.IsAlive && cell.CellType == CellType.Basic && cell.Nationality >= 0)
				{
					if (Nations.TryGetValue(cell.Nationality, out var cellNation)
							&& cellNation.King != null && cellNation.King.IsAlive)
					{
						int threshold = (_columns + _rows) / 3;
						int dc = Math.Abs(cell.Column - cellNation.King.Column);
						int dr = Math.Abs(cell.Row - cellNation.King.Row);
						if (dc + dr > threshold)
						{
							cell.Nationality = -1;
							cell.Conditions.Add("neutral_cooldown:3");
						}
					}
				}

				// Tick neutral cooldown for Basic cells; suppress nation-join while active.
				if (cell.IsAlive && cell.CellType == CellType.Basic)
				{
					string? cooldownTag = null;
					foreach (var cond in cell.Conditions)
						if (cond.StartsWith("neutral_cooldown:"))
						{ cooldownTag = cond; break; }
					if (cooldownTag != null)
					{
						cell.Conditions.Remove(cooldownTag);
						int colonIdx = cooldownTag.IndexOf(':');
						int remaining = int.Parse(cooldownTag.AsSpan(colonIdx + 1)) - 1;
						if (remaining > 0)
							cell.Conditions.Add($"neutral_cooldown:{remaining}");
					}
				}

				// Nation-join: nationless living cell scans within 3 Chebyshev tiles.
				bool hasCooldown = false;
				foreach (var cond in cell.Conditions)
					if (cond.StartsWith("neutral_cooldown:"))
					{ hasCooldown = true; break; }

				// Nationless-by-design types never join a nation via proximity.
				bool isNationlessType = cell.CellType == CellType.Islander
						|| cell.CellType == CellType.Barbarian
						|| cell.CellType == CellType.Wayfinder
						|| cell.CellType == CellType.PlagueRat
						|| cell.CellType == CellType.Zombie
						|| cell.CellType == CellType.Necromancer
						|| cell.CellType == CellType.Bomber;

				if (cell.IsAlive && cell.Age >= 1 && cell.Nationality < 0
						&& !hasCooldown && !isNationlessType && _settings.NationsEnabled)
				{
					_nationJoinScratch.Clear();
					int c3lo = Math.Max(0, c - 3), c3hi = Math.Min(_columns - 1, c + 3);
					int r3lo = Math.Max(0, r - 3), r3hi = Math.Min(_rows - 1, r + 3);
					for (int nc = c3lo; nc <= c3hi; nc++)
						for (int nr = r3lo; nr <= r3hi; nr++)
						{
							if (nc == c && nr == r)
								continue;
							var n = CellGrid[nc, nr];
							if (n.IsAlive && n.Nationality >= 0 && !_nationJoinScratch.Contains(n.Nationality))
								_nationJoinScratch.Add(n.Nationality);
						}
					if (_nationJoinScratch.Count > 0)
						cell.Nationality = _nationJoinScratch[SimRandom.Range(0, _nationJoinScratch.Count)];
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

	// ── Special actions ───────────────────────────────────────────────────────────

	/// <summary>
	/// Calls <see cref="Cell.SpecialActions"/> on every cell.
	/// Neighbourhoods must have been rebuilt immediately before this call.
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

		CheckKingDuels();
		CheckRevolutionaryAssassinations();
	}

	// ── Selected-cell observation ─────────────────────────────────────────────────

	private bool _selectedWasAlive;
	private CellType _selectedType;
	private int _selectedNat;
	private int _selectedLivingNeighbors;
	private HashSet<string> _selectedConditionsBefore = new();

	/// <summary>
	/// Captures the state of the selected cell at the start of the step so we can
	/// compare after all actions have run and describe what changed.
	/// </summary>
	private void EmitSelectedCellSnapshot()
	{
		if (SelectedCol < 0 || SelectedRow < 0 || SelectedCol >= _columns || SelectedRow >= _rows)
			return;

		var cell = CellGrid[SelectedCol, SelectedRow];
		_selectedWasAlive = cell.IsAlive;
		_selectedType = cell.CellType;
		_selectedNat = cell.Nationality;
		_selectedConditionsBefore = new HashSet<string>(cell.Conditions);
		_selectedLivingNeighbors = cell.CellNeighborhood.NeighborhoodDict
				.Where(kv => kv.Key != "center" && kv.Value.IsAlive)
				.Count();
	}

	/// <summary>
	/// After the step, compares current cell state to the snapshot and emits
	/// human-readable <c>selected_cell:</c> events for any notable change.
	/// </summary>
	private void EmitSelectedCellOutcome()
	{
		if (SelectedCol < 0 || SelectedRow < 0 || SelectedCol >= _columns || SelectedRow >= _rows)
			return;

		var cell = CellGrid[SelectedCol, SelectedRow];
		string typeName = _selectedType.ToString();

		if (_selectedWasAlive && !cell.IsAlive)
		{
			string reason;
			if (_selectedConditionsBefore.Contains("cleanup"))
				reason = "replaced/converted";
			else if (_selectedConditionsBefore.Any(s => s.StartsWith("d_")))
				reason = "disease killed it";
			else if (_selectedConditionsBefore.Any(s => s.StartsWith("p_")))
				reason = "plague killed it";
			else if (_selectedConditionsBefore.Any(s => s.StartsWith("r_")))
				reason = "plague-rat strain killed it";
			else if (_selectedType == CellType.Diseased || _selectedType == CellType.Plague)
				reason = "disease countdown expired";
			else if (_selectedType == CellType.Bomber)
				reason = "bomber detonated";
			else if (_selectedType == CellType.Traveler || _selectedType == CellType.Explorer)
				reason = "isolation or crush";
			else if (_selectedType == CellType.Warrior || _selectedType == CellType.Hunter)
				reason = "combat or idle demotion";
			else if (_selectedLivingNeighbors < _settings.MinLivingNeighbors)
				reason = $"underpopulation ({_selectedLivingNeighbors} neighbors)";
			else if (_selectedLivingNeighbors > _settings.MaxLivingNeighbors)
				reason = $"overpopulation ({_selectedLivingNeighbors} neighbors)";
			else
				reason = "cause unknown";
			PendingEvents.Add($"selected_cell:[{typeName} @{SelectedCol},{SelectedRow}] died — {reason}");
			return;
		}

		if (!_selectedWasAlive && cell.IsAlive)
		{
			PendingEvents.Add($"selected_cell:[{cell.CellType} @{SelectedCol},{SelectedRow}] born/spawned this step");
			return;
		}

		if (!_selectedWasAlive)
			return;

		if (cell.CellType != _selectedType)
			PendingEvents.Add($"selected_cell:[{typeName} @{SelectedCol},{SelectedRow}] changed → {cell.CellType}");

		if (cell.Nationality != _selectedNat)
		{
			string from = _selectedNat < 0 ? "none" : _selectedNat.ToString();
			string to = cell.Nationality < 0 ? "none" : cell.Nationality.ToString();
			PendingEvents.Add($"selected_cell:[{cell.CellType} @{SelectedCol},{SelectedRow}] nation {from} → {to}");
		}

		foreach (var cond in cell.Conditions)
			if (!_selectedConditionsBefore.Contains(cond))
				PendingEvents.Add($"selected_cell:[{cell.CellType} @{SelectedCol},{SelectedRow}] gained condition: {cond}");

		foreach (var cond in _selectedConditionsBefore)
			if (!cell.Conditions.Contains(cond))
				PendingEvents.Add($"selected_cell:[{cell.CellType} @{SelectedCol},{SelectedRow}] lost condition: {cond}");
	}

	// ── Revolutionary vs King ─────────────────────────────────────────────────────

	/// <summary>
	/// If any living Revolutionary is Chebyshev-1 adjacent to a living King of a
	/// different nation, the King dies; the Revolutionary survives (it is its victory).
	/// </summary>
	private void CheckRevolutionaryAssassinations()
	{
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (!cell.IsAlive || cell.CellType != CellType.Revolutionary)
					continue;

				foreach (var neighbor in cell.CellNeighborhood.NeighborhoodDict.Values)
				{
					if (!neighbor.IsAlive || neighbor.CellType != CellType.King)
						continue;
					if (neighbor.Nationality == cell.Nationality)
						continue;

					neighbor.Die();
					neighbor.Conditions.Add("cleanup");
					PendingEvents.Add(
							$"revolution_start:Revolutionary of Nation {cell.Nationality} " +
							$"assassinated the King of Nation {neighbor.Nationality}!");
				}
			}
	}

	/// <summary>
	/// Scans every living King and checks whether any other living King from a different
	/// nation occupies one of its 8 immediate neighbours.  When two such Kings are found
	/// they both die and a <c>regicide_duel</c> event is logged.
	/// </summary>
	private void CheckKingDuels()
	{
		var kings = new List<Cell>();
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (cell.IsAlive && cell.CellType == CellType.King)
					kings.Add(cell);
			}

		if (kings.Count < 2)
			return;

		var duelled = new HashSet<Cell>();
		for (int i = 0; i < kings.Count; i++)
		{
			var a = kings[i];
			if (!a.IsAlive || duelled.Contains(a))
				continue;

			for (int j = i + 1; j < kings.Count; j++)
			{
				var b = kings[j];
				if (!b.IsAlive || duelled.Contains(b))
					continue;
				if (a.Nationality == b.Nationality)
					continue;

				int dc = Math.Abs(a.Column - b.Column);
				int dr = Math.Abs(a.Row - b.Row);
				if (dc > 1 || dr > 1)
					continue;

				a.Die();
				a.Conditions.Add("cleanup");
				b.Die();
				b.Conditions.Add("cleanup");
				duelled.Add(a);
				duelled.Add(b);
				PendingEvents.Add(
						$"regicide_duel:Kings of Nations {a.Nationality} and {b.Nationality} " +
						$"met in single combat — both fell!");
			}
		}
	}
}
