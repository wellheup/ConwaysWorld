using UnityEngine;
using static ConwaysWorld.Cell_Generator;
using System.Collections.Generic;

namespace ConwaysWorld
{
    public class Cell_Diseased : Cell
    {
        protected int CountDown = 3, TransmissionRate = 25;
        public string Disease;
        public Cell_Diseased(int column, int row, bool isAlive)
        {
            IsAlive = isAlive;
            Column = column;
            Row = row;
            CellType = E_CellType.Cell_Diseased;
            Conditions = new List<string>();
            Disease = RandomCondition('d');
        }

        public override void Live()
        {
            IsAlive = true;
            Age++;
            if (Age > MatureAge && !Conditions.Contains("mature"))
            {
                Conditions.Add("mature");
            }
            CellType = E_CellType.Cell_Diseased;
            ChooseNation();
        }

        public override void Die()
        {
            IsAlive = false;
            Conditions.Remove(Disease);
            Nationality = -1;
        }

        public override bool CalcCellAliveNextGen()
        {
            CountDown--;
            if (CountDown <= 0)
            {
                return false;
            }
            return LiveBasic();
        }

        public static Cell Infect(Cell cell, string disease)
        {
            if (cell.GetIsAlive() && cell.GetType() != typeof(Cell_Diseased))
            {
                Cell temp = ReplaceCell(cell, E_CellType.Cell_Diseased, true);
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
