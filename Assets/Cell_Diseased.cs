using System.Collections.Generic;
using UnityEngine;

public class Cell_Diseased : Cell
{
    private int countDown = 6;
    public Cell_Diseased(int column, int row, bool isAlive = true)
    {
        this.IsAlive = isAlive;
        LiveColor = Color.green;
        DeadColor = Color.white;
        CurrentColor = isAlive ? LiveColor : DeadColor;
        Column = column;
        Row = row;
    }

    public Cell_Diseased(int column, int row, int countDown, Cell[,] cellGrid, bool isAlive = true, bool isAliveNextGen = true)
    {
        this.IsAlive = isAlive;
        this.IsAliveNextGen = true;
        LiveColor = Color.green;
        DeadColor = Color.blue;
        CurrentColor = isAlive ? LiveColor : DeadColor;
        Column = column;
        Row = row;
        this.countDown = countDown;
        this.CellNeighborhood = new Neighborhood(cellGrid, column, row);
    }

    public override void Live(Cell[,] cellGrid)
    {
        IsAlive = true;
        CurrentColor = LiveColor;
        InfectNeighbors(cellGrid, CellNeighborhood);
    }

    public override bool DetermineAliveNextGen(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        countDown--;
        if (countDown > 0)
        {
            if (countDown == 1)
            {
                Debug.Log("countDown " + countDown);
            }
            IsAliveNextGen = LiveBasic(neighborhood);
        }
        CellNeighborhood = neighborhood;

        return IsAliveNextGen;
    }

    private void InfectNeighbors(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        // this is getting called, but I can't see any updates happening, it doesn't seem like the new cells are being added to the main grid, else I think they should actually live forever...
        for (int i = 0; i < neighborhood.NeighborHoodKeys.Length; i++)
        {
            // if (Random.Range(1, 101) < 15)
            if (true)
            {
                Cell current = neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]];
                current.SetAllColors(Color.magenta);
                // Debug.Break();

                if (current.GetIsAlive() && current.GetType() != typeof(Cell_Diseased) && neighborhood.NeighborHoodKeys[i] != "center")
                {
                    Debug.Log("infect neighbor " + neighborhood.NeighborHoodKeys[i] + " of " + Column + ", " + Row + " it is " + current.Column + ", " + current.Row + ", " + current.GetType() + " and, IsAlive = " + current.GetIsAlive());

                    // current = new Cell_Diseased(current.Column, current.Row, countDown - 1, true);
                    cellGrid[current.Column, current.Row] = new Cell_Diseased(current.Column, current.Row, countDown - 1, cellGrid, true, true);
                }
            }
        }
    }
}
