using UnityEngine;
using static ConwaysWorld.Cell_Generator;
using System.Collections.Generic;
namespace ConwaysWorld
{
	/// [summary>
	/// kills neighboring cells from other nations, zombies, and diseased cells then moves into its tile
	/// 2 warrior cells flip a coin for the victor
	/// [/summary>
	/// [remarks>
	/// 
	/// [/remarks>
	public class Cell_Warrior : Cell_Hunter
	{
		List<E_CellType> PreyTypes;
		public Cell_Warrior(int column, int row, bool isAlive) : base(column, row, isAlive)
		{
			IsAlive = isAlive;
			Column = column;
			Row = row;
			CellType = E_CellType.Cell_Warrior;
			Conditions = new List<string>();
			PreyTypes = new() { E_CellType.Cell_Diseased, E_CellType.Cell_Plague, E_CellType.Cell_Zombie };
		}

		public override void Live()
		{
			IsAlive = true;
			Age++;
			ChooseNation();
		}

		private bool IsCellEnemy(Cell otherCell)
		{
			return otherCell.Nationality != Nationality && PreyTypes.Contains(otherCell.CellType) && otherCell.GetIsAlive();
		}

		private bool IsCombatWinner(Cell targetCell)
		{
			int myPower = GetArmyStrength(this);
			int targetPower = GetArmyStrength(targetCell);
			myPower = Age > targetCell.Age ? myPower++ : targetPower++;
			if (myPower == targetPower)
			{
				return UnityEngine.Random.Range(0, 2) == 0;
			}
			return myPower > targetPower;
		}

		private int GetArmyStrength(Cell cell)
		{
			int power = 0;
			foreach (Cell neighbor in cell.CellNeighborhood.NeighborsDict.Values)
			{
				if (neighbor.Nationality == cell.Nationality)
				{
					power++;
					if (neighbor.GetType() == typeof(Cell_Warrior)) power++;
					if (neighbor.GetType() == typeof(Cell_King)) power += 2;
				}
			}
			return power;
		}

		protected void Fight(Cell[,] cellGrid)
		{
			Cell targetCell = SelectNearbyCellByRule(cellGrid, IsCellEnemy, 2);
			if (targetCell != null)
			{
				if (targetCell.GetType() == typeof(Cell_Warrior) && !IsCombatWinner(targetCell)) //if cell loses combat to another warrior
				{
					CellThrowExceptionInRender($"({Column}, {Row}) {this.GetType()}[{Nationality}] targeted ({targetCell.Column}, {targetCell.Row}){targetCell.GetType()}[{targetCell.Nationality}] and died!");
					Die();
					Conditions.Add("cleanup");
				}
				else
				{
					SwapCells(this, targetCell, cellGrid);
					targetCell.Die(); //TODO: is this valid? or should i mark the cell for death and let model kill it somehow?
					targetCell.Conditions.Add("cleanup");
				}
			}
		}

		public override void SpecialActions(Cell[,] cellGrid)
		{
			if (IsAlive)
			{
				Fight(cellGrid);
			}
		}
	}
}