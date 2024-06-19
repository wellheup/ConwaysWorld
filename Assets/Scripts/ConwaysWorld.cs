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
        public int BasePercentLiving = 10,
            MinLifePercent = 5,
            Generation = 0, //make these private later
            AttemptsAtLife = 1,
            CurrentPopulation = 0,
            Columns = 0,
            Rows = 0,
            GridLimit;
        public float timeBeforeStart = 1.0f,
            TimeBetweenGenerations = 0.5f;

        // Start is called before the first frame update
        void Start()
        {
            Canvas canvas = FindObjectOfType<Canvas>();

            float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
            float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
            // Columns = Columns >= 0 ? Columns : (int)canvasWidth / (int)_baseTilePrefab.Image.rectTransform.sizeDelta.y;
            // Rows = Rows >= 0 ? Rows : (int)canvasHeight / (int)_baseTilePrefab.Image.rectTransform.sizeDelta.y;

            Columns = (int)canvasWidth / (int)_baseTilePrefab.Image.rectTransform.sizeDelta.y;
            Rows = (int)canvasHeight / (int)_baseTilePrefab.Image.rectTransform.sizeDelta.y;

            // Populate the grid backend initially
            BackEnd = new Model(Columns, Rows, BasePercentLiving, MinLifePercent, GridLimit);

            // Prepare the view
            FrontEnd = viewObject_Prefab.GetComponent<View>();
            FrontEnd.InitiateDisplayGrid(BackEnd.CellGrid, canvasWidth, canvasHeight);
            FrontEnd.IsRendering = IsRendering;

            // Start the game
            InvokeRepeating(nameof(SimulationUpdate), timeBeforeStart, TimeBetweenGenerations);
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
- right now it seems like I"m not actually making nations...
    - make nations draw from the 20 colors available
    - make nations outline visually
    - change nations from using a name to just using an index 0-19 and the matching color
    - Cells don't need to contain their visualization information
        - move nation colors to View    
        - move cell colors to View

- add class and method descriptions using /// notation (vscode should suggest a template)
- add a Cell_Grid type to contain all grid-based functions
- move more of conditions updates to SpecialActions()?
- Add to Conway's world an event that uses a "find the largest island" algorithm
- add visualization for nations
    - reduce nations to a pool of like 25
- Add different types of specialized cells inheriting from Cell
    - king (each turn, it assesses the number of cells in its nation and converts them to its kingdom. If 2 kingdoms touch, create a warfront, but start by doing nothing)
        - use island finding algorithm?
        - if cell has 3 neighbors of same nation promote to king?
    - voyager (version of the explorer cell which goes farther and specifically targets the nearest other nation)
        - once it reaches that nation as a neighbor, it adds that nationality to its conditions and seeks a new one not on its list
    - necromancer (revives neighbors the turn after they die)
    - zombie (die if their necromancer dies, do not die from overpopulation)
    - warrior (moves in a random direction and kills cells from other nations, zombies, and diseased cells, 2 warrior cells flip a coin for the victor)
    - mutant/ mutator (has a small chance every turn to randomly alter surrounding cells to another cell type)
    - islander (dies if there are more than x number of nearby cells within like 10 cells, moves til finding empty space if it's crowded)
    - bomber (kills all cells in 2 cell radius)
    - savior (moves in a direction, cells follow it)
    - conqueror (moves in a direction until it leave its nation, when hitting another nation, random chance that it kills several of them, and if they killed a large enough percent of the island they're touching, the nation converts)
    - teacher/ elder (random chance to promote adjacent basic_cells to a new type)
    - irradiated (cell cannot live ever again except under certain circumstance)
    - spy (similar to diplomat, but instead of moving directly toward target, must move through living neighbors)
    - hunter (picks a random live (immortal?) cell as a target on the grid and traverses moving toward the nearest dead cell then toward the target. Uses memoized djikstra's algorithm to compute fastest route. Only 1 alive at a time. chooses new target if target dies. Can kill immortals.)
    - god? (effects every living cell on the board in some way)
    - natural disasters? opportunity for largest island?
- add an increased chance to spawn doctors near diseases
- make minimum allowable grid size 5x5
- utilize a Number of Islands and a Max/Min size of an island algorithm for some cell type
- reset grid size after world ending events
- make each update frame fade between the 2 more smoothly

Visuals
- add an outline to cells from nations
- add symbols for each type of cell
    - generated some placeholder ones with https://deepai.org/machine-learning-model/logo-generator
    Cell descriptions:
        Cell_Basic -- lives and dies
        Cell_Immortal -tree- lives forever unless it gets too lonely
        Cell_Diseased -skull- spreads deadly disease
        Cell_Plague -super skull?- spreads extra deadly disease
        Cell_Traveler -??- picks a direction and goes, unless it gets too lonely 
        Cell_Explorer -explorer hat- picks a direction and goes, unless it gets too lonely, can also expand the grid
        Cell_Voyager -spyglass- picks a nation other than its own and travels to it, trying to collect all of the nations
        Cell_Doctor -needle?- vaccinates diseased and plague-ridden cells
        Cell_Necromancer -??- resurrects nearby cells as zombie version of themselves
        Cell_Zombie -??- live until it their necromancer dies, do not die until their necromancer dies, do not die from overpopulation, spread like disease
        Cell_Warrior -sword- moves in a random direction and kills cells from other nations, zombies, and diseased cells
        Cell_Teacher -graduation cap- upgrades 1 nearby cell per turn from basic
        Cell_Mutant -??- has a small chance every turn to randomly alter surrounding cells to another cell type
        Cell_Islander -palm tree- dies if there are more than x number of nearby cells within like 10 cells, moves til finding empty space if it's crowded
        Cell_Bomber -bomb- kills all cells within a radius proportional to the size of the grid
        Cell_Savior -cross- moves in a direction and cells within range will change their nation to the savior's nation and also shift in that direction
        Cell_Conqueror -flag- moves in a direction until it leave its nation, when hitting another nation, random chance that it kills several of them, and if they killed a large enough percent of the island they're touching, the nation converts to its nation
        Cell_Irradiated -nuclear symbol- cannot live ever again except maybe under a special, yet-to-be-determined circumstance
        Cell_Diplomat -quill- when a nation is large enough, a random member-cell becomes a diplomat and attempts to spread its nation to the nearest other nation
        Cell_Hunter -spear- searches for the nearest prey at spawn, selecting only cellTypes from a list of preys within a range. Travels toward that prey and upon reaching adjacentcy to prey, kills prey, then seeks new prey
        Cell_God -cloud- effects every living cell on the board in some way...
        Cell_King -crown- spawns from nations of significant size. each turn, it assesses the number of cells in its nation and converts them to its kingdom. If 2 kingdoms touch, create a warfront of warrior cells
        Cell_Dead -nothing?- it does, NOTHING!
    */
