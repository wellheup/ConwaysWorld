namespace ConwaysWorld.Simulation;

public class Cell_Warrior : Cell_Hunter
{
    private readonly List<CellType> _warriorPreyTypes;

    public Cell_Warrior(int column, int row, bool isAlive)
        : base(column, row, isAlive)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.Warrior;
        Conditions = new HashSet<string>();
        _warriorPreyTypes = new List<CellType> { CellType.Diseased, CellType.Plague };
    }

    public override void Live()
    {
        IsAlive = true;
        Age++;
        ChooseNation();
    }

    private bool IsEnemy(Cell c) =>
        c.IsAlive &&
        c.Nationality != Nationality &&
        _warriorPreyTypes.Contains(c.CellType);

    private bool IsCombatWinner(Cell target)
    {
        int myPower = GetStrength(this);
        int theirPower = GetStrength(target);
        if (Age > target.Age) myPower++; else theirPower++;
        if (myPower == theirPower) return SimRandom.CoinFlip();
        return myPower > theirPower;
    }

    private static int GetStrength(Cell cell)
    {
        int power = 0;
        foreach (var neighbor in cell.CellNeighborhood.NeighborsDict.Values)
        {
            if (neighbor.Nationality == cell.Nationality)
            {
                power++;
                if (neighbor.CellType == CellType.Warrior) power++;
                if (neighbor.CellType == CellType.King) power += 2;
            }
        }
        return power;
    }

    protected bool Fight(Cell[,] cellGrid)
    {
        var target = SelectNearbyCellByRule(cellGrid, IsEnemy, 2);
        if (target == null) return false;

        if (target.CellType == CellType.Warrior && !IsCombatWinner(target))
        {
            Die();
            Conditions.Add("cleanup");
            return false;
        }
        else
        {
            SwapCells(this, target, cellGrid);
            target.Die();
            target.Conditions.Add("cleanup");
            return true;
        }
    }

    public override void SpecialActions(Cell[,] cellGrid)
    {
        if (!IsAlive) return;
        bool didFight = Fight(cellGrid);
        if (didFight)
            IdleTurns = 0;
        else
            IdleTurns++;
    }
}
