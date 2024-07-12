using System;
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
        public E_CellType CellType = E_CellType.Cell;
        public int Age = 0,
            Column = 0,
            Row = 0,
            MatureAge = 3,
            Nationality = -1,
            MinLivingNeighbors = 2,
            MaxLivingNeighbors = 3;

        public bool GetIsAlive()
        {
            return IsAlive;
        }

        public virtual void Live()
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
            else if (!IsAlive && CellNeighborhood.NumNeighbors == MinLivingNeighbors)
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
                case E_CellType.Cell_King:
                    cell = new Cell_King(column, row, true);
                    break;
                case E_CellType.Cell_Hunter:
                    cell = new Cell_Hunter(column, row, true);
                    break;
                case E_CellType.Cell_Bomber:
                    cell = new Cell_Bomber(column, row, true);
                    break;
                case E_CellType.Cell_Warrior:
                    cell = new Cell_Bomber(column, row, true);
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
            if (IsAlive)
            {
                Age = 0;
                List<Cell> cells = new List<Cell>();
                foreach (KeyValuePair<string, Cell> cell in CellNeighborhood.NeighborhoodDict)
                {
                    if (cell.Value != null && cell.Value.GetIsAlive() == false)
                    {
                        cells.Add(cell.Value);
                    }
                }
                int randNeighbor = UnityEngine.Random.Range(0, cells.Count);
                cells[randNeighbor] = ReplaceCell(cells[randNeighbor], CellType, true);
            }
        }

        private void LiveNoNeighbors(Cell[,] CellGrid, Cell cell)
        {
            if (cell.CellNeighborhood.NumNeighbors == 0)
            {
                cell.CellNeighborhood = new Cell_Neighborhood(CellGrid, cell.Column, cell.Row);
                cell.Live();
            }
        }

        public virtual void Immaculate(Cell[,] CellGrid)
        {
            Conditions.RemoveAll(item => item == "immaculate");
            LiveNoNeighbors(CellGrid, this);
            if (IsAlive)
            {
                if (UnityEngine.Random.Range(1, 3) == 1)
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
                stringChars[i] = chars[UnityEngine.Random.Range(1, chars.Length)];
            }

            return prefix + "_" + new string(stringChars);
        }

        public void ChooseNation() // this method should always be called in Live() because cellNeighborhood should be defined first if possible
        {
            if (IsAlive)
            {
                if (Nationality < 0)
                {
                    if (CellNeighborhood != null && CellNeighborhood.NumNeighbors > 0)
                    {
                        List<int> neighborNations = new();
                        foreach (Cell neighbor in CellNeighborhood.NeighborhoodDict.Values)
                        {
                            if (neighbor.GetIsAlive() && neighbor.Nationality > 0)
                            {
                                neighborNations.Add(neighbor.Nationality);
                            }
                        }
                        int rand = UnityEngine.Random.Range(0, neighborNations.Count);
                        Nationality = rand > 0 ? neighborNations[rand] : -1;
                    }
                    else
                    {
                        Nationality = -1;
                    }
                }
            }
        }

        protected Cell SelectNearbyCellByRule(Cell[,] cellGrid,
            Func<Cell, bool> searchRule,
            int maxRange)
        {
            List<Cell> nearestOthers = new();
            int range = 1;
            while (nearestOthers.Count == 0 && range < maxRange)
            {
                for (int x = range * -1; x <= range; x++)
                {
                    //row beneath
                    int targetCol = (Column + x + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                    int targetRow = (Row + range * -1 + cellGrid.GetLength(1)) % cellGrid.GetLength(1);
                    if (searchRule(cellGrid[targetCol, targetRow]))
                    {
                        nearestOthers.Add(cellGrid[targetCol, targetRow]);
                    }
                    //row above
                    targetRow = (Row + range + cellGrid.GetLength(1)) % cellGrid.GetLength(1);
                    if (searchRule(cellGrid[targetCol, targetRow]))
                    {
                        nearestOthers.Add(cellGrid[targetCol, targetRow]);
                    }
                }
                for (int y = range * -1 + 1; y <= range - 1; y++)
                {
                    //col left
                    int targetCol = (Column + range * -1 + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                    int targetRow = (Row + y + cellGrid.GetLength(1)) % cellGrid.GetLength(1);
                    if (searchRule(cellGrid[targetCol, targetRow]))
                    {
                        nearestOthers.Add(cellGrid[targetCol, targetRow]);
                    }
                    //col right
                    targetCol = (Column + range + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                    if (searchRule(cellGrid[targetCol, targetRow]))
                    {
                        nearestOthers.Add(cellGrid[targetCol, targetRow]);
                    }
                }
                range++;
            }

            if (nearestOthers.Count > 0)
            {
                // select a target cell to travel toward
                return nearestOthers[UnityEngine.Random.Range(0, nearestOthers.Count)];
            }
            return null;
        }

        protected List<Cell> GetAllCellsInRangeByRule(
            Cell[,] cellGrid,
            Func<Cell, bool> searchRule,
            int maxRange)
        {
            List<Cell> cellsInRange = new();
            for (int columnOffset = -1; columnOffset <= maxRange; columnOffset++)
            {
                for (int rowOffset = -1; rowOffset <= maxRange; rowOffset++)
                {
                    int neighborColumn = (Column + columnOffset + cellGrid.GetLength(0)) % cellGrid.GetLength(0);
                    int neighborRow = (Row + rowOffset + cellGrid.GetLength(1)) % cellGrid.GetLength(1);

                    if (cellGrid[neighborColumn, neighborRow] != this && searchRule(cellGrid[neighborColumn, neighborRow]))
                    {
                        cellsInRange.Add(cellGrid[neighborColumn, neighborRow]);
                    }
                }
            }
            return cellsInRange;
        }

        public Cell FindNeighborInDirOfCell(Cell[,] cellGrid, Cell target)
        {
            if (target != null)
            {
                int innerDist = Math.Abs(Column - target.Column);
                int outerDist = Math.Abs(cellGrid.GetLength(0) - innerDist);
                int targetDir = Column == target.Column ? 0 : Column < target.Column ? 1 : -1;
                int fastestDir = innerDist <= outerDist ? 1 : -1;
                int nearestCol = (Column + targetDir * fastestDir + cellGrid.GetLength(0)) % cellGrid.GetLength(0);

                innerDist = Math.Abs(Row - target.Row);
                outerDist = Math.Abs(cellGrid.GetLength(0) - innerDist);
                targetDir = Row == target.Row ? 0 : Row < target.Row ? 1 : -1;
                fastestDir = innerDist <= outerDist ? 1 : -1;
                int nearestRow = (Row + targetDir * fastestDir + cellGrid.GetLength(1)) % cellGrid.GetLength(1);

                return cellGrid[nearestCol, nearestRow];
            }
            return this;
        }

        public static void CellThrowException(string log)
        {
            throw new Exception(log);
        }
    }
}