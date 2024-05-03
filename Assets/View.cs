using UnityEngine;

public class View : MonoBehaviour
{
    public GameObject Cell_Basic_Prefab;
    private GameObject[,] DisplayGrid;
    public bool IsRendering = false;
    private int AttemptsAtLife = 0, Generation = 0, CurrentPopulation = 0;
    private Cell[,] CellGrid;

    public void InitiateDisplayGrid(Cell[,] grid, float vertical, float horizontal)
    {
        int cellNum = 0;
        DisplayGrid = new GameObject[grid.GetLength(0), grid.GetLength(1)];
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                DisplayGrid[x, y] = Instantiate(Cell_Basic_Prefab, new Vector3(x, y, 0), Quaternion.identity);
                DisplayGrid[x, y].transform.position = new Vector3(x - (horizontal - 0.5f), y - (vertical - 0.5f));
                DisplayGrid[x, y].name = cellNum + " (" + x + ", " + y + ")";
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

    public void RenderWorldState()
    {
        for (int x = 0; x < CellGrid.GetLength(0); x++)
        {
            for (int y = 0; y < CellGrid.GetLength(1); y++)
            {
                DisplayGrid[x, y].GetComponent<Renderer>().material.SetColor("_Color", CellGrid[x, y].GetCurrentColor());
                // Color[] randColors = new[] { Color.black, Color.blue, Color.cyan, Color.red, Color.green, Color.yellow, Color.magenta };
                // displayGrid[x, y].GetComponent<Renderer>().material.SetColor("_Color", randColors[Random.Range(0, randColors.Length)]);
            }
        }
        // PrintWorldStats(attemptsAtLife, generation, currentPopulation);
    }

    public void RenderWorldState(Cell[,] cellGrid, int attemptsAtLife, int generation, int currentPopulation)
    {
        this.AttemptsAtLife = attemptsAtLife;
        this.Generation = generation;
        this.CurrentPopulation = currentPopulation;
        this.CellGrid = cellGrid;
        for (int x = 0; x < cellGrid.GetLength(0); x++)
        {
            for (int y = 0; y < cellGrid.GetLength(1); y++)
            {
                DisplayGrid[x, y].GetComponent<Renderer>().material.SetColor("_Color", CellGrid[x, y].GetCurrentColor());
                // Color[] randColors = new[] { Color.black, Color.blue, Color.cyan, Color.red, Color.green, Color.yellow, Color.magenta };
                // displayGrid[x, y].GetComponent<Renderer>().material.SetColor("_Color", randColors[Random.Range(0, randColors.Length)]);
            }
        }
        // PrintWorldStats(AttemptsAtLife, Generation, CurrentPopulation);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (IsRendering)
        {
            RenderWorldState();
        }
    }
}
