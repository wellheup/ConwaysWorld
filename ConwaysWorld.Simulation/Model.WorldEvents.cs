namespace ConwaysWorld.Simulation;

public partial class Model
{
	// ── World-event state ─────────────────────────────────────────────────────────

	private bool _famineActive = false;
	private int _famineDurationCount = 0;
	private int _stepsSinceLastFamineEnd = 0;
	private int _famineQuadrant = 0;

	private bool _floodActive = false;
	private int _floodCooldownCount = 0;
	private int _floodTriggerAt = 100 + 75;

	private static readonly string[] QuadrantNames = { "Northwest", "Northeast", "Southwest", "Southeast" };

	// ── World-event public accessors ──────────────────────────────────────────────

	/// <summary>Whether a Famine world event is currently active.</summary>
	public bool FamineActive => _famineActive;

	/// <summary>Which grid quadrant the active famine visually affects (0=NW, 1=NE, 2=SW, 3=SE).</summary>
	public int FamineQuadrant => _famineQuadrant;

	/// <summary>Whether a Flood world event is currently active.</summary>
	public bool FloodActive => _floodActive;

	// ── Famine ────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Advances the Famine world-event state machine by one step.
	/// While active, kills a random 5 % of the living cells inside the famine quadrant each step.
	/// Ends after <see cref="SimulationSettings.FamineDuration"/> steps; then a cooldown of
	/// <see cref="SimulationSettings.FamineCooldown"/> steps must pass before a new famine can
	/// roll (10 % chance per step after cooldown).
	/// </summary>
	private void UpdateFamine()
	{
		if (!_settings.FamineEnabled)
		{
			_famineActive = false;
			return;
		}

		if (_famineActive)
		{
			ApplyFamineStep();
			_famineDurationCount++;
			if (_famineDurationCount >= _settings.FamineDuration)
			{
				_famineActive = false;
				_famineDurationCount = 0;
				_stepsSinceLastFamineEnd = 0;
				PendingEvents.Add($"famine_end:The famine in the {QuadrantNames[_famineQuadrant]} quadrant has passed.");
			}
		}
		else
		{
			_stepsSinceLastFamineEnd++;
			if (_stepsSinceLastFamineEnd >= _settings.FamineCooldown && SimRandom.Range(0, 10) == 0)
			{
				_famineActive = true;
				_famineDurationCount = 0;
				_famineQuadrant = SimRandom.Range(0, 4);
				PendingEvents.Add($"famine_start:Famine strikes the {QuadrantNames[_famineQuadrant]} quadrant!");
			}
		}
	}

	/// <summary>
	/// Kills a random 5 % of the living cells inside the active famine quadrant.
	/// Called once per step while a famine is active.
	/// Killed cells are marked <c>"cleanup"</c> so they are replaced with dead Basic
	/// cells during <see cref="UpdateCellConditions"/>.
	/// </summary>
	private void ApplyFamineStep()
	{
		int halfCols = _columns / 2;
		int halfRows = _rows / 2;

		var living = new List<Cell>();
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
				if (CellGrid[c, r].IsAlive && IsInFamineQuadrant(c, r, halfCols, halfRows))
					living.Add(CellGrid[c, r]);

		if (living.Count == 0)
			return;

		int killCount = Math.Max(1, (int)(living.Count * 0.05f));

		// Fisher-Yates partial shuffle to pick random victims.
		for (int i = living.Count - 1; i > living.Count - 1 - killCount && i > 0; i--)
		{
			int j = SimRandom.Range(0, i + 1);
			(living[i], living[j]) = (living[j], living[i]);
		}

		for (int i = living.Count - killCount; i < living.Count; i++)
		{
			living[i].Die();
			living[i].Conditions.Add("cleanup");
		}
	}

	/// <summary>Returns <c>true</c> if the cell at (<paramref name="col"/>, <paramref name="row"/>)
	/// falls within the currently active famine quadrant.</summary>
	private bool IsInFamineQuadrant(int col, int row, int halfCols, int halfRows) =>
		_famineQuadrant switch
		{
			0 => col < halfCols && row < halfRows,
			1 => col >= halfCols && row < halfRows,
			2 => col < halfCols && row >= halfRows,
			3 => col >= halfCols && row >= halfRows,
			_ => false,
		};

	// ── Flood ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Advances the Flood world-event state machine by one step.
	/// When no flood is active the cooldown counter increments until <c>_floodTriggerAt</c>
	/// (100 + random 50–100 extra steps) is reached.  While active, animation is driven
	/// externally via <see cref="DoFloodLayer"/> and <see cref="FinalizeFlood"/>; this
	/// method is a no-op until those calls complete.
	/// </summary>
	private void UpdateFlood()
	{
		if (!_settings.FloodEnabled)
		{
			_floodActive = false;
			return;
		}

		if (_floodActive)
		{
			// Layer-by-layer animation is driven externally by the UI.
			// DoFloodLayer() and FinalizeFlood() are called there; nothing to do here.
			return;
		}

		_floodCooldownCount++;
		if (_floodCooldownCount >= _floodTriggerAt)
		{
			_floodActive = true;
			_floodCooldownCount = 0;
			_floodTriggerAt = 100 + SimRandom.Range(50, 101);
			PendingEvents.Add("flood_start:The flood rises! Nations are being pushed apart.");
		}
	}

	/// <summary>
	/// Kills one contact-border layer and returns <c>true</c> if nations are still adjacent
	/// (more layers remain).  Only nations that currently have at least one inter-nation
	/// contact point are included in the kill set — nations whose touch-points have already
	/// been relieved are never touched.  Returns <c>false</c> immediately if no contacts exist.
	/// </summary>
	public bool DoFloodLayer()
	{
		if (!AreAnyNationsAdjacent())
			return false;
		ApplyFloodStep();
		return AreAnyNationsAdjacent();
	}

	/// <summary>
	/// Marks the flood as finished, resets cooldown state, and queues the
	/// <c>flood_end</c> event into <see cref="PendingEvents"/>.
	/// Call after the last <see cref="DoFloodLayer"/> returns <c>false</c>.
	/// </summary>
	public void FinalizeFlood()
	{
		_floodActive = false;
		_floodCooldownCount = 0;
		_floodTriggerAt = 100 + SimRandom.Range(50, 101);
		PendingEvents.Add("flood_end:The flood recedes. Nations are separated.");
	}

	/// <summary>
	/// Kills the current inter-nation contact border, respecting isolation:
	/// <list type="number">
	///   <item>Pass 1 — identify which nations still have at least one living cell
	///         adjacent to a cell of a different nation (<em>touching nations</em>).</item>
	///   <item>Pass 2 — collect border cells, but <em>only</em> from touching nations.
	///         Nations that are already fully isolated are never included in the kill set.</item>
	///   <item>Kill all collected border cells.</item>
	/// </list>
	/// Killed cells receive the <c>"cleanup"</c> condition so they are replaced with dead
	/// Basic cells during <see cref="UpdateCellConditions"/>.
	/// </summary>
	private void ApplyFloodStep()
	{
		// Pass 1: determine which nations still have at least one inter-nation contact.
		var touchingNations = new HashSet<int>();
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (!cell.IsAlive || cell.Nationality < 0)
					continue;
				foreach (var nb in cell.CellNeighborhood.NeighborsDict.Values)
					if (nb.IsAlive && nb.Nationality >= 0 && nb.Nationality != cell.Nationality)
					{
						touchingNations.Add(cell.Nationality);
						break;
					}
			}

		// Pass 2: collect border cells, skipping nations that are already isolated.
		// The full-grid scan naturally finds each border cell exactly once from its own
		// position, so adding the neighbour explicitly is not needed.
		var border = new HashSet<Cell>();
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (!cell.IsAlive || cell.Nationality < 0)
					continue;
				if (!touchingNations.Contains(cell.Nationality))
					continue;
				foreach (var nb in cell.CellNeighborhood.NeighborsDict.Values)
					if (nb.IsAlive && nb.Nationality >= 0 && nb.Nationality != cell.Nationality)
					{
						border.Add(cell);
						break;
					}
			}

		foreach (var cell in border)
		{
			cell.Die();
			cell.Conditions.Add("cleanup");
		}
	}

	/// <summary>
	/// Returns <c>true</c> if any two living cells of different nations share an
	/// immediately adjacent (Moore neighbourhood) slot.
	/// </summary>
	private bool AreAnyNationsAdjacent()
	{
		for (int c = 0; c < _columns; c++)
		{
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (!cell.IsAlive || cell.Nationality < 0)
					continue;
				foreach (var neighbor in cell.CellNeighborhood.NeighborsDict.Values)
				{
					if (neighbor.IsAlive && neighbor.Nationality >= 0 &&
						neighbor.Nationality != cell.Nationality)
						return true;
				}
			}
		}
		return false;
	}
}
