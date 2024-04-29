using System.Collections.Generic;
using UnityEngine;

public class Cell_Diseased : Cell
{
    protected int CountDown = 3, TransmissionRate = 50;
    public Cell_Diseased(int column, int row, bool isAlive)
    {
        this.IsAlive = isAlive;
        LiveColor = Color.green;
        DeadColor = Color.white;
        // CurrentColor = isAlive ? LiveColor : DeadColor;
        Column = column;
        Row = row;
        Conditions = new List<string>();
    }

    public override void Live(Cell[,] cellGrid)
    {
        IsAlive = true;
        CurrentColor = LiveColor;
        Age++;
        SpreadDisease(cellGrid, CellNeighborhood); //ToDo: SEE IF I CAN DO THIS WITHOUT PASSING CELLGRID
        if (Age > MatureAge && !Conditions.Contains("mature"))
        {
            Conditions.Add("mature");
        }
        CellType = 3;
    }

    public override void Die()
    {
        IsAlive = false;
        CurrentColor = DeadColor;
        Conditions.Remove("infected");
    }

    public override bool CalcCellAliveNextGen()
    {
        CountDown--;
        if (CountDown <= 0)
        {
            return false;
        }
        return LiveBasic();
    }

    public static Cell Infect(Cell cell)
    {
        if (cell.GetIsAlive() && cell.GetType() != typeof(Cell_Diseased))
        {
            return ReplaceCell(cell, 2, true);
        }
        return cell;
    }

    private void SpreadDisease(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        // mark neighbors as infected
        for (int i = 0; i < neighborhood.NeighborHoodKeys.Length; i++)
        {
            if (Random.Range(1, 101) < TransmissionRate && neighborhood.NeighborHoodKeys[i] != "center")
            {
                int nCellCol = neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].Column;
                int nCellRow = neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].Row;
                cellGrid[nCellCol, nCellRow].Conditions.Add("infected");
            }
        }
    }
}

public class Cell_Plague : Cell_Diseased
{
    // plague(diseased cell that spreads disease with higher infection rate than diseased to all touching cells)
    public Cell_Plague(int column, int row, bool isAlive) : base(column, row, isAlive)
    {
        this.IsAlive = isAlive;
        LiveColor = Color.green;
        DeadColor = Color.white;
        // CurrentColor = isAlive ? LiveColor : DeadColor;
        Column = column;
        Row = row;
        // Conditions = new List<string>();
        TransmissionRate = 75;
    }

    public override void Live(Cell[,] cellGrid)
    {
        IsAlive = true;
        CurrentColor = LiveColor;
        Age++;
        SpreadDisease(cellGrid, CellNeighborhood); //ToDo: SEE IF I CAN DO THIS WITHOUT PASSING CELLGRID
        if (Age > MatureAge && !Conditions.Contains("mature"))
        {
            Conditions.Add("mature");
        }
        CellType = 3;
    }

    public override void Die()
    {
        IsAlive = false;
        CurrentColor = DeadColor;
        Conditions.Remove("plagued");
    }

    private void SpreadDisease(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        // mark neighbors as infected
        for (int i = 0; i < neighborhood.NeighborHoodKeys.Length; i++)
        {
            if (Random.Range(1, 101) < TransmissionRate && neighborhood.NeighborHoodKeys[i] != "center")
            {
                int nCellCol = neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].Column;
                int nCellRow = neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].Row;
                cellGrid[nCellCol, nCellRow].Conditions.Add("plagued");
            }
        }
    }
}