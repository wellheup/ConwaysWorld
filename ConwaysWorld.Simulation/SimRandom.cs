namespace ConwaysWorld.Simulation;

public static class SimRandom
{
    private static Random _rng = new();

    public static void SetSeed(int seed) => _rng = new Random(seed);

    public static int Range(int minInclusive, int maxExclusive) =>
        _rng.Next(minInclusive, maxExclusive);

    public static float Value => (float)_rng.NextDouble();

    public static bool CoinFlip() => _rng.Next(2) == 0;

    public static T Choice<T>(IList<T> list) => list[_rng.Next(list.Count)];
}
