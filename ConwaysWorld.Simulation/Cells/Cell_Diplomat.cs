namespace ConwaysWorld.Simulation;

public class Cell_Diplomat : Cell
{
    private Cell? _target;
    private int _idleTurns = 0;

    public Cell_Diplomat(int column, int row, bool isAlive)
    {
        Column = column;
        Row = row;
        IsAlive = isAlive;
        CellType = CellType.Diplomat;
        Conditions = new HashSet<string>();
        _target = null;
    }

    public override void Live()
    {
        base.Live();
    }

    private bool IsForeignAlive(Cell c) =>
        c.IsAlive && c.Nationality != Nationality && c.Nationality >= 0;

    private void Convert(Cell[,] cellGrid)
    {
        foreach (var neighbor in CellNeighborhood.NeighborsDict.Values)
        {
            var target = cellGrid[neighbor.Column, neighbor.Row];
            if (target.IsAlive && target.Nationality != Nationality && target.Nationality >= 0)
            {
                if (SimRandom.Range(0, 4) == 0)
                {
                    target.Nationality = Nationality;
                    _idleTurns = 0;
                }
            }
        }
    }

    private void MoveTowardForeignNation(Cell[,] cellGrid)
    {
        if (_target == null || !_target.IsAlive || _target.Nationality == Nationality)
            _target = SelectNearbyCellByRule(cellGrid, IsForeignAlive, 8);

        if (_target != null)
        {
            var step = FindNeighborInDirOfCell(cellGrid, _target);
            if (step != this)
                SwapCells(this, step, cellGrid);
        }
        else
        {
            _idleTurns++;
        }
    }

    public override void SpecialActions(Cell[,] cellGrid)
    {
        if (!IsAlive) return;
        Convert(cellGrid);
        MoveTowardForeignNation(cellGrid);
    }
}
