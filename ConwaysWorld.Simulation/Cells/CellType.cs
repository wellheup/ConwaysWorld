namespace ConwaysWorld.Simulation;

/// <summary>
/// Identifies the type of a cell in the simulation grid.
/// <para>
/// <b>Dead</b> is the only non-living state. All other values represent living cell types
/// that can be spawned at initialisation or promoted/converted during gameplay.
/// Warriors and Kings are never spawned directly — they are promoted from existing cells
/// (Basic→Warrior via the "toWar" condition; any citizen→King via <see cref="Cell_Nation.CrownKing"/>).
/// </para>
/// </summary>
public enum CellType
{
	/// <summary>An empty, non-living grid slot.</summary>
	Dead,

	/// <summary>Standard Conway cell. 25 % immune chance; 1 % immaculate chance at spawn.</summary>
	Basic,

	/// <summary>Persists indefinitely unless isolated for more than 8 consecutive steps.</summary>
	Immortal,

	/// <summary>Spreads a unique <c>d_</c> strain to neighbours; dies after a 3-step countdown.</summary>
	Diseased,

	/// <summary>Like Diseased but with 40 % higher transmission rate and a <c>p_</c> strain.</summary>
	Plague,

	/// <summary>Swaps with a random neighbour every step; dies if alone &gt; 3 steps or crushed &gt; 3 steps.</summary>
	Traveler,

	/// <summary>Traveler variant that triggers grid expansion when it reaches an edge cell.</summary>
	Explorer,

	/// <summary>Cures neighbouring Diseased/Plague cells and stamps <c>vax_</c> immunity markers.</summary>
	Doctor,

	/// <summary>Targets foreign Diseased/Plague cells within range 2; demotes to Basic after 3 idle steps.</summary>
	Warrior,

	/// <summary>Hunts Immortals and Kings within range 5; demotes to Basic after 3 idle steps.</summary>
	Hunter,

	/// <summary>Detonates at age 2, killing all living cells within a 2-cell radius, then dies itself.</summary>
	Bomber,

	/// <summary>Elected from large nations; travels toward foreign nations and converts adjacent cells.</summary>
	Diplomat,

	/// <summary>Crowned from a nation with ≥ 5 citizens; marks neighbouring Basic cells with "toWar".</summary>
	King,

	/// <summary>Short-lived diplomat variant with 3× conversion rate; created by Revolutionaries. Hunted by Warriors and Hunters.</summary>
	Rebel,

	/// <summary>Defects from a dominant nation, founds a rival nation, and recruits Warriors and Rebels from the old homeland.</summary>
	Revolutionary,
}
/*
- TODO: maybe cells should be more likely to survive death for every neighbor of the same nation?
- TODO: make MinLivingNeighbors and MaxLivingNeighbors for cells accessible and alterable in ConwaysWorld object
- TODO: Add to Conway's world an event that uses a "find the largest island" algorithm
- TODO: Add different types of specialized cells inheriting from Cell
- TODO: maybe if cells are too far from their king they die?
        - TODO: voyager (version of the explorer cell which goes farther and specifically targets the nearest other nation)
                - TODO: once it reaches that nation as a neighbor, it either spawns a diplomat for its nation or spreads plague/disease
        - TODO: necromancer (revives neighbors the turn after they die) - I think this should wait til after refactor...
        - TODO: zombie (die if their necromancer dies, do not die from overpopulation)
        - TODO: mutant/ mutator (has a small chance every turn to Randomly alter surrounding cells to another cell type)
        - TODO: islander (dies if there are more than x number of nearby cells within like 10 cells, moves til finding empty space if it's crowded)
        - TODO: savior (moves in a direction, cells follow it)
        - TODO: conqueror (moves in a direction until it leave its nation, when hitting another nation, Random chance that it kills several of them, and if they killed a large enough percent of the island they're touching, the nation converts)
        - TODO: teacher/ elder (Random chance to promote adjacent basic_cells to a new type)
        - TODO: irradiated (cell cannot live ever again except under certain circumstance)
        - TODO: spy (similar to diplomat, but instead of moving directly toward target, must move through living neighbors)
        - TODO: god? (effects every living cell on the board in some way)
        - TODO: natural disasters? opportunity for largest island?
        - TODO: add coup event where 3 warriors spawn and try to kill a king
        - TODO: add a behavior to diplomats to instigate a rebellion event where, if a nation is large enough, and significantly larger than others (if there are any), it will become a revolutionary cell
        - TODO: revolutionary cells spawn a bunch of rebels and warriors for a new nation, which they become king of
        - TODO: rebel cells are like diplomats with higher conversion rates but shorter lifespans but are hunted by other nations warriors and hunters
- TODO: add an increased chance to spawn doctors near diseases
- TODO: make minimum allowable grid size 5x5
- TODO: utilize a Number of Islands and a Max/Min size of an island algorithm for some cell type
- TODO: reset grid size after world ending events
- TODO: make each update frame fade between the 2 more smoothly
- TODO: the system would be more stable and  likely faster if each cellType has its own phase of the update, so there is a heirarchical order to cell special actions
- TODO: make doctors more aggressive. Maybe make range a modifiable number in ConwaysWorld?

Visuals
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
                Cell_Warrior -sword- moves in a UnityEngine.Random direction and kills cells from other nations, zombies, and diseased cells
                Cell_Teacher -graduation cap- upgrades 1 nearby cell per turn from basic
                Cell_Mutant -??- has a small chance every turn to UnityEngine.Randomly alter surrounding cells to another cell type
                Cell_Islander -palm tree- dies if there are more than x number of nearby cells within like 10 cells, moves til finding empty space if it's crowded
                Cell_Bomber -bomb- kills all cells within a radius proportional to the size of the grid
                Cell_Savior -cross- moves in a direction and cells within range will change their nation to the savior's nation and also shift in that direction
                Cell_Conqueror -flag- moves in a direction until it leave its nation, when hitting another nation, UnityEngine.Random chance that it kills several of them, and if they killed a large enough percent of the island they're touching, the nation converts to its nation
                Cell_Irradiated -nuclear symbol- cannot live ever again except maybe under a special, yet-to-be-determined circumstance
                Cell_Diplomat -quill- when a nation is large enough, a UnityEngine.Random member-cell becomes a diplomat and attempts to spread its nation to the nearest other nation
                Cell_Hunter -spear- searches for the nearest prey at spawn, selecting only cellTypes from a list of preys within a range. Travels toward that prey and upon reaching adjacentcy to prey, kills prey, then seeks new prey
                Cell_God -cloud- effects every living cell on the board in some way...
                Cell_King -crown- spawns from nations of significant size. each turn, it assesses the number of cells in its nation and converts them to its kingdom. If 2 kingdoms touch, create a warfront of warrior cells
                Cell_Dead -nothing?- it does, NOTHING!
        */
