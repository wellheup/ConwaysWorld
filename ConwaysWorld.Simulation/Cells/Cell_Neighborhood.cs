namespace ConwaysWorld.Simulation;

public class Cell_Neighborhood
{
    public int NumNeighbors { get; private set; }
    public int CenterColumn { get; }
    public int CenterRow { get; }
    public Cell Center;
    public Dictionary<string, Cell> NeighborhoodDict;

    public Dictionary<string, Cell> NeighborsDict
    {
        get
        {
            if (NeighborhoodDict == null) return null!;
            var d = new Dictionary<string, Cell>(NeighborhoodDict);
            d.Remove("center");
            return d;
        }
    }

    public static readonly string[] NeighborHoodKeys =
    {
        "southWest", "west", "northWest",
        "south",               "north",
        "southEast", "east",  "northEast",
        "center",
    };

    public Cell_Neighborhood(Cell[,] cellGrid, int column, int row)
    {
        CenterColumn = column;
        CenterRow = row;
        NeighborhoodDict = new Dictionary<string, Cell>();
        int keyIndex = 0;
        NumNeighbors = 0;
        Center = cellGrid[column, row];

        for (int colOff = -1; colOff <= 1; colOff++)
        {
            for (int rowOff = -1; rowOff <= 1; rowOff++)
            {
                int nc = (column + colOff + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                int nr = (row    + rowOff + cellGrid.GetLength(1)) % cellGrid.GetLength(1);

                if (colOff == 0 && rowOff == 0)
                {
                    NeighborhoodDict["center"] = cellGrid[nc, nr];
                }
                else
                {
                    if (cellGrid[nc, nr].IsAlive) NumNeighbors++;
                    NeighborhoodDict[NeighborHoodKeys[keyIndex++]] = cellGrid[nc, nr];
                }
            }
        }
    }
}
