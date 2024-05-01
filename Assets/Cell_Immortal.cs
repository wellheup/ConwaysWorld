using UnityEngine;
using System.Collections.Generic;

public class Cell_Immortal : Cell
{
    private int DeathCounter = 0;
    private int MaxAloneTime = 10;
    public Cell_Immortal(int column, int row, bool isAlive = true)
    {
        this.IsAlive = isAlive;
        LiveColor = Color.red;
        DeadColor = Color.white;
        CurrentColor = isAlive ? LiveColor : DeadColor;
        Column = column;
        Row = row;
        Conditions = new List<string>();
        CellType = 2;
    }

    public override void Live(Cell[,] cellGrid)
    {
        IsAlive = true;
        CurrentColor = LiveColor;
        Age++;
        if (Age > MatureAge && !Conditions.Contains("mature"))
        {
            Conditions.Add("mature");
        }
        if (CellNeighborhood.NumNeighbors == 0)
        {
            DeathCounter++;
        }
        else
        {
            DeathCounter = 0;
        }
    }

    public override bool CalcCellAliveNextGen()
    {
        if (DeathCounter > MaxAloneTime)
        {
            return false;
        }
        return IsAlive;
    }
}
