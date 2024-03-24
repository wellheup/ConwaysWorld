using UnityEngine;

public class Cell_Immortal : Cell
{
    public Cell_Immortal(bool isAlive)
    {
        this.isAlive = isAlive;
        liveColor = Color.red;
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

    public override void Die()
    {
        isAlive = true;
        currentColor = liveColor;
    }

    public override bool IsAliveNextGen(Neighborhood neighborhood)
    {
        return isAlive;
    }
}
