namespace ConwaysWorld.Simulation;

/// <summary>
/// Thin wrapper around <see cref="System.Random"/> that provides a Unity-compatible API surface,
/// making it straightforward to swap in <c>UnityEngine.Random</c> when porting back to Unity.
/// <para>
/// All simulation randomness is routed through this class so that a fixed seed can be set for
/// reproducible replays or unit testing via <see cref="SetSeed"/>.
/// </para>
/// </summary>
public static class SimRandom
{
	private static Random _rng = new();

	/// <summary>
	/// Replaces the internal RNG instance with one seeded by <paramref name="seed"/>.
	/// Call before creating a <see cref="Model"/> for a deterministic run.
	/// </summary>
	public static void SetSeed(int seed) => _rng = new Random(seed);

	/// <summary>
	/// Returns a random integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
	/// Mirrors the signature of <c>UnityEngine.Random.Range(int, int)</c>.
	/// </summary>
	public static int Range(int minInclusive, int maxExclusive) =>
		_rng.Next(minInclusive, maxExclusive);

	/// <summary>
	/// Returns a random <see cref="float"/> in [0, 1).
	/// Mirrors <c>UnityEngine.Random.value</c>.
	/// </summary>
	public static float Value => (float)_rng.NextDouble();

	/// <summary>Returns <c>true</c> or <c>false</c> with equal probability.</summary>
	public static bool CoinFlip() => _rng.Next(2) == 0;

	/// <summary>Returns a uniformly random element from <paramref name="list"/>.</summary>
	public static T Choice<T>(IList<T> list) => list[_rng.Next(list.Count)];
}
