namespace ConwaysWorld.Simulation;

public class Cell_Basic : Cell
{
    public Cell_Basic(int column, int row, bool isAlive)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.Basic;
        Conditions = new HashSet<string>();
    }
}
