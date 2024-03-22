using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ConwaysWorld : MonoBehaviour
{

    private Cell[,] cellGrid;
    private int[,] currentNeighborsGrid;
    public GameObject ViewObject_Prefab;
    private GameObject worldViewObject;
    private View worldView;
    public bool lifeGoesOn = false;
    public int spawnPercent = 10;
    private int generation = 0,
        attemptsAtLife = 1,
        currentPopulation = 0;
    float vertical,
        horizontal,
        columns,
        rows;
    public float timeBeforeStart = 1.0f,
        timeBetweenGenerations = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        vertical = Camera.main.orthographicSize;
        horizontal = vertical * (Screen.width / Screen.height);
        columns = horizontal * 2f;
        rows = vertical * 2f;
        PopulateGrid((int)columns, (int)rows, spawnPercent);

        worldViewObject = Instantiate(ViewObject_Prefab, new Vector3(0, 0, 0), Quaternion.identity);
        worldView = worldViewObject.GetComponent<View>();
        worldView.InitiateDisplayGrid(cellGrid, vertical, horizontal);
        worldView.RenderWorldState(cellGrid, attemptsAtLife, generation, currentPopulation);


        InvokeRepeating("GridUpdate", timeBeforeStart, timeBetweenGenerations);
        lifeGoesOn = true;
    }

    private Cell SpawnCell(int spawnPercent)
    {
        int cellType = Random.Range(1, 101);
        Cell cell;
        if (cellType == 1)
        {
            cell = new Cell_Immortal(true);
        }
        else if (cellType > 1 && cellType < spawnPercent)
        {
            cell = new Cell_Basic(true);
        }
        else
        {
            cell = new Cell_Basic(false);
        }
        return cell;
    }

    private void PopulateGrid(int columns, int rows, int spawnPercent)
    {
        cellGrid = new Cell[columns, rows];
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                cellGrid[i, j] = SpawnCell(spawnPercent);
            }
        }
    }

    private void PopulateGrid(Cell[,] cellGrid, int spawnPercent)
    {
        for (int i = 0; i < cellGrid.GetLength(0); i++)
        {
            for (int j = 0; j < cellGrid.GetLength(1); j++)
            {
                cellGrid[i, j] = SpawnCell(spawnPercent);
            }
        }
    }

    private int FindCellNeighbors(Cell[,] grid, int x, int y)
    {
        int cellNeighbors = 0;
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                // Make sure we don't count the cell itself.
                if (xOffset == 0 && yOffset == 0)
                {
                    continue;
                }

                // Wrap around the edges of the grid.
                int neighborX = (x + xOffset + grid.GetLength(0)) % grid.GetLength(0);
                int neighborY = (y + yOffset + grid.GetLength(1)) % grid.GetLength(1);

                // Count the live neighbor.
                if (grid[neighborX, neighborY].GetIsAlive())
                {
                    cellNeighbors++;
                }
            }
        }
        return cellNeighbors;
    }

    private void UpdateCurrentNeighborsGrid()
    {
        currentNeighborsGrid = new int[cellGrid.GetLength(0), cellGrid.GetLength(1)];
        for (int x = 0; x < cellGrid.GetLength(0); x++)
        {
            for (int y = 0; y < cellGrid.GetLength(1); y++)
            {
                currentNeighborsGrid[x, y] = FindCellNeighbors(cellGrid, x, y);
            }
        }
    }

    private void UpdateCellGridLifeStatuses()
    {
        currentPopulation = 0;
        bool[,] liveNextGen = new bool[cellGrid.GetLength(0), cellGrid.GetLength(1)];
        for (int x = 0; x < cellGrid.GetLength(0); x++)
        {
            for (int y = 0; y < cellGrid.GetLength(1); y++)
            {
                liveNextGen[x, y] = cellGrid[x, y].IsAliveNextGen(currentNeighborsGrid[x, y]);
            }
        }
        for (int x = 0; x < cellGrid.GetLength(0); x++)
        {
            for (int y = 0; y < cellGrid.GetLength(1); y++)
            {
                if (liveNextGen[x, y])
                {
                    cellGrid[x, y].Live();
                    currentPopulation++;
                }
                else
                {
                    cellGrid[x, y].Die();
                }
            }
        }
    }

    private void GridUpdate()
    {
        print("Grid Updating");
        if (lifeGoesOn)
        {
            UpdateCurrentNeighborsGrid();
            UpdateCellGridLifeStatuses();
            generation++;
            worldView.RenderWorldState(cellGrid, attemptsAtLife, generation, currentPopulation);

            if (currentPopulation == 0)
            {
                Restart();
            }
        }
    }

    private void Restart()
    {
        lifeGoesOn = false;
        PopulateGrid(cellGrid, spawnPercent);
        generation = 0;
        attemptsAtLife++;
        currentPopulation = 0;
        worldView.RenderWorldState(cellGrid, attemptsAtLife, generation, currentPopulation);

        lifeGoesOn = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("r"))
        {
            print("\nRestarting World\n");
            Restart();
        }
    }
}
/*TO DO
- **** STOP UPDATING THE PREFABS IN VIEW FROM THE CELL OBJECTS, INSTEAD USE A VIEWER THAT INTERPRETS WHAT EACH PREFAB SHOULD LOOK LIKE AND BE UPDATED TO
- Add different types of specialzed cells inheriting from Cell
    - Simple
        - disease vector
        - immune
        - invincible
    - Complex
        - explorer
        - doctor/ vaccine
        - necromancer (revives neighbors the turn after they die)
        - zombie (die if their necromancer dies, do not die from overpopulation)
        - warrior
        - breeder
        - mutant/ mutator
        - islander
        - bomber
        - savior (cells follow it)
        - conqueror
        - teacher/ elder
        - irradiated (cell cannot live ever again except under certain circumstance)
- add fields for "nations" to distinguish between different cell groups
- add getters/setters for fields
- make some of Cell class private
- make some of Cell class static
- make some of Cell class protected
- add cloning method to Cell class
- add circumstances for the live() and die() methods
- add a way to change the size of the grid
*/

/*
- enums instead of ints
- make cell an abstract class
    - it MUST implement its own version of differing methods
- in ConwaysWorld update method, you'll need to tell the number of cells and surrounding info to each cell object in the grid so they can update appropriately
- move Grid into its own class
- mvc "model view controller"
*/
