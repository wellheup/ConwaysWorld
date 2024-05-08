using static ConwaysWorld.Cell_Generator;
namespace ConwaysWorld
{

    public class Cell_Basic : Cell
    {
        public Cell_Basic(int column, int row, bool isAlive) : base(column, row, isAlive)
        {
            IsAlive = isAlive;
            Column = column;
            Row = row;
            CellType = E_CellType.Cell_Basic;
            LiveColor = Cell_Colors.Cell_Basic;
        }
    }
}