using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ConwaysWorld : MonoBehaviour
{
    public GameObject viewObject_Prefab;
    private View FrontEnd;
    public Model BackEnd;

    public bool LifeGoesOn = false;
    public bool FToContinue = false;
    public bool IsRendering = false;
    public int SpawnPercent = 10;
    public int Generation = 0, //make these private later
        AttemptsAtLife = 1,
        CurrentPopulation = 0;
    float Vertical,
        Horizontal,
        Columns,
        Rows;
    public float timeBeforeStart = 1.0f,
        TimeBetweenGenerations = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        // Get camera info
        Vertical = Camera.main.orthographicSize;
        Horizontal = Vertical * (Screen.width / Screen.height);
        Columns = Horizontal * 2f;
        Rows = Vertical * 2f;

        // Columns = 5; //TEMP FOR 5 x 5 GRID
        // Rows = 5; //TEMP FOR 5 x 5 GRID

        // Populate the grid backend initially
        BackEnd = new Model((int)Columns, (int)Rows, SpawnPercent);

        // Prepare the view
        FrontEnd = Instantiate(viewObject_Prefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<View>();
        FrontEnd.InitiateDisplayGrid(BackEnd.CellGrid, Vertical, Horizontal);
        FrontEnd.IsRendering = true;


        // Start the game
        InvokeRepeating("SimulationUpdate", timeBeforeStart, TimeBetweenGenerations);
    }

    private void SimulationUpdate()
    {
        if (LifeGoesOn)
        {
            if (FToContinue)
            {
                LifeGoesOn = false;
            }
            CurrentPopulation = BackEnd.UpdateCellsGrid();
            Generation++;

            // if (CurrentPopulation == 0)
            // {
            //     Restart();
            // }
        }
    }

    private void Restart()
    {
        LifeGoesOn = false;
        IsRendering = false;
        FrontEnd.IsRendering = false;
        BackEnd.PopulateGrid();
        Generation = 0;
        AttemptsAtLife++;

        LifeGoesOn = true;
        IsRendering = true;
        FrontEnd.IsRendering = IsRendering;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("r"))
        {
            print("\nRestarting World\n");
            Restart();
        }
        if (Input.GetKeyDown("f"))
        {
            // print("\nContinuing Life\n");
            LifeGoesOn = true;
        }
        if (Input.GetKeyDown("t"))
        {
            print("\nStart/Stop rendering\n");
            FrontEnd.IsRendering = !FrontEnd.IsRendering;
        }
        if (Input.GetKeyDown("z"))
        {
            FrontEnd.IsRendering = true;
            FrontEnd.RenderWorldState(BackEnd.CellGrid, AttemptsAtLife, Generation, CurrentPopulation);
            FrontEnd.IsRendering = IsRendering;
        }
        FrontEnd.RenderWorldState(BackEnd.CellGrid, AttemptsAtLife, Generation, CurrentPopulation);
    }
}
/*TO DO
- test if immaculate triplets work
    - immaculate cells seem to be getting marked as isAliveNextGen, but are not getting brought to life at all... still..
- remove NeighborHoodsGrid becaus there is no sense keeping track of neighborhoods twice...
- add a struct in Model that contains booleans for all types of cells so you can exclude/include when creating a new world and edit in inspector
- consider removing the column and row from Cell types
- add a behavior that causes spontaneous life explosions if there are only immortals left?
- add namespace?
- Add different types of specialzed cells inheriting from Cell
    - Simple
        - triple spawn immaculate birth (3 in a row, so if they're alone they'll just survive continuously)
    - Complex
        - plague (diseased cell spreads disease with 50% infection rate to all touching cells )
        - explorer (picks a random direction to move each turn, expands grid when going over edges, can last 3 cycles without neighbors)
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
        - hunter (picks a random live (immortal?) cell as a target on the grid and traverses moving toward the nearest dead cell then toward the target. Uses memoized djikstra's algorithm to compute fastest route. Only 1 alive at a time. chooses new target if target dies. Can kill immortals.)
        - god? (effects every living cell on the board in some way)
- add fields for "nations" to distinguish between different cell groups
    - if, at spawn, a grup is an island, then they form a nation (random string)
        -spawns a diplomat if island is larger than x number of cells
- add a way to change the size of the grid
- utilize a Number of Islands and a Max/Min size of an island algorithm for some cell type
- enums instead of ints?
- move Grid into its own class
*/
