using UnityEngine;
using static ConwaysWorld.Cell_Generator;
using System.Collections.Generic;

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
        public Cell_Hunter(int column, int row, bool isAlive) : base(column, row, isAlive)
        {
            IsAlive = isAlive;
            Column = column;
            Row = row;
            CellType = E_CellType.Cell_Hunter;
            LiveColor = Cell_Colors.Cell_Hunter;
            DeadColor = Cell_Colors.Cell_Dead;
            CurrentColor = isAlive ? LiveColor : DeadColor;
            Conditions = new List<string>();
            PreyTypes = new() { E_CellType.Cell_Immortal, E_CellType.Cell_Zombie, E_CellType.Cell_King };
            CurrentPrey = null;
        }

        public override void Live(Cell[,] cellGrid)
        {
            IsAlive = true;
            CurrentColor = LiveColor;
            Age++;
            if (CellNeighborhood.NumNeighbors == 0)
            {
                DeathCountDown++;
            }
            else
            {
                DeathCountDown = 0;
            }
            SpecialPerformed = false;
            ChooseNation();
        }

        public override void Die()
        {
            base.Die();
            SpecialPerformed = true;
            Nationality = null;
        }

        public override bool CalcCellAliveNextGen()
        {
            if (DeathCountDown > MaxAloneTime)
            {
                return false;
            }
            return true;
        }

        private Cell SeekPrey(Cell[,] cellGrid)
        {
            //TODO: implement FindNearestOther into Cell such that it takes a rule and returns based on that rule

            return null;
        }

        public override void SpecialActions(Cell[,] cellGrid)
        {
            if (IsAlive && !SpecialPerformed)
            {
                /*
                if currentPrey is null look for prey adjacent
                    if CurrentPrey is adjacent kill it
                if no prey adjacent look for prey in range
                    CurrentPrey ??= SeekPrey(cellGrid);
                    if prey in range move toward prey
                    if no prey in range move in random direction
                */

                SwapCells(this, CellNeighborhood.NeighborhoodDict[Direction], cellGrid);

                SpecialPerformed = true;
            }
        }
    }
}