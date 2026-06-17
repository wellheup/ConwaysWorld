namespace ConwaysWorld.Simulation;

public class Cell_Diseased : Cell
{
    protected int CountDown = 3;
    protected int TransmissionRate = 10;
    public string Disease;

    public Cell_Diseased(int column, int row, bool isAlive)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.Diseased;
        Conditions = new HashSet<string>();
        Disease = RandomCondition('d');
    }

    public override void Live()
    {
        IsAlive = true;
        Age++;
        if (Age > MatureAge) Conditions.Add("mature");
        CellType = CellType.Diseased;
        ChooseNation();
    }

    public override void Die()
    {
        IsAlive = false;
        Conditions.Remove(Disease);
        base.Die();
    }

    public override bool CalcCellAliveNextGen()
    {
        CountDown--;
        if (CountDown <= 0) return false;
        return LiveBasic();
    }

    public static Cell Infect(Cell cell, string disease, CellType cellType)
    {
        if (!cell.IsAlive) return cell;
        if (cell.CellType == cellType) return cell;
        if (cell.CellType == CellType.Immortal) return cell;
        var vaxKey = "vax_" + disease;
        if (cell.Conditions.Contains(vaxKey)) return cell;

        var temp = ReplaceCell(cell, cellType, true);
        temp.Conditions.Add(disease);
        return temp;
    }

    public override void SpecialActions(Cell[,] cellGrid) => SpreadDisease(cellGrid);

    protected void SpreadDisease(Cell[,] cellGrid)
    {
        if (!IsAlive) return;

        foreach (var key in Cell_Neighborhood.NeighborHoodKeys)
        {
            if (key == "center") continue;
            if (SimRandom.Range(1, 101) > TransmissionRate) continue;

            var neighbor = CellNeighborhood.NeighborhoodDict[key];
            int nc = neighbor.Column;
            int nr = neighbor.Row;
            var target = cellGrid[nc, nr];

            if (target.CellType == CellType.Immortal) continue;
            if (target.Conditions.Contains("immune")) continue;
            var vaxKey = "vax_" + Disease;
            if (target.Conditions.Contains(vaxKey)) continue;

            cellGrid[nc, nr].Conditions.Add(Disease);
        }
    }
}
