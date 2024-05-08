using UnityEngine;

namespace ConwaysWorld
{
    public class ConwaysWorld : MonoBehaviour
    {
        public GameObject viewObject_Prefab;
        [SerializeField] private BaseTile _baseTilePrefab;
        private View FrontEnd;
        public Model BackEnd;

        public bool LifeGoesOn = false;
        public bool RestartAtZero = false;
        public bool FToContinue = false;
        public bool IsRendering = false;
        public int BasePercentLiving = 10;
        public int MinLifePercent = 5;
        public int Generation = 0, //make these private later
            AttemptsAtLife = 1,
            CurrentPopulation = 0;
        int Columns,
            Rows;
        public float timeBeforeStart = 1.0f,
            TimeBetweenGenerations = 0.5f;

        // Start is called before the first frame update
        void Start()
        {
            Canvas canvas = FindObjectOfType<Canvas>();

            float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
            float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
            Columns = (int)canvasWidth / (int)_baseTilePrefab.Image.rectTransform.sizeDelta.y;
            Rows = (int)canvasHeight / (int)_baseTilePrefab.Image.rectTransform.sizeDelta.y;

            // Populate the grid backend initially
            BackEnd = new Model(Columns, Rows, BasePercentLiving, MinLifePercent);

            // Prepare the view
            FrontEnd = viewObject_Prefab.GetComponent<View>();
            FrontEnd.InitiateDisplayGrid(BackEnd.CellGrid, canvasWidth, canvasHeight);
            FrontEnd.IsRendering = IsRendering;

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
                if (IsRendering)
                    FrontEnd.RenderWorldState(BackEnd.CellGrid, AttemptsAtLife, Generation, CurrentPopulation);

                if (RestartAtZero && CurrentPopulation == 0)
                {
                    Restart();
                }
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
            if (Input.GetKeyDown("f")) //stop u
            {
                LifeGoesOn = true;
            }
            if (Input.GetKeyDown("t")) //stop rendering
            {
                print("\nStart/Stop rendering\n");
                FrontEnd.IsRendering = !FrontEnd.IsRendering;
            }
            if (Input.GetKeyDown("z")) //force render
            {
                FrontEnd.IsRendering = true;
                FrontEnd.RenderWorldState(BackEnd.CellGrid, AttemptsAtLife, Generation, CurrentPopulation);
                FrontEnd.IsRendering = IsRendering;
            }
        }
    }
}
/*TODO:
- clear cells are slipping into spawn somehow...
    - am I not setting color when creating cells somewhere? maybe when they live for the first time?
- add class and method descriptions using /// notation (vscode should suggest a template)
- add a Cell_Grid type to contain all grid-based functions
- optimize grid traversal so that only changed cells are touched and we don't loop through the whole thing
- move more of conditions updates to SpecialActions()
- right now once regular life gets going, there aren't many opportunities for variations, introduce more variations on cells set to live next gen...
- Add to Conway's world an event that uses a "find the largest island"  algorithm
- Add different types of specialzed cells inheriting from Cell
    - Complex
        - voyager (version of the explorer cell which goes farther and specifically targets the nearest other nation)
        - doctor/ vaccine
        - necromancer (revives neighbors the turn after they die)
        - zombie (die if their necromancer dies, do not die from overpopulation)
        - warrior (moves in random direction and kills cells it hits)
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
- add a way to change the size of the starting grid
- utilize a Number of Islands and a Max/Min size of an island algorithm for some cell type
- move Grid into its own class
*/
