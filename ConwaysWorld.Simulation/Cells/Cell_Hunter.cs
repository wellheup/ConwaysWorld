namespace ConwaysWorld.Simulation;

public class Cell_Hunter : Cell_Traveler
{
    protected List<CellType> PreyTypes;
    protected Cell? CurrentPrey;

    public Cell_Hunter(int column, int row, bool isAlive)
        : base(column, row, isAlive)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.Hunter;
        Conditions = new HashSet<string>();
        PreyTypes = new List<CellType> { CellType.Immortal, CellType.King };
        CurrentPrey = null;
    }

    public override void Live()
    {
        IsAlive = true;
        Age++;
        if (CellNeighborhood.NumNeighbors == 0)
            DeathCountDown++;
        else
            DeathCountDown = 0;
        ChooseNation();
    }

    public override bool CalcCellAliveNextGen()
    {
        if (DeathCountDown > MaxAloneTime) return false;
        return true;
    }

    private bool IsPrey(Cell c) => PreyTypes.Contains(c.CellType) && c.IsAlive;

    protected bool Hunt(Cell[,] cellGrid)
    {
        if (CurrentPrey == null || !CurrentPrey.IsAlive)
        {
            CurrentPrey = SelectNearbyCellByRule(cellGrid, IsPrey, 5);
        }

        Cell cellToSwap;
        if (CurrentPrey != null)
            cellToSwap = FindNeighborInDirOfCell(cellGrid, CurrentPrey);
        else
            cellToSwap = CellNeighborhood.NeighborhoodDict[ChooseTravelDirection()];

        if (CurrentPrey != null && cellToSwap == CurrentPrey && cellToSwap.IsAlive)
        {
            SwapCells(this, cellToSwap, cellGrid);
            cellToSwap.Die();
            CurrentPrey = null;
            return true;
        }
        else if (IsPrey(cellToSwap))
        {
            SwapCells(this, cellToSwap, cellGrid);
            cellToSwap.Die();
            return true;
        }
        else
        {
            SwapCells(this, cellToSwap, cellGrid);
            return false;
        }
    }

    public override void SpecialActions(Cell[,] cellGrid)
    {
        if (!IsAlive) return;
        bool didKill = Hunt(cellGrid);
        if (didKill)
            IdleTurns = 0;
        else
            IdleTurns++;
    }
}
