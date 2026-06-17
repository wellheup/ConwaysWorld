namespace ConwaysWorld.Simulation;

public class Cell_Immortal : Cell
{
    private int _deathCounter = 0;
    private const int MaxAloneTime = 8;

    public Cell_Immortal(int column, int row, bool isAlive = true)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.Immortal;
        Conditions = new HashSet<string>();
    }

    public override void Live()
    {
        IsAlive = true;
        Age++;
        ChooseNation();
        if (Age > MatureAge) Conditions.Add("mature");

        if (CellNeighborhood.NumNeighbors == 0)
            _deathCounter++;
        else
            _deathCounter = 0;
    }

    public override bool CalcCellAliveNextGen()
    {
        if (_deathCounter > MaxAloneTime) return false;
        return IsAlive;
    }
}
