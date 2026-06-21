namespace ConwaysWorld.Simulation;

/// <summary>
/// An Islander is a nationless cell that follows standard Conway survival rules with two
/// additional constraints:
/// <list type="bullet">
///   <item><term>Crowding death</term><description>Dies (and is cleaned up) if 20 or more
///         living cells exist within its 5-tile Chebyshev neighbourhood for 2 consecutive steps.</description></item>
///   <item><term>Nation contact</term><description>Converts to a <see cref="Cell_Barbarian"/>
///         the moment any adjacent cell belongs to a nation (Nationality ≥ 0).</description></item>
/// </list>
/// Islanders never join a nation — <see cref="Cell.ChooseNation"/> is skipped for this type.
/// </summary>
public class Cell_Islander : Cell
{
        /// <summary>Consecutive steps this Islander has been over the crowding threshold.</summary>
        private int _crowdedSteps = 0;

        /// <summary>Creates an Islander cell at the given position.</summary>
        public Cell_Islander(int column, int row, bool isAlive)
        {
                Column = column;
                Row = row;
                IsAlive = isAlive;
                CellType = CellType.Islander;
                Conditions = new HashSet<string>();
                Nationality = -1;
        }

        /// <summary>
        /// Like the base <see cref="Cell.Live"/> but skips <see cref="Cell.ChooseNation"/>
        /// so Islanders never inherit a nationality from neighbours.
        /// </summary>
        public override void Live()
        {
                IsAlive = true;
                Age++;
                if (Age > MatureAge)
                        Conditions.Add("mature");
        }

        /// <inheritdoc/>
        public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
        {
                if (!IsAlive)
                        return;

                // Nation contact: become a Barbarian if any Moore neighbour has a nationality.
                foreach (var nb in CellNeighborhood.NeighborhoodDict.Values)
                {
                        if (nb != this && nb.IsAlive && nb.Nationality >= 0)
                        {
                                var barb = ReplaceCell(this, CellType.Barbarian, true);
                                barb.Nationality = -1;
                                cellGrid[Column, Row] = barb;
                                return;
                        }
                }

                // Crowding death: count all living cells within 5-tile Chebyshev radius.
                int nearby = GetAllCellsInRangeByRule(cellGrid, c => c.IsAlive, 5).Count;
                if (nearby >= 20)
                {
                        _crowdedSteps++;
                        if (_crowdedSteps >= 2)
                        {
                                Die();
                                Conditions.Add("cleanup");
                        }
                }
                else
                {
                        _crowdedSteps = 0;
                }
        }
}
