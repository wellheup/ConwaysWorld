using System.Collections.Generic;
using UnityEngine;
namespace ConwaysWorld
{
    /// <summary>
    /// keeps track of a list of cells supposedly in thenation but does not track life or death. 
    /// </summary>
    /// <remarks>
    /// a census should be taken for each nation each turn to see if those cells are still this nation or not
    /// TODO: if King should become a cell type, then diplomats should never be elected as king
    /// TODO: if king should become a cell type, update variable to be Cell_King, not Cell and replace cell when setting
    /// TODO: when diplomat cells are a thing, change Diplomats List to a list of Cell_Diplomat
    /// </remarks>
    public class Cell_Nation
    {
        public string Name;
        public Cell King = null;
        public List<Cell> Citizens;
        public List<Cell> Diplomats;
        // public GameObject Flag;

        public Cell_Nation(Cell citizenZero)
        {
            Name = citizenZero.Nationality;
            Diplomats = new();
            Citizens = new List<Cell> { citizenZero };
        }

        public void SetKing(Cell king)
        {
            King = king;
        }

        public void Census()
        {
            List<Cell> temp = new();
            foreach (Cell _ in Citizens)
            {
                if (_.GetIsAlive() && _.Nationality == Name)
                {
                    temp.Add(_);
                }
            }
            Citizens = temp;
            if (Citizens.Count > 5)
            {
                SetKing(Citizens[Random.Range(0, Citizens.Count)]);
            }
        }

        public void ElectDiplomat(Cell[,] cellGrid)
        {
            if (Diplomats.Count < .1f * Citizens.Count)
            {
                Cell newDiplomat = King;
                int maxTries = 5, attempt = 0;
                //select a cell whom is not a diplomat already
                while (attempt < maxTries && (newDiplomat == King || Diplomats.Contains(newDiplomat)))
                {
                    newDiplomat = Citizens[Random.Range(0, Citizens.Count)];
                    attempt++;
                }

                cellGrid[newDiplomat.Column, newDiplomat.Row] = Cell.ReplaceCell(newDiplomat, Cell_Generator.E_CellType.Cell_Diplomat, true);
                newDiplomat.Nationality = Name;

                Diplomats.Add(newDiplomat);
            }
        }
    }
}