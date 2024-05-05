using UnityEngine;
using static ConwaysWorld.CellGenerator;
namespace ConwaysWorld
{

    public class Cell_Plague : Cell_Diseased
    {
        // plague(diseased cell that spreads disease with higher infection rate than diseased to all touching cells)
        public Cell_Plague(int column, int row, bool isAlive) : base(column, row, isAlive)
        {
            this.IsAlive = isAlive;
            LiveColor = Color.green;
            DeadColor = Color.white;
            Column = column;
            Row = row;
            TransmissionRate = 75;
            CellType = E_CellType.Cell_Plague;

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

        private void SpreadDisease(Cell[,] cellGrid, Neighborhood neighborhood)
        {
            // mark neighbors as infected
            for (int i = 0; i < Neighborhood.NeighborHoodKeys.Length; i++)
            {
                if (Random.Range(1, 101) < TransmissionRate && Neighborhood.NeighborHoodKeys[i] != "center")
                {
                    int nCellCol = neighborhood.NeighborhoodDict[Neighborhood.NeighborHoodKeys[i]].Column;
                    int nCellRow = neighborhood.NeighborhoodDict[Neighborhood.NeighborHoodKeys[i]].Row;
                    cellGrid[nCellCol, nCellRow].Conditions.Add("plagued");
                }
            }
        }
    }
}