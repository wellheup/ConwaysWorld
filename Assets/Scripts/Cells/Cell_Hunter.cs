using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ConwaysWorld.Cell_Generator;

namespace ConwaysWorld
{
	/// <summary>
	/// hunter searches for the nearest prey at spawn, selecting only cellTypes from a list of preys within a range. Travels toward that prey
	/// upon reaching adjacentcy to the prey, the prey cell dies and the hunter seeks a new prey
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	public class Cell_Hunter : Cell_Traveler
	{
		List<E_CellType> PreyTypes;
		Cell CurrentPrey;

		public Cell_Hunter(int column, int row, bool isAlive)
			: base(column, row, isAlive)
		{
			IsAlive = isAlive;
			Column = column;
			Row = row;
			CellType = E_CellType.Cell_Hunter;
			Conditions = new List<string>();
			PreyTypes = new() { E_CellType.Cell_Immortal, E_CellType.Cell_Zombie, E_CellType.Cell_King };
			CurrentPrey = null;
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
			ChooseNation();
		}

		public override void Die()
		{
			base.Die();
		}

		public override bool CalcCellAliveNextGen()
		{
			if (DeathCountDown > MaxAloneTime)
			{
				return false;
			}
			return true;
		}

		private bool IsCellPrey(Cell otherCell)
		{
			return PreyTypes.Contains(otherCell.CellType) && otherCell.GetIsAlive();
		}

		private bool IsCellCurrentPrey(Cell otherCell)
		{
			return otherCell == CurrentPrey && otherCell.GetIsAlive();
		}

		protected void Hunt(Cell[,] cellGrid)
		{
			Cell targetCell = CurrentPrey;
			//if current prey is invalid
			if (CurrentPrey == null || !CurrentPrey.GetIsAlive())
			{
				//seek new current prey, choose random if invalid
				targetCell = SelectNearbyCellByRule(cellGrid, IsCellPrey, 5);
				if (targetCell != null)
				{
					CurrentPrey = targetCell;
				}
				else
				{
					targetCell = CellNeighborhood.NeighborhoodDict[ChooseTravelDirection()];
				}
			}

			Cell cellToSwap = FindNeighborInDirOfCell(cellGrid, targetCell); // find nearest cell in dir of target (may be the actual target)
			if (CurrentPrey != null && IsCellCurrentPrey(cellToSwap)) //if prey was valid or new prey was found
			{
				SwapCells(this, cellToSwap, cellGrid);
				cellToSwap.Die(); //TODO: is this valid? or should i mark the cell for death and let model kill it somehow?
				CurrentPrey = null;
			}
			else if (IsCellPrey(cellToSwap))
			{
				SwapCells(this, cellToSwap, cellGrid);
				cellToSwap.Die(); //TODO: is this valid? or should i mark the cell for death and let model kill it somehow?
			}
			else
			{
				SwapCells(this, cellToSwap, cellGrid);
			}
		}

		public override void SpecialActions(Cell[,] cellGrid)
		{
			if (IsAlive)
			{
				Hunt(cellGrid);
			}
		}
	}
}
