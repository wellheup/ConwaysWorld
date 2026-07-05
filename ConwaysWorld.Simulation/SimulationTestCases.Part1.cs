namespace ConwaysWorld.Simulation;

public static partial class SimulationTestCases
{
	private static readonly IReadOnlyList<SimulationTestCase> AllPart1 =
			new List<SimulationTestCase>
			{
		new(
			"Conway Basics",
			"Pure Conway rules: only Basic cells, tiny grid. Observe classic birth/survival/death patterns.",
			new SimulationSettings
			{
				StartColumns = 10, StartRows = 10, MaxGridSize = 500,
				MaxNations = 0, StartClusters = 1,
				PopMode = PopMode.Count, PopValue = 10,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				NationsEnabled = false,
				SpawnWeights = new()
				{
					[CellType.Basic] = 50,
					[CellType.Dead]  = 50,
				},
			}),

		new(
			"Disease Spread",
			"Watch Diseased and Plague cells spread infection through a Basic population. Doctor cells attempt to vaccinate.",
			new SimulationSettings
			{
				StartColumns = 30, StartRows = 30, MaxGridSize = 200,
				MaxNations = 2, StartClusters = 1,
				PopMode = PopMode.Count, PopValue = 80,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]    = 60,
					[CellType.Diseased] = 15,
					[CellType.Plague]   = 10,
					[CellType.Doctor]   = 15,
					[CellType.Dead]     = 0,
				},
			}),

		new(
			"Two-Nation War",
			"Two nations grow until kings are crowned, then Warriors and Diplomats compete for territory.",
			new SimulationSettings
			{
				StartColumns = 40, StartRows = 40, MaxGridSize = 300,
				MaxNations = 2, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 120,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]    = 70,
					[CellType.Warrior]  = 15,
					[CellType.Diplomat] = 15,
					[CellType.Dead]     = 0,
				},
			}),

		new(
			"Savior & Followers",
			"One Savior spawns and moves across the grid, recruiting Followers, while Warriors hunt it.",
			new SimulationSettings
			{
				StartColumns = 50, StartRows = 50, MaxGridSize = 400,
				MaxNations = 3, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 200,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]    = 60,
					[CellType.Warrior]  = 15,
					[CellType.Hunter]   = 15,
					[CellType.Savior]   = 5,
					[CellType.Follower] = 5,
					[CellType.Dead]     = 0,
				},
			}),

		new(
			"Necromancer Rising",
			"A Necromancer spawns and raises nearby dead cells as Zombies. Doctors, Warriors, and Hunters must contain it.",
			new SimulationSettings
			{
				StartColumns = 40, StartRows = 40, MaxGridSize = 300,
				MaxNations = 2, StartClusters = 1,
				PopMode = PopMode.Count, PopValue = 100,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]       = 50,
					[CellType.Doctor]      = 10,
					[CellType.Warrior]     = 15,
					[CellType.Hunter]      = 15,
					[CellType.Necromancer] = 10,
					[CellType.Dead]        = 0,
				},
			}),

		new(
			"Mutant Takeover",
			"Mutant cells stamp mutation conditions on neighbours, gradually replacing the population with random cell types.",
			new SimulationSettings
			{
				StartColumns = 30, StartRows = 30, MaxGridSize = 200,
				MaxNations = 2, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 80,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]  = 70,
					[CellType.Mutant] = 30,
					[CellType.Dead]   = 0,
				},
			}),

		new(
			"Islander Invasion",
			"Nationless Islanders settle and form Barbarian raiders. A small nation must hold them off.",
			new SimulationSettings
			{
				StartColumns = 50, StartRows = 50, MaxGridSize = 400,
				MaxNations = 2, StartClusters = 1,
				PopMode = PopMode.Count, PopValue = 150,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]    = 30,
					[CellType.Islander] = 50,
					[CellType.Barbarian] = 20,
					[CellType.Dead]     = 0,
				},
			}),

		new(
			"Immortal Endurance",
			"Immortals live forever unless isolated for 8+ steps and are immune to disease. Hunters seek them out and eliminate them.",
			new SimulationSettings
			{
				StartColumns = 30, StartRows = 30, MaxGridSize = 200,
				MaxNations = 2, StartClusters = 1,
				PopMode = PopMode.Count, PopValue = 80,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]    = 30,
					[CellType.Immortal] = 50,
					[CellType.Hunter]   = 20,
					[CellType.Dead]     = 0,
				},
			}),

		new(
			"Nomad Crossing",
			"Travelers move every step and die if isolated or fully surrounded. Explorers do the same but expand the grid when they reach an edge.",
			new SimulationSettings
			{
				StartColumns = 25, StartRows = 25, MaxGridSize = 200,
				MaxNations = 1, StartClusters = 1,
				PopMode = PopMode.Count, PopValue = 50,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = true,
				SpawnWeights = new()
				{
					[CellType.Basic]    = 20,
					[CellType.Traveler] = 40,
					[CellType.Explorer] = 40,
					[CellType.Dead]     = 0,
				},
			}),

		new(
			"Bomber Chain Reaction",
			"Bombers detonate at age 2, killing everything within 2 cells. Watch chain explosions tear through dense clusters of Basic cells.",
			new SimulationSettings
			{
				StartColumns = 25, StartRows = 25, MaxGridSize = 200,
				MaxNations = 2, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 80,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]  = 60,
					[CellType.Bomber] = 40,
					[CellType.Dead]   = 0,
				},
			}),
			};
}
