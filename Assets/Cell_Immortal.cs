using UnityEngine;
using System.Collections.Generic;

public class Cell_Immortal : Cell
{
    public Cell_Immortal(int column, int row, bool isAlive = true)
    {
        this.IsAlive = isAlive;
        LiveColor = Color.red;
        DeadColor = Color.white;
        CurrentColor = isAlive ? LiveColor : DeadColor;
        IsAliveNextGen = IsAlive;
        Column = column;
        Row = row;
        Conditions = new List<string>();
        CellType = 2;
    }

    public override void Die()
    {
        if (Age > 15 && CellNeighborhood.NumNeighbors <= 1)
        {
            IsAlive = false;
            CurrentColor = DeadColor;
            Age = 0;
        }
        else
        {
            IsAlive = true;
            CurrentColor = LiveColor;
        }
    }

    public override bool SetAliveNextGen(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        CellNeighborhood = neighborhood;

        return true;
    }
}
