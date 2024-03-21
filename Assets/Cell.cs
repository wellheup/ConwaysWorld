using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    private int cellType = 0;
    private Color color = Color.white;

    public void updateCellType(int cellType)
    {
        this.cellType = cellType;
        switch (cellType)
        {
            case 1:
                live();
                break;
            default:
                die();
                break;
        }
    }

    public int isAlive()
    {
        return cellType;
    }

    public void live()
    {
        cellType = 1;
        color = Color.black;
        GetComponent<Renderer>().material.SetColor("_Color", color);
    }

    public void die()
    {
        cellType = 0;
        color = Color.white;
        GetComponent<Renderer>().material.SetColor("_Color", Color.white);
    }

    public int applyLife(int liveNeighbors)
    {
        int nextVal;
        // Apply the rules of the game.
        if (cellType == 1 && liveNeighbors < 2)
        {
            nextVal = 0; // Die due to underpopulation
        }
        else if (cellType == 1 && (liveNeighbors == 2 || liveNeighbors == 3))
        {
            nextVal = 1; // Live on
        }
        else if (cellType == 1 && liveNeighbors > 3)
        {
            nextVal = 0; // Die due to overpopulation
        }
        else if (cellType == 0 && liveNeighbors == 3)
        {
            nextVal = 1; // Become alive due to reproduction
        }
        else if (cellType == 0 && liveNeighbors != 3)
        {
            nextVal = 0; // Stays dead
        }
        else
        {
            nextVal = cellType; // Stay the same
        }

        if (nextVal == 1)
        {
            live();
        }
        else
        {
            die();
        }
        return nextVal;
    }
}
