using UnityEngine;

public class Model
{
    public Cell[,] CellGrid;
    public Neighborhood[,] NeighborhoodsGrid;
    public bool[,] AliveNextGenGrid;
    private CellGenerator Generator;

    private int BasePercentLiving = 10;
    private int MinLifePercent = 5;
    private int CurrentPopulation;
    public bool UseThreeGroup = false;

    public Model(int columns, int rows, int basePercentLiving, int minLifePercent)
    {
        BasePercentLiving = basePercentLiving;
        MinLifePercent = minLifePercent;
        Generator = new CellGenerator(BasePercentLiving);
        PopulateGrid(columns, rows, basePercentLiving);
    }

    public void PopulateGrid(int columns, int rows, int basePercentLiving)
    {
        CellGrid = new Cell[columns, rows];
        NeighborhoodsGrid = new Neighborhood[columns, rows];
        AliveNextGenGrid = new bool[columns, rows];

        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                CellGrid[column, row] = Generator.InitializeCell(column, row);

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
                CellGrid[column, row] = Generator.InitializeCell(column, row);
            }
        }
    }

    public void AddRandomLife(int percentOfGrid)
    {
        if (CurrentPopulation > 0 && CurrentPopulation / (CellGrid.GetLength(0) * CellGrid.GetLength(1)) <= MinLifePercent)
        {
            int numNewLives = CellGrid.GetLength(0) * CellGrid.GetLength(1) * percentOfGrid / 100;
            int counter = 0;
            while (counter < numNewLives)
            {
                int randCol = Random.Range(0, CellGrid.GetLength(0));
                int randRow = Random.Range(0, CellGrid.GetLength(1));
                if (!CellGrid[randCol, randRow].GetIsAlive() && !AliveNextGenGrid[randCol, randRow])
                {
                    CellGrid[randCol, randRow] = Generator.InitializeCell(randCol, randRow);
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

    public void PerformSpecialActions()
    {
        for (int column = 0; column < CellGrid.GetLength(0); column++)
        {
            for (int row = 0; row < CellGrid.GetLength(1); row++)
            {
                CellGrid[column, row].SpecialActions(CellGrid);
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
        PerformSpecialActions();
        // ObserveCellConditions(); //for debugging
        AddRandomLife(BasePercentLiving);

        return CurrentPopulation;
    }

}

