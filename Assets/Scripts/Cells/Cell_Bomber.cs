using System.Collections.Generic;
using UnityEngine;
using static ConwaysWorld.Cell_Generator;

/// <summary>
/// bomber (kills all cells in 2 cell radius)
/// </summary>
namespace ConwaysWorld
{
	public class Cell_Bomber : Cell
	{
		public Cell_Bomber(int column, int row, bool isAlive)
		{
			IsAlive = isAlive;
			Column = column;
			Row = row;
			CellType = E_CellType.Cell_Bomber;
			Conditions = new List<string>();
		}

		public override void SpecialActions(Cell[,] cellGrid) => Detonate(cellGrid);

		public override bool CalcCellAliveNextGen()
		{
			return true;
		}

		private void Detonate(Cell[,] cellGrid)
		{
			if (Age >= 2)
			{
				List<Cell> cellsInRange = GetAllCellsInRangeByRule(
					cellGrid,
					(Cell x) =>
					{
						return x.GetIsAlive();
					},
					2
				);
				foreach (Cell _ in cellsInRange)
				{
					_.Die();
				}
				Die();
			}
		}
	}
}
