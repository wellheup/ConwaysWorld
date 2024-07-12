
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

        public override void Live()
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

        private bool IsCellOtherNation(Cell otherCell)
        {
            return this.Nationality != otherCell.Nationality && otherCell.GetIsAlive();
        }

        public override void SpecialActions(Cell[,] cellGrid)
        {
            if (IsAlive && !SpecialPerformed)
            {
                Cell targetCell = SelectNearbyCellByRule(cellGrid, IsCellOtherNation, 5);
                Cell cellToSwap = FindNeighborInDirOfCell(cellGrid, targetCell);
                if (cellToSwap != null)
                {
                    if (IsCellOtherNation(cellToSwap)) cellToSwap.Nationality = Nationality;
                    SwapCells(this, cellToSwap, cellGrid);
                }
                else
                {
                    SwapCells(this, CellNeighborhood.NeighborhoodDict[ChooseTravelDirection()], cellGrid);
                }
                SpecialPerformed = true;
            }
        }
    }
}