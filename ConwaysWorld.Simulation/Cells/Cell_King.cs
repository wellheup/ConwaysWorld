namespace ConwaysWorld.Simulation;

public class Cell_King : Cell
{
    public Cell_King(int column, int row, bool isAlive)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.King;
        Conditions = new HashSet<string>();
    }

    public override void Die()
    {
        IsAlive = false;
        Age = 0;
        Nationality = -1;
    }

    private void MakeArmy()
    {
        foreach (var neighbor in CellNeighborhood.NeighborhoodDict.Values)
        {
            if (neighbor.IsAlive && neighbor != this && neighbor.CellType == CellType.Basic)
                neighbor.Conditions.Add("toWar");
        }
    }

    public override void SpecialActions(Cell[,] cellGrid) => MakeArmy();
}
