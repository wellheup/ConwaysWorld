namespace ConwaysWorld.Simulation;

public class Cell_Traveler : Cell
{
    protected int DeathCountDown = 0;
    protected int MaxAloneTime = 3;
    protected string Direction;
    protected bool SpecialPerformed = false;

    public Cell_Traveler(int column, int row, bool isAlive)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.Traveler;
        Conditions = new HashSet<string>();
        Direction = ChooseTravelDirection();
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
        ChooseNation();
    }

    public override void Die()
    {
        base.Die();
        SpecialPerformed = true;
    }

    protected virtual string ChooseTravelDirection()
    {
        string dir = "center";
        while (dir == "center")
            dir = Cell_Neighborhood.NeighborHoodKeys[SimRandom.Range(0, Cell_Neighborhood.NeighborHoodKeys.Length)];
        return dir;
    }

    public override bool CalcCellAliveNextGen()
    {
        if (DeathCountDown > MaxAloneTime) return false;
        if (CrushCountDown > 3) return false;
        return true;
    }

    public override void SpecialActions(Cell[,] cellGrid)
    {
        if (IsAlive && !SpecialPerformed)
        {
            Direction = ChooseTravelDirection();
            SwapCells(this, CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
            SpecialPerformed = true;
        }
    }
}
