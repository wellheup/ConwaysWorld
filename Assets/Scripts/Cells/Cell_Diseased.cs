using System.Collections.Generic;
using UnityEngine;
using static ConwaysWorld.Cell_Generator;

namespace ConwaysWorld
{
	public class Cell_Diseased : Cell
	{
		protected int CountDown = 3,
			TransmissionRate = 25;
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
			base.Die();
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

		public static Cell Infect(Cell cell, string disease, E_CellType cellType)
		{
			if (cell.GetIsAlive() && cell.CellType != cellType)
			{
				Cell temp = ReplaceCell(cell, cellType, true);
				temp.Conditions.Add(disease);
				return temp;
			}
			return cell;
		}

		public override void SpecialActions(Cell[,] cellGrid)
		{
			SpreadDisease(cellGrid);
		}

		protected void SpreadDisease(Cell[,] cellGrid)
		{
			if (IsAlive)
			{
				// mark neighbors as infected
				for (int i = 0; i < Cell_Neighborhood.NeighborHoodKeys.Length; i++)
				{
					if (
						UnityEngine.Random.Range(1, 101) < TransmissionRate
						&& Cell_Neighborhood.NeighborHoodKeys[i] != "center"
					)
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
}
