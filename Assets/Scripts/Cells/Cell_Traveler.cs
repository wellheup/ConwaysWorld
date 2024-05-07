using UnityEngine;
using static ConwaysWorld.Cell_Generator;
namespace ConwaysWorld
{

    /// <summary>
    /// traveler (swaps places with random neighbor each turn)
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public class Cell_Traveler : Cell
    {
        protected int DeathCountDown = 0;
        protected int MaxAloneTime = 3;
        protected string Direction;
        protected bool SpecialPerformed = false;

        public Cell_Traveler(int column, int row, bool isAlive) : base(column, row, isAlive)
        {
            IsAlive = isAlive;
            Column = column;
            Row = row;
            Direction = ChooseTravelDirection();
            CellType = E_CellType.Cell_Traveler;
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
        }

        public override void Die()
        {
            base.Die();
            SpecialPerformed = true;
        }

        protected string ChooseTravelDirection()
        {
            string direction = "center";
            while (direction == "center")
            {
                direction = Cell_Neighborhood.NeighborHoodKeys[Random.Range(0, Cell_Neighborhood.NeighborHoodKeys.Length)];
            }
            return direction;
        }

        public override bool CalcCellAliveNextGen()
        {
            if (DeathCountDown > MaxAloneTime)
            {
                return false;
            }
            return true;
        }

        public override void SpecialActions(Cell[,] cellGrid)
        {
            if (IsAlive && !SpecialPerformed)
            {
                SwapCells(CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
                SpecialPerformed = true;
            }
        }
    }
}