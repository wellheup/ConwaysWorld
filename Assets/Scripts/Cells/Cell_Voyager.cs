
using System;
using UnityEngine;
using static ConwaysWorld.Cell_Generator;
/// <summary>
/// explorer (picks a random direction to move each turn, expands grid when going over edges, can last 3 cycles without neighbors)
/// </summary>
namespace ConwaysWorld
{
    public class Cell_Voyager : Cell_Explorer
    {
        public Cell_Voyager(int column, int row, bool isAlive) : base(column, row, isAlive)
        {
            CellType = E_CellType.Cell_Voyager;
            MaxAloneTime = 4;
            LiveColor = Cell_Colors.Cell_Voyager;
        }

        public override void Live(Cell[,] cellGrid)
        {
            IsAlive = true;
            CurrentColor = LiveColor;
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
            if (IsNeighborOverEdge(CellNeighborhood.NeighborhoodDict[Direction]))
            {
                Conditions.Add("exploring");
            }
        }

        public override void SpecialActions(Cell[,] cellGrid)
        {
            if (IsAlive && !SpecialPerformed)
            {
                Conditions.RemoveAll(item => item == "exploring");
                SwapCells(CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
                SpecialPerformed = true;
            }
        }
    }
}