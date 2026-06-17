using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ConwaysWorld
{
	/// <summary>
	/// keeps track of a list of cells supposedly in thenation but does not track life or death. 
	/// </summary>
	/// <remarks>
	/// a census should be taken for each nation each turn to see if those cells are still this nation or not
	/// </remarks>
	public class Cell_Nation
	{
		public int NationNum;
		public Cell King = null;
		public List<Cell> CitizensList;
		public List<Cell> DiplomatsList;
		public static List<Color> Nation_Colors { get; set; } = new()
		{
			new(0.74f, 0.00f, 1.00f, 1.00f),
			new(0.28f, 0.08f, 0.09f, 1.00f),
			new(0.64f, 0.09f, 0.11f, 1.00f),
			new(0.00f, 0.96f, 0.26f, 1.00f),
			new(0.38f, 0.50f, 0.11f, 1.00f),
			new(0.96f, 0.00f, 0.02f, 1.00f),
			new(0.28f, 0.24f, 0.08f, 1.00f),
			new(0.09f, 0.64f, 0.24f, 1.00f),
			new(0.64f, 0.52f, 0.09f, 1.00f),
			new(0.16f, 0.61f, 0.68f, 1.00f),
			new(0.00f, 0.86f, 1.00f, 1.00f),
			new(0.96f, 0.75f, 0.00f, 1.00f),
			new(0.14f, 0.17f, 0.46f, 1.00f),
			new(1.00f, 0.44f, 0.00f, 1.00f),
			new(0.00f, 0.10f, 0.96f, 1.00f),
			new(0.68f, 1.00f, 0.00f, 1.00f),
			new(0.50f, 0.28f, 0.11f, 1.00f),
			new(0.40f, 0.11f, 0.50f, 1.00f),
			new(0.11f, 0.17f, 0.72f, 1.00f),
			new(0.09f, 0.28f, 0.14f, 1.00f)
		};

		public Cell_Nation(int nationNum)
		{
			DiplomatsList = new();
			CitizensList = new();
			NationNum = nationNum;
		}

		public void Census(Cell[,] cellGrid)
		{
			List<Cell> tempCits = new();
			List<Cell> tempDips = new();
			for (int x = 0; x < cellGrid.GetLength(0); x++)
			{
				for (int y = 0; y < cellGrid.GetLength(1); y++)
				{
					if (cellGrid[x, y].Nationality == NationNum && cellGrid[x, y].GetIsAlive())
					{
						if (cellGrid[x, y].CellType == Cell_Generator.E_CellType.Cell_Diplomat)
						{
							tempDips.Add(cellGrid[x, y]);
							tempCits.Add(cellGrid[x, y]);
						}
						else
						{
							tempCits.Add(cellGrid[x, y]);
						}
					}
				}
			}
			CitizensList = tempCits;
			DiplomatsList = tempDips;

			ElectDiplomat(cellGrid);
			CrownKing(cellGrid);
		}

		private void ElectDiplomat(Cell[,] cellGrid)
		{
			if (DiplomatsList.Count < .1f * CitizensList.Count && CitizensList.Count >= 5)
			{
				Cell diplomatElect = null;
				int maxTries = 5, attempt = 0;
				//select a cell whom is not a diplomat already
				while (attempt < maxTries && (diplomatElect == King || DiplomatsList.Contains(diplomatElect)))
				{
					diplomatElect = CitizensList[UnityEngine.Random.Range(0, CitizensList.Count)];
					attempt++;
				}
				if (diplomatElect != null && diplomatElect != King && diplomatElect.GetIsAlive() && !DiplomatsList.Contains(diplomatElect))
				{
					diplomatElect = Cell.ReplaceCell(diplomatElect, Cell_Generator.E_CellType.Cell_Diplomat, true);
					cellGrid[diplomatElect.Column, diplomatElect.Row] = diplomatElect;
					CitizensList.Add(diplomatElect);
					DiplomatsList.Add(diplomatElect);
				}
			}
		}

		private void CrownKing(Cell[,] cellGrid)
		{
			if (King != null && !King.GetIsAlive())
			{
				King.Conditions.Add("cleanup");
				King = null;
			}
			if (CitizensList.Count > DiplomatsList.Count)
			{
				if (King == null)
				{
					Cell newKing = null;
					int maxTries = 5, attempt = 0;

					while (attempt < maxTries && (newKing == null || newKing.GetIsAlive() == false))
					{
						newKing = CitizensList[UnityEngine.Random.Range(0, CitizensList.Count)];
						attempt++;
					}
					if (newKing != null && newKing.GetIsAlive())
					{
						King = Cell.ReplaceCell(newKing, Cell_Generator.E_CellType.Cell_King, true);
						cellGrid[King.Column, King.Row] = King;
						CitizensList.Add(King);
						DiplomatsList.Remove(King);
					}
				}
			}
		}
	}
}