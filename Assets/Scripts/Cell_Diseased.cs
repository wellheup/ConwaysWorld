using UnityEngine;
using static ConwaysWorld.CellGenerator;
namespace ConwaysWorld
{

    public class Cell_Diseased : Cell
    {
        protected int CountDown = 3, TransmissionRate = 50;
        public Cell_Diseased(int column, int row, bool isAlive) : base(column, row, isAlive)
        {
            LiveColor = Color.green;
            DeadColor = Color.white;
            CellType = E_CellType.Cell_Diseased;
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
            CellType = E_CellType.Cell_Diseased;
        }

        public override void Die()
        {
            IsAlive = false;
            CurrentColor = DeadColor;
            Conditions.Remove("infected");
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

        public static Cell Infect(Cell cell)
        {
            if (cell.GetIsAlive() && cell.GetType() != typeof(Cell_Diseased))
            {
                return ReplaceCell(cell, E_CellType.Cell_Diseased, true);
            }
            return cell;
        }

        private void SpreadDisease(Cell[,] cellGrid, Neighborhood neighborhood)
        {
            // mark neighbors as infected
            for (int i = 0; i < Neighborhood.NeighborHoodKeys.Length; i++)
            {
                if (Random.Range(1, 101) < TransmissionRate && Neighborhood.NeighborHoodKeys[i] != "center")
                {
                    int nCellCol = neighborhood.NeighborhoodDict[Neighborhood.NeighborHoodKeys[i]].Column;
                    int nCellRow = neighborhood.NeighborhoodDict[Neighborhood.NeighborHoodKeys[i]].Row;
                    cellGrid[nCellCol, nCellRow].Conditions.Add("infected");
                }
            }
        }
    }
}
