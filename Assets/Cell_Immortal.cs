using UnityEngine;
using System.Collections.Generic;

public class Cell_Immortal : Cell
{
    public Cell_Immortal(int column, int row, bool isAlive = true)
    {
        this.IsAlive = isAlive;
        LiveColor = Color.black;
        DeadColor = Color.white;
        CurrentColor = isAlive ? LiveColor : DeadColor;
        IsAliveNextGen = IsAlive;
        Column = column;
        Row = row;
        Conditions = new List<string>();
    }

    public override void Die()
    {
        IsAlive = true;
        CurrentColor = LiveColor;
    }

    public override bool SetAliveNextGen(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        CellNeighborhood = neighborhood;
        return IsAlive;
    }
}
