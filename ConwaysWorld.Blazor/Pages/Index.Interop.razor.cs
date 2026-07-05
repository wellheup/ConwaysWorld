using ConwaysWorld.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ConwaysWorld.Blazor.Pages;

public partial class Index
{
	// ── Simulation playback controls ──────────────────────────────────────────────

	private async Task TogglePlay()
	{
		if (_editMode)
			return;
		_running = !_running;
		if (_running)
			_timer.Start();
		else
			_timer.Stop();
		await Task.CompletedTask;
	}

	private async Task StepOnce()
	{
		if (_editMode)
			return;
		CapturePrevCells();
		_model.Step();
		CollectEvents();
		await RenderFrame();
		UpdateTypeCounts();
	}

	private async Task Restart()
	{
		_timer.Stop();
		_running = false;
		_showFailurePopup = false;
		_failureMessage = "";
		_failureStats = "";
		if (_autoFitGrid)
		{ await RecalcRowsFromCols(); InitSettings(); _model = new Model(_settings); }
		else
			_model.Restart();
		_prevCellMap.Clear();
		await RenderFrame();
		CapturePrevCells();
		UpdateTypeCounts();
	}

	private void OnSpeedChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var v))
		{
			_intervalMs = Math.Clamp(v, 50, 10000);
			_timer.Interval = _intervalMs;
		}
	}

	private void OnSpeedInputChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var v))
		{
			_intervalMs = Math.Clamp(v, 50, 10000);
			_timer.Interval = _intervalMs;
		}
	}

	private async Task ToggleFullscreen()
	{
		await JS.InvokeVoidAsync("ConwaysInterop.toggleFullscreen");
	}

	// ── JS-invokable callbacks (keyboard + mouse) ─────────────────────────────────

	[JSInvokable]
	public void OnHover(int col, int row, double mouseX, double mouseY)
	{
		if (col < 0 || row < 0 || col >= _model.Columns || row >= _model.Rows)
		{
			_tooltip = null;
			InvokeAsync(StateHasChanged);
			return;
		}
		var cell = _model.CellGrid[col, row];
		if (!cell.IsAlive)
		{
			_tooltip = null;
			InvokeAsync(StateHasChanged);
			return;
		}
		var conds = cell.Conditions
				.Where(c => !c.StartsWith("vax_") && c != "mature" && c != "immaculate")
				.Take(6)
				.ToArray();
		_tooltip = new TooltipData(cell.CellType.ToString(), cell.Nationality, cell.Age, conds);
		_tooltipX = mouseX + 14;
		_tooltipY = mouseY + 14;
		InvokeAsync(StateHasChanged);
	}

	[JSInvokable]
	public void OnCellClick(int col, int row)
	{
		_model.SelectedCol = col;
		_model.SelectedRow = row;
		InvokeAsync(StateHasChanged);
	}

	[JSInvokable]
	public void OnKeyTogglePlay()
	{
		if (_editMode)
			return;
		_running = !_running;
		if (_running)
			_timer.Start();
		else
			_timer.Stop();
		InvokeAsync(StateHasChanged);
	}

	[JSInvokable]
	public void OnKeyRestart()
	{
		_timer.Stop();
		_running = false;
		_showFailurePopup = false;
		_failureMessage = "";
		_failureStats = "";
		_model.Restart();
		_prevCellMap.Clear();
		_ = InvokeAsync(async () =>
		{
			await RenderFrame();
			CapturePrevCells();
			UpdateTypeCounts();
			StateHasChanged();
		});
	}

	[JSInvokable]
	public void OnKeyEscape()
	{
		if (_editMode)
		{
			_ = InvokeAsync(ToggleEditMode);
			return;
		}
		if (_showSettings)
			_showSettings = false;
		else if (_showFailurePopup)
			_showFailurePopup = false;
		InvokeAsync(StateHasChanged);
	}

	[JSInvokable]
	public void OnFullscreenChange(bool isFullscreen)
	{
		_isFullscreen = isFullscreen;
		InvokeAsync(StateHasChanged);
	}

	// ── Failure popup parsing ─────────────────────────────────────────────────────

	/// <summary>
	/// Parses a FailureReason string into structured popup fields.
	/// Formats: extinction:…  failure_pop:…  failure_pop_growth:…  failure_stagnation:…
	/// </summary>
	private void ParseFailureReason(string reason, int generation)
	{
		int colon = reason.IndexOf(':');
		string prefix = colon >= 0 ? reason[..colon] : "";
		string body = colon >= 0 ? reason[(colon + 1)..].Trim() : reason;
		int pop = _model.CurrentPopulation;

		switch (prefix)
		{
			case "extinction":
				_failureIcon = "💀";
				_failureTitle = "Extinction";
				_failureMessage = "All cells have died — the simulation has gone extinct.";
				_failureStats = $"Generation {generation}  ·  Final pop 0";
				break;
			case "failure_pop":
				_failureIcon = "📉";
				_failureTitle = "Population Threshold";
				_failureMessage = body;
				_failureStats = $"Generation {generation}  ·  Pop {pop}  ·  Threshold {_failurePopThreshold}";
				break;
			case "failure_pop_growth":
				_failureIcon = "📉";
				_failureTitle = "Post-Growth Collapse";
				_failureMessage = body;
				_failureStats = $"Generation {generation}  ·  Pop {pop}  ·  Threshold {_failurePopAfterGrowthThreshold}";
				break;
			case "failure_stagnation":
				_failureIcon = "⏸";
				_failureTitle = "Stagnation";
				_failureMessage = body;
				_failureStats = $"Generation {generation}  ·  Pop {pop}  ·  Idle steps {_stagnationSteps}";
				break;
			default:
				_failureIcon = "⚠️";
				_failureTitle = "Simulation Ended";
				_failureMessage = body.Length > 0 ? body : reason;
				_failureStats = $"Generation {generation}  ·  Pop {pop}";
				break;
		}
	}
}
