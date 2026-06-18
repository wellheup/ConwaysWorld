namespace ConwaysWorld.Simulation;

/// <summary>
/// The Doctor cures disease in adjacent cells and vaccinates them against re-infection.
/// <para>
/// Each step the Doctor:
/// <list type="number">
///   <item>Runs <c>CureDisease</c>: for each known disease strain, removes the condition from
///         any living neighbour and stamps a <c>vax_&lt;strain&gt;</c> immunity marker.
///         If the neighbour was a full Diseased/Plague cell it is converted back to Basic.</item>
///   <item>Runs <c>SeekDisease</c>: scans neighbours for new <c>d_</c>/<c>p_</c> conditions
///         to add to its known-strains set, so it can cure them in future steps.</item>
/// </list>
/// </para>
/// <para>
/// Survival rule: the Doctor survives the step unconditionally if it cured <i>at least one new</i>
/// disease (first-time vaccination of a neighbour sets <see cref="SpecialPerformed"/>).
/// If it did no new cures it falls back to standard Conway rules.
/// This means a Doctor in a healthy neighbourhood will eventually die out naturally.
/// </para>
/// </summary>
public class Cell_Doctor : Cell
{
        /// <summary>All disease/plague strains this Doctor has ever encountered and can now cure.</summary>
        private readonly HashSet<string> _knownDiseases = new();

        /// <summary>
        /// Set to <c>true</c> when the Doctor successfully applies at least one new vaccination
        /// this step.  Cleared at the start of each <see cref="Live"/> call.
        /// </summary>
        protected bool SpecialPerformed = false;

        /// <summary>Creates a Doctor cell at the given position.</summary>
        public Cell_Doctor(int column, int row, bool isAlive)
        {
                Column = column;
                Row = row;
                IsAlive = isAlive;
                CellType = CellType.Doctor;
                Conditions = new HashSet<string>();
        }

        /// <inheritdoc/>
        public override void Live()
        {
                base.Live();
                SpecialPerformed = false;
        }

        /// <summary>
        /// Survives unconditionally if it performed a new cure this step;
        /// otherwise falls back to standard Conway survival rules.
        /// </summary>
        public override bool CalcCellAliveNextGen()
        {
                if (SpecialPerformed) return true;
                return LiveBasic();
        }

        /// <summary>
        /// Scans each neighbour for <c>d_</c> or <c>p_</c> condition tags and records
        /// any newly seen strains in <see cref="_knownDiseases"/> for future curing.
        /// </summary>
        private void SeekDisease(Cell[,] cellGrid)
        {
                foreach (var neighbor in CellNeighborhood.NeighborhoodDict.Values)
                {
                        var target = cellGrid[neighbor.Column, neighbor.Row];
                        foreach (var cond in target.Conditions)
                        {
                                if (cond.StartsWith("d_") || cond.StartsWith("p_"))
                                        _knownDiseases.Add(cond);
                        }
                }
        }

        /// <summary>
        /// For each known disease strain, iterates over living neighbours:
        /// <list type="bullet">
        ///   <item>Removes the strain condition from the neighbour.</item>
        ///   <item>Adds a <c>vax_&lt;strain&gt;</c> immunity marker (sets <see cref="SpecialPerformed"/> the first time).</item>
        ///   <item>If the neighbour was a Diseased or Plague cell, replaces it with a living Basic cell.</item>
        /// </list>
        /// Rebuilds this cell's neighbourhood at the end since neighbour types may have changed.
        /// </summary>
        private void CureDisease(Cell[,] cellGrid)
        {
                foreach (var neighbor in CellNeighborhood.NeighborhoodDict.Values)
                {
                        var target = cellGrid[neighbor.Column, neighbor.Row];
                        if (!target.IsAlive || target == this) continue;

                        foreach (var disease in _knownDiseases)
                        {
                                if (target.Conditions.Contains(disease))
                                {
                                        target.Conditions.Remove(disease);
                                        var vaxKey = "vax_" + disease;
                                        if (!target.Conditions.Contains(vaxKey))
                                        {
                                                target.Conditions.Add(vaxKey);
                                                SpecialPerformed = true;
                                        }

                                        if (target.CellType == CellType.Diseased || target.CellType == CellType.Plague)
                                        {
                                                cellGrid[target.Column, target.Row] = ReplaceCell(target, CellType.Basic, true);
                                        }
                                }
                        }
                }
                CellNeighborhood = new Cell_Neighborhood(cellGrid, Column, Row);
        }

        /// <summary>Cures disease in neighbours first, then seeks new strains to remember.</summary>
        public override void SpecialActions(Cell[,] cellGrid, List<MoveRecord>? moves = null)
        {
                CureDisease(cellGrid);
                SeekDisease(cellGrid);
        }
}
