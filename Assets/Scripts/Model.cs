using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace ConwaysWorld
{
	public class Model
	{
		public Cell[,] CellGrid;
		public Cell_Neighborhood[,] NeighborhoodsGrid;
		public bool[,] AliveNextGenGrid;
		public Dictionary<int, Cell_Nation> Nations;

		private Cell_Generator Generator;

		private int BasePercentLiving = 10,
			MinLifePercent = 5,
			MinCellsPerNation = 1,
			CurrentPopulation,
			Columns,
			Rows,
			GridLimit;
		public bool UseThreeGroup = false;

		public Model(
			int columns,
			int rows,
			int basePercentLiving,
			int minLifePercent,
			int gridLimit,
			int minCellsPerNation
		)
		{
			Columns = columns;
			Rows = rows;
			BasePercentLiving = basePercentLiving;
			MinLifePercent = minLifePercent;
			MinCellsPerNation = minCellsPerNation;
			GridLimit = gridLimit;
			Generator = new Cell_Generator(BasePercentLiving);
			PopulateGrid(Columns, Rows);
		}

		public void PopulateGrid(int columns, int rows)
		{
			Columns = columns;
			Rows = rows;
			InitializeGrid();
		}

		public void PopulateGrid()
		{
			InitializeGrid();
		}

		private void InitializeGrid()
		{
			CellGrid = new Cell[Columns, Rows];
			NeighborhoodsGrid = new Cell_Neighborhood[Columns, Rows];
			AliveNextGenGrid = new bool[Columns, Rows];
			for (int column = 0; column < Columns; column++)
			{
				for (int row = 0; row < Rows; row++)
				{
					CellGrid[column, row] = Generator.InitializeRandomCell(column, row);
				}
			}
			InitializeNations();
		}

		private void InitializeNations()
		{
			Nations = new Dictionary<int, Cell_Nation>();
			float numNations = BasePercentLiving / 100f * Columns * Rows / MinCellsPerNation;
			numNations = numNations < Cell_Nation.Nation_Colors.Count ? numNations : Cell_Nation.Nation_Colors.Count;
			for (int i = 0; i < numNations; i++)
			{
				Nations.Add(i, new Cell_Nation(i));
			}
		}

		private void ResizeCellGrid()
		{
			Cell[,] tempGrid = CellGrid;
			CellGrid = new Cell[Columns + 2, Rows + 2];
			for (int column = 0; column < Columns + 2; column++)
			{
				for (int row = 0; row < Rows + 2; row++)
				{
					if (column == 0 || column == Columns + 1 || row == 0 || row == Rows + 1)
					{
						CellGrid[column, row] = new Cell_Basic(column, row, false);
					}
					else
					{
						CellGrid[column, row] = tempGrid[column - 1, row - 1];
						CellGrid[column, row].Column = column;
						CellGrid[column, row].Row = row;
					}
				}
			}

			Columns += 2;
			Rows += 2;
			UpdateNeighborhoodsGrid();
			UpdateAliveNextGenGrid();
		}

		public void AddRandomLife(int percentOfGrid)
		{
			if (
				CurrentPopulation > 0
				&& CurrentPopulation / (CellGrid.GetLength(0) * CellGrid.GetLength(1)) <= MinLifePercent / 100
			)
			{
				int numNewLives = CellGrid.GetLength(0) * CellGrid.GetLength(1) * percentOfGrid / 100;
				int counter = 0;
				while (counter < numNewLives)
				{
					int randCol = UnityEngine.Random.Range(0, CellGrid.GetLength(0));
					int randRow = UnityEngine.Random.Range(0, CellGrid.GetLength(1));
					if (!CellGrid[randCol, randRow].GetIsAlive() && !AliveNextGenGrid[randCol, randRow])
					{
						CellGrid[randCol, randRow] = Generator.InitializeRandomCell(randCol, randRow);
						counter++;
					}
				}
			}
		}

		// public static bool IsGridResized<T>(T[,] grid, int columns, int rows)
		// {
		//     if (grid.GetLength(0) > columns || grid.GetLength(1) > rows)
		//     {
		//         return true;
		//     }
		//     return false;
		// }

		public void UpdateNeighborhoodsGrid()
		{
			NeighborhoodsGrid = new Cell_Neighborhood[Columns, Rows];
			for (int column = 0; column < Columns; column++)
			{
				for (int row = 0; row < Rows; row++)
				{
					NeighborhoodsGrid[column, row] = new Cell_Neighborhood(CellGrid, column, row);
					CellGrid[column, row].CellNeighborhood = NeighborhoodsGrid[column, row];
				}
			}
		}

		public void UpdateAliveNextGenGrid()
		{
			AliveNextGenGrid = new bool[Columns, Rows];
			for (int column = 0; column < Columns; column++)
			{
				for (int row = 0; row < Rows; row++)
				{
					AliveNextGenGrid[column, row] = CellGrid[column, row].CalcCellAliveNextGen();
				}
			}
		}

		public int UpdateCellLives()
		{
			CurrentPopulation = 0;
			// Update population state
			for (int column = 0; column < Columns; column++)
			{
				for (int row = 0; row < Rows; row++)
				{
					if (CellGrid[column, row].GetIsAlive())
					{
						if (AliveNextGenGrid[column, row])
						{
							// is alive and stays alive
							// Debug.Log("Cell " + column + ", " + row + " stay alive ");
							CellGrid[column, row].Live();
							if (CellGrid[column, row].Nationality != -1)
								Nations[CellGrid[column, row].Nationality].CitizensList.Add(CellGrid[column, row]);
						}
						else
						{
							// Debug.Log("Cell " + column + ", " + row + " die ");
							CellGrid[column, row].Die();
						}
						CurrentPopulation++; //was cell alive this update (not "will it be alive next update?")
					}
					else
					{
						if (AliveNextGenGrid[column, row])
						{
							//replace the cell with a fresh one, rather than leaving an opening for data to leak into a new cell from previous life (MAY WANT TO KEEP PREV-LIFE DATA LATER)
							//treats the cell as a newborn rather than a revived cell
							CellGrid[column, row].Live();
						}
						else
						{
							// Debug.Log("Cell " + column + ", " + row + " stay dead ");
							// is dead and stays dead
						}
					}
				}
			}
			return CurrentPopulation;
		}

		public void UpdateCellConditions()
		{
			bool _resize = false;
			for (int column = 0; column < Columns; column++)
			{
				for (int row = 0; row < Rows; row++)
				{
					// Chain of ifs for different conditions
					if (CellGrid[column, row].Conditions.Contains("cleanup"))
					{
						CellGrid[column, row] = Cell.ReplaceCell(
							CellGrid[column, row],
							Cell_Generator.E_CellType.Cell_Basic,
							false
						);
					}
					if (CellGrid[column, row].Conditions.Contains("immune")) //manage immune, not sure if it works...
					{
						CellGrid[column, row].Conditions.RemoveAll(item => item.Contains("d_"));
						CellGrid[column, row].Conditions.RemoveAll(item => item.Contains("p_"));
					}
					if (CellGrid[column, row].CellType != Cell_Generator.E_CellType.Cell_Doctor)
					{
						List<string> conditions = CellGrid[column, row].Conditions;
						for (int i = 0; i < conditions.Count; i++)
						{
							if (conditions[i].Contains("d_"))
							{ //manage infected
								CellGrid[column, row] = Cell_Diseased.Infect(
									CellGrid[column, row],
									conditions[i],
									Cell_Generator.E_CellType.Cell_Diseased
								);
							}
							else if (conditions[i].Contains("p_"))
							{ //manage plague
								CellGrid[column, row] = Cell_Diseased.Infect(
									CellGrid[column, row],
									conditions[i],
									Cell_Generator.E_CellType.Cell_Plague
								);
								break;
							}
						}
					}
					if (CellGrid[column, row].Conditions.Contains("mature")) //manage mature
					{
						CellGrid[column, row].Breed();
					}
					if (CellGrid[column, row].Conditions.Contains("immaculate")) //manage immaculate birth
					{
						CellGrid[column, row].Immaculate(CellGrid);
					}
					if (CellGrid[column, row].GetIsAlive() && CellGrid[column, row].Conditions.Contains("exploring")) //manage grid expansion
					{
						_resize = true;
					}
					if (CellGrid[column, row].Age >= 1 && CellGrid[column, row].Nationality == -1) //make edge case cells choose nationality
					{
						CellGrid[column, row].Nationality = UnityEngine.Random.Range(0, Nations.Count);
					}
					if (
						CellGrid[column, row].Conditions.Contains("toWar")
						&& CellGrid[column, row].GetIsAlive()
						&& CellGrid[column, row].CellType == Cell_Generator.E_CellType.Cell_Basic
					)
					{
						CellGrid[column, row] = Cell.ReplaceCell(
							CellGrid[column, row],
							Cell_Generator.E_CellType.Cell_Warrior,
							true
						);
						CellGrid[column, row].Conditions.RemoveAll(item => item.Contains("toWar"));
					}
				}
			}
			if (_resize && !IsMaxGrid())
			{
				ResizeCellGrid();
			}
		}

		private bool IsMaxGrid()
		{
			if (GridLimit == 0)
			{
				return false;
			}
			else if (CellGrid.GetLength(0) > GridLimit || CellGrid.GetLength(1) > GridLimit)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public void UpdateSpecialActions()
		{
			for (int column = 0; column < CellGrid.GetLength(0); column++)
			{
				for (int row = 0; row < CellGrid.GetLength(1); row++)
				{
					CellGrid[column, row].SpecialActions(CellGrid);
				}
			}
		}

		public void UpdateNations()
		{
			// TODO: make Census() a static function that returns a new nations dictionary
			foreach (Cell_Nation nation in Nations.Values)
			{
				nation.Census(CellGrid);
			}
			float numNations = BasePercentLiving / 100f * Columns * Rows / MinCellsPerNation;
			numNations =
				(float)(BasePercentLiving / 100f * Columns * Rows / MinCellsPerNation) < Cell_Nation.Nation_Colors.Count
					? (float)(BasePercentLiving / 100f * Columns * Rows / MinCellsPerNation)
					: Cell_Nation.Nation_Colors.Count;
			for (int i = Nations.Count; i < numNations; i++)
			{
				Nations.Add(i, new Cell_Nation(i));
			}
		}

		public int UpdateCellsGrid()
		{
			UpdateNeighborhoodsGrid();
			UpdateAliveNextGenGrid();
			UpdateCellLives();
			UpdateCellConditions();
			UpdateSpecialActions();
			AddRandomLife(BasePercentLiving);
			UpdateNations();

			return CurrentPopulation;
		}
	}
}
