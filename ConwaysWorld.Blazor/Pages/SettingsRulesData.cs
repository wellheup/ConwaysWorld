using System.Collections.Generic;
using System.Linq;

namespace ConwaysWorld.Blazor.Pages;

/// <summary>
/// Static reference data for the Rules tab: cell descriptions and simulation mechanics.
/// </summary>
public static class SettingsRulesData
{
	public static readonly (string Name, string Desc)[] CellRules =
	{
		("Barbarian",      "Nationless aggressor spawned from Islanders. Converts adjacent Islanders into more Barbarians and destroys nearby nation cells. Reverts to Islander when no valid targets remain in range."),
		("Basic",          "Standard Conway cells. Born with a 25% chance of innate immunity. 1% chance of being born immaculate — permanently unable to be infected. Eligible to be promoted to Warrior by a King."),
		("Bomber",         "Always nationless. Reaches maturity at age 2 and immediately detonates, destroying every cell within a 2-cell radius. Cannot be defused."),
		("Conquistador",   "Like a Voyager but on arrival teleports the 10 nearest home-nation cells to the landing zone and immediately converts all of them — plus itself — into Soldiers for a concentrated assault."),
		("Diplomat",       "Elected from nations above the Diplomat population threshold. Travels toward the nearest foreign nation and converts adjacent cells to its own nationality 1 at a time."),
		("Diseased",       "Attempts to spread the d_ infection strain to every adjacent living cell each step. Dies after a 3-step internal countdown. Infected neighbours that aren't immune convert to Diseased on the next step."),
		("Doctor",         "Each step cures all adjacent diseased cells and stamps a vax_ immunity marker for each strain cured, preventing re-infection with that specific strain. Survives Conway death as long as it successfully vaccinates at least 1 new cell per step."),
		("Explorer",       "Behaves like a Traveler but triggers grid expansion when it reaches an edge. Drives the world to grow up to the Max Grid Size limit set in Settings."),
		("Follower",       "Created by a Savior. Waits 3 steps, then moves 1 cell per step in the Savior's last broadcast direction. Blocked by Kings, Revolutionaries, and other Followers; reverts to Basic after 4 consecutive blocked steps. Immune to Conway rules. Hunted by all Warriors and Hunters."),
		("Hunter",         "Hunts Immortals and Kings within range 5, advancing toward and attacking them. Also targets Saviors, Followers, and Rebels of any nation. Demotes to Basic after 3 consecutive idle steps with no eligible targets."),
		("Immortal",       "Lives indefinitely regardless of neighbour count. Dies only if isolated (0 live neighbours) for more than 8 consecutive steps. Immune to all disease and plague strains; cannot be infected or converted."),
		("Irradiated",     "Permanent hazard tile. Any cell that moves or is displaced onto an Irradiated tile is immediately destroyed. Not counted as living; immune to Conway rules; cannot be removed."),
		("Islander",       "Nationless cell that follows standard Conway survival rules. Dies from overcrowding if ≥20 live cells exist within a 5-tile radius. Converts to a Barbarian when any nation cell moves into an adjacent tile."),
		("King",           "Crowned from a nation with ≥5 citizens. Marks neighbouring Basic cells with a toWar tag, promoting them to Warriors. On death, Basic cells more than (columns+rows)/3 tiles away lose nationality and enter a 3-step neutral cooldown before they can rejoin any nation."),
		("Mutant",         "Nationless wildcard. Each step there is a chance it randomly mutates into a different cell type, fully adopting that type's behaviour and appearance. Long-term behaviour is entirely unpredictable."),
		("Necromancer",    "Spawns randomly into the grid. On spawn, immediately resurrects the 3 nearest recently-dead cells as Zombies; resurrects 1 additional Zombie each step thereafter. Survives only while ≥2 of its Zombies remain alive. Its death instantly destroys all of its Zombies."),
		("Plague",         "Identical to Diseased but with a 40% higher transmission rate and spreads the p_ strain. More likely to overwhelm immune defences in dense populations."),
		("PlagueRat",      "Nationless roamer. Spreads the r_ plague strain to neighbouring cells each step, converting them toward the Plague type. Hunted by Warriors and Hunters of all nations."),
		("Rebel",          "Short-lived Diplomat variant created by Revolutionaries. Converts at 3× the normal Diplomat rate. Hunted by Warriors and Hunters of every nation, not just foreign ones."),
		("Revolutionary",  "Defects from the dominant nation when it grows too large, founds a rival nation, then returns to the original homeland to recruit Rebels and Warriors from among its former allies."),
		("Savior",         "At most 1 alive at a time; only spawns when ≥2 nations exist. Flees its birth nation toward a random foreign nation, converting adjacent Basic cells into Followers with a 50% chance each step. On reaching the target King: 50% assimilates (Savior → Immortal, Followers → Basic in target nation) or 50% dies (Followers → Zealots). Immune to Conway rules. Hunted by all Warriors and Hunters."),
		("Soldier",        "Combat cell created by Spies and Conquistadors. Kills adjacent enemy cells outright; advances toward the nearest distant enemy otherwise. When the last Soldier in a wave dies, a nation-merge check fires to see if the attacker absorbs the defender."),
		("Spy",            "Dispatched from a minority nation into enemy territory. Seeks the enemy King by swapping into adjacent living cells one step at a time, converting each displaced cell into a Soldier. Appears invisible to most cell behaviours."),
		("Traveler",       "Moves 1 cell per step toward open space. Dies if isolated (0 live neighbours) for more than 3 consecutive steps, or if fully surrounded with no room to move for more than 3 consecutive steps."),
		("Voyager",        "Travels toward a disconnected foreign nation cluster. On arrival: 50% chance to spawn Diplomats and Warriors; 50% chance to seed 4 Plague cells in the target territory."),
		("Wayfinder",      "Scans the grid for the emptiest region and travels there. On arrival, spawns 5 Islander cells to populate the empty space. Dies if ≥20 cells crowd within 5 tiles at any point during travel."),
		("Warrior",        "Attacks foreign Diseased and Plague cells within range 2, converting or destroying them. Hunts Saviors, Followers, and Rebels regardless of nation. Demotes to Basic after 3 consecutive idle steps with no eligible targets."),
		("Zealot",         "Born when a Savior dies. Attacks any adjacent living cell each step regardless of nation. Advances 1 cell toward the nearest living cell when no adjacent target exists."),
		("Zombie",         "Resurrected from a recently dead cell by a Necromancer. Visually retains the appearance of its original type. Immune to Conway rules, disease, and old age. Invisible to other cells' neighbour counts. Permanently destroyed (cannot be revived again) if killed by a Doctor, Warrior, or Hunter. Dies automatically when its Necromancer dies."),
	};

	public static readonly Dictionary<string, string> CellDescriptions =
		CellRules.ToDictionary(r => r.Name, r => r.Desc);

	public static readonly (string Label, string Desc)[] SimulationEvents =
	{
		("Nations",                     "Cells in the same spawn cluster share a nationality. New cells join the nearest nation of the same colour within proximity range."),
		("Census",                      "Every step all living cells are counted per nation. Kings and Diplomats are elected or crowned when nations reach population thresholds."),
		("King Crowning",               "A nation with ≥5 citizens can crown a Basic cell as King. The King marks nearby Basic cells with 'toWar', promoting them to Warriors."),
		("King-Distance Neutralisation","Basic cells further than (columns+rows)/3 from their King lose nationality and gain a 3-step neutral cooldown before they can rejoin any nation."),
		("Diplomat Election",           "Large nations elect a Diplomat that travels to the nearest foreign nation and converts adjacent cells to its own nationality."),
		("Revolutionary Defection",     "When one nation becomes too dominant, a member may defect and become a Revolutionary, founding a rival nation and recruiting Rebels and Warriors."),
		("Famine",                      "Periodically kills cells in a random grid quadrant, simulating resource scarcity. Controlled by the Famine Cooldown and Famine Duration settings."),
		("Flood",                       "Periodically wipes the outer border ring of the grid, separating nation clusters and resetting expansion pressure."),
		("Random Life Injection",       "When population falls below the injection threshold, random cells are spawned to prevent total extinction. Threshold is set by % or absolute count."),
		("Grid Expansion",              "Explorer cells trigger the grid to grow when they reach an edge. Growth continues up to the Max Grid Size limit."),
		("Failure & Auto-Restart",      "The simulation ends (or auto-restarts) on: full extinction; population below Min Pop Threshold; population collapse after post-growth; or N-step stagnation."),
	};
}
