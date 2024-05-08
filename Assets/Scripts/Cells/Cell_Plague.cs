using UnityEngine;
using System.Collections.Generic;
using static ConwaysWorld.Cell_Generator;

namespace ConwaysWorld
{
    public class Cell_Plague : Cell_Diseased
    {
        // plague(diseased cell that spreads disease with higher infection rate than diseased to all touching cells)
        public Cell_Plague(int column, int row, bool isAlive) : base(column, row, isAlive)
        {
            this.IsAlive = isAlive;
            LiveColor = Cell_Colors.Cell_Plague;
            DeadColor = Color.white;
            CurrentColor = isAlive ? LiveColor : DeadColor;
            Column = column;
            Row = row;
            TransmissionRate = 75;
            CellType = E_CellType.Cell_Plague;
            Conditions = new List<string>();
        }

        public override void Live(Cell[,] cellGrid)
        {
            IsAlive = true;
            CurrentColor = LiveColor;
            Age++;
            SpreadDisease(cellGrid, CellNeighborhood);
            if (Age > MatureAge && !Conditions.Contains("mature"))
            {
                Conditions.Add("mature");
            }
            CellType = E_CellType.Cell_Plague;
        }

        public override void Die()
        {
            IsAlive = false;
            CurrentColor = DeadColor;
            Conditions.Remove("plagued");
        }

        private void SpreadDisease(Cell[,] cellGrid, Cell_Neighborhood neighborhood)
        {
            // mark neighbors as infected
            for (int i = 0; i < Cell_Neighborhood.NeighborHoodKeys.Length; i++)
            {
                if (Random.Range(1, 101) < TransmissionRate && Cell_Neighborhood.NeighborHoodKeys[i] != "center")
                {
                    int nCellCol = neighborhood.NeighborhoodDict[Cell_Neighborhood.NeighborHoodKeys[i]].Column;
                    int nCellRow = neighborhood.NeighborhoodDict[Cell_Neighborhood.NeighborHoodKeys[i]].Row;
                    cellGrid[nCellCol, nCellRow].Conditions.Add("plagued");
                }
            }
        }
    }
}