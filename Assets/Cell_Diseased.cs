using System.Collections.Generic;
using UnityEngine;

public class Cell_Diseased : Cell
{
    private int countDown = 3;
    public Cell_Diseased(int column, int row, bool isAlive = true)
    {
        this.IsAlive = isAlive;
        LiveColor = Color.green;
        DeadColor = Color.white;
        CurrentColor = isAlive ? LiveColor : DeadColor;
        Column = column;
        Row = row;
        Conditions = new List<string>();
    }

    public override void Live(Cell[,] cellGrid)
    {
        IsAlive = true;
        CurrentColor = LiveColor;
        InfectNeighbors(cellGrid, CellNeighborhood); //ToDo: SEE IF I CAN DO THIS WITHOUT PASSING CELLGRID
    }

    public override bool SetAliveNextGen(Cell[,] cellGrid, Neighborhood neighborhood)
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
        else
        {
            IsAliveNextGen = false;
        }
        CellNeighborhood = neighborhood;

        return IsAliveNextGen;
    }

    private void InfectNeighbors(Cell[,] cellGrid, Neighborhood neighborhood)
    {
        // mark neighbors as infected
        for (int i = 0; i < neighborhood.NeighborHoodKeys.Length; i++)
        {
            if (Random.Range(1, 101) < 15 && neighborhood.NeighborHoodKeys[i] != "center")
            {
                int nCellCol = neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].Column;
                int nCellRow = neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].Row;
                cellGrid[nCellCol, nCellRow].Conditions.Add("infected");

                Debug.Log("infect neighbor " + neighborhood.NeighborHoodKeys[i] + " of " + Column + ", " + Row + " it is " + neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].Column + ", " + neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].Row + ", " + neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].GetType() + " and, IsAlive = " + neighborhood.NeighborhoodDict[neighborhood.NeighborHoodKeys[i]].GetIsAlive());
            }
        }
    }
}
