
using UnityEngine;
using static ConwaysWorld.CellGenerator;
/// <summary>
/// explorer (picks a random direction to move each turn, expands grid when going over edges, can last 3 cycles without neighbors)
/// </summary>
namespace ConwaysWorld
{
    public class Cell_Explorer : Cell_Traveler
    {
        public Cell_Explorer(int column, int row, bool isAlive) : base(column, row, isAlive)
        {
            IsAlive = isAlive;
            Column = column;
            Row = row;
            Direction = Neighborhood.NeighborHoodKeys[Random.Range(0, Neighborhood.NeighborHoodKeys.Length)];
            CellType = E_CellType.Cell_Traveler;
            MaxAloneTime = 3;
            LiveColor = Color.magenta;
        }

        protected bool IsNeighborOverEdge(Cell neighbor)
        {


            return true;
        }

        public override void SpecialActions(Cell[,] cellGrid)
        {
            if (IsAlive && !SpecialPerformed)
            {
                //TODO: implement

                if (true) //at edge of grid?
                {
                    //report to everything that the grid is growing
                    //update the columns/rows for every cell
                    //update the neighborhoods for all cells
                    //move the camera or resize the cells
                    SwapCells(CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
                    SpecialPerformed = true;
                }
                else
                {
                    SwapCells(CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
                    SpecialPerformed = true;
                }
            }
        }
    }
}