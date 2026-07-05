using System.Text.Json;
using System.Text.Json.Serialization;
using ConwaysWorld.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ConwaysWorld.Blazor.Pages;

public partial class Index
{
	// ── Build SimulationSettings from UI fields ────────────────────────────────────

	private void InitSettings()
	{
		_settings = new SimulationSettings
		{
			StartColumns = _startCols,
			StartRows = _startRows,
			MaxGridSize = _maxGrid,
			MaxNations = Math.Clamp(_maxNations, 1, 20),
			StartClusters = Math.Max(0, _startClusters),
			PopMode = PopMode.Count,
			PopValue = _popCount,
			MinLivingNeighbors = Math.Clamp(_minNeighbors, 0, 7),
			MaxLivingNeighbors = Math.Clamp(_maxNeighbors, 0, 8),
			BirthNeighborCount = Math.Clamp(_birthNeighbors, 0, 8),
			FamineEnabled = _famineEnabled,
			FamineCooldown = Math.Max(1, _famineCooldown),
			FamineDuration = Math.Max(1, _famineDuration),
			FloodEnabled = _floodEnabled,
			RandomLifeEnabled = _randomLifeEnabled,
			ReactiveDoctor = _reactiveDoctor,
			MinLifePercent = (float)(_randomLifeThresholdPct / 100.0),
			AllowGridExpansion = _allowGridExpansion,
			NationsEnabled = _nationsEnabled,
			FailurePopThreshold = _failurePopThreshold,
			FailurePopAfterGrowthThreshold = _failurePopAfterGrowthThreshold,
			StagnationSteps = _stagnationSteps,
		};
		foreach (var kv in _spawnWeights)
			if (Enum.TryParse<CellType>(kv.Key, out var ct))
				_settings.SpawnWeights[ct] = kv.Value;
	}

	// ── Persist / restore settings via localStorage ───────────────────────────────

	private void ApplySettingsJson(string json)
	{
		try
		{
			var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var s = JsonSerializer.Deserialize<SavedSettings>(json, opts);
			if (s == null)
				return;
			_startCols = Math.Clamp(s.StartCols, 5, 300);
			_startRows = Math.Clamp(s.StartRows, 5, 300);
			_maxGrid = Math.Clamp(s.MaxGrid, 0, 500);
			_maxNations = Math.Clamp(s.MaxNations, 1, 20);
			_startClusters = Math.Max(0, s.StartClusters);
			if (s.PopCount >= 0)
			{
				_popCount = Math.Clamp(s.PopCount, 0, _startCols * _startRows);
				double pct = _startCols * _startRows > 0
												? Math.Round(_popCount * 100.0 / (_startCols * _startRows), 1) : 0;
				_popPercent = pct;
				_popPercentStr = pct.ToString("F1");
			}
			else
			{
				_popPercent = s.PopPercent <= 0 ? 10.0 : Math.Clamp(s.PopPercent, 0.1, 100);
				_popPercentStr = _popPercent.ToString("F1");
				_popCount = (int)Math.Round(_popPercent / 100.0 * _startCols * _startRows);
			}
			_intervalMs = Math.Clamp(s.IntervalMs, 50, 5000);
			_animationEnabled = s.AnimationEnabled;
			_minNeighbors = Math.Clamp(s.MinNeighbors, 0, 7);
			_maxNeighbors = Math.Clamp(s.MaxNeighbors, 0, 8);
			_birthNeighbors = Math.Clamp(s.BirthNeighbors, 0, 8);
			_famineEnabled = s.FamineEnabled;
			_famineCooldown = Math.Max(1, s.FamineCooldown);
			_famineDuration = Math.Max(1, s.FamineDuration);
			_floodEnabled = s.FloodEnabled;
			_randomLifeEnabled = s.RandomLifeEnabled;
			_reactiveDoctor = s.ReactiveDoctor;
			_randomLifeThresholdPct = s.RandomLifeThresholdPct;
			_randomLifeThresholdPctStr = _randomLifeThresholdPct.ToString("F1");
			_randomLifeThresholdCount = (int)Math.Round(_randomLifeThresholdPct / 100.0 * _startCols * _startRows);
			_autoContinue = s.AutoContinue;
			_allowGridExpansion = s.AllowGridExpansion;
			_nationsEnabled = s.NationsEnabled;
			_failurePopThreshold = s.FailurePopThreshold;
			_failurePopAfterGrowthThreshold = s.FailurePopAfterGrowthThreshold;
			_stagnationSteps = s.StagnationSteps;
			_autoFitGrid = s.AutoFitGrid;
			if (s.SpawnWeights != null && s.Version >= SettingsVersion)
				foreach (var kv in s.SpawnWeights)
					if (_spawnWeights.ContainsKey(kv.Key))
						_spawnWeights[kv.Key] = kv.Value;
		}
		catch { }
	}

	private string SerializeSettings()
	{
		var s = new SavedSettings
		{
			Version = SettingsVersion,
			StartCols = _startCols,
			StartRows = _startRows,
			MaxGrid = _maxGrid,
			MaxNations = _maxNations,
			StartClusters = _startClusters,
			PopPercent = _popPercent,
			PopCount = _popCount,
			IntervalMs = _intervalMs,
			AnimationEnabled = _animationEnabled,
			MinNeighbors = _minNeighbors,
			MaxNeighbors = _maxNeighbors,
			BirthNeighbors = _birthNeighbors,
			FamineEnabled = _famineEnabled,
			FamineCooldown = _famineCooldown,
			FamineDuration = _famineDuration,
			FloodEnabled = _floodEnabled,
			RandomLifeEnabled = _randomLifeEnabled,
			ReactiveDoctor = _reactiveDoctor,
			RandomLifeThresholdPct = _randomLifeThresholdPct,
			AutoContinue = _autoContinue,
			AllowGridExpansion = _allowGridExpansion,
			NationsEnabled = _nationsEnabled,
			FailurePopThreshold = _failurePopThreshold,
			FailurePopAfterGrowthThreshold = _failurePopAfterGrowthThreshold,
			StagnationSteps = _stagnationSteps,
			AutoFitGrid = _autoFitGrid,
			SpawnWeights = new Dictionary<string, int>(_spawnWeights),
		};
		return JsonSerializer.Serialize(s);
	}

	// ── Grid/pop dimension helpers ────────────────────────────────────────────────

	private async Task RecalcRowsFromCols()
	{
		try
		{
			var size = await JS.InvokeAsync<System.Text.Json.JsonElement>("ConwaysInterop.getCanvasSize");
			int w = size.GetProperty("width").GetInt32();
			int h = size.GetProperty("height").GetInt32();
			if (w > 0 && h > 0)
				_startRows = Math.Clamp((int)Math.Round(_startCols * (double)h / w), 5, 300);
		}
		catch { }
	}

	private async Task RecalcColsFromRows()
	{
		try
		{
			var size = await JS.InvokeAsync<System.Text.Json.JsonElement>("ConwaysInterop.getCanvasSize");
			int w = size.GetProperty("width").GetInt32();
			int h = size.GetProperty("height").GetInt32();
			if (w > 0 && h > 0)
				_startCols = Math.Clamp((int)Math.Round(_startRows * (double)w / h), 5, 300);
		}
		catch { }
	}

	// ── Settings event handlers ───────────────────────────────────────────────────

	private async Task OnStartColsChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var v))
		{
			_startCols = Math.Clamp(v, 5, 300);
			if (_autoFitGrid)
				await RecalcRowsFromCols();
		}
	}

	private async Task OnStartRowsChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var v))
		{
			_startRows = Math.Clamp(v, 5, 300);
			if (_autoFitGrid)
				await RecalcColsFromRows();
		}
	}

	private async Task OnAutoFitChanged(ChangeEventArgs e)
	{
		_autoFitGrid = e.Value is true;
		if (_autoFitGrid)
			await RecalcRowsFromCols();
	}

	private void OnPopPercentChange(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out var pct))
		{
			_popPercent = Math.Clamp(pct, 0, 100);
			_popPercentStr = _popPercent.ToString("F1");
			_popCount = (int)Math.Round(_popPercent / 100.0 * _startCols * _startRows);
		}
	}

	private void OnPopCountChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var cnt))
		{
			int total = _startCols * _startRows;
			_popCount = Math.Clamp(cnt, 0, total);
			_popPercent = total > 0 ? Math.Round(_popCount * 100.0 / total, 1) : 0;
			_popPercentStr = _popPercent.ToString("F1");
		}
	}

	private void OnRandomLifeThresholdPctChange(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out var pct))
		{
			_randomLifeThresholdPct = Math.Clamp(pct, 0, 100);
			_randomLifeThresholdPctStr = _randomLifeThresholdPct.ToString("F1");
			_randomLifeThresholdCount = (int)Math.Round(_randomLifeThresholdPct / 100.0 * _startCols * _startRows);
		}
	}

	private void OnRandomLifeThresholdCountChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var cnt))
		{
			int total = _startCols * _startRows;
			_randomLifeThresholdCount = Math.Clamp(cnt, 0, total);
			_randomLifeThresholdPct = total > 0 ? Math.Round(_randomLifeThresholdCount * 100.0 / total, 1) : 0;
			_randomLifeThresholdPctStr = _randomLifeThresholdPct.ToString("F1");
		}
	}

	private void OnWeightChange(string typeName, ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var v))
			_spawnWeights[typeName] = v;
	}

	// ── Settings actions ──────────────────────────────────────────────────────────

	private void ApplyTestCase()
	{
		if (_selectedTestCase < 0 || _selectedTestCase >= SimulationTestCases.All.Count)
			return;
		var tc = SimulationTestCases.All[_selectedTestCase];
		var s = tc.Settings;
		_startCols = s.StartColumns;
		_startRows = s.StartRows;
		_maxGrid = s.MaxGridSize;
		_maxNations = s.MaxNations;
		_startClusters = s.StartClusters;
		_popCount = s.PopValue;
		_popPercent = Math.Round((double)s.PopValue / (s.StartColumns * s.StartRows) * 100.0, 1);
		_popPercentStr = _popPercent.ToString("F1");
		_famineEnabled = s.FamineEnabled;
		_floodEnabled = s.FloodEnabled;
		_randomLifeEnabled = s.RandomLifeEnabled;
		_allowGridExpansion = s.AllowGridExpansion;
		foreach (var kv in s.SpawnWeights)
		{
			var key = kv.Key.ToString();
			if (_spawnWeights.ContainsKey(key))
				_spawnWeights[key] = kv.Value;
		}
		_settingsTab = 0;
		_showSettings = true;
	}

	private async Task ApplyAndRestart()
	{
		_timer.Stop();
		_running = false;
		_showSettings = false;
		_showFailurePopup = false;
		_failureMessage = "";
		_failureStats = "";
		if (_autoFitGrid)
			await RecalcRowsFromCols();
		InitSettings();
		_eventLog.Clear();
		_model = new Model(_settings);
		_timer.Interval = _intervalMs;
		try
		{ await JS.InvokeVoidAsync("ConwaysInterop.saveSettings", SerializeSettings()); }
		catch { }
		_prevCellMap.Clear();
		await RenderFrame();
		CapturePrevCells();
		UpdateTypeCounts();
	}

	private async Task RandomizeAndRestart()
	{
		_timer.Stop();
		_running = false;
		_startCols = _uiRandom.Next(5, 301);
		_startRows = _uiRandom.Next(5, 301);
		_maxGrid = _uiRandom.Next(0, 1001);
		_maxNations = _uiRandom.Next(1, 21);
		_startClusters = _uiRandom.Next(0, 11);
		_popPercent = Math.Round(_uiRandom.NextDouble() * 29.0 + 1.0, 1);
		_popPercentStr = _popPercent.ToString("F1");
		_popCount = (int)Math.Round(_popPercent / 100.0 * _startCols * _startRows);
		_intervalMs = _uiRandom.Next(1, 41) * 50;
		_timer.Interval = _intervalMs;
		_animationEnabled = _uiRandom.Next(2) == 0;
		_minNeighbors = _uiRandom.Next(1, 4);
		_maxNeighbors = _uiRandom.Next(_minNeighbors, 9);
		_birthNeighbors = _minNeighbors + 1;
		_famineEnabled = _uiRandom.Next(2) == 0;
		_famineCooldown = _uiRandom.Next(5, 51);
		_famineDuration = _uiRandom.Next(5, 31);
		_floodEnabled = _uiRandom.Next(2) == 0;
		_randomLifeEnabled = _uiRandom.Next(4) == 0;
		var keys = _spawnWeights.Keys.ToList();
		bool anyPositive = false;
		foreach (var k in keys)
		{
			int w = _uiRandom.Next(0, 21);
			_spawnWeights[k] = w;
			if (w > 0)
				anyPositive = true;
		}
		if (!anyPositive && keys.Count > 0)
			_spawnWeights[keys[_uiRandom.Next(keys.Count)]] = _uiRandom.Next(1, 21);
		InitSettings();
		_eventLog.Clear();
		_model = new Model(_settings);
		try
		{ await JS.InvokeVoidAsync("ConwaysInterop.saveSettings", SerializeSettings()); }
		catch { }
		_prevCellMap.Clear();
		await RenderFrame();
		CapturePrevCells();
		UpdateTypeCounts();
	}

	private async Task ResetToDefaults()
	{
		_timer.Stop();
		_running = false;
		_startCols = 50;
		_startRows = 50;
		_maxGrid = 120;
		_maxNations = 4;
		_startClusters = 2;
		_popPercent = 0.4;
		_popPercentStr = "0.4";
		_popCount = 10;
		_intervalMs = 1000;
		_animationEnabled = true;
		_minNeighbors = 2;
		_maxNeighbors = 3;
		_birthNeighbors = _minNeighbors + 1;
		_famineEnabled = true;
		_famineCooldown = 15;
		_famineDuration = 10;
		_floodEnabled = true;
		_randomLifeEnabled = false;
		_randomLifeThresholdPct = 5.0;
		_randomLifeThresholdPctStr = "5.0";
		_randomLifeThresholdCount = (int)Math.Round(5.0 / 100.0 * _startCols * _startRows);
		_autoContinue = false;
		_allowGridExpansion = true;
		_nationsEnabled = true;
		_failurePopThreshold = 0;
		_failurePopAfterGrowthThreshold = 0;
		_stagnationSteps = 10;
		var defaultSettings = new SimulationSettings();
		_spawnWeights = defaultSettings.SpawnWeights.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
		_timer.Interval = _intervalMs;
		InitSettings();
		_eventLog.Clear();
		_model = new Model(_settings);
		try
		{ await JS.InvokeVoidAsync("ConwaysInterop.clearSettings"); }
		catch { }
		_prevCellMap.Clear();
		await RenderFrame();
		CapturePrevCells();
		UpdateTypeCounts();
	}

	private void OnRandomLifeThresholdPctKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Enter")
			OnRandomLifeThresholdPctChange(new ChangeEventArgs { Value = _randomLifeThresholdPctStr });
	}
	private void OnRandomLifeThresholdCountKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Enter")
			OnRandomLifeThresholdCountChange(new ChangeEventArgs { Value = _randomLifeThresholdCount.ToString() });
	}
}
