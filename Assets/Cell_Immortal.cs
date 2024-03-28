using UnityEngine;

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
    }

    public override void Die()
    {
        IsAlive = true;
        CurrentColor = LiveColor;
    }

    public override bool DetermineAliveNextGen(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        return IsAlive;
    }
}
