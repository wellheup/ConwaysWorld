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
	/// While active, kills a random 5 % of the living population each step.
	/// Ends after <see cref="SimulationSettings.FamineDuration"/> steps; then a
	/// cooldown of <see cref="SimulationSettings.FamineCooldown"/> steps must pass
	/// before a new famine can roll (10 % chance per step after cooldown).
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
	/// Kills a random 5 % of the current living population.
	/// Called once per step while a famine is active.
	/// Killed cells are marked <c>"cleanup"</c> so they are replaced with dead Basic
	/// cells during <see cref="UpdateCellConditions"/>.
	/// </summary>
	private void ApplyFamineStep()
	{
		var living = new List<Cell>();
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
				if (CellGrid[c, r].IsAlive)
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

	// ── Flood ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Advances the Flood world-event state machine by one step.
	/// When no flood is active the cooldown counter increments; once it reaches
	/// <c>_floodTriggerAt</c> (100 + random 50–100 extra steps) the flood begins.
	/// While active, <see cref="ApplyFloodStep"/> kills the cell farthest from each
	/// nation's King (one cell per nation per step). The flood ends as soon as
	/// <see cref="AreAnyNationsAdjacent"/> returns <c>false</c>.
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
			if (!AreAnyNationsAdjacent())
			{
				_floodActive = false;
				_floodCooldownCount = 0;
				_floodTriggerAt = 100 + SimRandom.Range(50, 101);
				PendingEvents.Add("flood_end:The flood recedes. Nations are separated.");
				return;
			}
			ApplyFloodStep();
		}
		else
		{
			_floodCooldownCount++;
			if (_floodCooldownCount >= _floodTriggerAt)
			{
				_floodActive = true;
				_floodCooldownCount = 0;
				_floodTriggerAt = 100 + SimRandom.Range(50, 101);
				PendingEvents.Add("flood_start:The flood rises! Nations are being pushed apart.");
			}
		}
	}

	/// <summary>
	/// Kills the living cell that is farthest (by Manhattan distance) from its nation's
	/// King in each active nation. One cell per nation is killed per call.
	/// If a nation has no King, the centroid of its citizens is used as the reference point.
	/// Killed cells receive the <c>"cleanup"</c> condition so they are replaced with dead
	/// Basic cells during <see cref="UpdateCellConditions"/>.
	/// </summary>
	private void ApplyFloodStep()
	{
		foreach (var nation in Nations.Values)
		{
			var citizens = nation.CitizensList.Where(c => c.IsAlive).ToList();
			if (citizens.Count == 0)
				continue;

			float refCol, refRow;
			var king = nation.King;
			if (king != null && king.IsAlive)
			{
				refCol = king.Column;
				refRow = king.Row;
			}
			else
			{
				refCol = (float)citizens.Average(c => c.Column);
				refRow = (float)citizens.Average(c => c.Row);
			}

			var target = citizens
				.OrderByDescending(c => Math.Abs(c.Column - refCol) + Math.Abs(c.Row - refRow))
				.First();

			target.Die();
			target.Conditions.Add("cleanup");
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
