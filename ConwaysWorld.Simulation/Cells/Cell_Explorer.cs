namespace ConwaysWorld.Simulation;

public class Cell_Explorer : Cell_Traveler
{
    private bool _hasExplored = false;

    public Cell_Explorer(int column, int row, bool isAlive)
        : base(column, row, isAlive)
    {
        CellType = CellType.Explorer;
        MaxAloneTime = 4;
        Conditions = new HashSet<string>();
        ChooseNation();
    }

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

        if (!_hasExplored && IsNeighborOverEdge(CellNeighborhood.NeighborhoodDict[Direction]))
            Conditions.Add("exploring");
    }

    private bool IsNeighborOverEdge(Cell neighbor) =>
        Math.Abs(Column - neighbor.Column) > 1 || Math.Abs(Row - neighbor.Row) > 1;

    public override void SpecialActions(Cell[,] cellGrid)
    {
        if (IsAlive && !SpecialPerformed)
        {
            Conditions.Remove("exploring");
            Direction = ChooseTravelDirection();
            SwapCells(this, CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
            SpecialPerformed = true;
        }
    }
}
