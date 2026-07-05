namespace ConwaysWorld.Simulation;

/// <summary>
/// Pre-configured simulation scenarios for development testing and demonstration.
/// Each entry is a <see cref="SimulationTestCase"/> whose <see cref="SimulationTestCase.Settings"/>
/// can be passed directly to <see cref="Model"/>.
/// </summary>
public static partial class SimulationTestCases
{
	/// <summary>All available test cases in display order.</summary>
	public static IReadOnlyList<SimulationTestCase> All { get; private set; } = null!;

	static SimulationTestCases()
	{
		var list = new List<SimulationTestCase>(AllPart1);
		list.AddRange(AllPart2);
		All = list.AsReadOnly();
	}
}
