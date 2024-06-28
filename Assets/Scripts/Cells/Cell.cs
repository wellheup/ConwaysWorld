using System.Collections.Generic;
using UnityEngine;
using static ConwaysWorld.Cell_Generator;
namespace ConwaysWorld
{

    public abstract class Cell
    {
        public Cell_Neighborhood CellNeighborhood;
        public List<string> Conditions;
        protected bool IsAlive = false;
        public int Column = 0, Row = 0, Age = 0, MatureAge = 10;
        public E_CellType CellType = E_CellType.Cell;
        public int Nationality = -1;
        public int MinLivingNeighbors = 2;
        public int MaxLivingNeighbors = 4;


        public bool GetIsAlive()
        {
            return IsAlive;
        }

        public virtual void Live(Cell[,] cellGrid)
        {
            IsAlive = true;
            Age++;
            if (Age > MatureAge && !Conditions.Contains("mature"))
            {
                Conditions.Add("mature");
            }
            ChooseNation();
        }

        public virtual void Die()
        {
            IsAlive = false;
            Age = 0;
            Nationality = -1;
        }

        public virtual bool CalcCellAliveNextGen()
        {
            return LiveBasic();
        }

        protected virtual bool LiveBasic()
        {
            if (IsAlive && CellNeighborhood.NumNeighbors < MinLivingNeighbors)
            {
                return false; // Die due to underpopulation
            }
            else if (IsAlive && CellNeighborhood.NumNeighbors >= MinLivingNeighbors && CellNeighborhood.NumNeighbors <= MaxLivingNeighbors)
            {
                return true; // Live on
            }
            else if (IsAlive && CellNeighborhood.NumNeighbors > MaxLivingNeighbors)
            {
                return false; // Die due to overpopulation
            }
            else if (!IsAlive && CellNeighborhood.NumNeighbors == MaxLivingNeighbors)
            {
                return true; // Become alive due to reproduction
            }
            else if (!IsAlive && CellNeighborhood.NumNeighbors != MaxLivingNeighbors)
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
                case E_CellType.Cell_Doctor:
                    cell = new Cell_Doctor(column, row, isAlive);
                    break;
                case E_CellType.Cell_Diplomat:
                    cell = new Cell_Diplomat(column, row, isAlive);
                    break;
                default:
                    cell = new Cell_Basic(column, row, isAlive); //this should not occur...
                    break;
            }
            cell.Conditions = oldCell.Conditions;
            cell.CellNeighborhood = oldCell.CellNeighborhood;
            if (oldCell.Nationality == -1) cell.ChooseNation();
            else cell.Nationality = oldCell.Nationality;

            return cell;
        }

        public static void SwapCells(Cell originCell, Cell dest, Cell[,] cellGrid)
        {
            int oldCol = originCell.Column, oldRow = originCell.Row;

            cellGrid[dest.Column, dest.Row] = originCell;
            cellGrid[originCell.Column, originCell.Row] = dest;

            originCell.Column = dest.Column;
            originCell.Row = dest.Row;

            dest.Column = oldCol;
            dest.Row = oldRow;

            originCell.CellNeighborhood = new Cell_Neighborhood(cellGrid, originCell.Column, originCell.Row);
            dest.CellNeighborhood = new Cell_Neighborhood(cellGrid, dest.Column, dest.Row);
        }

        public virtual void Breed()
        {
            Conditions.RemoveAll(item => item == "mature");
            Age = 0;
            List<Cell> cells = new List<Cell>();
            foreach (KeyValuePair<string, Cell> cell in CellNeighborhood.NeighborhoodDict)
            {
                if (cell.Value.GetIsAlive() == false)
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

        public static string RandomCondition(char prefix)
        {
            var chars = "0123456789";
            var stringChars = new char[8];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[Random.Range(1, chars.Length)];
            }

            return prefix + "_" + new string(stringChars);
        }

        public void ChooseNation() // this method should always be called in Live() because cellNeighborhood should be defined first if possible
        {
            if (Nationality < 0 || Nationality >= Cell_Nation.Nation_Colors.Count)
            {
                if (CellNeighborhood != null && CellNeighborhood.NumNeighbors > 0)
                {
                    List<int> neighborNations = new();
                    foreach (Cell neighbor in CellNeighborhood.NeighborhoodDict.Values)
                    {
                        if (neighbor.GetIsAlive() && (neighbor.Nationality < 0 || neighbor.Nationality < Cell_Nation.Nation_Colors.Count))
                        {
                            neighborNations.Add(neighbor.Nationality);
                        }
                    }
                    int rand = Random.Range(0, neighborNations.Count + 1);
                    Nationality = rand == neighborNations.Count ? Random.Range(0, Cell_Nation.Nation_Colors.Count) : neighborNations[rand];
                }
                else
                {
                    Nationality = Random.Range(0, Cell_Nation.Nation_Colors.Count);
                }
            }
        }
    }
}