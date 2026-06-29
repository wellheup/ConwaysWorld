namespace ConwaysWorld.Simulation;

/// <summary>
/// Pre-configured simulation scenarios for development testing and demonstration.
/// Each entry is a <see cref="SimulationTestCase"/> whose <see cref="SimulationTestCase.Settings"/>
/// can be passed directly to <see cref="Model"/>.
/// </summary>
public static class SimulationTestCases
{
	/// <summary>All available test cases in display order.</summary>
	public static IReadOnlyList<SimulationTestCase> All { get; } = new List<SimulationTestCase>
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

		new(
			"King's Court",
			"Nations grow until a King is crowned. Kings mark nearby Basic cells as Warriors. Watch the power structure emerge and compete.",
			new SimulationSettings
			{
				StartColumns = 40, StartRows = 40, MaxGridSize = 300,
				MaxNations = 3, StartClusters = 3,
				PopMode = PopMode.Count, PopValue = 150,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]    = 70,
					[CellType.Diplomat] = 15,
					[CellType.Warrior]  = 10,
					[CellType.King]     = 5,
					[CellType.Dead]     = 0,
				},
			}),

		new(
			"Revolutionary Schism",
			"Revolutionaries defect from the dominant nation, founding a rival faction and recruiting Rebels. Warriors hunt the Rebels down.",
			new SimulationSettings
			{
				StartColumns = 40, StartRows = 40, MaxGridSize = 300,
				MaxNations = 3, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 120,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]          = 55,
					[CellType.Warrior]        = 20,
					[CellType.Revolutionary]  = 15,
					[CellType.Rebel]          = 10,
					[CellType.Dead]           = 0,
				},
			}),

		new(
			"Voyager Fleets",
			"Voyagers travel to disconnected foreign nations. On arrival they either plant 4 Plague cells or spawn Diplomats and Warriors.",
			new SimulationSettings
			{
				StartColumns = 50, StartRows = 50, MaxGridSize = 400,
				MaxNations = 4, StartClusters = 4,
				PopMode = PopMode.Count, PopValue = 150,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]    = 60,
					[CellType.Voyager]  = 20,
					[CellType.Diplomat] = 10,
					[CellType.Warrior]  = 10,
					[CellType.Dead]     = 0,
				},
			}),

		new(
			"Conquistador Landing",
			"Conquistadors arrive in enemy territory and teleport the 10 nearest home-nation cells to the landing zone, converting all into Soldiers.",
			new SimulationSettings
			{
				StartColumns = 50, StartRows = 50, MaxGridSize = 400,
				MaxNations = 3, StartClusters = 3,
				PopMode = PopMode.Count, PopValue = 150,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]        = 70,
					[CellType.Conquistador] = 15,
					[CellType.Soldier]      = 15,
					[CellType.Dead]         = 0,
				},
			}),

		new(
			"Spy Network",
			"Spies from minority nations infiltrate enemy territory, swapping through living cells and converting each into a Soldier.",
			new SimulationSettings
			{
				StartColumns = 40, StartRows = 40, MaxGridSize = 300,
				MaxNations = 3, StartClusters = 3,
				PopMode = PopMode.Count, PopValue = 120,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]   = 70,
					[CellType.Spy]     = 20,
					[CellType.Soldier] = 10,
					[CellType.Dead]    = 0,
				},
			}),

		new(
			"Zealot Fury",
			"When a Savior dies its Followers become Zealots that attack any adjacent living cell regardless of nation. Contain the frenzy.",
			new SimulationSettings
			{
				StartColumns = 40, StartRows = 40, MaxGridSize = 300,
				MaxNations = 3, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 150,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]   = 55,
					[CellType.Warrior] = 20,
					[CellType.Zealot]  = 10,
					[CellType.Hunter]  = 10,
					[CellType.Savior]  = 5,
					[CellType.Dead]    = 0,
				},
			}),

		new(
			"Irradiated Wasteland",
			"Permanent Irradiated tiles kill any cell that moves onto them. Watch nations route around the hazard zones or perish.",
			new SimulationSettings
			{
				StartColumns = 40, StartRows = 40, MaxGridSize = 300,
				MaxNations = 2, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 100,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]      = 60,
					[CellType.Irradiated] = 30,
					[CellType.Warrior]    = 10,
					[CellType.Dead]       = 0,
				},
			}),

		new(
			"Plague Rats",
			"Nationless PlagueRats roam the grid spreading the r_ disease strain. Doctors vaccinate survivors while Warriors hunt the rats.",
			new SimulationSettings
			{
				StartColumns = 35, StartRows = 35, MaxGridSize = 250,
				MaxNations = 2, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 100,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]     = 60,
					[CellType.PlagueRat] = 25,
					[CellType.Doctor]    = 10,
					[CellType.Warrior]   = 5,
					[CellType.Dead]      = 0,
				},
			}),

		new(
			"Wayfinder's Journey",
			"Wayfinders seek the emptiest region of the grid and travel there, seeding 5 Islander cells on arrival.",
			new SimulationSettings
			{
				StartColumns = 50, StartRows = 50, MaxGridSize = 400,
				MaxNations = 2, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 100,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]     = 50,
					[CellType.Wayfinder] = 30,
					[CellType.Islander]  = 20,
					[CellType.Dead]      = 0,
				},
			}),

		new(
			"Epidemic Response",
			"Diseased and Plague cells sweep through a dense Basic population. Doctors race to vaccinate survivors before the outbreak becomes unstoppable.",
			new SimulationSettings
			{
				StartColumns = 35, StartRows = 35, MaxGridSize = 250,
				MaxNations = 2, StartClusters = 2,
				PopMode = PopMode.Count, PopValue = 150,
				FamineEnabled = false, FloodEnabled = false,
				RandomLifeEnabled = false, AllowGridExpansion = false,
				SpawnWeights = new()
				{
					[CellType.Basic]    = 55,
					[CellType.Diseased] = 18,
					[CellType.Plague]   = 12,
					[CellType.Doctor]   = 15,
					[CellType.Dead]     = 0,
				},
			}),

		new(
			"Full Chaos",
			"All 28 cell types active simultaneously. Nations rise and fall amid disease, war, invasions, and supernatural events.",
			new SimulationSettings
			{
				StartColumns = 60, StartRows = 60, MaxGridSize = 500,
				MaxNations = 6, StartClusters = 4,
				PopMode = PopMode.Count, PopValue = 300,
				FamineEnabled = true, FloodEnabled = true,
				RandomLifeEnabled = true, AllowGridExpansion = true,
				SpawnWeights = new()
				{
					[CellType.Basic]         = 30,
					[CellType.Immortal]      = 8,
					[CellType.Diseased]      = 5,
					[CellType.Plague]        = 3,
					[CellType.Traveler]      = 5,
					[CellType.Explorer]      = 5,
					[CellType.Doctor]        = 5,
					[CellType.Warrior]       = 5,
					[CellType.Hunter]        = 5,
					[CellType.Bomber]        = 3,
					[CellType.Diplomat]      = 5,
					[CellType.King]          = 3,
					[CellType.Rebel]         = 3,
					[CellType.Revolutionary] = 3,
					[CellType.Voyager]       = 3,
					[CellType.Wayfinder]     = 3,
					[CellType.Islander]      = 5,
					[CellType.Barbarian]     = 3,
					[CellType.Spy]           = 3,
					[CellType.Soldier]       = 3,
					[CellType.Conquistador]  = 3,
					[CellType.Savior]        = 2,
					[CellType.Follower]      = 3,
					[CellType.Zealot]        = 2,
					[CellType.Irradiated]    = 3,
					[CellType.PlagueRat]     = 3,
					[CellType.Necromancer]   = 3,
					[CellType.Dead]          = 0,
				},
			}),
	};
}
