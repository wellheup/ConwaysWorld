namespace ConwaysWorld.Simulation;

/// <summary>
/// An Immortal cell persists indefinitely as long as it has at least one living neighbour.
/// If isolated (zero neighbours) for more than <see cref="MaxAloneTime"/> consecutive steps it dies.
/// <para>
/// Key differences from Basic:
/// <list type="bullet">
///   <item>Ignores Conway over/under-population rules entirely — it always votes to survive.</item>
///   <item>Cannot be infected by Diseased or Plague cells (immunity is unconditional).</item>
///   <item>Cannot be converted to a diseased type via the condition system.</item>
///   <item>Counts as a target for <see cref="Cell_Hunter"/> prey.</item>
/// </list>
/// </para>
/// </summary>
public class Cell_Immortal : Cell
{
        /// <summary>Steps this cell has spent with zero living neighbours.</summary>
        private int _deathCounter = 0;

        /// <summary>
        /// Maximum consecutive isolated steps before the Immortal finally dies.
        /// Equivalent to the original Unity value of 8.
        /// </summary>
        private const int MaxAloneTime = 8;

        /// <summary>Creates an Immortal cell at the given position.</summary>
        public Cell_Immortal(int column, int row, bool isAlive = true)
        {
                Column = column;
                Row = row;
                IsAlive = isAlive;
                CellType = CellType.Immortal;
                Conditions = new HashSet<string>();
        }

        /// <summary>
        /// Increments the isolation counter when no neighbours are present,
        /// or resets it when at least one neighbour exists.
        /// Does NOT call <c>base.Live()</c> so Conway population rules are bypassed.
        /// </summary>
        public override void Live()
        {
                IsAlive = true;
                Age++;
                ChooseNation();

                if (CellNeighborhood.NumNeighbors == 0)
                        _deathCounter++;
                else
                        _deathCounter = 0;
        }

        /// <summary>
        /// Returns <c>false</c> only once the isolation counter exceeds <see cref="MaxAloneTime"/>;
        /// otherwise always returns <c>true</c>, overriding the standard Conway rules.
        /// </summary>
        public override bool CalcCellAliveNextGen()
        {
                if (_deathCounter > MaxAloneTime) return false;
                return IsAlive;
        }
}
