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
        Column = column;
        Row = row;
        Conditions = new List<string>();
        // CellType = e_CellType.Cell_Basic;
        CellType = 1;
    }
}
