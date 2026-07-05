namespace ConwaysWorld.Simulation;

public static partial class SimulationTestCases
{
	private static readonly IReadOnlyList<SimulationTestCase> AllPart2 =
			new List<SimulationTestCase>
			{

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
