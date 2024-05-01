using System.Collections.Generic;
using UnityEngine;

public abstract class Cell
{
    public Color LiveColor;
    public Color DeadColor;
    public Color CurrentColor;
    public Neighborhood CellNeighborhood;
    public List<string> Conditions;
    protected bool IsAlive = false;
    public int Column = 0, Row = 0, Age = 0, CellType = 0, MatureAge = 10;
    // public e_CellType CellType = e_CellType.Cell;

    public Cell()
    {
        LiveColor = Color.black;
        DeadColor = Color.white;
        CurrentColor = IsAlive ? LiveColor : DeadColor;
        Conditions = new List<string>();
    }

    public Cell(int column, int row, bool isAlive)
    {
        this.IsAlive = isAlive;
        LiveColor = Color.black;
        DeadColor = Color.white;
        CurrentColor = isAlive ? LiveColor : DeadColor;
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

    public Color GetCurrentColor()
    {
        return this.CurrentColor;
    }

    public virtual void Live(Cell[,] cellGrid)
    {
        IsAlive = true;
        CurrentColor = LiveColor;
        Age++;
        if (Age > MatureAge && !Conditions.Contains("mature"))
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

    public virtual bool CalcCellAliveNextGen()
    {
        return LiveBasic();
    }

    protected virtual bool LiveBasic()
    {
        if (IsAlive && CellNeighborhood.NumNeighbors < 2)
        {
            return false; // Die due to underpopulation
        }
        else if (IsAlive && (CellNeighborhood.NumNeighbors == 2 || CellNeighborhood.NumNeighbors == 3))
        {
            return true; // Live on
        }
        else if (IsAlive && CellNeighborhood.NumNeighbors > 3)
        {
            return false; // Die due to overpopulation
        }
        else if (!IsAlive && CellNeighborhood.NumNeighbors == 3)
        {
            return true; // Become alive due to reproduction
        }
        else if (!IsAlive && CellNeighborhood.NumNeighbors != 3)
        {
            return false; // Stays dead
        }
        else
        {
            return IsAlive; // Stay the same
        }
    }

    public virtual void SpecialActions(Cell[,] cellGrid)
    {

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
            cell = new Cell_Basic(column, row, isAlive); //this should not occur...
        }
        cell.Conditions = oldCell.Conditions;
        cell.CellNeighborhood = oldCell.CellNeighborhood;

        return cell;
    }

    public void SwapCells(Cell dest, Cell[,] cellGrid)
    {
        int oldCol = this.Column, oldRow = this.Row;
        cellGrid[dest.Column, dest.Row] = this;
        cellGrid[this.Column, this.Row] = dest;

        this.CellNeighborhood = new Neighborhood(cellGrid, dest.Column, dest.Row);
        dest.CellNeighborhood = new Neighborhood(cellGrid, oldCol, oldRow);
    }

    public virtual void Breed()
    {
        Conditions.RemoveAll(item => item == "mature");
        Age = 0;
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

    private void LiveNoNeighbors(Cell[,] CellGrid, Cell cell)
    {
        if (cell.CellNeighborhood.NumNeighbors == 0)
        {
            cell.CellNeighborhood = new Neighborhood(CellGrid, cell.Column, cell.Row);
            cell.Live(CellGrid);
        }
    }

    public virtual void Immaculate(Cell[,] CellGrid)
    {
        Conditions.RemoveAll(item => item == "immaculate");
        LiveNoNeighbors(CellGrid, this);
        if (IsAlive)
        {
            if (Random.Range(1, 3) == 1)
            {
                LiveNoNeighbors(CellGrid, CellNeighborhood.NeighborhoodDict["north"]);
                LiveNoNeighbors(CellGrid, CellNeighborhood.NeighborhoodDict["south"]);
            }
            else
            {
                LiveNoNeighbors(CellGrid, CellNeighborhood.NeighborhoodDict["west"]);
                LiveNoNeighbors(CellGrid, CellNeighborhood.NeighborhoodDict["east"]);
            }

        }

    }
}


public enum E_CellType
{
    Cell,
    Cell_Basic,
    Cell_Immortal,
    Cell_Diseased,
}