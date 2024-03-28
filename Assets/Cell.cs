using UnityEngine;

public abstract class Cell
{
    virtual protected Color LiveColor { get; set; }
    virtual protected Color DeadColor { get; set; }
    virtual protected Color CurrentColor { get; set; }
    protected Neighborhood CellNeighborhood;
    protected bool IsAliveNextGen = false;
    public string[] Attributes;
    protected bool IsAlive = false;
    public int Column = 0, Row = 0;

    public Cell()
    {
        LiveColor = Color.black;
        DeadColor = Color.white;
        CurrentColor = IsAlive ? LiveColor : DeadColor;
        IsAliveNextGen = IsAlive;
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
    }

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
    }

    public virtual void Die()
    {
        IsAlive = false;
        CurrentColor = DeadColor;
    }

    public virtual bool DetermineAliveNextGen(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        LiveBasic(neighborhood);
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
}
