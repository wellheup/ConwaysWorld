using System.Text.Json;
using System.Text.Json.Serialization;
using ConwaysWorld.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ConwaysWorld.Blazor.Pages;

public partial class Index
{
	// ── Runtime state ─────────────────────────────────────────────────────────────

	private Model _model = null!;
	private SimulationSettings _settings = new();
	private System.Timers.Timer _timer = null!;
	private DotNetObjectReference<Index>? _dotNetRef;
	private bool _running = false;
	private bool _toolbarVisible = true;
	private bool _sidebarVisible = true;
	private bool _showSettings = false;
	private bool _isFullscreen = false;
	private int _selectedTestCase = 0;
	private int _settingsTab = 0;
	private int _intervalMs = 1000;
	private bool _animationEnabled = true;
	private bool _canvasReady = false;

	// ── Edit Mode state ───────────────────────────────────────────────────────────

	private bool _editMode = false;
	private bool _editWasRunning = false;
	private int _editSidebarTab = 0;
	private int _editBrushType = 1;  // -1 = eraser; >=1 = CellType int value
	private int _editNation = -1;    // -1 = Nationless
	private bool _editMoveMode = false;

	private record EditSnapshot(
					int Col, int Row,
					bool OldAlive, CellType OldType, int OldNat,
					bool NewAlive, CellType NewType, int NewNat);

	private readonly Dictionary<(int, int), EditSnapshot> _currentStrokeCells = new();
	private const int MaxUndoHistory = 200;
	private readonly LinkedList<List<EditSnapshot>> _undoStack = new();
	private readonly LinkedList<List<EditSnapshot>> _redoStack = new();

	private static readonly HashSet<CellType> NationCapableTypes = new()
				{
								CellType.Basic, CellType.Immortal, CellType.Diseased, CellType.Plague,
								CellType.Doctor, CellType.Warrior, CellType.Hunter, CellType.Diplomat,
								CellType.King, CellType.Rebel, CellType.Revolutionary, CellType.Voyager,
								CellType.Spy, CellType.Soldier, CellType.Conquistador,
								CellType.Traveler, CellType.Explorer,
				};

	// ── Display state ──────────────────────────────────────────────────────────────

	private Dictionary<(int col, int row), (int type, int nat)> _prevCellMap = new();
	private List<(string Name, string Color, int Count)> _typeCounts = new();

	private record TooltipData(string TypeName, int Nation, int Age, string[] Conditions);
	private TooltipData? _tooltip;
	private double _tooltipX = 0, _tooltipY = 0;

	private record SimEvent(string Message, string CssClass);
	private List<SimEvent> _eventLog = new();
	private const int MaxEventLog = 60;
	private bool _showEventLog = false;

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
								("Savior",        "At most one per grid. Flees birth nation toward a foreign nation, converting Basic cells into Followers. Hunted by Warriors/Hunters of all nations."),
								("Follower",      "Created by a Savior. Follows the Savior's broadcast direction. Reverts to Basic after 4 consecutive blocked steps. Hunted by Warriors/Hunters of all nations."),
								("Zealot",        "Created when a Savior dies. Attacks any adjacent living cell regardless of nation."),
								("Irradiated",    "Permanent hazard tile. Kills any cell that moves onto it. Not counted as living."),
								("PlagueRat",     "Nationless roamer that spreads a unique plague strain. Hunted by Warriors and Hunters."),
								("Zombie",        "Resurrected by a Necromancer. Immune to Conway rules, disease, and old age. Invisible to non-zombie Conway counts. Permanently destroyed by Doctor/Warrior/Hunter."),
								("Necromancer",   "Spawns randomly. Resurrects the nearest 3 dead cells as zombies on spawn, then 1 more each step. Survives while ≥2 zombies are alive."),
								("Mutant",        "Randomly transforms into another cell type each step."),
				};

	private static readonly Dictionary<string, string> CellDescriptions =
					CellRules.ToDictionary(r => r.Name, r => r.Desc);

	private static readonly (string Label, string Desc)[] SimulationEvents =
	{
								("Nation Formation",            "Cells inherit nationality from living neighbours. The first live cell in an area seeds a new nation."),
								("King Crowning",               "A nation with 5+ citizens may crown a King, which marks nearby Basic cells with toWar to promote them to Warriors."),
								("King-Distance Neutralisation","Basic cells further than (columns+rows)/3 from their King lose nationality and gain a 3-step neutral cooldown before they can rejoin any nation."),
								("Diplomat Election",           "Large nations elect a Diplomat that travels to the nearest foreign nation and converts adjacent cells to its own nationality."),
								("Revolutionary Defection",     "When one nation becomes too dominant, a member may defect and become a Revolutionary, founding a rival nation and recruiting Rebels and Warriors."),
								("Famine",                      "Periodically kills cells in a random grid quadrant, simulating resource scarcity. Controlled by the Famine Cooldown and Famine Duration settings."),
								("Flood",                       "Periodically wipes the outer border ring of the grid, separating nation clusters and resetting expansion pressure."),
								("Random Life Injection",       "When population falls below the injection threshold, random cells are spawned to prevent total extinction. Threshold is set by % or absolute count."),
								("Grid Expansion",              "Explorer cells trigger the grid to grow when they reach an edge. Growth continues up to the Max Grid Size limit."),
								("Failure & Auto-Restart",      "The simulation ends (or auto-restarts) on: full extinction; population below Min Pop Threshold; population collapse after post-growth; or N-step stagnation."),
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

	// ── Lifecycle ─────────────────────────────────────────────────────────────────

	protected override void OnInitialized()
	{
		var defaultSettings = new SimulationSettings();
		_spawnWeights = defaultSettings.SpawnWeights
						.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
		InitSettings();
		_model = new Model(_settings);
		_timer = new System.Timers.Timer(_intervalMs);
		_timer.Elapsed += async (_, _) => await OnTimerTick();
		_timer.AutoReset = false;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				var json = await JS.InvokeAsync<string?>("ConwaysInterop.loadSettings");
				if (!string.IsNullOrEmpty(json))
				{
					ApplySettingsJson(json);
					_timer.Interval = _intervalMs;
					InitSettings();
					_model = new Model(_settings);
				}
			}
			catch { }

			_dotNetRef = DotNetObjectReference.Create(this);
			await JS.InvokeVoidAsync("ConwaysInterop.init",
							"sim-canvas",
							_model.Columns,
							_model.Rows,
							18,
							_dotNetRef);
			_canvasReady = true;
			await JS.InvokeVoidAsync("ConwaysInterop.watchToolbarHeight");
			await RenderFrame();
			CapturePrevCells();
			UpdateTypeCounts();
			try
			{
				var vpWidth = await JS.InvokeAsync<double>("eval", "window.innerWidth");
				if (vpWidth <= 700)
				{ _toolbarVisible = false; _sidebarVisible = false; }
			}
			catch { }
			StateHasChanged();
		}
	}

	// ── Event log and frame capture ───────────────────────────────────────────────

	private SimEvent ParseEvent(string raw)
	{
		if (raw.StartsWith("king_crowned:"))
			return new SimEvent(raw["king_crowned:".Length..], "ev-crown");
		if (raw.StartsWith("king_fallen:"))
			return new SimEvent(raw["king_fallen:".Length..], "ev-fallen");
		if (raw.StartsWith("kingdom_destroyed:"))
			return new SimEvent(raw["kingdom_destroyed:".Length..], "ev-destroy");
		if (raw.StartsWith("famine_start:"))
			return new SimEvent(raw["famine_start:".Length..], "ev-famine");
		if (raw.StartsWith("famine_end:"))
			return new SimEvent(raw["famine_end:".Length..], "ev-famine-end");
		if (raw.StartsWith("revolution_start:"))
			return new SimEvent(raw["revolution_start:".Length..], "ev-revolution");
		if (raw.StartsWith("flood_start:"))
			return new SimEvent(raw["flood_start:".Length..], "ev-flood");
		if (raw.StartsWith("flood_end:"))
			return new SimEvent(raw["flood_end:".Length..], "ev-flood-end");
		if (raw.StartsWith("regicide_duel:"))
			return new SimEvent(raw["regicide_duel:".Length..], "ev-duel");
		if (raw.StartsWith("selected_cell:"))
			return new SimEvent(raw["selected_cell:".Length..], "ev-selected");
		return new SimEvent(raw, "");
	}

	private void CollectEvents()
	{
		foreach (var raw in _model.PendingEvents)
		{
			var ev = ParseEvent(raw);
			var msg = new SimEvent($"Gen {_model.Generation}  {ev.Message}", ev.CssClass);
			_eventLog.Insert(0, msg);
		}
		if (_eventLog.Count > MaxEventLog)
			_eventLog.RemoveRange(MaxEventLog, _eventLog.Count - MaxEventLog);
	}

	private void CapturePrevCells()
	{
		_prevCellMap.Clear();
		var grid = _model.CellGrid;
		for (int c = 0; c < _model.Columns; c++)
			for (int r = 0; r < _model.Rows; r++)
			{
				var cell = grid[c, r];
				if (cell.IsAlive)
					_prevCellMap[(c, r)] = ((int)cell.CellType, cell.Nationality);
			}
	}

	// ── Timer / render loop ───────────────────────────────────────────────────────

	private async Task OnTimerTick()
	{
		if (!_running)
			return;
		CapturePrevCells();
		_model.Step();
		var hadEvents = _model.PendingEvents.Count > 0;
		bool floodStarted = _model.PendingEvents.Any(e => e.StartsWith("flood_start:"));
		await InvokeAsync(async () =>
		{
			if (hadEvents)
				CollectEvents();
			await RenderFrame();
			UpdateTypeCounts();
			if (_model.FailureReason != null)
			{
				_running = false;
				_timer.Stop();
				if (_autoContinue)
				{
					await Restart();
					_running = true;
					_timer.Start();
				}
				else
				{
					ParseFailureReason(_model.FailureReason!, _model.Generation);
					_showFailurePopup = true;
				}
			}
			StateHasChanged();
			if (floodStarted)
				await RunFloodAnimation();
			if (_running)
				_timer.Start();
		});
	}

	// Delay between each rendered flood layer (ms).
	private const int FloodLayerDelayMs = 350;

	/// <summary>
	/// Animates the flood by rendering one border-layer kill per frame, pausing
	/// the regular simulation timer until all layers are done.
	/// </summary>
	private async Task RunFloodAnimation()
	{
		_model.PendingMoves.Clear();
		bool moreLayersRemain = true;
		while (moreLayersRemain)
		{
			await Task.Delay(FloodLayerDelayMs);
			CapturePrevCells();
			moreLayersRemain = _model.DoFloodLayer();
			await RenderFrame();
			StateHasChanged();
		}
		_model.FinalizeFlood();
		CollectEvents();
		StateHasChanged();
		await Task.Delay(FloodLayerDelayMs);
	}

	private async Task RenderFrame()
	{
		if (!_canvasReady)
			return;

		var grid = _model.CellGrid;
		int c = _model.Columns;
		int r = _model.Rows;
		var cells = new List<object>(c * r);

		for (int col = 0; col < c; col++)
			for (int row = 0; row < r; row++)
			{
				var cell = grid[col, row];
				if (!cell.IsAlive)
					continue;
				cells.Add(new { col, row, type = (int)cell.CellType, nat = cell.Nationality, alive = cell.IsAlive });
			}

		var moves = _model.PendingMoves
						.Select(m => new { fromCol = m.FromCol, fromRow = m.FromRow, toCol = m.ToCol, toRow = m.ToRow, type = m.CellType, nat = m.Nationality })
						.ToList<object>();

		var births = new List<object>();
		var deaths = new List<object>();
		var epicDeaths = new List<object>();
		var coronations = new List<object>();

		if (_prevCellMap.Count > 0)
		{
			var moveSrcSet = _model.PendingMoves.Select(m => (m.FromCol, m.FromRow)).ToHashSet();
			var moveDstSet = _model.PendingMoves.Select(m => (m.ToCol, m.ToRow)).ToHashSet();

			foreach (var kv in _prevCellMap)
			{
				var (col, row) = kv.Key;
				var (type, nat) = kv.Value;
				if (col < c && row < r
								&& !grid[col, row].IsAlive
								&& !moveSrcSet.Contains((col, row))
								&& !moveDstSet.Contains((col, row)))
				{
					bool isEpic = type == (int)CellType.King || type == (int)CellType.Immortal;
					if (isEpic)
						epicDeaths.Add(new { col, row, type, nat });
					else
						deaths.Add(new { col, row, type, nat });
				}
			}

			for (int col = 0; col < c; col++)
				for (int row = 0; row < r; row++)
				{
					var cell = grid[col, row];
					if (cell.IsAlive
									&& !_prevCellMap.ContainsKey((col, row))
									&& !moveSrcSet.Contains((col, row))
									&& !moveDstSet.Contains((col, row)))
					{
						births.Add(new { col, row, type = (int)cell.CellType, nat = cell.Nationality });
					}
					if (cell.IsAlive && cell.CellType == CellType.King
									&& _prevCellMap.TryGetValue((col, row), out var prev)
									&& prev.type != (int)CellType.King)
					{
						coronations.Add(new { col, row, type = (int)cell.CellType, nat = cell.Nationality });
					}
				}
		}

		var nationColors = Cell_Nation.NationColors;
		var liveNationIndices = _model.Nations.Keys.ToList();
		var famine = new { active = _model.FamineActive, quadrant = _model.FamineQuadrant };
		var flood = new { active = _model.FloodActive };
		await JS.InvokeVoidAsync("ConwaysInterop.renderFrame",
						cells, nationColors, liveNationIndices, c, r,
						moves, births, deaths, epicDeaths, coronations,
						_animationEnabled, _intervalMs, famine, flood);
	}

	private void UpdateTypeCounts()
	{
		var counts = new int[TypeNames.Length];
		var grid = _model.CellGrid;
		for (int col = 0; col < _model.Columns; col++)
			for (int row = 0; row < _model.Rows; row++)
				if (grid[col, row].IsAlive)
					counts[(int)grid[col, row].CellType]++;

		_typeCounts = Enumerable.Range(1, TypeNames.Length - 1)
						.Where(i => counts[i] > 0 || !_spawnWeights.TryGetValue(TypeNames[i], out var w) || w > 0)
						.Select(i => (TypeNames[i], TypeColors[i], counts[i]))
						.OrderBy(t => t.Item1)
						.ToList();
	}

	// ── Dispose ───────────────────────────────────────────────────────────────────

	public async ValueTask DisposeAsync()
	{
		_timer.Stop();
		_timer.Dispose();
		_dotNetRef?.Dispose();
		await Task.CompletedTask;
	}
}
