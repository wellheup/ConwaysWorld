using System.Collections.Generic;
using UnityEngine;
using static ConwaysWorld.Cell_Generator;
namespace ConwaysWorld
{

    public abstract class Cell
    {
        public Color LiveColor;
        public Color DeadColor;
        public Color CurrentColor;
        public Cell_Neighborhood CellNeighborhood;
        public List<string> Conditions;
        protected bool IsAlive = false;
        public int Column = 0, Row = 0, Age = 0, MatureAge = 10;
        public E_CellType CellType = E_CellType.Cell;

        // public Cell(int column, int row, bool isAlive)
        // {
        //     this.IsAlive = isAlive;
        //     DeadColor = Color.white;
        //     CurrentColor = isAlive ? LiveColor : DeadColor;
        //     Column = column;
        //     Row = row;
        //     Conditions = new List<string>();
        // }

        // Should only be used for debugging
        public void SetAllColors(Color color)
        {
            this.LiveColor = color;
            this.DeadColor = color;
            this.CurrentColor = color;
        }

        public bool GetIsAlive()
        {
            return IsAlive;
        }

        public Color GetCurrentColor()
        {
            return this.CurrentColor;
        }

        public virtual void Live(Cell[,] cellGrid)
        {
            IsAlive = true;
            CurrentColor = LiveColor;
            Age++;
            if (Age > MatureAge && !Conditions.Contains("mature"))
            {
                Conditions.Add("mature");
            }
        }

        public virtual void Die()
        {
            IsAlive = false;
            CurrentColor = DeadColor;
            Age = 0;
        }

        public virtual bool CalcCellAliveNextGen()
        {
            return LiveBasic();
        }

        protected virtual bool LiveBasic()
        {
            if (IsAlive && CellNeighborhood.NumNeighbors < 2)
            {
                return false; // Die due to underpopulation
            }
            else if (IsAlive && (CellNeighborhood.NumNeighbors == 2 || CellNeighborhood.NumNeighbors == 3))
            {
                return true; // Live on
            }
            else if (IsAlive && CellNeighborhood.NumNeighbors > 3)
            {
                return false; // Die due to overpopulation
            }
            else if (!IsAlive && CellNeighborhood.NumNeighbors == 3)
            {
                return true; // Become alive due to reproduction
            }
            else if (!IsAlive && CellNeighborhood.NumNeighbors != 3)
            {
                return false; // Stays dead
            }
            else
            {
                return IsAlive; // Stay the same
            }
        }

        public virtual void SpecialActions(Cell[,] cellGrid)
        {
            if (IsAlive)
            {

            }
        }

        public static Cell ReplaceCell(Cell oldCell, E_CellType cellType, bool isAlive)
        {
            int column = oldCell.Column;
            int row = oldCell.Row;
            Cell cell;
            switch (cellType)
            {
                case E_CellType.Cell_Basic:
                    cell = new Cell_Basic(column, row, isAlive);
                    break;
                case E_CellType.Cell_Immortal:
                    cell = new Cell_Immortal(column, row, isAlive);
                    break;
                case E_CellType.Cell_Diseased:
                    cell = new Cell_Diseased(column, row, isAlive);
                    break;
                case E_CellType.Cell_Plague:
                    cell = new Cell_Plague(column, row, isAlive);
                    break;
                case E_CellType.Cell_Traveler:
                    cell = new Cell_Traveler(column, row, isAlive);
                    break;
                default:
                    cell = new Cell_Basic(column, row, isAlive); //this should not occur...
                    break;
            }
            cell.Conditions = oldCell.Conditions;
            cell.CellNeighborhood = oldCell.CellNeighborhood;

            return cell;
        }

        public void SwapCells(Cell dest, Cell[,] cellGrid)
        {
            int oldCol = Column, oldRow = Row;

            cellGrid[dest.Column, dest.Row] = this;
            cellGrid[Column, Row] = dest;

            Column = dest.Column;
            Row = dest.Row;

            dest.Column = oldCol;
            dest.Row = oldRow;

            CellNeighborhood = new Cell_Neighborhood(cellGrid, Column, Row);
            dest.CellNeighborhood = new Cell_Neighborhood(cellGrid, dest.Column, dest.Row);
        }

        public virtual void Breed()
        {
            Conditions.RemoveAll(item => item == "mature");
            Age = 0;
            List<Cell> cells = new List<Cell>();
            foreach (KeyValuePair<string, Cell> cell in CellNeighborhood.NeighborhoodDict)
            {
                if (!cell.Value.GetIsAlive())
                {
                    cells.Add(cell.Value);
                }
            }
            int randNeighbor = Random.Range(0, cells.Count);
            cells[randNeighbor] = ReplaceCell(cells[randNeighbor], CellType, true);

        }

        private void LiveNoNeighbors(Cell[,] CellGrid, Cell cell)
        {
            if (cell.CellNeighborhood.NumNeighbors == 0)
            {
                cell.CellNeighborhood = new Cell_Neighborhood(CellGrid, cell.Column, cell.Row);
                cell.Live(CellGrid);
            }
        }

        public virtual void Immaculate(Cell[,] CellGrid)
        {
            Conditions.RemoveAll(item => item == "immaculate");
            LiveNoNeighbors(CellGrid, this);
            if (IsAlive)
            {
                if (Random.Range(1, 3) == 1)
                {
                    LiveNoNeighbors(CellGrid, CellNeighborhood.NeighborhoodDict["north"]);
                    LiveNoNeighbors(CellGrid, CellNeighborhood.NeighborhoodDict["south"]);
                }
                else
                {
                    LiveNoNeighbors(CellGrid, CellNeighborhood.NeighborhoodDict["west"]);
                    LiveNoNeighbors(CellGrid, CellNeighborhood.NeighborhoodDict["east"]);
                }

            }
        }
    }
}