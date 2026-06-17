namespace ConwaysWorld.Simulation;

public class Cell_Nation
{
    public int NationNum;
    public Cell? King = null;
    public List<Cell> CitizensList = new();
    public List<Cell> DiplomatsList = new();

    public static readonly List<string> NationColors = new()
    {
        "#bc00ff",
        "#471415",
        "#a3181c",
        "#00f542",
        "#617f1c",
        "#f50005",
        "#473d14",
        "#17a33d",
        "#a38517",
        "#299bae",
        "#00dbff",
        "#f5bf00",
        "#232b75",
        "#ff7000",
        "#0019f5",
        "#adff00",
        "#7f4719",
        "#671f80",
        "#1c2bb8",
        "#1c4724",
    };

    public Cell_Nation(int nationNum)
    {
        NationNum = nationNum;
        CitizensList = new List<Cell>();
        DiplomatsList = new List<Cell>();
    }

    public void Census(Cell[,] cellGrid)
    {
        var tempCits = new List<Cell>();
        var tempDips = new List<Cell>();

        for (int x = 0; x < cellGrid.GetLength(0); x++)
        {
            for (int y = 0; y < cellGrid.GetLength(1); y++)
            {
                var cell = cellGrid[x, y];
                if (cell.Nationality == NationNum && cell.IsAlive)
                {
                    tempCits.Add(cell);
                    if (cell.CellType == CellType.Diplomat)
                        tempDips.Add(cell);
                }
            }
        }

        CitizensList = tempCits;
        DiplomatsList = tempDips;

        ElectDiplomat(cellGrid);
        CrownKing(cellGrid);
    }

    private void ElectDiplomat(Cell[,] cellGrid)
    {
        if (DiplomatsList.Count >= 0.05f * CitizensList.Count || CitizensList.Count < 10)
            return;

        Cell? elect = null;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            elect = CitizensList[SimRandom.Range(0, CitizensList.Count)];
            if (elect != King && !DiplomatsList.Contains(elect)) break;
            elect = null;
        }

        if (elect != null && elect != King && elect.IsAlive && !DiplomatsList.Contains(elect))
        {
            var diplomat = Cell.ReplaceCell(elect, CellType.Diplomat, true);
            cellGrid[diplomat.Column, diplomat.Row] = diplomat;
            CitizensList.Add(diplomat);
            DiplomatsList.Add(diplomat);
        }
    }

    private void CrownKing(Cell[,] cellGrid)
    {
        if (King != null && !King.IsAlive)
        {
            King.Conditions.Add("cleanup");
            King = null;
        }

        if (King != null) return;
        if (CitizensList.Count < 5 || CitizensList.Count <= DiplomatsList.Count) return;

        Cell? newKing = null;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            newKing = CitizensList[SimRandom.Range(0, CitizensList.Count)];
            if (newKing.IsAlive) break;
            newKing = null;
        }

        if (newKing != null && newKing.IsAlive)
        {
            King = Cell.ReplaceCell(newKing, CellType.King, true);
            cellGrid[King.Column, King.Row] = King;
            CitizensList.Add(King);
            DiplomatsList.Remove(King);
        }
    }
}
