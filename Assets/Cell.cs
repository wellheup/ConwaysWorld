using System.Collections.Generic;
using UnityEngine;

public abstract class Cell
{
    virtual protected Color LiveColor { get; set; }
    virtual protected Color DeadColor { get; set; }
    virtual protected Color CurrentColor { get; set; }
    protected Neighborhood CellNeighborhood;
    public bool IsAliveNextGen = false;
    public List<string> Conditions;
    protected bool IsAlive = false;
    public int Column = 0, Row = 0, Age = 0, CellType = 0;
    // public e_CellType CellType = e_CellType.Cell;

    public Cell()
    {
        LiveColor = Color.black;
        DeadColor = Color.white;
        CurrentColor = IsAlive ? LiveColor : DeadColor;
        IsAliveNextGen = IsAlive;
        Conditions = new List<string>();
    }

    public Cell(int column, int row, bool isAlive)
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

    // Should only be used for debugging
    public void SetAllColors(Color color)
    {
        this.LiveColor = color;
        this.DeadColor = color;
        this.CurrentColor = color;
    }

    public bool GetIsAlive()
    {
        return IsAlive;
    }

    public bool GetIsAliveNextGen()
    {
        return IsAliveNextGen;
    }

    public Color GetCurrentColor()
    {
        return this.CurrentColor;
    }

    public virtual void Live(Cell[,] cellGrid)
    {
        IsAlive = true;
        CurrentColor = LiveColor;
        Age++;
        if (Age > 10 && !Conditions.Contains("mature"))
        {
            Conditions.Add("mature");
        }
    }

    public virtual void Die()
    {
        IsAlive = false;
        CurrentColor = DeadColor;
        Age = 0;
    }

    public virtual bool SetAliveNextGen(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        IsAliveNextGen = LiveBasic(neighborhood);
        CellNeighborhood = neighborhood;

        return IsAliveNextGen;
    }

    protected virtual bool LiveBasic(Neighborhood neighborhood)
    {
        // Apply the rules of the game.
        if (IsAlive && neighborhood.NumNeighbors < 2)
        {
            IsAliveNextGen = false; // Die due to underpopulation
        }
        else if (IsAlive && (neighborhood.NumNeighbors == 2 || neighborhood.NumNeighbors == 3))
        {
            IsAliveNextGen = true; // Live on
        }
        else if (IsAlive && neighborhood.NumNeighbors > 3)
        {
            IsAliveNextGen = false; // Die due to overpopulation
        }
        else if (!IsAlive && neighborhood.NumNeighbors == 3)
        {
            IsAliveNextGen = true; // Become alive due to reproduction
            // Debug.Log("Cell " + Column + ", " + Row + " it is " + (IsAlive ? "alive " : "dead ") + "and " + (IsAliveNextGen ? "will " : "will not ") + "live next gen. It has " + neighborhood.NumNeighbors + " neighbors.");

        }
        else if (!IsAlive && neighborhood.NumNeighbors != 3)
        {
            IsAliveNextGen = false; // Stays dead
        }
        else
        {
            IsAliveNextGen = IsAlive; // Stay the same
        }
        return IsAliveNextGen;
    }

    public static Cell ReplaceCell(Cell oldCell, int cellType, bool isAlive)
    {
        int column = oldCell.Column;
        int row = oldCell.Row;
        Cell cell;
        if (cellType == 1)
        {
            cell = new Cell_Immortal(column, row, isAlive);
        }
        else if (cellType == 2)
        {
            cell = new Cell_Diseased(column, row, isAlive);
        }
        else if (cellType == 3)
        {
            cell = new Cell_Basic(column, row, isAlive);
        }
        else
        {
            cell = new Cell_Basic(column, row, isAlive);
        }
        CloneCellData(oldCell, cell);

        return cell;
    }

    // copy any bits of data that need copying but are not initialized 
    public static void CloneCellData(Cell oldCell, Cell newCell)
    {
        newCell.Conditions = oldCell.Conditions;
        newCell.IsAliveNextGen = oldCell.IsAliveNextGen;

    }

    public virtual void Breed(Neighborhood neighborhood)
    {
        CellNeighborhood = neighborhood;
        List<Cell> cells = new List<Cell>();
        foreach (KeyValuePair<string, Cell> cell in CellNeighborhood.NeighborhoodDict)
        {
            if (!cell.Value.GetIsAlive())
            {
                cells.Add(cell.Value);
            }
        }
        int randNeighbor = Random.Range(0, cells.Count);
        cells[randNeighbor] = ReplaceCell(cells[randNeighbor], CellType, true);
    }
}


public enum E_CellType
{
    Cell,
    Cell_Basic,
    Cell_Immortal,
    Cell_Diseased,
}