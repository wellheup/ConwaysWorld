namespace ConwaysWorld.Simulation;

public class Cell_Doctor : Cell
{
    private readonly HashSet<string> _knownDiseases = new();
    protected bool SpecialPerformed = false;

    public Cell_Doctor(int column, int row, bool isAlive)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.Doctor;
        Conditions = new HashSet<string>();
    }

    public override void Live()
    {
        base.Live();
        SpecialPerformed = false;
    }

    public override bool CalcCellAliveNextGen()
    {
        if (SpecialPerformed) return true;
        return LiveBasic();
    }

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

    public override void SpecialActions(Cell[,] cellGrid)
    {
        CureDisease(cellGrid);
        SeekDisease(cellGrid);
    }
}
