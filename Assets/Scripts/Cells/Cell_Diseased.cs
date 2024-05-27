using UnityEngine;
using static ConwaysWorld.Cell_Generator;
using System.Collections.Generic;

namespace ConwaysWorld
{
    public class Cell_Diseased : Cell
    {
        protected int CountDown = 3, TransmissionRate = 50;
        public string Disease;
        public Cell_Diseased(int column, int row, bool isAlive)
        {
            IsAlive = isAlive;
            Column = column;
            Row = row;
            LiveColor = Cell_Colors.Cell_Diseased;
            DeadColor = Cell_Colors.Cell_Dead;
            CurrentColor = isAlive ? LiveColor : DeadColor;
            CellType = E_CellType.Cell_Diseased;
            Conditions = new List<string>();
            Disease = RandomCondition('d');
        }

        public override void Live(Cell[,] cellGrid)
        {
            IsAlive = true;
            CurrentColor = LiveColor;
            Age++;
            SpreadDisease(cellGrid);
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
            CurrentColor = DeadColor;
            Conditions.Remove(Disease);
            Nationality = null;
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

        private void SpreadDisease(Cell[,] cellGrid)
        {
            // mark neighbors as infected
            for (int i = 0; i < Cell_Neighborhood.NeighborHoodKeys.Length; i++)
            {
                if (Random.Range(1, 101) < TransmissionRate && Cell_Neighborhood.NeighborHoodKeys[i] != "center")
                {
                    int nCellCol = CellNeighborhood.NeighborhoodDict[Cell_Neighborhood.NeighborHoodKeys[i]].Column;
                    int nCellRow = CellNeighborhood.NeighborhoodDict[Cell_Neighborhood.NeighborHoodKeys[i]].Row;
                    cellGrid[nCellCol, nCellRow].Conditions.Add(Disease);
                }
            }
        }
    }
}
