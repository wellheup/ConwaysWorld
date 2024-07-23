using System;
using System.Collections.Generic;
namespace ConwaysWorld
{

    public class Cell_Neighborhood
    {
        public int NumNeighbors { get; }
        public int CenterColumn { get; }
        public int CenterRow { get; }
        public Cell Center;
        public Dictionary<string, Cell> NeighborhoodDict;
        public Dictionary<string, Cell> NeighborsDict
        {
            get
            {
                if (NeighborhoodDict == null) return null;

                var neighbors = new Dictionary<string, Cell>(NeighborhoodDict);
                neighbors.Remove("center");
                return neighbors;
            }
        }
        public static string[] NeighborHoodKeys = new string[] {
            "southWest",
            "west",
            "northWest",
            "south",
            "north",
            "southEast",
            "east",
            "northEast",
            "center"
        }; //used to traverse dictionary in center-last order

        public Cell_Neighborhood(Cell[,] cellGrid, int column, int row)
        {
            CenterColumn = column;
            CenterRow = row;
            NeighborhoodDict = new Dictionary<string, Cell>();
            int neighborhoodKeyIndex = 0;
            NumNeighbors = 0;
            Center = cellGrid[column, row];
            //cycle through adjacent cells, clockwise, starting with southWest cell
            for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
                {
                    // Wrap around the edges of the grid.
                    int neighborColumn = (column + columnOffset + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                    int neighborRow = (row + rowOffset + cellGrid.GetLength(1)) % cellGrid.GetLength(1);

                    if (columnOffset == 0 && rowOffset == 0)
                    {
                        NeighborhoodDict.Add("center", cellGrid[neighborColumn, neighborRow]);
                    }
                    else
                    {
                        if (cellGrid[neighborColumn, neighborRow].GetIsAlive())
                        {
                            NumNeighbors++;
                        }
                        NeighborhoodDict.Add(NeighborHoodKeys[neighborhoodKeyIndex++], cellGrid[neighborColumn, neighborRow]);
                    }
                }
            }
        }
    }
}