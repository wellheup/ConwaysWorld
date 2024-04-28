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
        Column = column;
        Row = row;
        Conditions = new List<string>();
        CellType = 2;
    }

    public override bool CalcCellAliveNextGen()
    {
        if (Age > 15 && CellNeighborhood.NumNeighbors == 0)
        {
            return false;
        }
        return IsAlive;
    }
}
