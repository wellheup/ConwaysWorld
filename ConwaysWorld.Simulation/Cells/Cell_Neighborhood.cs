namespace ConwaysWorld.Simulation;

/// <summary>
/// Represents the Moore neighbourhood (the 8 orthogonal + diagonal cells) surrounding a single
/// grid position, with <b>toroidal wrapping</b> so cells on the edges have full neighbourhoods.
/// <para>
/// Instances are rebuilt every step by <see cref="Model.UpdateNeighborhoodsGrid"/> because cells
/// can move or be replaced between steps (Travelers, Doctors, etc.).
/// </para>
/// </summary>
public class Cell_Neighborhood
{
	/// <summary>Number of living cells in the 8 surrounding slots (excludes center).</summary>
	public int NumNeighbors { get; private set; }

	/// <summary>
	/// Like <see cref="NumNeighbors"/> but excludes Zombie cells.
	/// Used by non-zombie living cells for Conway survival/birth checks so zombies are
	/// invisible to normal cells' population counts.
	/// </summary>
	public int NumNonZombieNeighbors { get; private set; }

	/// <summary>Column index of the center cell.</summary>
	public int CenterColumn { get; }

	/// <summary>Row index of the center cell.</summary>
	public int CenterRow { get; }

	/// <summary>Reference to the center cell itself.</summary>
	public Cell Center;

	/// <summary>
	/// The 8 surrounding neighbour cells (center excluded), in the same order as
	/// <see cref="NeighborHoodKeys"/> (indices 0-7). Use this for fast iteration instead of
	/// <see cref="NeighborhoodDict"/>.Values to avoid dictionary overhead.
	/// </summary>
	public readonly Cell[] NeighborCells = new Cell[8];

	/// <summary>
	/// All 9 cells in the 3×3 block including <c>"center"</c>.
	/// Keys are compass directions: <c>"north"</c>, <c>"south"</c>, <c>"east"</c>, <c>"west"</c>,
	/// <c>"northEast"</c>, <c>"northWest"</c>, <c>"southEast"</c>, <c>"southWest"</c>, <c>"center"</c>.
	/// Pre-sized to capacity 9 to avoid bucket-array resizing.
	/// </summary>
	public Dictionary<string, Cell> NeighborhoodDict;

	/// <summary>
	/// The 8 surrounding cells only (center excluded).
	/// Iterates <see cref="NeighborCells"/> without any dictionary allocation.
	/// </summary>
	public IEnumerable<Cell> NeighborValues => NeighborCells;

	/// <summary>
	/// Ordered key names for the 9-slot neighbourhood, matching the iteration order used in the
	/// constructor (column-major, -1→+1 column offset, -1→+1 row offset within each column).
	/// </summary>
	public static readonly string[] NeighborHoodKeys =
	{
								"southWest", "west", "northWest",
								"south",               "north",
								"southEast", "east",  "northEast",
								"center",
		};

	/// <summary>
	/// Builds the neighbourhood for cell at (<paramref name="column"/>, <paramref name="row"/>)
	/// using toroidal (wrap-around) addressing so edge cells see the opposite edge as neighbours.
	/// </summary>
	/// <param name="cellGrid">The full simulation grid.</param>
	/// <param name="column">Column of the center cell.</param>
	/// <param name="row">Row of the center cell.</param>
	public Cell_Neighborhood(Cell[,] cellGrid, int column, int row)
	{
		CenterColumn = column;
		CenterRow = row;
		NeighborhoodDict = new Dictionary<string, Cell>(9);
		int keyIndex = 0;
		NumNeighbors = 0;
		NumNonZombieNeighbors = 0;
		Center = cellGrid[column, row];

		// Cache dimensions once — avoids repeated GetLength() calls inside the inner loop.
		int cols = cellGrid.GetLength(0);
		int rows = cellGrid.GetLength(1);

		for (int colOff = -1; colOff <= 1; colOff++)
		{
			for (int rowOff = -1; rowOff <= 1; rowOff++)
			{
				int nc = (column + colOff + cols) % cols;
				int nr = (row + rowOff + rows) % rows;

				if (colOff == 0 && rowOff == 0)
				{
					NeighborhoodDict["center"] = cellGrid[nc, nr];
				}
				else
				{
					var neighbor = cellGrid[nc, nr];
					if (neighbor.IsAlive)
					{
						NumNeighbors++;
						if (neighbor.CellType != CellType.Zombie)
							NumNonZombieNeighbors++;
					}
					NeighborCells[keyIndex] = neighbor;
					NeighborhoodDict[NeighborHoodKeys[keyIndex]] = neighbor;
					keyIndex++;
				}
			}
		}
	}
}
