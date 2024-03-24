using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public struct Neighborhood
{
    public int numNeighbors { get; }
    public int centerX { get; }
    public int centerY { get; }
    private Dictionary<string, Cell> neighborhoodDict;
    public string[] neighborHoodKeys;
    public Neighborhood(Cell[,] cellGrid, int x, int y)
    {
        centerX = x;
        centerY = y;
        neighborHoodKeys = new string[] { "northWest", "north", "northEast", "west", "center", "east", "southWest", "south", "southEast" };
        neighborhoodDict = new Dictionary<string, Cell>();
        int neighborhoodKeyNumber = 0;
        numNeighbors = 0;
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                // Wrap around the edges of the grid.
                int neighborX = (x + xOffset + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                int neighborY = (y + yOffset + cellGrid.GetLength(1)) % cellGrid.GetLength(1);

                // Count only live neighbors.
                if (neighborHoodKeys[neighborhoodKeyNumber] != "center" && cellGrid[neighborX, neighborY].GetIsAlive())
                {
                    numNeighbors++;
                }

                // Add the directional name of cell to list
                neighborhoodDict.Add(neighborHoodKeys[neighborhoodKeyNumber], cellGrid[neighborX, neighborY]);
                neighborhoodKeyNumber++;
            }
        }
    }
}

public class ConwaysWorld : MonoBehaviour
{
    private Cell[,] cellGrid;
    private int[,] currentNeighborsGrid;
    private Neighborhood[,] neighborhoods;
    public GameObject viewObject_Prefab;
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
        // Get camera info
        vertical = Camera.main.orthographicSize;
        horizontal = vertical * (Screen.width / Screen.height);
        columns = horizontal * 2f;
        rows = vertical * 2f;

        // Populate the grid backend initially
        PopulateGrid((int)columns, (int)rows, spawnPercent);

        // Prepare the view
        worldViewObject = Instantiate(viewObject_Prefab, new Vector3(0, 0, 0), Quaternion.identity);
        worldView = worldViewObject.GetComponent<View>();
        worldView.InitiateDisplayGrid(cellGrid, vertical, horizontal);
        worldView.RenderWorldState(cellGrid, attemptsAtLife, generation, currentPopulation);

        // Start the game
        InvokeRepeating("GridUpdate", timeBeforeStart, timeBetweenGenerations);
        lifeGoesOn = true;
    }

    private Cell SpawnCell(int spawnPercent)
    {
        int cellType = Random.Range(1, 101);
        Cell cell;
        /*if (cellType == 1)
        {
            cell = new Cell_Immortal(true);
        }
        else */
        if (cellType > 1 && cellType < spawnPercent)
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
                // TEMP CODE
                if (i == 3 && j > 3 && j < 7)
                {
                    cellGrid[i, j] = new Cell_Basic(true);
                }
                else
                {
                    cellGrid[i, j] = new Cell_Basic(false);
                }
                // END TEMP CODE
                // cellGrid[i, j] = SpawnCell(spawnPercent);
            }
        }
    }

    private void PopulateGrid()
    {
        for (int i = 0; i < cellGrid.GetLength(0); i++)
        {
            for (int j = 0; j < cellGrid.GetLength(1); j++)
            {
                cellGrid[i, j] = SpawnCell(spawnPercent);
            }
        }
    }

    private void UpdateCurrentNeighborhoodsGrid()
    {
        neighborhoods = new Neighborhood[cellGrid.GetLength(0), cellGrid.GetLength(1)];
        for (int x = 0; x < cellGrid.GetLength(0); x++)
        {
            for (int y = 0; y < cellGrid.GetLength(1); y++)
            {
                neighborhoods[x, y] = new Neighborhood(cellGrid, x, y);
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
                liveNextGen[x, y] = cellGrid[x, y].IsAliveNextGen(neighborhoods[x, y]);
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
            worldView.RenderWorldState(cellGrid, attemptsAtLife, generation, currentPopulation);
            UpdateCurrentNeighborhoodsGrid();
            UpdateCellGridLifeStatuses();
            generation++;

            if (currentPopulation == 0)
            {
                Restart();
            }
        }
    }

    private void Restart()
    {
        lifeGoesOn = false;
        PopulateGrid();
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
- EXTERNALIZE SOME OF THE COMPONENTS OF CONWAYSWORLD TO MODEL CLASS
- ADD DISEASE VECTOR AND FIGURE OUT HOW ATTRIBUTES WILL WORK 
- Add different types of specialzed cells inheriting from Cell
    - Simple
        - disease vector
        - immune
        - triple spawn (3 in a row, so if they're alone they'll just survive continuously)
    - Complex
        - explorer (picks a random direction to move each turn, expands grid when going over edges)
        - doctor/ vaccine
        - necromancer (revives neighbors the turn after they die)
        - zombie (die if their necromancer dies, do not die from overpopulation)
        - warrior (moves in random direction and kills cells it hits)
        - breeder (can have 1/2 additional neighbors, adds a neighbor every turn if space)
        - mutant/ mutator (has a small chance every turn to upgrade to another cell type)
        - islander (dies if there are more than x number of nearby cells within like 10 cells)
        - bomber (kills all cells in 2 cell radius)
        - savior (moves in a direction, cells follow it)
        - conqueror (moves in a direction until it leave its nation, when hitting another nation, random chance that it kills several of them, and if they killed a large enough percent of the island they're touching, the nation converts)
        - teacher/ elder (random chance to promote adjacent basic_cells to a new type)
        - irradiated (cell cannot live ever again except under certain circumstance)
        - diplomat (explorer but does not expand world, small chance to add new nation to its own, reverts to basic cell when done)
- add fields for "nations" to distinguish between different cell groups
    - if, at spawn, a grup is an island, then they form a nation (random string)
        -spawns a diplomat if island is larger than x number of cells
- add a way to change the size of the grid
- enums instead of ints?
- move Grid into its own class
- consider moving IsAliveNextGen into a CellLifeCycle method/object that manages all life functions
*/
