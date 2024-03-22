using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class View : MonoBehaviour
{
    public GameObject Cell_Basic_Prefab;
    private GameObject[,] displayGrid;

    public void InitiateDisplayGrid(Cell[,] grid, float vertical, float horizontal)
    {
        displayGrid = new GameObject[grid.GetLength(0), grid.GetLength(1)];
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                displayGrid[x, y] = Instantiate(Cell_Basic_Prefab, new Vector3(x, y, 0), Quaternion.identity);
                displayGrid[x, y].transform.position = new Vector3(x - (horizontal - 0.5f), y - (vertical - 0.5f));
                displayGrid[x, y].name = "x: " + x + " y: " + y + "isAlive: " + grid[x, y].GetIsAlive();
            }
        }
    }

    public void PrintWorldStats(int attemptsAtLife, int generation, int currentPopulation)
    {
        print(
            "Attempt at Life: "
                + attemptsAtLife
                + "    Generation: "
                + generation
                + "    Current Population: "
                + currentPopulation
        );
    }
    public void RenderWorldState(Cell[,] grid, int attemptsAtLife, int generation, int currentPopulation)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                displayGrid[x, y].GetComponent<Renderer>().material.SetColor("_Color", grid[x, y].getCurrentColor());
                // Color[] randColors = new[] { Color.black, Color.blue, Color.cyan, Color.red, Color.green, Color.yellow, Color.magenta };
                // displayGrid[x, y].GetComponent<Renderer>().material.SetColor("_Color", randColors[Random.Range(0, randColors.Length)]);
            }
        }
        PrintWorldStats(attemptsAtLife, generation, currentPopulation);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
