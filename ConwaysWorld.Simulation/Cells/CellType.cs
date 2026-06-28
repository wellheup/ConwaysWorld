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

	/// <summary>Travels across open space to reach a disconnected foreign nation; on arrival either spawns diplomats and warriors (becoming an Explorer) or turns into Plague and seeds four Plague cells.</summary>
	Voyager,

	/// <summary>Locates the emptiest region of the grid and travels toward it; on arrival spawns five Islander cells.</summary>
	Wayfinder,

	/// <summary>Nationless cell that lives by standard Conway rules but dies from crowding (20+ cells within 5 tiles). Converts to Barbarian when touched by a nation cell.</summary>
	Islander,

	/// <summary>Nationless aggressor spawned from Islanders. Converts adjacent Islanders and kills nearby nation cells each step; reverts to Islander when no targets remain.</summary>
	Barbarian,

	/// <summary>Infiltrates enemy territory; belongs to a minority nation; seeks the enemy King by swapping through living cells, converting each displaced cell into a Soldier.</summary>
	Spy,

	/// <summary>Combat cell created by Spies and Conquistadors; kills adjacent enemies and advances toward distant ones; triggers a nation-merge check when the last of its wave dies.</summary>
	Soldier,

	/// <summary>Like Voyager but on arrival teleports the nearest 10 home-nation cells to the landing zone and converts them (and itself) into Soldiers.</summary>
	Conquistador,

	/// <summary>At most one per grid. Flees its birth nation toward a random foreign nation, converting adjacent Basic cells into Followers. On reaching the target King: 50% assimilates (becomes Immortal, Followers become Basic in target nation) or 50% dies (Followers become Zealots).</summary>
	Savior,

	/// <summary>Created by a Savior. Waits 3 steps then follows the Savior's last broadcast direction. Reverts to Basic after 4 consecutive blocked steps. Immune to Conway crowding/isolation.</summary>
	Follower,

	/// <summary>Created when a Savior dies. Attacks any adjacent cell regardless of nation. Like a Soldier but with no nation allegiance checks.</summary>
	Zealot,

	/// <summary>
	/// Nationless undead cell. Immune to Conway rules, disease, and old age.
	/// Invisible to other living cells' Conway neighbor counts but can see other zombies.
	/// Belongs to its necromancer's "nation"; dies if its necromancer dies.
	/// Killed by a Doctor (treated as curing a disease).
	/// </summary>
	Zombie,

	/// <summary>
	/// Spawns randomly; survives as long as it has ≥2 active zombies OR is explicitly killed.
	/// On spawn, immediately resurrects the 3 nearest dead cells with a prior type as zombies.
	/// Each step, resurrects the nearest dead cell that has a prior type as a zombie.
	/// </summary>
	Necromancer,

	/// <summary>
	/// Permanent hazard tile. Cannot be moved, killed, converted, or given conditions.
	/// Does not count as a living cell for simulation-over checks.
	/// Any cell that swaps onto this tile dies instantly; the Irradiated cell is unaffected.
	/// </summary>
	Irradiated,

	/// <summary>
	/// Spawns at the emptiest grid region, then travels in one direction for 1/3 of the
	/// grid span, infecting only cells it physically swaps with (100% transmission, no roll).
	/// Dies immediately when it stops. Nationless, cannot be converted.
	/// </summary>
	PlagueRat,

	/// <summary>
	/// Marks each of its 8 immediate neighbours with a <c>mutate_</c> condition each step.
	/// On the following conditions pass the marked cells are replaced with a randomly chosen
	/// living cell type (any type, including promoted types like Warrior, King, Diplomat).
	/// Mutation supersedes all other scheduled changes for that step.
	/// Dies after a 5-step countdown. Low default spawn weight.
	/// </summary>
	Mutant,
}
/*TODO:
- maybe cells should be more likely to survive death for every neighbor of the same nation?
- Add to Conway's world an event that uses a "find the largest island" algorithm
- add hover documentation for all cell types in both side panel and settings
- add documentation for all rules in a new tab in the settings menu so you can see what all cells do, what events do, etc.
- add context on hover for all of the settings in the settings menu
- the sim seems to get stuck in cycles where there are a bunch of rebels and they won't die, then they kill off the population and the sim fails
- simulation can get stuck in a fail state where it believes that it has been stuck at a certain cell pop even on reset
- when the population goes extinct, the sim won't stop saying it has failed even on a restart. It only gets fixed when doing Apply & Restart in the settings
- when you set a test case, the sim doesn't update the settings panel, so you can't tweak the test cases
- the settings panel Apply & Restart button should close the settings weindow
- cell images are still not showing up in the sim for the github pages hosting
- make a fullscreen option for the sim
- make the escape key exit from the test cases or settings menus
- when nations have 0 population, remove them from the nation counts at the bottom of the screen
- I need better error logging for when the simulation fails
- it looks like when cell populations initially spawn they are overwriting each other, so that the most recently spawned cluster is overwriting earlier spawned 
- it looks like the simulation does not wrap around the edges of the grid when spawning initial population clusters
- I ended up with 360 wayfinders in a population of 14000 all clusterd in a grid without spaces, which is way too many. I need a way for wayfinders to die when they are too close to neighbors
- some of the bombers have nations, which is not supposed to happen I think
- bombers spawn out in the middle of nowhere all of the time and I don't think I like it
- wayfinders should follow similar crowding rules to islanders, also if they are not moving they should revert to barbiarians after 3 turns
- add to the plan that we need to change the wayfinder cell so that instead of checking for empty spots at intervals, just check for one at spawn, then travel towards it, but when reaching within a 5-tile chebyshev neighbourhood of that destination, check if that destination still has no population in the 5-tile radius. If there are cells there, then set a new destionation, otherwise proceed with the arrival behavior when reaching the destination
-	 teacher/ elder (Random chance to promote adjacent basic_cells to a new type)
		- god? (effects every living cell on the board in some way)
		- natural disasters? opportunity for largest island?
- utilize a Number of Islands and a Max/Min size of an island algorithm for some cell type
- make doctors more aggressive. Maybe make range a modifiable number in ConwaysWorld? Make this a setting called responsive doctors, which 
	will modulate doctor spawn weight depending on the number of diseases on the grid
*/
