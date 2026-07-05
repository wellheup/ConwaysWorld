namespace ConwaysWorld.Simulation;

public partial class Model
{
	// ── Revolutionary spawning ────────────────────────────────────────────────────

	/// <summary>
	/// Promotes a random eligible citizen of <paramref name="nation"/> to Revolutionary,
	/// founding a new nation if a slot is available, or defecting to the second-largest nation
	/// as a Warrior otherwise.
	/// </summary>
	private void TrySpawnRevolutionaryForNation(Cell_Nation nation)
	{
		var candidates = nation.CitizensList
			.Where(c => c.IsAlive
				&& c != nation.King
				&& c.CellType != CellType.Warrior
				&& c.CellType != CellType.Diplomat
				&& c.CellType != CellType.Revolutionary
				&& c.CellType != CellType.Rebel)
			.ToList();

		if (candidates.Count == 0)
			return;

		var chosen = candidates[SimRandom.Range(0, candidates.Count)];
		int oldNation = nation.NationNum;

		int cap = Math.Min(_settings.MaxNations, Cell_Nation.NationColors.Count);
		if (Nations.Count < cap)
		{
			int newNationNum = 0;
			while (Nations.ContainsKey(newNationNum))
				newNationNum++;
			Nations[newNationNum] = new Cell_Nation(newNationNum);

			var rev = (Cell_Revolutionary)Cell.ReplaceCell(chosen, CellType.Revolutionary, true);
			rev.Nationality = newNationNum;
			rev.OldNationality = oldNation;
			CellGrid[rev.Column, rev.Row] = rev;
		}
		else if (Nations.Count >= 2)
		{
			var second = Nations.Values
				.Where(n => n.NationNum != oldNation && n.CitizensList.Count > 0)
				.OrderByDescending(n => n.CitizensList.Count)
				.FirstOrDefault();
			if (second != null)
			{
				var warrior = Cell.ReplaceCell(chosen, CellType.Warrior, true);
				warrior.Nationality = second.NationNum;
				CellGrid[warrior.Column, warrior.Row] = warrior;
			}
		}
	}

	// ── Nationless cluster formation ──────────────────────────────────────────────

	/// <summary>
	/// Scans the grid for connected groups of nationless living cells (Chebyshev-3 connectivity).
	/// Any group with at least <see cref="SimulationSettings.NationFormThreshold"/> cells
	/// is assigned a new nation, provided the nation cap has not been reached.
	/// </summary>
	private void FormNationsFromNationlessClusters()
	{
		int cap = Math.Min(_settings.MaxNations, Cell_Nation.NationColors.Count);
		if (Nations.Count >= cap)
			return;

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
						if (!visited.Contains(neighbor) && neighbor.IsAlive && neighbor.Nationality < 0)
						{
							visited.Add(neighbor);
							queue.Enqueue(neighbor);
						}
					}
			}
			groups.Add(group);
		}

		int threshold = _settings.NationFormThreshold;
		foreach (var group in groups)
		{
			if (group.Count < threshold)
				continue;
			if (Nations.Count >= cap)
				break;

			int newNat = 0;
			while (Nations.ContainsKey(newNat))
				newNat++;

			var nation = new Cell_Nation(newNat);
			Nations[newNat] = nation;

			foreach (var cell in group)
				cell.Nationality = newNat;

			nation.Census(CellGrid);
			PendingEvents.Add($"king_crowned:Nation {newNat} has formed!");
		}
	}

	// ── Revolution check ──────────────────────────────────────────────────────────

	/// <summary>
	/// Checks whether any nation dominates by holding at least twice the citizens of the
	/// second-largest nation.  If so, promotes a random citizen to Revolutionary.
	/// </summary>
	private void CheckRevolution()
	{
		if (Nations.Count < 2)
			return;

		var dominantCheck = Nations.Values
			.OrderByDescending(n => n.CitizensList.Count)
			.FirstOrDefault();
		if (dominantCheck == null || dominantCheck.King == null || !dominantCheck.King.IsAlive)
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
			.Where(c => c != dominant.King
				&& c.CellType != CellType.Warrior
				&& c.CellType != CellType.Diplomat
				&& c.CellType != CellType.Revolutionary
				&& c.CellType != CellType.Rebel
				&& c.IsAlive)
			.ToList();

		if (candidates.Count == 0)
			return;

		var chosen = candidates[SimRandom.Range(0, candidates.Count)];
		int oldNation = dominant.NationNum;

		int cap = Math.Min(_settings.MaxNations, Cell_Nation.NationColors.Count);
		if (Nations.Count < cap)
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
}
