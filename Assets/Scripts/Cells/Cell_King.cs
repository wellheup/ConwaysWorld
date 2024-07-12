using static ConwaysWorld.Cell_Generator;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// king (makes basic_cells in neighborhood into warriors)
/// </summary>
namespace ConwaysWorld
{
    public class Cell_King : Cell
    {
        public Cell_King(int column, int row, bool isAlive)
        {
            IsAlive = isAlive;
            Column = column;
            Row = row;
            CellType = E_CellType.Cell_King;
            Conditions = new List<string>();
        }

        public void MakeArmy()
        {
            foreach (Cell _ in CellNeighborhood.NeighborhoodDict.Values)
            {
                if (_.GetIsAlive() && _ != this && _.CellType == E_CellType.Cell_Basic)
                {
                    _.Conditions.Add("toWar");
                }
            }
        }

        public override void SpecialActions(Cell[,] cellGrid)
        {
            MakeArmy();
        }

    }
}