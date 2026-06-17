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
            TransmissionRate = 50;
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

        public override void SpecialActions(Cell[,] cellGrid)
        {
            SpreadDisease(cellGrid);
        }
    }
}