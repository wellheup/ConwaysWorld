
using System;
using UnityEngine;
using System.Collections.Generic;
using static ConwaysWorld.Cell_Generator;
/// <summary>
/// explorer (picks a UnityEngine.Random direction to move each turn, expands grid when going over edges, can last 3 cycles without neighbors)
/// </summary>
namespace ConwaysWorld
{
	public class Cell_Explorer : Cell_Traveler
	{
		bool HasExplored = false;
		public Cell_Explorer(int column, int row, bool isAlive) : base(column, row, isAlive)
		{
			CellType = E_CellType.Cell_Explorer;
			MaxAloneTime = 4;
			Conditions = new List<string>();
			ChooseNation();
		}

		public override void Live()
		{
			IsAlive = true;
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
			if (!HasExplored && IsNeighborOverEdge(CellNeighborhood.NeighborhoodDict[Direction]))
			{
				Conditions.Add("exploring");
			}
		}

		protected bool IsNeighborOverEdge(Cell neighbor)
		{
			if (Math.Abs(Column - neighbor.Column) > 1 || Math.Abs(Row - neighbor.Row) > 1)
			{
				return true;
			}
			return false;
		}

		public override void SpecialActions(Cell[,] cellGrid)
		{
			if (IsAlive && !SpecialPerformed)
			{
				Conditions.RemoveAll(item => item == "exploring");
				SwapCells(this, CellNeighborhood.NeighborhoodDict[Direction], cellGrid);
				SpecialPerformed = true;
			}
		}
	}
}