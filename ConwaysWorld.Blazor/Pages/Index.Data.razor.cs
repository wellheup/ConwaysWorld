using System.Text.Json;
using System.Text.Json.Serialization;
using ConwaysWorld.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ConwaysWorld.Blazor.Pages;

public partial class Index
{
	// ── In-app documentation ──────────────────────────────────────────────────────

	private static readonly (string Name, string Desc)[] CellRules =
	{
			("Basic",         "Standard Conway cells. 25% chance of immune tag. 1% immaculate spawn chance."),
			("Immortal",      "Lives forever unless isolated for more than 8 steps. Immune to disease."),
			("Diseased",      "Spreads a unique disease strain to neighbours. Dies after a 3-step countdown."),
			("Plague",        "Like Diseased but with 40% higher transmission rate."),
			("Traveler",      "Moves each step. Dies if isolated >3 steps or surrounded >3 steps."),
			("Explorer",      "Like Traveler. Triggers grid expansion when reaching an edge."),
			("Doctor",        "Cures nearby disease and stamps vaccination markers. Survives while active."),
			("Warrior",       "Fights foreign Diseased/Plague within range 2. Also hunts Saviors/Followers of any nation. Demotes to Basic after 3 idle steps."),
			("Hunter",        "Hunts Immortals and Kings within range 5. Also hunts Saviors/Followers of any nation. Demotes to Basic after 3 idle steps."),
			("Bomber",        "Detonates at age 2, killing all cells within a 2-cell radius."),
			("Diplomat",      "Elected from large nations. Travels to foreign nations and converts adjacent cells."),
			("King",          "Crowned from nations with 5+ citizens. Marks nearby Basic cells with toWar. Death triggers a neutralisation cooldown for distant cells."),
			("Rebel",         "Short-lived diplomat variant with 3× conversion rate. Created by Revolutionaries. Hunted by Warriors and Hunters."),
			("Revolutionary", "Defects from a dominant nation, founds a rival nation, recruits Warriors and Rebels."),
			("Voyager",       "Travels to a disconnected foreign nation. On arrival either spawns diplomats and warriors or seeds 4 Plague cells."),
			("Wayfinder",     "Finds the emptiest grid region and travels there. On arrival spawns 5 Islander cells."),
			("Islander",      "Nationless. Lives by Conway rules but dies from overcrowding (20+ within 5 tiles). Converts to Barbarian when touched by a nation cell."),
			("Barbarian",     "Nationless aggressor. Converts adjacent Islanders and kills nearby nation cells. Reverts to Islander when no targets remain."),
			("Spy",           "Infiltrates enemy territory, seeking the enemy King. Converts displaced cells to Soldiers."),
			("Soldier",       "Created by Spies and Conquistadors. Kills adjacent enemies; triggers nation-merge check when the last of its wave dies."),
			("Conquistador",  "Like Voyager but on arrival teleports the nearest 10 home-nation cells to the landing zone and converts them into Soldiers."),
			("Savior",        "At most one per grid. Flees birth nation toward a foreign nation, converting Basic cells into Followers. Hunted by Warriors/Hunters."),
			("Follower",      "Created by a Savior. Follows the Savior's broadcast direction. Reverts to Basic after 4 blocked steps. Hunted by Warriors/Hunters."),
			("Zealot",        "Created when a Savior dies. Attacks any adjacent living cell regardless of nation."),
			("Irradiated",    "Permanent hazard tile. Kills any cell that moves onto it. Not counted as living."),
			("PlagueRat",     "Nationless roamer that spreads a unique plague strain. Hunted by Warriors and Hunters."),
			("Zombie",        "Resurrected by a Necromancer. Immune to Conway rules and old age. Invisible to non-zombie counts. Destroyed by Doctor/Warrior/Hunter."),
			("Necromancer",   "Spawns randomly. Resurrects the nearest 3 dead cells as zombies on spawn, then 1 more each step. Survives while ≥2 zombies are alive."),
			("Mutant",        "Randomly transforms into another cell type each step."),
				};

	private static readonly Dictionary<string, string> CellDescriptions =
					CellRules.ToDictionary(r => r.Name, r => r.Desc);

	private static readonly (string Label, string Desc)[] SimulationEvents =
	{
			("Nation Formation",            "Cells inherit nationality from living neighbours. The first live cell in an area seeds a new nation."),
			("King Crowning",               "A nation with 5+ citizens may crown a King, which marks nearby Basic cells with toWar to promote them to Warriors."),
			("King-Distance Neutralisation","Cells past (cols+rows)/3 from their King lose nationality and gain a 3-step cooldown before rejoining any nation."),
			("Diplomat Election",           "Large nations elect a Diplomat that travels to the nearest foreign nation and converts adjacent cells."),
			("Revolutionary Defection",     "When a nation becomes dominant, a member may defect, found a rival nation, and recruit Rebels and Warriors."),
			("Famine",                      "Kills cells in a random quadrant each cycle. Frequency and duration controlled by Famine Cooldown and Duration settings."),
			("Flood",                       "Periodically wipes the outer border ring of the grid, separating nation clusters and resetting expansion pressure."),
			("Random Life Injection",       "Spawns random cells when population falls below the threshold. Threshold set by % or absolute count."),
			("Grid Expansion",              "Explorer cells trigger the grid to grow when they reach an edge. Growth continues up to the Max Grid Size limit."),
			("Failure & Auto-Restart",      "Ends (or auto-restarts) on: full extinction; population below threshold; post-growth collapse; or N-step stagnation."),
				};

	// ── Simulation settings fields ────────────────────────────────────────────────

	private int _startCols = 50;
	private int _startRows = 50;
	private bool _autoFitGrid = false;
	private int _maxGrid = 120;
	private int _maxNations = 4;
	private int _startClusters = 2;
	private double _popPercent = 0.4;
	private int _popCount = 10;
	private string _popPercentStr = "0.4";
	private int _minNeighbors = 2;
	private int _maxNeighbors = 3;
	private int _birthNeighbors = 3;
	private bool _nationsEnabled = true;
	private bool _famineEnabled = true;
	private int _famineCooldown = 15;
	private int _famineDuration = 10;
	private bool _floodEnabled = true;
	private bool _randomLifeEnabled = false;
	private bool _reactiveDoctor = false;
	private double _randomLifeThresholdPct = 5.0;
	private string _randomLifeThresholdPctStr = "5.0";
	private int _randomLifeThresholdCount = 10;
	private bool _autoContinue = false;
	private bool _allowGridExpansion = true;
	private int _failurePopThreshold = 0;
	private int _failurePopAfterGrowthThreshold = 0;
	private int _stagnationSteps = 10;
	private bool _showFailurePopup = false;
	private string _failureIcon = "⚠️";
	private string _failureTitle = "Simulation Ended";
	private string _failureMessage = "";
	private string _failureStats = "";
	private Dictionary<string, int> _spawnWeights = new();

	// ── Static lookup tables ──────────────────────────────────────────────────────

	private static readonly string[] TypeNames =
	{
			"Dead","Basic","Immortal","Diseased","Plague",
			"Traveler","Explorer","Doctor","Warrior","Hunter",
			"Bomber","Diplomat","King","Rebel","Revolutionary","Voyager",
			"Wayfinder","Islander","Barbarian","Spy","Soldier","Conquistador",
			"Savior","Follower","Zealot","Zombie","Necromancer","Irradiated","PlagueRat","Mutant"
				};

	private static readonly string[] TypeColors =
	{
			"#111","#e8e8e8","#e0c060","#7a2020","#c01010",
			"#4090d0","#20c0e0","#e050a0","#d08020","#c04040",
			"#e0a000","#a060e0","#f0d000","#ff5533","#9b1a4a","#00d4aa",
			"#1e90ff","#c8a040","#bb2200","#3a3a5a","#5a9e20","#c87800",
			"#ffffff","#b0e0ff","#ff4400","#111111","#111111","#55ff00","#8b2020","#cc44ff"
				};

	private static readonly System.Random _uiRandom = new();

	// Bump this whenever spawn-weight defaults change so stale localStorage weights
	// are ignored and fresh code defaults are used instead.
	private const int SettingsVersion = 1;

	private class SavedSettings
	{
		[JsonPropertyName("v")] public int Version { get; set; } = 0;
		[JsonPropertyName("startCols")] public int StartCols { get; set; } = 40;
		[JsonPropertyName("startRows")] public int StartRows { get; set; } = 40;
		[JsonPropertyName("maxGrid")] public int MaxGrid { get; set; } = 120;
		[JsonPropertyName("maxNations")] public int MaxNations { get; set; } = 4;
		[JsonPropertyName("startClusters")] public int StartClusters { get; set; } = 2;
		[JsonPropertyName("popPercent")] public double PopPercent { get; set; } = 10.0;
		[JsonPropertyName("popCount")] public int PopCount { get; set; } = -1;
		[JsonPropertyName("intervalMs")] public int IntervalMs { get; set; } = 1000;
		[JsonPropertyName("animationEnabled")] public bool AnimationEnabled { get; set; } = true;
		[JsonPropertyName("minNeighbors")] public int MinNeighbors { get; set; } = 2;
		[JsonPropertyName("maxNeighbors")] public int MaxNeighbors { get; set; } = 3;
		[JsonPropertyName("birthNeighbors")] public int BirthNeighbors { get; set; } = 3;
		[JsonPropertyName("famineEnabled")] public bool FamineEnabled { get; set; } = true;
		[JsonPropertyName("famineCooldown")] public int FamineCooldown { get; set; } = 15;
		[JsonPropertyName("famineDuration")] public int FamineDuration { get; set; } = 10;
		[JsonPropertyName("floodEnabled")] public bool FloodEnabled { get; set; } = true;
		[JsonPropertyName("randomLifeEnabled")] public bool RandomLifeEnabled { get; set; } = false;
		[JsonPropertyName("reactiveDoctor")] public bool ReactiveDoctor { get; set; } = false;
		[JsonPropertyName("randomLifeThresholdPct")] public double RandomLifeThresholdPct { get; set; } = 5.0;
		[JsonPropertyName("autoContinue")] public bool AutoContinue { get; set; } = false;
		[JsonPropertyName("allowGridExpansion")] public bool AllowGridExpansion { get; set; } = true;
		[JsonPropertyName("nationsEnabled")] public bool NationsEnabled { get; set; } = true;
		[JsonPropertyName("failurePopThreshold")] public int FailurePopThreshold { get; set; } = 0;
		[JsonPropertyName("failurePopAfterGrowthThreshold")] public int FailurePopAfterGrowthThreshold { get; set; } = 0;
		[JsonPropertyName("stagnationSteps")] public int StagnationSteps { get; set; } = 10;
		[JsonPropertyName("autoFitGrid")] public bool AutoFitGrid { get; set; } = false;
		[JsonPropertyName("spawnWeights")] public Dictionary<string, int>? SpawnWeights { get; set; }
	}
}
