using UnityEngine;
using static CellGenerator;
/// <summary>
/// traveler (swaps places with random neighbor each turn)
/// </summary>
/// <remarks>
/// 
/// </remarks>
public class Cell_Traveler : Cell
{
    protected int DeathCountDown = 0;
    protected int MaxAloneTime = 3;
    protected string Direction;
    protected bool SpecialPerformed = false;

    public Cell_Traveler(int column, int row, bool isAlive) : base(column, row, isAlive)
    {
        IsAlive = isAlive;
        Column = column;
        Row = row;
        Direction = Neighborhood.NeighborHoodKeys[Random.Range(0, Neighborhood.NeighborHoodKeys.Length)];
        CellType = E_CellType.Cell_Traveler;
    }

    public override void Live(Cell[,] cellGrid)
    {
        IsAlive = true;
        CurrentColor = LiveColor;
        Age++;
        if (CellNeighborhood.NumNeighbors == 0)
        {
            DeathCountDown++;
        }
        else
        {
            DeathCountDown = 0;
        }
        SpecialPerformed = false;
    }

    public override void Die()
    {
        base.Die();
        SpecialPerformed = true;
    }

    public override bool CalcCellAliveNextGen()
    {
        if (DeathCountDown > MaxAloneTime)
        {
            return false;
        }
        return true;
    }

    public override void SpecialActions(Cell[,] cellGrid)
    {
        if (IsAlive && !SpecialPerformed)
        {
            SwapCells(CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
            SpecialPerformed = true;
        }
    }
}
