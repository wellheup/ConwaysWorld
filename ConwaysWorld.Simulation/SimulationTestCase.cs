namespace ConwaysWorld.Simulation;

/// <summary>
/// A named, pre-configured simulation scenario used for development testing and demonstration.
/// The <see cref="Settings"/> object is ready to pass directly to <see cref="Model"/>.
/// </summary>
/// <param name="Name">Short display name shown in the test-case picker.</param>
/// <param name="Description">One-sentence description of what the scenario demonstrates.</param>
/// <param name="Settings">Pre-configured <see cref="SimulationSettings"/> for this scenario.</param>
public record SimulationTestCase(string Name, string Description, SimulationSettings Settings);
