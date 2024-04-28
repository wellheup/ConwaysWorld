using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public Neighborhood[,] NeighborhoodsGrid;
    public bool[,] AliveNextGenGrid;

    private int SpawnPercent = 10;
    private int CurrentPopulation;
    public bool UseThreeGroup = false;

    public Model(int columns, int rows, int spawnPercent)
    {
        SpawnPercent = spawnPercent;
        PopulateGrid((int)columns, (int)rows, spawnPercent);

    }

    private Cell InitializeCell(int column, int row, int spawnPercent)
    {
        if (UseThreeGroup)
        {
            return ThreeGroup(column, row);
        }
        else
        {
            int cellType = Random.Range(1, 101);
            Cell cell;
            /*if (cellType == 1)
            {
                cell = new Cell_Immortal(column, row, true);
            }
            else */
            if (cellType == 2)
            {
                cell = new Cell_Diseased(column, row, true);
            }
            else if (cellType > 3 && cellType < spawnPercent)
            {
                cell = new Cell_Basic(column, row, true);
                if (Random.Range(1, 5) == 1)// 1/4 chance disease immunity
                {
                    cell.Conditions.Add("immune");
                }
                if (Random.Range(1, 101) == 1)
                {
                    cell.Conditions.Add("immaculate");
                }
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
                // return new Cell_Diseased(column, row, true);
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
        NeighborhoodsGrid = new Neighborhood[columns, rows];
        AliveNextGenGrid = new bool[columns, rows];
        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                CellGrid[column, row] = InitializeCell(column, row, spawnPercent);
            }
        }
    }

    public void PopulateGrid()
    {
        CellGrid = new Cell[CellGrid.GetLength(0), CellGrid.GetLength(1)];
        NeighborhoodsGrid = new Neighborhood[CellGrid.GetLength(0), CellGrid.GetLength(1)];
        AliveNextGenGrid = new bool[CellGrid.GetLength(0), CellGrid.GetLength(1)];
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                CellGrid[column, row] = InitializeCell(column, row, SpawnPercent);
            }
        }
    }

    public void AddRandomLife(int percentOfGrid)
    {
        if (CurrentPopulation > 0 && CurrentPopulation / (CellGrid.GetLength(0) * CellGrid.GetLength(1)) <= 2)
        {
            int numNewLives = CellGrid.GetLength(0) * CellGrid.GetLength(1) * percentOfGrid / 100;
            int counter = 0;
            while (counter < numNewLives)
            {
                int randCol = Random.Range(0, CellGrid.GetLength(0));
                int randRow = Random.Range(0, CellGrid.GetLength(1));
                if (!CellGrid[randCol, randRow].GetIsAlive() && !AliveNextGenGrid[randCol, randRow])
                {
                    CellGrid[randCol, randRow] = InitializeCell(randCol, randRow, SpawnPercent);
                    counter++;
                }
            }
        }
    }

    public void UpdateNeighborhoodsGrid()
    {
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                NeighborhoodsGrid[column, row] = new Neighborhood(CellGrid, column, row);
                CellGrid[column, row].CellNeighborhood = NeighborhoodsGrid[column, row];
            }
        }
    }

    public void UpdateAliveNextGenGrid()
    {
        // Assess population 
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                AliveNextGenGrid[column, row] = CellGrid[column, row].CalcCellAliveNextGen();
            }
        }
    }

    public int UpdateCellLives()
    {
        CurrentPopulation = 0;
        // Update population state
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                if (CellGrid[column, row].GetIsAlive())
                {
                    if (AliveNextGenGrid[column, row])
                    {
                        // is alive and stays alive
                        // Debug.Log("Cell " + column + ", " + row + " stay alive ");
                        CellGrid[column, row].Live(CellGrid);
                    }
                    else
                    {
                        // Debug.Log("Cell " + column + ", " + row + " die ");
                        CellGrid[column, row].Die();
                    }
                    CurrentPopulation++;
                }
                else
                {
                    if (AliveNextGenGrid[column, row])
                    {
                        //replace the cell with a fresh one, rather than leaving an opening for data to leak into a new cell from previous life (MAY WANT TO KEEP PREV-LIFE DATA LATER)
                        //treats the cell as a newborn rather than a revived cell
                        CellGrid[column, row].Live(CellGrid);
                    }
                    else
                    {
                        // Debug.Log("Cell " + column + ", " + row + " stay dead ");
                        // is dead and stays dead
                    }
                }
            }
        }
        return CurrentPopulation;
    }

    public void UpdateCellConditions()
    {
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                // Chain of ifs for different conditions
                // if (CellGrid[column, row].Conditions.Contains("immune"))//manage immune, not sure if it works...
                // {
                //     CellGrid[column, row].Conditions.RemoveAll(item => item == "immune");
                // }
                if (CellGrid[column, row].Conditions.Contains("infected")) //manage infected
                {
                    CellGrid[column, row] = Cell_Diseased.Infect(CellGrid[column, row]);
                }
                if (CellGrid[column, row].Conditions.Contains("mature")) //manage mature
                {
                    CellGrid[column, row].Breed();
                }
                if (CellGrid[column, row].Conditions.Contains("immaculate"))//manage immaculate birth
                {
                    CellGrid[column, row].Immaculate(CellGrid);
                }
            }
        }
    }

    public void ObserveCellConditions() //for debugging
    {
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                if (CellGrid[column, row].CurrentColor != Color.white)
                {
                    Debug.Log("Column: " + CellGrid[column, row].Column + ", Row: " + CellGrid[column, row].Row + " IsAlive: " + CellGrid[column, row].GetIsAlive());
                }
            }
        }
    }

    public int UpdateCellsGrid()
    {
        UpdateNeighborhoodsGrid();
        UpdateAliveNextGenGrid();
        UpdateCellLives();
        UpdateCellConditions();
        // ObserveCellConditions(); //for debugging
        AddRandomLife(6);

        return CurrentPopulation;
    }

}

