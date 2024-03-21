using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ConwaysWorld : MonoBehaviour
{
    public Cell cell_Prefab;
    private Cell[,] grid;
    public bool lifeGoesOn = false;
    public int spawnRate = 10;
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
        grid = populateGrid(columns, rows, spawnRate);

        InvokeRepeating("GridUpdate", timeBeforeStart, timeBetweenGenerations);
        lifeGoesOn = true;
    }

    private Cell[,] populateGrid(float columns, float rows, int spawnRate)
    {
        Cell[,] newGrid = new Cell[(int)columns, (int)rows];
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                if (Random.Range(1, 101) < 100 - spawnRate)
                {
                    newGrid[i, j] = SpawnCell(0, i, j);
                }
                else
                {
                    newGrid[i, j] = SpawnCell(1, i, j);
                }
            }
        }
        return newGrid;
    }

    private void eradicateGrid(Cell[,] grid)
    {
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                grid[i, j].die();
            }
        }
    }

    private void resetGrid(Cell[,] grid, int spawnRate)
    {
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                if (Random.Range(1, 101) < 100 - spawnRate)
                {
                    grid[i, j].die();
                }
                else
                {
                    grid[i, j].live();
                }
            }
        }
    }

    private Cell SpawnCell(int cellType, int x, int y)
    {
        Cell cell = Instantiate(cell_Prefab, new Vector3(x, y, 0), Quaternion.identity); //FIGURE OUT HOW TO USE CONSTRUCTOR METHODS...
        cell.updateCellType(cellType);
        cell.name = "x: " + x + " y: " + y;
        cell.transform.position = new Vector3(x - (horizontal - 0.5f), y - (vertical - 0.5f));
        return cell;
    }

    private static int findNeighbors(Cell[,] grid, int x, int y)
    {
        int liveNeighbors = 0;
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
                if (grid[neighborX, neighborY].isAlive() != 0)
                {
                    liveNeighbors++;
                }
            }
        }
        return liveNeighbors;
    }

    private void GridUpdate()
    {
        if (lifeGoesOn)
        {
            int[,] liveNeighbors = new int[grid.GetLength(0), grid.GetLength(1)];
            currentPopulation = 0;
            // Loop through every cell
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    liveNeighbors[x, y] = findNeighbors(grid, x, y);
                }
            }
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    int isAlive = grid[x, y].applyLife(liveNeighbors[x, y]);
                    if (isAlive != 0)
                    {
                        currentPopulation++;
                    }
                }
            }
            generation++;
            print(
                "Attempt at Life: "
                    + attemptsAtLife
                    + "    Generation: "
                    + generation
                    + "    Current Population: "
                    + currentPopulation
            );
            if (currentPopulation == 0)
            {
                Restart();
            }
        }
    }

    private void Restart()
    {
        lifeGoesOn = false;
        eradicateGrid(grid);
        resetGrid(grid, spawnRate);
        generation = 0;
        attemptsAtLife++;
        currentPopulation = 0;

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
- create new cell types inheriting from Cell, but overwriting their color
    - change the live() functions
- change the applyLife function for different cells

- Add different types of specialzed cells inheriting from Cell
    - disease vector
    - immune
    - explorer
    - invincible
    - doctor/ vaccine
    - warrior
    - breeder
    - mutant/ mutator
    - islander
    - bomber
    - savior
    - conqueror
    - teacher/ elder
- add fields for "nations" to distinguish between different cell groups
- add getters/setters for fields
- make some of Cell class private
- make some of Cell class static
- make some of Cell class protected
- add cloning method to Cell class
- add circumstances for the live() and die() methods
- add a way to change the size of the grid
*/
