namespace ConwaysWorld.Simulation;

public partial class Model
{
	// ── Nation census and events ──────────────────────────────────────────────────

	/// <summary>
	/// Runs <see cref="Cell_Nation.Census"/> for each existing nation, fires
	/// king-crowned / king-fallen events, then checks for new nations forming from
	/// nationless clusters via <see cref="FormNationsFromNationlessClusters"/>.
	/// </summary>
	public void UpdateNations()
	{
		if (!_settings.NationsEnabled)
		{
			for (int c = 0; c < _columns; c++)
				for (int r = 0; r < _rows; r++)
					CellGrid[c, r].Nationality = -1;
			Nations.Clear();
			return;
		}

		foreach (var nation in Nations.Values)
		{
			nation.PreCensusKing = nation.King;
			nation.PreCensusCount = nation.CitizensList.Count;
		}

		foreach (var nation in Nations.Values)
			nation.Census(CellGrid);

		foreach (var kv in Nations)
		{
			var nation = kv.Value;
			var oldKing = nation.PreCensusKing;
			int oldCount = nation.PreCensusCount;
			var newKing = nation.King;

			if (oldKing != null && !oldKing.IsAlive)
				PendingEvents.Add($"king_fallen:Nation {kv.Key}: The King has fallen!");

			if (newKing != null && newKing != oldKing)
				PendingEvents.Add($"king_crowned:Nation {kv.Key}: A new King is crowned!");

			if (nation.CitizensList.Count == 0 && oldCount > 0)
				PendingEvents.Add($"kingdom_destroyed:Nation {kv.Key}: Kingdom destroyed!");
		}

		// King max-age and consecutive-growth revolutionary triggers.
		foreach (var nation in Nations.Values.ToList())
		{
			if (nation.King != null && nation.King.IsAlive && nation.King.Age >= Cell_King.MaxAge)
			{
				nation.AgedOutKingColumn = nation.King.Column;
				nation.AgedOutKingRow = nation.King.Row;

				if (SimRandom.Range(0, 100) < 20)
				{
					TrySpawnRevolutionaryForNation(nation);
					PendingEvents.Add($"revolution_start:Nation {nation.NationNum}: The aging King's reign inspired a Revolutionary!");
				}

				nation.King.Die();
				nation.King.Conditions.Add("cleanup");
				nation.King = null;
				PendingEvents.Add($"king_fallen:Nation {nation.NationNum}: The King has aged out — a successor will be chosen nearby!");
			}

			int growthThreshold = Cell_King.MaxAge / 2;
			if (nation.ConsecutiveGrowthSteps >= growthThreshold && nation.CitizensList.Count >= 5)
			{
				nation.ConsecutiveGrowthSteps = 0;
				TrySpawnRevolutionaryForNation(nation);
				PendingEvents.Add($"revolution_start:Nation {nation.NationNum}: Rapid growth has spawned a Revolutionary!");
			}
		}

		FormNationsFromNationlessClusters();
		CheckRevolution();

		const int KinglessGracePeriod = 8;
		foreach (var nation in Nations.Values)
		{
			if (nation.King == null)
				nation.StepsKingless++;
			else
				nation.StepsKingless = 0;
		}

		// Dissolve kingless nations that can't crown a King or have exhausted their grace window.
		var toDissolve = Nations
			.Where(kv => kv.Value.King == null &&
				(kv.Value.CitizensList.Count >= 5 || kv.Value.StepsKingless >= KinglessGracePeriod))
			.Select(kv => kv.Key)
			.ToList();
		foreach (var nat in toDissolve)
		{
			foreach (var cell in Nations[nat].CitizensList)
				cell.Nationality = -1;
			Nations.Remove(nat);
			PendingEvents.Add($"kingdom_destroyed:Nation {nat}: Too few citizens — nation dissolved.");
		}
	}

	// ── Savior tracking ───────────────────────────────────────────────────────────

	/// <summary>
	/// Returns true if any of the eight Moore neighbours at (c, r) is a living Islander.
	/// Used during Conway birth to propagate Islander identity to newborn cells.
	/// </summary>
	private bool HasIslanderNeighbour(int c, int r)
	{
		for (int dc = -1; dc <= 1; dc++)
			for (int dr = -1; dr <= 1; dr++)
			{
				if (dc == 0 && dr == 0)
					continue;
				int nc = (c + dc + _columns) % _columns;
				int nr = (r + dr + _rows) % _rows;
				var nb = CellGrid[nc, nr];
				if (nb.IsAlive && nb.CellType == CellType.Islander)
					return true;
			}
		return false;
	}

	/// <summary>Returns true if a Savior may spawn: no Savior currently alive and ≥2 nations exist.</summary>
	private bool CanSpawnSaviorNow() => ActiveSavior == null && Nations.Count >= 2;

	/// <summary>
	/// Scans the grid to find the current living Savior (if any) and update <see cref="ActiveSavior"/>.
	/// Called once per step after UpdateSpecialActions.
	/// </summary>
	private void TrackActiveSavior()
	{
		if (ActiveSavior != null && !ActiveSavior.IsAlive)
		{
			ActiveSavior.ConvertFollowersToZealots(CellGrid);
			PendingEvents.Add($"savior_fallen:The Savior has fallen! Its followers become Zealots.");
			ActiveSavior = null;
		}

		if (ActiveSavior != null)
			return;

		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (cell.IsAlive && cell.CellType == CellType.Savior && cell is Cell_Savior savior)
				{
					ActiveSavior = savior;
					return;
				}
			}
	}

	// ── Necromancer tracking ──────────────────────────────────────────────────────

	/// <summary>
	/// Maintains <see cref="_activeNecromancers"/>: discovers new Necromancers, kills their
	/// zombies on death, and permanently clears LastType on any dead zombie slot.
	/// </summary>
	private void UpdateNecromancers()
	{
		for (int i = _activeNecromancers.Count - 1; i >= 0; i--)
		{
			var necro = _activeNecromancers[i];
			if (!necro.IsAlive)
			{
				necro.KillAllZombies(CellGrid);
				_activeNecromancers.RemoveAt(i);
				PendingEvents.Add("necromancer_fallen:A Necromancer has fallen! All its zombies have been destroyed.");
			}
		}

		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				if (CellGrid[c, r] is Cell_Necromancer necro && necro.IsAlive
					&& !_activeNecromancers.Contains(necro))
					_activeNecromancers.Add(necro);
			}

		if (_activeNecromancers.Count > 0)
			foreach (var n in _activeNecromancers)
				if (n.Zombies.Count > 0)
				{ _zombiesEverExisted = true; break; }

		if (!_zombiesEverExisted)
			return;

		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (!cell.IsAlive && cell.LastType == CellType.Zombie)
					cell.LastType = null;
			}
	}

	// ── Soldier attack outcome tracking ───────────────────────────────────────────

	private HashSet<(int, int)> CollectActiveSoldierPairs()
	{
		var pairs = new HashSet<(int, int)>();
		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (cell.IsAlive && cell.CellType == CellType.Soldier && cell.Nationality >= 0
					&& cell is Cell_Soldier soldier && soldier.TargetNation >= 0)
					pairs.Add((cell.Nationality, soldier.TargetNation));
			}
		return pairs;
	}

	/// <summary>
	/// Detects when an attack wave ends and conditionally merges the defeated nation
	/// into the victor when the defender's population is below 75 % of the attacker's.
	/// </summary>
	private void CheckSoldierAttackOutcomes()
	{
		var current = CollectActiveSoldierPairs();
		foreach (var (attacker, defender) in _prevSoldierPairs)
		{
			if (current.Contains((attacker, defender)))
				continue;
			if (!Nations.TryGetValue(attacker, out var attackNation) ||
				!Nations.TryGetValue(defender, out var defendNation))
				continue;
			if (defendNation.CitizensList.Count > 0 &&
				defendNation.CitizensList.Count < 0.75f * attackNation.CitizensList.Count)
				MergeNationInto(defender, attacker);
		}
		_prevSoldierPairs = current;
	}

	/// <summary>
	/// Absorbs <paramref name="loserNation"/> into <paramref name="winnerNation"/>:
	/// kills the loser's King, re-tags all loser cells as the winner, and removes the
	/// loser from <see cref="Nations"/>.
	/// </summary>
	private void MergeNationInto(int loserNation, int winnerNation)
	{
		if (!Nations.ContainsKey(loserNation) || !Nations.ContainsKey(winnerNation))
			return;

		var loser = Nations[loserNation];

		if (loser.King != null && loser.King.IsAlive)
		{
			loser.King.Die();
			loser.King.Conditions.Add("cleanup");
		}
		loser.King = null;

		for (int c = 0; c < _columns; c++)
			for (int r = 0; r < _rows; r++)
			{
				var cell = CellGrid[c, r];
				if (cell.IsAlive && cell.Nationality == loserNation)
					cell.Nationality = winnerNation;
			}

		Nations.Remove(loserNation);
		PendingEvents.Add($"kingdom_destroyed:Nation {loserNation} was conquered! Absorbed into Nation {winnerNation}.");
	}
}
