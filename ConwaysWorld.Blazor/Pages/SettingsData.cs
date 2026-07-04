using System.Collections.Generic;

namespace ConwaysWorld.Blazor.Pages;

/// <summary>
/// Mutable snapshot of all settings shown in the Settings modal.
/// Populated from Index.razor fields when the modal opens; copied back on Apply.
/// </summary>
public class SettingsData
{
	public bool AutoFitGrid { get; set; }
	public int StartCols { get; set; } = 50;
	public int StartRows { get; set; } = 50;
	public int MaxGrid { get; set; } = 120;
	public int MaxNations { get; set; } = 4;
	public int StartClusters { get; set; } = 2;
	public bool AnimationEnabled { get; set; } = true;
	public double PopPercent { get; set; } = 0.4;
	public string PopPercentStr { get; set; } = "0.4";
	public int PopCount { get; set; } = 10;
	public int MinNeighbors { get; set; } = 2;
	public int MaxNeighbors { get; set; } = 3;
	public int BirthNeighbors { get; set; } = 3;
	public bool FamineEnabled { get; set; } = true;
	public int FamineCooldown { get; set; } = 15;
	public int FamineDuration { get; set; } = 10;
	public bool FloodEnabled { get; set; } = true;
	public bool RandomLifeEnabled { get; set; }
	public bool ReactiveDoctor { get; set; }
	public double RandomLifeThresholdPct { get; set; } = 5.0;
	public string RandomLifeThresholdPctStr { get; set; } = "5.0";
	public int RandomLifeThresholdCount { get; set; } = 10;
	public bool AutoContinue { get; set; }
	public bool AllowGridExpansion { get; set; } = true;
	public bool NationsEnabled { get; set; } = true;
	public int FailurePopThreshold { get; set; }
	public int FailurePopAfterGrowthThreshold { get; set; }
	public int StagnationSteps { get; set; } = 10;
	public Dictionary<string, int> SpawnWeights { get; set; } = new();
}
