namespace ConwaysWorld.Simulation;

public class Model
{
    public Cell[,] CellGrid = null!;
    public bool[,] AliveNextGenGrid = null!;
    public Dictionary<int, Cell_Nation> Nations = new();

    private readonly SimulationSettings _settings;
    private Cell_Generator _generator;
    private int _currentPopulation;
    private int _columns;
    private int _rows;

    public int Columns => _columns;
    public int Rows => _rows;
    public int CurrentPopulation => _currentPopulation;
    public int Generation { get; private set; }

    public Model(SimulationSettings settings)
    {
        _settings = settings;
        _generator = new Cell_Generator(settings);
        _columns = settings.StartColumns;
        _rows = settings.StartRows;
        PopulateGrid();
    }

    public void Restart()
    {
        _generator = new Cell_Generator(_settings);
        _columns = _settings.StartColumns;
        _rows = _settings.StartRows;
        Generation = 0;
        _currentPopulation = 0;
        PopulateGrid();
    }

    public void PopulateGrid()
    {
        CellGrid = new Cell[_columns, _rows];
        AliveNextGenGrid = new bool[_columns, _rows];

        for (int c = 0; c < _columns; c++)
            for (int r = 0; r < _rows; r++)
                CellGrid[c, r] = _generator.InitializeRandomCell(c, r);

        InitializeNations();
        UpdateNeighborhoodsGrid();
        CountInitialPopulation();
        UpdateNations();
    }

    private void CountInitialPopulation()
    {
        _currentPopulation = 0;
        for (int c = 0; c < _columns; c++)
            for (int r = 0; r < _rows; r++)
                if (CellGrid[c, r].IsAlive) _currentPopulation++;
    }

    private void InitializeNations()
    {
        Nations = new Dictionary<int, Cell_Nation>();
        float basePct = _settings.BasePercentLiving;
        float numNations = basePct * _columns * _rows / _settings.MinCellsPerNation;
        int count = (int)Math.Min(numNations, Cell_Nation.NationColors.Count);
        for (int i = 0; i < count; i++)
            Nations[i] = new Cell_Nation(i);
    }

    private void ResizeCellGrid()
    {
        var old = CellGrid;
        int newCols = _columns + 2;
        int newRows = _rows + 2;
        var newGrid = new Cell[newCols, newRows];

        for (int c = 0; c < newCols; c++)
        {
            for (int r = 0; r < newRows; r++)
            {
                if (c == 0 || c == newCols - 1 || r == 0 || r == newRows - 1)
                {
                    newGrid[c, r] = new Cell_Basic(c, r, false);
                }
                else
                {
                    newGrid[c, r] = old[c - 1, r - 1];
                    newGrid[c, r].Column = c;
                    newGrid[c, r].Row = r;
                }
            }
        }

        _columns = newCols;
        _rows = newRows;
        CellGrid = newGrid;
        AliveNextGenGrid = new bool[_columns, _rows];
        UpdateNeighborhoodsGrid();
    }

    public void UpdateNeighborhoodsGrid()
    {
        for (int c = 0; c < _columns; c++)
            for (int r = 0; r < _rows; r++)
                CellGrid[c, r].CellNeighborhood = new Cell_Neighborhood(CellGrid, c, r);
    }

    public void UpdateAliveNextGenGrid()
    {
        for (int c = 0; c < _columns; c++)
            for (int r = 0; r < _rows; r++)
                AliveNextGenGrid[c, r] = CellGrid[c, r].CalcCellAliveNextGen();
    }

    public int UpdateCellLives()
    {
        _currentPopulation = 0;
        foreach (var nation in Nations.Values)
            nation.CitizensList.Clear();

        for (int c = 0; c < _columns; c++)
        {
            for (int r = 0; r < _rows; r++)
            {
                bool wasAlive = CellGrid[c, r].IsAlive;
                bool willLive = AliveNextGenGrid[c, r];

                if (wasAlive)
                {
                    if (willLive)
                    {
                        CellGrid[c, r].Live();
                        var nat = CellGrid[c, r].Nationality;
                        if (nat >= 0 && Nations.ContainsKey(nat))
                            Nations[nat].CitizensList.Add(CellGrid[c, r]);
                    }
                    else
                    {
                        CellGrid[c, r].Die();
                    }
                    _currentPopulation++;
                }
                else
                {
                    if (willLive)
                        CellGrid[c, r].Live();
                }
            }
        }
        return _currentPopulation;
    }

    public void UpdateCellConditions()
    {
        bool needResize = false;

        for (int c = 0; c < _columns; c++)
        {
            for (int r = 0; r < _rows; r++)
            {
                var cell = CellGrid[c, r];

                if (cell.Conditions.Contains("cleanup"))
                {
                    CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Basic, false);
                    continue;
                }

                if (cell.Conditions.Contains("immune"))
                {
                    cell.Conditions.RemoveWhere(s => s.StartsWith("d_") || s.StartsWith("p_"));
                }

                if (cell.CellType != CellType.Doctor && cell.CellType != CellType.Immortal)
                {
                    string? diseaseFound = null;
                    string? plagueFound = null;
                    foreach (var cond in cell.Conditions)
                    {
                        if (cond.StartsWith("d_")) { diseaseFound = cond; break; }
                        if (cond.StartsWith("p_")) { plagueFound = cond; break; }
                    }
                    if (plagueFound != null)
                        CellGrid[c, r] = Cell_Diseased.Infect(CellGrid[c, r], plagueFound, CellType.Plague);
                    else if (diseaseFound != null)
                        CellGrid[c, r] = Cell_Diseased.Infect(CellGrid[c, r], diseaseFound, CellType.Diseased);
                }

                if (cell.Conditions.Contains("mature"))
                    cell.Breed(CellGrid);

                if (cell.Conditions.Contains("immaculate"))
                    cell.Immaculate(CellGrid);

                if (cell.IsAlive && cell.Conditions.Contains("exploring"))
                    needResize = true;

                if (cell.IsAlive && cell.Age >= 1 && cell.Nationality < 0)
                    cell.Nationality = SimRandom.Range(0, Nations.Count > 0 ? Nations.Count : 1);

                if (cell.IsAlive && cell.CellType == CellType.Basic && cell.Conditions.Contains("toWar"))
                {
                    CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Warrior, true);
                    CellGrid[c, r].Conditions.Remove("toWar");
                }

                if (cell.CellType is CellType.Hunter or CellType.Warrior && cell.IdleTurns >= 3)
                {
                    CellGrid[c, r] = Cell.ReplaceCell(cell, CellType.Basic, cell.IsAlive);
                }
            }
        }

        if (needResize && !IsMaxGrid())
            ResizeCellGrid();
    }

    public void UpdateSpecialActions()
    {
        for (int c = 0; c < _columns; c++)
            for (int r = 0; r < _rows; r++)
                CellGrid[c, r].SpecialActions(CellGrid);
    }

    public void AddRandomLife()
    {
        float totalCells = _columns * _rows;
        if (totalCells == 0) return;
        if (_currentPopulation / totalCells > _settings.MinLifePercent) return;

        int numNew;
        if (_settings.PopMode == PopMode.Percent)
            numNew = (int)(totalCells * _settings.PopValue / 100f);
        else
            numNew = _settings.PopValue;

        int added = 0;
        int attempts = 0;
        int maxAttempts = numNew * 10;

        while (added < numNew && attempts < maxAttempts)
        {
            int rc = SimRandom.Range(0, _columns);
            int rr = SimRandom.Range(0, _rows);
            if (!CellGrid[rc, rr].IsAlive && !AliveNextGenGrid[rc, rr])
            {
                CellGrid[rc, rr] = _generator.InitializeRandomCell(rc, rr);
                added++;
            }
            attempts++;
        }
    }

    public void UpdateNations()
    {
        foreach (var nation in Nations.Values)
            nation.Census(CellGrid);

        float basePct = _settings.BasePercentLiving;
        float numNations = basePct * _columns * _rows / _settings.MinCellsPerNation;
        int target = (int)Math.Min(numNations, Cell_Nation.NationColors.Count);
        for (int i = Nations.Count; i < target; i++)
            Nations[i] = new Cell_Nation(i);
    }

    public int Step()
    {
        UpdateNeighborhoodsGrid();
        UpdateAliveNextGenGrid();
        UpdateCellLives();
        UpdateCellConditions();
        UpdateNeighborhoodsGrid();
        UpdateSpecialActions();
        AddRandomLife();
        UpdateNations();
        Generation++;
        return _currentPopulation;
    }

    private bool IsMaxGrid()
    {
        int limit = _settings.MaxGridSize;
        if (limit <= 0) return false;
        return _columns >= limit || _rows >= limit;
    }
}
