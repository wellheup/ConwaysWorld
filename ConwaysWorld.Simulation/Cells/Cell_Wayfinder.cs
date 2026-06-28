namespace ConwaysWorld.Simulation;

/// <summary>
/// A Wayfinder locates the emptiest region of the grid and travels toward it, ignoring
/// standard Conway survival rules during transit.  On arrival it becomes an Islander and
/// seeds four additional Islander cells in nearby vacant slots.
/// <para>
/// Extends <see cref="Cell_ArrivalTraveler"/> which provides the <c>_specialPerformed</c>
/// guard, transit survival (<see cref="Cell_ArrivalTraveler.CalcCellAliveNextGen"/>),
/// <see cref="Cell.Live"/> / <see cref="Cell.Die"/> lifecycle, and the shared
/// <see cref="Cell_ArrivalTraveler.NearestVacant"/> utility.
/// </para>
/// <para>
/// Target selection: samples the grid at regular intervals to find the spot whose
/// 5-tile Chebyshev neighbourhood has the fewest living cells.
/// </para>
/// <para>
/// Movement: moves 2 cells per step toward the target, falling back to 1 cell if
/// the 2-cell destination is occupied.  Stays put if both are blocked.
/// </para>
/// <para>
/// Arrival: triggered when the Wayfinder reaches the target position (distance ≤ 1).
/// </para>
/// <para>
/// Crowding: dies if ≥ 20 living cells exist within 5 tiles (same rule as Islanders).
/// Idle reversion: reverts to a Barbarian after 3 consecutive steps without movement.
/// </para>
/// </summary>
public class Cell_Wayfinder : Cell_ArrivalTraveler
{
        private int _targetCol = -1;
        private int _targetRow = -1;
        private bool _hasTarget = false;

        /// <summary>Counts consecutive steps on which the Wayfinder could not move.</summary>
        private int _idleMoveTurns = 0;

        /// <summary>Creates a Wayfinder cell at the given position.</summary>
        public Cell_Wayfinder(int column, int row, bool isAlive)
        {
                Column = column;
                Row = row;
                IsAlive = isAlive;
                CellType = CellType.Wayfinder;
                Conditions = new HashSet<string>();
        }

        // Live(), Die(), CalcCellAliveNextGen(), and NearestVacant() inherited from Cell_ArrivalTraveler.

        // ── Private helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Counts living cells within a Chebyshev radius around (<paramref name="c"/>, <paramref name="r"/>).
        /// </summary>
        private static int CountLivingInRadius(Cell[,] cellGrid, int c, int r, int radius)
        {
                int cols = cellGrid.GetLength(0);
                int rows = cellGrid.GetLength(1);
                int count = 0;
                for (int dc = -radius; dc <= radius; dc++)
                        for (int dr = -radius; dr <= radius; dr++)
                        {
                                if (dc == 0 && dr == 0) continue; // exclude self
                                int nc = (c + dc + cols) % cols;
                                int nr = (r + dr + rows) % rows;
                                if (cellGrid[nc, nr].IsAlive)
                                        count++;
                        }
                return count;
        }

        /// <summary>
        /// Scans the grid (sampled every <c>step</c> cells) to find the location with the
        /// fewest living cells within a 5-tile radius.  Sets <see cref="_targetCol"/> /
        /// <see cref="_targetRow"/> and <see cref="_hasTarget"/>.
        /// </summary>
        private void SelectTarget(Cell[,] cellGrid)
        {
                int cols = cellGrid.GetLength(0);
                int rows = cellGrid.GetLength(1);
                int step = Math.Max(1, Math.Min(cols, rows) / 15);

                int bestCount = int.MaxValue;
                int bestCol = -1, bestRow = -1;

                for (int c = 0; c < cols; c += step)
                        for (int r = 0; r < rows; r += step)
                        {
                                // Prefer landing on dead cells so we don't displace living ones.
                                if (cellGrid[c, r].IsAlive)
                                        continue;
                                int count = CountLivingInRadius(cellGrid, c, r, 5);
                                if (count < bestCount)
                                {
                                        bestCount = count;
                                        bestCol = c;
                                        bestRow = r;
                                }
                        }

                if (bestCol >= 0)
                {
                        _targetCol = bestCol;
                        _targetRow = bestRow;
                        _hasTarget = true;
                }
                else
                {
                        _hasTarget = false;
                }
        }

        /// <summary>
        /// Steps toward the stored target position by up to 2 cells this turn.
        /// Tries the 2-cell jump first; falls back to 1 cell if blocked; stays if both are blocked.
        /// </summary>
        /// <returns><c>true</c> if the Wayfinder actually moved; <c>false</c> if both destinations were blocked.</returns>
        private bool MoveTowardTarget(Cell[,] cellGrid, List<MoveRecord>? moves)
        {
                int cols = cellGrid.GetLength(0);
                int rows = cellGrid.GetLength(1);

                int dc = _targetCol - Column;
                if (Math.Abs(dc) > cols / 2)
                        dc = -Math.Sign(dc) * (cols - Math.Abs(dc));
                int dr = _targetRow - Row;
                if (Math.Abs(dr) > rows / 2)
                        dr = -Math.Sign(dr) * (rows - Math.Abs(dr));

                int dirC = Math.Sign(dc);
                int dirR = Math.Sign(dr);
                if (dirC == 0 && dirR == 0)
                        return false;

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
                        return true;
                }
                else if (!dest1.IsAlive && dest1 != this)
                {
                        moves?.Add(new MoveRecord(StepStartColumn, StepStartRow, col1, row1, (int)CellType, Nationality));
                        SwapCells(this, dest1, cellGrid);
                        return true;
                }
                return false;
        }

        /// <summary>
        /// Resolves the arrival: the Wayfinder becomes an Islander and plants four more
        /// Islanders in the nearest vacant slots (using <see cref="Cell_ArrivalTraveler.NearestVacant"/>).
        /// </summary>
        private void Arrive(Cell[,] cellGrid)
        {
                int col = Column, row = Row;
                var vacant = NearestVacant(cellGrid, col, row, 4);

                var self = ReplaceCell(this, CellType.Islander, true);
                self.Nationality = -1;
                cellGrid[col, row] = self;

                foreach (var slot in vacant)
                {
                        var isle = ReplaceCell(slot, CellType.Islander, true);
                        isle.Nationality = -1;
                        cellGrid[slot.Column, slot.Row] = isle;
                }
        }

        /// <summary>Returns the Chebyshev distance to the stored target position.</summary>
        private int DistToTarget(int cols, int rows)
        {
                int dc = Math.Abs(Column - _targetCol);
                int dr = Math.Abs(Row - _targetRow);
                return Math.Max(Math.Min(dc, cols - dc), Math.Min(dr, rows - dr));
        }

        /// <inheritdoc/>
        public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
        {
                if (!IsAlive || _specialPerformed)
                        return;
                _specialPerformed = true;

                // Crowding death: die if ≥ 20 live cells exist within 5 tiles.
                if (CountLivingInRadius(cellGrid, Column, Row, 5) >= 20)
                {
                        Die();
                        return;
                }

                if (!_hasTarget)
                        SelectTarget(cellGrid);

                if (!_hasTarget)
                {
                        // No target found this step — still counts as idle.
                        _idleMoveTurns++;
                        if (_idleMoveTurns >= 3)
                        {
                                var barb = ReplaceCell(this, CellType.Barbarian, true);
                                barb.Nationality = -1;
                                cellGrid[Column, Row] = barb;
                        }
                        return;
                }

                int cols = cellGrid.GetLength(0);
                int rows = cellGrid.GetLength(1);

                if (DistToTarget(cols, rows) <= 1)
                {
                        Arrive(cellGrid);
                        return;
                }

                bool moved = MoveTowardTarget(cellGrid, moves);
                if (moved)
                {
                        _idleMoveTurns = 0;
                }
                else
                {
                        _idleMoveTurns++;
                        // Revert to a Barbarian after 3 consecutive blocked steps.
                        if (_idleMoveTurns >= 3)
                        {
                                var barb = ReplaceCell(this, CellType.Barbarian, true);
                                barb.Nationality = -1;
                                cellGrid[Column, Row] = barb;
                        }
                }
        }
}
