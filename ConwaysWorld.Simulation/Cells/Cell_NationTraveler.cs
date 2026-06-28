namespace ConwaysWorld.Simulation;

/// <summary>
/// Abstract base for arrival travelers that seek a <em>disconnected</em> foreign nation —
/// one that shares no Moore-neighbourhood border with the traveler's own nation.
/// <para>
/// Subclasses: <see cref="Cell_Voyager"/>, <see cref="Cell_Conquistador"/>.
/// Both are identical in target selection and movement; they differ only in what
/// happens on arrival.  Subclasses implement the single abstract method
/// <see cref="Arrive"/> to define that effect.
/// </para>
/// <para>
/// Movement: up to 2 cells per step toward the target cell, falling back to 1 cell
/// if the 2-cell destination is occupied.  Stays put if both are blocked.
/// </para>
/// <para>
/// No-target handling: if no disconnected foreign nation can be found for 2 consecutive
/// steps the traveler replaces itself with a <see cref="Cell_Traveler"/>.
/// </para>
/// </summary>
public abstract class Cell_NationTraveler : Cell_ArrivalTraveler
{
	/// <summary>A living cell inside the target nation, used as the navigation goal.</summary>
	protected Cell? _target;

	/// <summary>Nationality index of the nation being approached.</summary>
	protected int _targetNation = -1;

	/// <summary>
	/// Consecutive steps without a valid target.  After 1 missed step the traveler
	/// converts to a <see cref="Cell_Traveler"/>.
	/// </summary>
	protected int _noTargetTurns = 0;

	/// <summary>
	/// Returns <c>true</c> when the current <see cref="_target"/> is still a valid
	/// navigation goal: alive, foreign, and still in the expected nation.
	/// </summary>
	protected bool IsTargetValid() =>
		_target != null && _target.IsAlive &&
		_targetNation >= 0 && _targetNation != Nationality &&
		_target.Nationality == _targetNation;

	/// <summary>
	/// Scans the entire grid to find the nearest foreign nation that has no cells
	/// adjacent (Moore neighbourhood) to any cell of this traveler's own nation.
	/// Sets <see cref="_target"/> and <see cref="_targetNation"/>; leaves both at their
	/// null/−1 defaults if no disconnected foreign nation exists.
	/// </summary>
	protected void SelectTarget(Cell[,] cellGrid)
	{
		_target = null;
		_targetNation = -1;

		if (Nationality < 0)
			return;

		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		// Build the set of all positions adjacent (Moore) to any own-nation cell.
		var ownAdj = new HashSet<(int, int)>();
		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				if (!cellGrid[c, r].IsAlive || cellGrid[c, r].Nationality != Nationality)
					continue;
				for (int dc = -1; dc <= 1; dc++)
					for (int dr = -1; dr <= 1; dr++)
						if (dc != 0 || dr != 0)
							ownAdj.Add(((c + dc + cols) % cols, (r + dr + rows) % rows));
			}

		// Separate foreign nations into those touching our border vs. fully disconnected.
		var disconnected = new HashSet<int>();
		var touching = new HashSet<int>();
		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (!cell.IsAlive || cell.Nationality == Nationality || cell.Nationality < 0)
					continue;
				if (ownAdj.Contains((c, r)))
					touching.Add(cell.Nationality);
				else
					disconnected.Add(cell.Nationality);
			}

		disconnected.ExceptWith(touching);
		if (disconnected.Count == 0)
			return;

		// Find the nearest cell from any disconnected nation.
		Cell? best = null;
		int bestDist = int.MaxValue;
		for (int c = 0; c < cols; c++)
			for (int r = 0; r < rows; r++)
			{
				var cell = cellGrid[c, r];
				if (!cell.IsAlive || !disconnected.Contains(cell.Nationality))
					continue;
				int distC = Math.Abs(c - Column);
				int distR = Math.Abs(r - Row);
				int dist = Math.Min(distC, cols - distC) + Math.Min(distR, rows - distR);
				if (dist < bestDist)
				{
					bestDist = dist;
					best = cell;
					_targetNation = cell.Nationality;
				}
			}

		_target = best;
	}

	/// <summary>
	/// Steps toward <see cref="_target"/> by up to 2 cells this turn using the shortest
	/// toroidal path.  Tries the 2-cell jump first; falls back to 1 cell if blocked;
	/// stays put if both destinations are occupied or alive.
	/// </summary>
	protected void MoveTowardTarget(Cell[,] cellGrid, List<MoveRecord>? moves)
	{
		if (_target == null)
			return;

		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		int dc = _target.Column - Column;
		if (Math.Abs(dc) > cols / 2)
			dc = -Math.Sign(dc) * (cols - Math.Abs(dc));
		int dr = _target.Row - Row;
		if (Math.Abs(dr) > rows / 2)
			dr = -Math.Sign(dr) * (rows - Math.Abs(dr));

		int dirC = Math.Sign(dc);
		int dirR = Math.Sign(dr);
		if (dirC == 0 && dirR == 0)
			return;

		int col2 = (Column + dirC * 2 + cols) % cols;
		int row2 = (Row + dirR * 2 + rows) % rows;
		int col1 = (Column + dirC + cols) % cols;
		int row1 = (Row + dirR + rows) % rows;

		var dest2 = cellGrid[col2, row2];
		var dest1 = cellGrid[col1, row1];

		if (!dest2.IsAlive && dest2 != this)
		{
			moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, col2, row2, (int)CellType, Nationality));
			SwapCells(this, dest2, cellGrid);
		}
		else if (!dest1.IsAlive && dest1 != this)
		{
			moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, col1, row1, (int)CellType, Nationality));
			SwapCells(this, dest1, cellGrid);
		}
		// Both blocked — stay put this step.
	}

	/// <summary>
	/// Resolves the arrival at the target nation.  Called when this traveler is
	/// adjacent to a live cell of <see cref="_targetNation"/>.  Each concrete subclass
	/// defines what happens (e.g. peaceful colonisation vs. military assault).
	/// </summary>
	/// <param name="cellGrid">The live simulation grid.</param>
	protected abstract void Arrive(Cell[,] cellGrid);

	/// <summary>
	/// Each step: validates/refreshes the target, handles no-target fallback, checks
	/// arrival adjacency (calling <see cref="Arrive"/>), or moves toward the target.
	/// </summary>
	public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
	{
		if (!IsAlive || _specialPerformed)
			return;
		_specialPerformed = true;

		if (!IsTargetValid())
			SelectTarget(cellGrid);

		if (_target == null)
		{
			if (_noTargetTurns >= 1)
			{
				// No disconnected nation found for two consecutive steps — become a Traveler.
				var traveler = ReplaceCell(this, CellType.Traveler, true);
				traveler.Nationality = Nationality;
				cellGrid[Column, Row] = traveler;
				return;
			}
			_noTargetTurns++;
			return;
		}
		_noTargetTurns = 0;

		// Check whether we are now adjacent to any cell of the target nation.
		foreach (var nb in CellNeighborhood.NeighborhoodDict.Values)
		{
			if (nb != this && nb.IsAlive && nb.Nationality == _targetNation)
			{
				Arrive(cellGrid);
				return;
			}
		}

		MoveTowardTarget(cellGrid, moves);
	}
}
