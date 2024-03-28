using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct Neighborhood
{
    public int NumNeighbors { get; }
    public int CenterColumn { get; }
    public int CenterRow { get; }
    public Dictionary<string, Cell> NeighborhoodDict;
    public string[] NeighborHoodKeys;

    public Neighborhood(Cell[,] cellGrid, int column, int row)
    {
        CenterColumn = column;
        CenterRow = row;
        NeighborHoodKeys = new string[] { "southWest", "west", "northWest", "south", "center", "north", "southEast", "east", "northEast" };
        NeighborhoodDict = new Dictionary<string, Cell>();
        int neighborhoodKeyNumber = 0;
        NumNeighbors = 0;
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

public class Model
{
    public Cell[,] CellGrid;
    private int SpawnPercent = 10;
    private Neighborhood[,] Neighborhoods;
    public bool UseThreeGroup = true;

    public Model(int columns, int rows, int spawnPercent)
    {
        this.SpawnPercent = spawnPercent;
        PopulateGrid((int)columns, (int)rows, spawnPercent);

    }

    private Cell SpawnCell(int column, int row, int spawnPercent)
    {
        if (UseThreeGroup)
        {
            return ThreeGroup(column, row);
        }
        else
        {
            int cellType = Random.Range(1, 101);
            Cell cell;
            if (cellType == 1 && false)
            {
                cell = new Cell_Immortal(column, row, true);
            }
            else if (cellType > 1 && cellType <= 2)
            {
                cell = new Cell_Diseased(column, row, true);
            }
            else if (cellType > 2 && cellType < spawnPercent)
            {
                cell = new Cell_Basic(column, row, true);
            }
            else
            {
                cell = new Cell_Basic(column, row, false);
            }
            return cell;
        }
    }

    public Cell ThreeGroup(int column, int row)
    {
        // TEMP CODE FOR 3 UNIT GROUP
        if (column == 2 && row >= 1 && row <= 3)
        {
            if (row == 2)
            {
                return new Cell_Diseased(column, row, true);
            }
            return new Cell_Basic(column, row, true);
        }
        else
        {
            return new Cell_Basic(column, row, false);
        }
        // END TEMP CODE
    }

    public void PopulateGrid(int columns, int rows, int spawnPercent)
    {
        CellGrid = new Cell[columns, rows];
        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                CellGrid[column, row] = SpawnCell(column, row, spawnPercent);
            }
        }
    }

    public void PopulateGrid()
    {
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                CellGrid[column, row] = SpawnCell(column, row, SpawnPercent);
            }
        }
    }

    public void UpdateCurrentNeighborhoodsGrid()
    {
        Neighborhoods = new Neighborhood[CellGrid.GetLength(0), CellGrid.GetLength(1)];
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                Neighborhoods[column, row] = new Neighborhood(CellGrid, column, row);
            }
        }
    }

    public int UpdateCellGridLifeStatuses()
    {
        int currentPopulation = 0;
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                CellGrid[column, row].DetermineAliveNextGen(CellGrid, Neighborhoods[column, row]);
            }
        }
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                //if cell is scheduled to be alive next gen and is not alive already
                if (CellGrid[column, row].GetIsAliveNextGen() && !CellGrid[column, row].GetIsAlive())
                {
                    //replace the cell with a fresh one, rather than leaving an opening for data to leak into a new cell from previous life (MAY WANT TO KEEP PREV-LIFE DATA LATER)
                    CellGrid[column, row] = SpawnCell(column, row, SpawnPercent);
                    currentPopulation++;
                }
                else if (CellGrid[column, row].GetIsAliveNextGen()) // if cell is already alive, just let it do it's thing
                {
                    CellGrid[column, row].Live(CellGrid);
                    currentPopulation++;
                }
                else
                {
                    CellGrid[column, row].Die();
                    if (CellGrid[column, row].GetType() == typeof(Cell_Immortal))
                    {
                        CellGrid[column, row] = new Cell_Basic(column, row, false);
                    }
                }
            }
        }
        return currentPopulation;
    }

}
