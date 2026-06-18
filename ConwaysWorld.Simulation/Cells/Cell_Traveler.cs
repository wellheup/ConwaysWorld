namespace ConwaysWorld.Simulation;

/// <summary>
/// A Traveler cell moves every step by swapping positions with a randomly chosen neighbour.
/// <para>
/// Survival rules:
/// <list type="bullet">
///   <item>Dies if isolated (zero neighbours) for more than <see cref="MaxAloneTime"/> (3) consecutive steps.</item>
///   <item>Dies if fully surrounded (8 neighbours) for more than 3 consecutive steps (<see cref="Cell.CrushCountDown"/>).</item>
///   <item>Otherwise always votes to survive, bypassing standard Conway rules.</item>
/// </list>
/// </para>
/// <para>
/// <see cref="Cell_Explorer"/> and <see cref="Cell_Hunter"/> both extend this class.
/// </para>
/// </summary>
public class Cell_Traveler : Cell
{
        /// <summary>Steps this cell has been isolated with zero living neighbours.</summary>
        protected int DeathCountDown = 0;

        /// <summary>Steps of isolation that trigger death.  Explorer overrides this to 4.</summary>
        protected int MaxAloneTime = 3;

        /// <summary>The neighbourhood key this cell will try to move toward next step.</summary>
        protected string Direction;

        /// <summary>Prevents <see cref="SpecialActions"/> from running twice in one step (e.g. after dying).</summary>
        protected bool SpecialPerformed = false;

        /// <summary>Creates a Traveler cell and picks a random initial travel direction.</summary>
        public Cell_Traveler(int column, int row, bool isAlive)
        {
                Column = column;
                Row = row;
                IsAlive = isAlive;
                CellType = CellType.Traveler;
                Conditions = new HashSet<string>();
                Direction = ChooseTravelDirection();
        }

        /// <summary>
        /// Updates isolation and crush counters, then resets <see cref="SpecialPerformed"/>
        /// so the cell can move this step.
        /// </summary>
        public override void Live()
        {
                IsAlive = true;
                Age++;

                if (CellNeighborhood.NumNeighbors == 0)
                        DeathCountDown++;
                else
                        DeathCountDown = 0;

                if (CellNeighborhood.NumNeighbors == 8)
                        CrushCountDown++;
                else
                        CrushCountDown = 0;

                SpecialPerformed = false;
                ChooseNation();
        }

        /// <summary>Marks movement as already performed so <see cref="SpecialActions"/> is skipped.</summary>
        public override void Die()
        {
                base.Die();
                SpecialPerformed = true;
        }

        /// <summary>
        /// Picks a random direction key from <see cref="Cell_Neighborhood.NeighborHoodKeys"/>,
        /// excluding <c>"center"</c> so the cell always moves to a different slot.
        /// </summary>
        protected virtual string ChooseTravelDirection()
        {
                string dir = "center";
                while (dir == "center")
                        dir = Cell_Neighborhood.NeighborHoodKeys[SimRandom.Range(0, Cell_Neighborhood.NeighborHoodKeys.Length)];
                return dir;
        }

        /// <summary>
        /// Dies if isolated too long or crushed too long; otherwise always votes to survive.
        /// </summary>
        public override bool CalcCellAliveNextGen()
        {
                if (DeathCountDown > MaxAloneTime) return false;
                if (CrushCountDown > 3) return false;
                return true;
        }

        /// <summary>
        /// Swaps this cell with the neighbour in <see cref="Direction"/> once per step.
        /// The swap is skipped if <see cref="SpecialPerformed"/> is already set.
        /// </summary>
        public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
        {
                if (IsAlive && !SpecialPerformed)
                {
                        Direction = ChooseTravelDirection();
                        var dest = CellNeighborhood.NeighborhoodDict[Direction];
                        if (dest != this)
                                moves?.Add(new MoveRecord(Column, Row, dest.Column, dest.Row, (int)CellType, Nationality));
                        SwapCells(this, dest, cellGrid);
                        SpecialPerformed = true;
                }
        }
}
