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
            TransmissionRate = 75;
            CellType = E_CellType.Cell_Plague;
            Disease = RandomCondition('p');
        }

        public override void Live()
        {
            IsAlive = true;
            Age++;
            if (Age > MatureAge && !Conditions.Contains("mature"))
            {
                Conditions.Add("mature");
            }
            CellType = E_CellType.Cell_Plague;
            ChooseNation();
        }

        public override void Die()
        {
            IsAlive = false;
            Conditions.Remove(Disease);
            Nationality = -1;
        }

        private static new Cell Infect(Cell cell, string disease)
        {
            if (cell.GetIsAlive() && cell.GetType() != typeof(Cell_Plague))
            {
                Cell temp = ReplaceCell(cell, E_CellType.Cell_Plague, true);
                temp.Conditions.Add(disease);
                return temp;
            }
            return cell;
        }

        public override void SpecialActions(Cell[,] cellGrid)
        {
            SpreadDisease(cellGrid);
        }

        private void SpreadDisease(Cell[,] cellGrid)
        {
            // mark neighbors as infected
            for (int i = 0; i < Cell_Neighborhood.NeighborHoodKeys.Length; i++)
            {
                if (UnityEngine.Random.Range(1, 101) < TransmissionRate && Cell_Neighborhood.NeighborHoodKeys[i] != "center")
                {
                    int nCellCol = CellNeighborhood.NeighborhoodDict[Cell_Neighborhood.NeighborHoodKeys[i]].Column;
                    int nCellRow = CellNeighborhood.NeighborhoodDict[Cell_Neighborhood.NeighborHoodKeys[i]].Row;
                    if (!cellGrid[nCellCol, nCellRow].Conditions.Contains("immune"))
                        cellGrid[nCellCol, nCellRow].Conditions.Add(Disease);
                }
            }
        }
    }
}