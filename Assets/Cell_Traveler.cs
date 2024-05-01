using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// traveler (swaps places with random neighbor each turn)
/// </summary>
/// <remarks>
/// 
/// </remarks>
public class Cell_Traveler : Cell
{
    private int DeathCountDown = 0;
    private int MaxAloneTime = 10;
    private string Direction;

    public Cell_Traveler(int column, int row, bool isAlive)
    {
        IsAlive = isAlive;
        Column = column;
        Row = row;
        Direction = Neighborhood.NeighborHoodKeys[Random.Range(0, Neighborhood.NeighborHoodKeys.Length)];
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
    }

    public override bool CalcCellAliveNextGen()
    {
        if (DeathCountDown > MaxAloneTime)
        {
            return false;
        }
        return IsAlive;
    }

    public override void SpecialActions(Cell[,] cellGrid)
    {
        SwapCells(CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
    }
}
