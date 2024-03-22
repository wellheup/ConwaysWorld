using UnityEngine;

public abstract class Cell
{
    virtual protected Color liveColor { get; set; }
    virtual protected Color deadColor { get; set; }
    virtual protected Color currentColor { get; set; }
    protected bool isAlive = false;

    public Cell()
    {

    }
    public Cell(bool isAlive)
    {
        this.isAlive = isAlive;
        liveColor = Color.black;
        deadColor = Color.white;
        if (isAlive)
        {
            currentColor = liveColor;
        }
        else
        {
            currentColor = deadColor;
        }
    }
    public bool GetIsAlive()
    {
        return isAlive;
    }

    public Color getCurrentColor()
    {
        return this.currentColor;
    }

    public virtual void Live()
    {
        isAlive = true;
        currentColor = liveColor;
    }

    public virtual void Die()
    {
        isAlive = false;
        currentColor = deadColor;
    }

    public virtual bool IsAliveNextGen(int liveNeighbors)
    {
        bool isAliveNextGen;
        // Apply the rules of the game.
        if (isAlive && liveNeighbors < 2)
        {
            isAliveNextGen = false; // Die due to underpopulation
        }
        else if (isAlive && (liveNeighbors == 2 || liveNeighbors == 3))
        {
            isAliveNextGen = true; // Live on
        }
        else if (isAlive && liveNeighbors > 3)
        {
            isAliveNextGen = false; // Die due to overpopulation
        }
        else if (!isAlive && liveNeighbors == 3)
        {
            isAliveNextGen = true; // Become alive due to reproduction
        }
        else if (!isAlive && liveNeighbors != 3)
        {
            isAliveNextGen = false; // Stays dead
        }
        else
        {
            isAliveNextGen = isAlive; // Stay the same
        }

        return isAliveNextGen;
    }
}
