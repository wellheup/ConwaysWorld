namespace ConwaysWorld.Simulation;

public abstract class Cell
{
    public Cell_Neighborhood CellNeighborhood = null!;
    public HashSet<string> Conditions = new();

    public bool IsAlive { get; protected set; } = false;
    public CellType CellType = CellType.Dead;
    public int Age = 0;
    public int Column = 0;
    public int Row = 0;
    public int MatureAge = 3;
    public int Nationality = -1;
    public int MinLivingNeighbors = 2;
    public int MaxLivingNeighbors = 3;

    public int IdleTurns = 0;
    public int CrushCountDown = 0;

    public virtual void Live()
    {
        IsAlive = true;
        Age++;
        if (Age > MatureAge) Conditions.Add("mature");
        ChooseNation();
    }

    public virtual void Die()
    {
        IsAlive = false;
        Age = 0;
        Nationality = -1;
    }

    public virtual bool CalcCellAliveNextGen() => LiveBasic();

    protected virtual bool LiveBasic()
    {
        if (IsAlive && CellNeighborhood.NumNeighbors < MinLivingNeighbors)
            return false;
        if (IsAlive && CellNeighborhood.NumNeighbors >= MinLivingNeighbors && CellNeighborhood.NumNeighbors <= MaxLivingNeighbors)
            return true;
        if (IsAlive && CellNeighborhood.NumNeighbors > MaxLivingNeighbors)
            return false;
        if (!IsAlive && CellNeighborhood.NumNeighbors == MinLivingNeighbors)
            return true;
        if (!IsAlive && CellNeighborhood.NumNeighbors != MaxLivingNeighbors)
            return false;
        return IsAlive;
    }

    public virtual void SpecialActions(Cell[,] cellGrid) { }

    public static Cell ReplaceCell(Cell oldCell, CellType cellType, bool isAlive)
    {
        int col = oldCell.Column;
        int row = oldCell.Row;
        Cell cell = cellType switch
        {
            CellType.Basic     => new Cell_Basic(col, row, isAlive),
            CellType.Immortal  => new Cell_Immortal(col, row, isAlive),
            CellType.Diseased  => new Cell_Diseased(col, row, isAlive),
            CellType.Plague    => new Cell_Plague(col, row, isAlive),
            CellType.Traveler  => new Cell_Traveler(col, row, isAlive),
            CellType.Explorer  => new Cell_Explorer(col, row, isAlive),
            CellType.Doctor    => new Cell_Doctor(col, row, isAlive),
            CellType.Diplomat  => new Cell_Diplomat(col, row, isAlive),
            CellType.King      => new Cell_King(col, row, isAlive),
            CellType.Hunter    => new Cell_Hunter(col, row, isAlive),
            CellType.Bomber    => new Cell_Bomber(col, row, isAlive),
            CellType.Warrior   => new Cell_Warrior(col, row, isAlive),
            _                  => new Cell_Basic(col, row, isAlive),
        };
        cell.Conditions = new HashSet<string>(oldCell.Conditions);
        cell.CellNeighborhood = oldCell.CellNeighborhood;
        cell.Nationality = isAlive ? oldCell.Nationality : -1;
        return cell;
    }

    public static void SwapCells(Cell origin, Cell dest, Cell[,] cellGrid)
    {
        int oldCol = origin.Column;
        int oldRow = origin.Row;

        cellGrid[dest.Column, dest.Row] = origin;
        cellGrid[origin.Column, origin.Row] = dest;

        origin.Column = dest.Column;
        origin.Row = dest.Row;
        dest.Column = oldCol;
        dest.Row = oldRow;

        origin.CellNeighborhood = new Cell_Neighborhood(cellGrid, origin.Column, origin.Row);
        dest.CellNeighborhood   = new Cell_Neighborhood(cellGrid, dest.Column,   dest.Row);
    }

    public virtual void Breed(Cell[,] cellGrid)
    {
        Conditions.Remove("mature");
        if (!IsAlive) return;

        Age = 0;
        var empties = new List<Cell>();
        foreach (var kv in CellNeighborhood.NeighborhoodDict)
            if (kv.Value != null && !kv.Value.IsAlive)
                empties.Add(kv.Value);

        if (empties.Count == 0) return;
        int idx = SimRandom.Range(0, empties.Count);
        var slot = empties[idx];
        var newCell = ReplaceCell(slot, CellType, true);
        cellGrid[slot.Column, slot.Row] = newCell;
    }

    private void LiveNoNeighbors(Cell[,] cellGrid, Cell cell)
    {
        if (cell.CellNeighborhood.NumNeighbors == 0)
        {
            cell.CellNeighborhood = new Cell_Neighborhood(cellGrid, cell.Column, cell.Row);
            cell.Live();
        }
    }

    public virtual void Immaculate(Cell[,] cellGrid)
    {
        Conditions.Remove("immaculate");
        LiveNoNeighbors(cellGrid, this);
        if (!IsAlive) return;

        if (SimRandom.Range(1, 3) == 1)
        {
            LiveNoNeighbors(cellGrid, CellNeighborhood.NeighborhoodDict["north"]);
            LiveNoNeighbors(cellGrid, CellNeighborhood.NeighborhoodDict["south"]);
        }
        else
        {
            LiveNoNeighbors(cellGrid, CellNeighborhood.NeighborhoodDict["west"]);
            LiveNoNeighbors(cellGrid, CellNeighborhood.NeighborhoodDict["east"]);
        }
    }

    public static string RandomCondition(char prefix)
    {
        const string chars = "0123456789";
        var result = new char[8];
        for (int i = 0; i < result.Length; i++)
            result[i] = chars[SimRandom.Range(0, chars.Length)];
        return prefix + "_" + new string(result);
    }

    public void ChooseNation()
    {
        if (!IsAlive || Nationality >= 0) return;

        if (CellNeighborhood != null && CellNeighborhood.NumNeighbors > 0)
        {
            var neighborNations = new List<int>();
            foreach (var neighbor in CellNeighborhood.NeighborhoodDict.Values)
                if (neighbor.IsAlive && neighbor.Nationality >= 0)
                    neighborNations.Add(neighbor.Nationality);

            if (neighborNations.Count > 0)
                Nationality = neighborNations[SimRandom.Range(0, neighborNations.Count)];
        }
    }

    protected Cell? SelectNearbyCellByRule(Cell[,] cellGrid, Func<Cell, bool> rule, int maxRange)
    {
        if (maxRange <= 1) return null;
        var candidates = new List<Cell>();
        int range = 1;
        int cols = cellGrid.GetLength(0);
        int rows = cellGrid.GetLength(1);

        while (candidates.Count == 0 && range < maxRange)
        {
            for (int x = -range; x <= range; x++)
            {
                int tc = (Column + x + cols) % cols;

                int tr = (Row - range + rows) % rows;
                if (rule(cellGrid[tc, tr])) candidates.Add(cellGrid[tc, tr]);

                tr = (Row + range + rows) % rows;
                if (rule(cellGrid[tc, tr])) candidates.Add(cellGrid[tc, tr]);
            }
            for (int y = -range + 1; y <= range - 1; y++)
            {
                int tr = (Row + y + rows) % rows;

                int tc = (Column - range + cols) % cols;
                if (rule(cellGrid[tc, tr])) candidates.Add(cellGrid[tc, tr]);

                tc = (Column + range + cols) % cols;
                if (rule(cellGrid[tc, tr])) candidates.Add(cellGrid[tc, tr]);
            }
            range++;
        }

        return candidates.Count > 0 ? candidates[SimRandom.Range(0, candidates.Count)] : null;
    }

    protected List<Cell> GetAllCellsInRangeByRule(Cell[,] cellGrid, Func<Cell, bool> rule, int maxRange)
    {
        var result = new List<Cell>();
        int cols = cellGrid.GetLength(0);
        int rows = cellGrid.GetLength(1);
        for (int co = -maxRange; co <= maxRange; co++)
        {
            for (int ro = -maxRange; ro <= maxRange; ro++)
            {
                int nc = (Column + co + cols) % cols;
                int nr = (Row    + ro + rows) % rows;
                var c = cellGrid[nc, nr];
                if (c != this && rule(c))
                    result.Add(c);
            }
        }
        return result;
    }

    public Cell FindNeighborInDirOfCell(Cell[,] cellGrid, Cell target)
    {
        if (target == null) return this;

        int cols = cellGrid.GetLength(0);
        int rows = cellGrid.GetLength(1);

        int innerDistC = Math.Abs(Column - target.Column);
        int outerDistC = Math.Abs(cols - innerDistC);
        int targetDirC = Column == target.Column ? 0 : (Column < target.Column ? 1 : -1);
        int fastestDirC = innerDistC <= outerDistC ? 1 : -1;
        int nearestCol = (Column + targetDirC * fastestDirC + cols) % cols;

        int innerDistR = Math.Abs(Row - target.Row);
        int outerDistR = Math.Abs(rows - innerDistR);
        int targetDirR = Row == target.Row ? 0 : (Row < target.Row ? 1 : -1);
        int fastestDirR = innerDistR <= outerDistR ? 1 : -1;
        int nearestRow = (Row + targetDirR * fastestDirR + rows) % rows;

        return cellGrid[nearestCol, nearestRow];
    }
}
