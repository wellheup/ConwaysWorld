using UnityEngine;
using System.Collections.Generic;

public class Cell_Basic : Cell
{
    public Cell_Basic(int column, int row, bool isAlive)
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
}
