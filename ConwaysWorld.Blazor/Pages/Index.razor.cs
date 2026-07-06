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
	private bool _pureConwayMode = false;
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

	// Nationless types: Islander, Barbarian, Wayfinder, PlagueRat, Zombie, Necromancer, Bomber.
	// Everything else is nation-capable.
	private static readonly HashSet<CellType> _nationlessTypes = new()
				{
								CellType.Islander, CellType.Barbarian, CellType.Wayfinder,
								CellType.PlagueRat, CellType.Zombie, CellType.Necromancer, CellType.Bomber,
				};
	private static bool IsNationCapable(CellType t) =>
					t != CellType.Dead && !_nationlessTypes.Contains(t);

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
						.Select(m => new
						{
							fromCol = m.FromCol,
							fromRow = m.FromRow,
							toCol = m.ToCol,
							toRow = m.ToRow,
							type = m.CellType,
							nat = m.Nationality,
						})
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
