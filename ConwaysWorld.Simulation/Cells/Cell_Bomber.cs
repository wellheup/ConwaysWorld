namespace ConwaysWorld.Simulation;

public class Cell_Bomber : Cell
{
    public Cell_Bomber(int column, int row, bool isAlive)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.Bomber;
        Conditions = new HashSet<string>();
    }

    public override bool CalcCellAliveNextGen() => true;

    public override void SpecialActions(Cell[,] cellGrid) => Detonate(cellGrid);

    private void Detonate(Cell[,] cellGrid)
    {
        if (Age < 2) return;

        var victims = GetAllCellsInRangeByRule(cellGrid, c => c.IsAlive, 2);
        foreach (var v in victims) v.Die();
        Die();
    }
}
