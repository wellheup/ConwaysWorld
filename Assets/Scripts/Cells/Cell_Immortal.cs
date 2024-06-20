using UnityEngine;
using System.Collections.Generic;
using static ConwaysWorld.Cell_Generator;
namespace ConwaysWorld
{

    public class Cell_Immortal : Cell
    {
        private int DeathCounter = 0;
        private int MaxAloneTime = 8;
        public Cell_Immortal(int column, int row, bool isAlive = true)// : base(column, row, isAlive)
        {
            this.IsAlive = isAlive;
            Column = column;
            Row = row;
            Conditions = new List<string>();
            CellType = E_CellType.Cell_Immortal;
        }

        public override void Live(Cell[,] cellGrid)
        {
            IsAlive = true;
            Age++;
            if (Age > MatureAge && !Conditions.Contains("mature"))
            {
                Conditions.Add("mature");
            }
            if (CellNeighborhood.NumNeighbors == 0)
            {
                DeathCounter++;
            }
            else
            {
                DeathCounter = 0;
            }
            ChooseNation();
        }

        public override bool CalcCellAliveNextGen()
        {
            if (DeathCounter > MaxAloneTime)
            {
                return false;
            }
            return IsAlive;
        }
    }
}