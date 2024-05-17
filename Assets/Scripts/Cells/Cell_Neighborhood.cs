using System.Collections.Generic;
namespace ConwaysWorld
{

    public struct Cell_Neighborhood
    {
        public int NumNeighbors { get; }
        public int CenterColumn { get; }
        public int CenterRow { get; }
        public Cell Center;
        public Dictionary<string, Cell> NeighborhoodDict;
        public static string[] NeighborHoodKeys = new string[] { "southWest", "west", "northWest", "south", "north", "southEast", "east", "northEast", "center" };

        public Cell_Neighborhood(Cell[,] cellGrid, int column, int row)
        {
            CenterColumn = column;
            CenterRow = row;
            NeighborhoodDict = new Dictionary<string, Cell>();
            int neighborhoodKeyNumber = 0;
            NumNeighbors = 0;
            Center = cellGrid[column, row];
            for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
                {
                    // Wrap around the edges of the grid.
                    int neighborColumn = (column + columnOffset + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                    int neighborRow = (row + rowOffset + cellGrid.GetLength(1)) % cellGrid.GetLength(1);

                    // Count only live neighbors.
                    if (NeighborHoodKeys[neighborhoodKeyNumber] != "center" && cellGrid[neighborColumn, neighborRow].GetIsAlive())
                    {
                        NumNeighbors++;
                    }

                    // Add the directional name of cell to list
                    NeighborhoodDict.Add(NeighborHoodKeys[neighborhoodKeyNumber], cellGrid[neighborColumn, neighborRow]);
                    neighborhoodKeyNumber++;
                }
            }
        }
    }
}