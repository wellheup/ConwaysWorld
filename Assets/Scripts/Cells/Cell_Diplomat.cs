
using UnityEngine;
using System.Collections.Generic;
using static ConwaysWorld.Cell_Generator;
/// <summary>
/// diplomat (when a nation is large enough, it spawns and attempts to spread its nation to the nearest other nation)
/// </summary>
namespace ConwaysWorld
{
    public class Cell_Diplomat : Cell_Traveler
    {
        public Cell_Diplomat(int column, int row, bool isAlive) : base(column, row, isAlive)
        {
            CellType = E_CellType.Cell_Diplomat;
            MaxAloneTime = 4;
            Conditions = new List<string>();
        }

        public override void Live(Cell[,] cellGrid)
        {
            IsAlive = true;
            Age++;
            if (CellNeighborhood.NumNeighbors == 0)
            {
                DeathCountDown++;
            }
            else
            {
                DeathCountDown = 0;
            }
            SpecialPerformed = false;
            ChooseNation();
        }

        private Cell FindNearestOther(Cell[,] cellGrid)
        {
            List<Cell> nearestOthers = new();
            int range = 1,
                maxRange = 5;
            while (nearestOthers.Count == 0 && range < maxRange)
            {
                for (int x = range * -1; x <= range; x++)
                {
                    //row beneath
                    int targetCol = (Column + x + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                    int targetRow = (Row + range * -1 + cellGrid.GetLength(1)) % cellGrid.GetLength(1);
                    if (cellGrid[targetCol, targetRow].Nationality != Nationality)
                    {
                        nearestOthers.Add(cellGrid[targetCol, targetRow]);
                    }
                    //row above
                    targetRow = (Row + range + cellGrid.GetLength(1)) % cellGrid.GetLength(1);
                    if (cellGrid[targetCol, targetRow].Nationality != Nationality)
                    {
                        nearestOthers.Add(cellGrid[targetCol, targetRow]);
                    }
                }
                for (int y = range * -1 + 1; y <= range - 1; y++)
                {
                    //col left
                    int targetCol = (Column + range * -1 + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                    int targetRow = (Row + y + cellGrid.GetLength(1)) % cellGrid.GetLength(1);
                    if (cellGrid[targetCol, targetRow].Nationality != Nationality)
                    {
                        nearestOthers.Add(cellGrid[targetCol, targetRow]);
                    }
                    //col right
                    targetCol = (Column + range + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                    if (cellGrid[targetCol, targetRow].Nationality != Nationality)
                    {
                        nearestOthers.Add(cellGrid[targetCol, targetRow]);
                    }
                }
                range++;
            }
            if (nearestOthers.Count > 0)
            {
                // select a target cell to travel toward
                return nearestOthers[Random.Range(0, nearestOthers.Count)];
            }
            return null;
        }

        public Cell GetDiplomaticCellSwap(Cell[,] cellGrid)
        {
            Cell nearestOther = FindNearestOther(cellGrid);
            int nearestCol = Column,
                nearestRow = Row;
            if (nearestOther != null)
            {
                if (nearestOther.Column > Column)
                {
                    nearestCol = nearestOther.Column++;
                }
                if (nearestOther.Column < Column)
                {
                    nearestCol = nearestOther.Column--;
                }
                if (nearestOther.Row > Row)
                {
                    nearestRow = nearestOther.Row++;
                }
                if (nearestOther.Row < Row)
                {
                    nearestRow = nearestOther.Row--;
                }
                return cellGrid[nearestCol, nearestRow];
            }
            return null;
        }

        public override void SpecialActions(Cell[,] cellGrid)
        {


            if (IsAlive && !SpecialPerformed)
            {
                // spread nationality
                if (CellNeighborhood.NeighborhoodDict[Direction].Nationality != Nationality)
                {
                    CellNeighborhood.NeighborhoodDict[Direction].Nationality = Nationality;
                    SwapCells(this, CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
                }
                else // look for nearest other nation and move toward it
                {
                    Cell cellToSwap = GetDiplomaticCellSwap(cellGrid);
                    if (cellToSwap != null)
                    {
                        SwapCells(this, cellToSwap, cellGrid);
                    }
                    else
                    {
                        SwapCells(this, CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
                    }
                }
                SpecialPerformed = true;
            }
        }
    }
}