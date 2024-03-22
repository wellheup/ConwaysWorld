using UnityEngine;

public class Cell_Basic : Cell
{
    public Cell_Basic(bool isAlive)
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
}
