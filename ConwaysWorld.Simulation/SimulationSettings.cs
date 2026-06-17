namespace ConwaysWorld.Simulation;

public enum PopMode { Percent, Count }

public class SimulationSettings
{
    public int StartColumns { get; set; } = 40;
    public int StartRows { get; set; } = 40;

    public int MaxGridSize { get; set; } = 120;

    public int UserCellSize { get; set; } = 14;

    public PopMode PopMode { get; set; } = PopMode.Percent;
    public int PopValue { get; set; } = 10;

    public int MinCellsPerNation { get; set; } = 3;
    public int MaxNations { get; set; } = 20;

    public float MinLifePercent { get; set; } = 0.05f;

    public Dictionary<CellType, int> SpawnWeights { get; set; } = new()
    {
        { CellType.Basic,    50 },
        { CellType.Immortal,  2 },
        { CellType.Diseased, 15 },
        { CellType.Plague,    3 },
        { CellType.Traveler,  6 },
        { CellType.Explorer,  3 },
        { CellType.Doctor,    5 },
        { CellType.Hunter,    5 },
        { CellType.Bomber,    8 },
    };

    public HashSet<CellType> SpawnEnabled { get; set; } = new()
    {
        CellType.Basic,
        CellType.Immortal,
        CellType.Diseased,
        CellType.Plague,
        CellType.Traveler,
        CellType.Explorer,
        CellType.Doctor,
        CellType.Hunter,
        CellType.Bomber,
    };

    public float BasePercentLiving
    {
        get
        {
            if (PopMode == PopMode.Percent)
                return PopValue / 100f;
            int totalCells = StartColumns * StartRows;
            return totalCells > 0 ? (float)PopValue / totalCells : 0.1f;
        }
    }
}
