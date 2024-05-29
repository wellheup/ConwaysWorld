
using System;
using UnityEngine;
using System.Collections.Generic;
using static ConwaysWorld.Cell_Generator;
/// <summary>
/// explorer (picks a random direction to move each turn, expands grid when going over edges, can last 3 cycles without neighbors)
/// </summary>
namespace ConwaysWorld
{
    public class Cell_Doctor : Cell
    {
        private List<string> Vaccines;
        protected bool SpecialPerformed = false;

        public Cell_Doctor(int column, int row, bool isAlive)
        {
            IsAlive = isAlive;
            Column = column;
            Row = row;
            CellType = E_CellType.Cell_Doctor;
            LiveColor = Cell_Colors.Cell_Doctor;
            DeadColor = Cell_Colors.Cell_Dead;
            CurrentColor = isAlive ? LiveColor : DeadColor;
            Conditions = new List<string>();
            Vaccines = new List<string>();
        }

        public override void Live(Cell[,] cellGrid)
        {
            base.Live(cellGrid);
            ChooseNation();
            SpecialPerformed = false;
        }

        public override bool CalcCellAliveNextGen()
        {
            if (SpecialPerformed)
            {
                return true;
            }
            return LiveBasic();
        }


        private void SeekDisease(Cell[,] cellGrid)
        {
            foreach (Cell neighbor in CellNeighborhood.NeighborhoodDict.Values)
            {
                int nCellCol = neighbor.Column;
                int nCellRow = neighbor.Row;
                foreach (string condition in cellGrid[nCellCol, nCellRow].Conditions)
                {
                    if (condition.Contains("d_") || condition.Contains("p_"))
                    {
                        Vaccines.Add(condition);
                    }
                }
            }
        }

        private void CureDisease(Cell[,] cellGrid)
        {
            foreach (Cell neighbor in CellNeighborhood.NeighborhoodDict.Values)
            {
                if (neighbor.GetIsAlive() && neighbor != this)
                {
                    foreach (string vaccine in Vaccines)
                    {
                        int numVaccinated = 0;
                        numVaccinated = neighbor.Conditions.RemoveAll(item => item == vaccine);
                        if (numVaccinated > 0)
                        {
                            ReplaceCell(cellGrid[neighbor.Column, neighbor.Row], E_CellType.Cell_Basic, true);
                        }
                    }
                }
            }
            CellNeighborhood = new Cell_Neighborhood(cellGrid, Column, Row);
        }

        public override void SpecialActions(Cell[,] cellGrid)
        {
            CureDisease(cellGrid); // cure all known diseases in locale, do it before SeekDisease, b/c it takes 1 turn to develop vaccine
            SeekDisease(cellGrid); // find new diseases in locale and add to vaccines list
        }
    }
}