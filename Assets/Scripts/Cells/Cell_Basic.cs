using System.Collections.Generic;
using UnityEngine;
using static ConwaysWorld.Cell_Generator;

namespace ConwaysWorld
{
	public class Cell_Basic : Cell
	{
		public Cell_Basic(int column, int row, bool isAlive)
		{
			IsAlive = isAlive;
			Column = column;
			Row = row;
			CellType = E_CellType.Cell_Basic;
			Conditions = new List<string>();
		}
	}
}
