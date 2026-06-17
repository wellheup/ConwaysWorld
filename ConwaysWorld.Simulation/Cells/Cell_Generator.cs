namespace ConwaysWorld.Simulation;

public class Cell_Generator
{
    private readonly struct SpawnFrequency
    {
        public CellType Type { get; init; }
        public float Freq { get; init; }
    }

    private readonly List<SpawnFrequency> _frequencies = new();

    public Cell_Generator(SimulationSettings settings)
    {
        BuildFrequencies(settings);
    }

    private void BuildFrequencies(SimulationSettings settings)
    {
        _frequencies.Clear();

        float basePct = settings.BasePercentLiving;
        int totalWeight = 0;
        foreach (var kv in settings.SpawnWeights)
            if (settings.SpawnEnabled.Contains(kv.Key))
                totalWeight += kv.Value;

        float livingBudget = basePct;
        foreach (var kv in settings.SpawnWeights)
        {
            if (!settings.SpawnEnabled.Contains(kv.Key)) continue;
            float share = totalWeight > 0 ? (float)kv.Value / totalWeight : 0f;
            _frequencies.Add(new SpawnFrequency { Type = kv.Key, Freq = livingBudget * share });
        }

        float deadPct = 1f - basePct;
        _frequencies.Add(new SpawnFrequency { Type = CellType.Dead, Freq = deadPct });
    }

    private CellType GetRandomCellType()
    {
        float roll = SimRandom.Value;
        float cumulative = 0f;
        foreach (var entry in _frequencies)
        {
            cumulative += entry.Freq;
            if (roll < cumulative) return entry.Type;
        }
        return CellType.Dead;
    }

    public Cell InitializeRandomCell(int column, int row)
    {
        float variant = SimRandom.Value;
        return GetRandomCellType() switch
        {
            CellType.Basic => CreateBasic(column, row),
            CellType.Immortal => new Cell_Immortal(column, row, true),
            CellType.Diseased => variant > 0.2f
                ? new Cell_Diseased(column, row, true)
                : (Cell)new Cell_Plague(column, row, true),
            CellType.Plague => new Cell_Plague(column, row, true),
            CellType.Traveler => variant > 0.4f
                ? new Cell_Traveler(column, row, true)
                : (Cell)new Cell_Explorer(column, row, true),
            CellType.Explorer => new Cell_Explorer(column, row, true),
            CellType.Doctor => new Cell_Doctor(column, row, true),
            CellType.Hunter => new Cell_Hunter(column, row, true),
            CellType.Bomber => new Cell_Bomber(column, row, true),
            _ => new Cell_Basic(column, row, false),
        };
    }

    private static Cell CreateBasic(int column, int row)
    {
        var cell = new Cell_Basic(column, row, true);
        if (SimRandom.Range(1, 5) == 1)
            cell.Conditions.Add("immune");
        if (SimRandom.Range(1, 101) == 1)
            cell.Conditions.Add("immaculate");
        return cell;
    }
}
