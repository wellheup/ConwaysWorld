using System.Collections.Generic;
using UnityEngine;
namespace ConwaysWorld
{
    public class Cell_Generator
    {
        public enum E_CellType
        {
            Cell,
            Cell_Basic,
            Cell_Immortal,
            Cell_Diseased,
            Cell_Plague,
            Cell_Traveler,
            Cell_Explorer,
            Cell_Voyager,
            Cell_Doctor,
            Cell_Necromancer,
            Cell_Zombie,
            Cell_Warrior,
            Cell_Mutant,
            Cell_Islander,
            Cell_Bomber,
            Cell_Savior,
            Cell_Conqueror,
            Cell_Teacher,
            Cell_Irradiated,
            Cell_Diplomat,
            Cell_Hunter,
            Cell_God,
            Cell_Dead,
        }

        private struct CellSpawnFrequency
        {
            public E_CellType Type;
            public float TypeSpawnFrequency;
        }

        private List<CellSpawnFrequency> _spawnFrequencies;
        private float BasePercentLiving;

        public Cell_Generator(int basePercentLiving)
        {
            BasePercentLiving = basePercentLiving;
            _spawnFrequencies = new List<CellSpawnFrequency>();
            InitializeFrequencies();
        }

        private void InitializeFrequencies()
        {
            // This assumes a 0 - 1 range, with TypeSpawnFrequency being the percent chance of a CellType occuring.
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Immortal, TypeSpawnFrequency = 0.1f * BasePercentLiving / 100 });
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Diseased, TypeSpawnFrequency = 0.1f * BasePercentLiving / 100 });
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Traveler, TypeSpawnFrequency = 0.1f * BasePercentLiving / 100 });
            // _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Immortal, TypeSpawnFrequency = 0.01f * BasePercentLiving/100 });

            float remainingPercentOfLiving = BasePercentLiving / 100;
            for (int i = 0; i < _spawnFrequencies.Count; i++)
            {
                remainingPercentOfLiving -= _spawnFrequencies[i].TypeSpawnFrequency;
            }

            // Fill in the remaining amount of the living cells with basic living
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Basic, TypeSpawnFrequency = remainingPercentOfLiving });

            // Everything left is dead cells
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Dead, TypeSpawnFrequency = (100f - BasePercentLiving) / 100 });
        }

        private E_CellType GetRandomCellType()
        {
            float value = Random.value;
            float cumulative = 0;
            int k;
            for (k = 0; k < _spawnFrequencies.Count; k++)
            {
                cumulative += _spawnFrequencies[k].TypeSpawnFrequency;

                if (cumulative > value)
                {
                    break;
                }
            }
            return _spawnFrequencies[k].Type;
        }

        public Cell InitializeCell(int column, int row)
        {
            Cell cell;
            float _rollForVariant = Random.value;
            switch (GetRandomCellType())
            {
                case E_CellType.Cell_Basic:
                    cell = new Cell_Basic(column, row, true);
                    if (Random.Range(1, 5) == 1)// 1/4 chance disease immunity
                        cell.Conditions.Add("immune");
                    if (Random.Range(1, 101) == 1) //1/100 chance immaculate
                        cell.Conditions.Add("immaculate");
                    break;
                case E_CellType.Cell_Immortal:
                    cell = new Cell_Immortal(column, row, true);
                    break;
                case E_CellType.Cell_Diseased:
                    if (_rollForVariant > 1 / 6)
                        cell = new Cell_Diseased(column, row, true);
                    else
                        cell = new Cell_Plague(column, row, true);
                    break;
                case E_CellType.Cell_Traveler:
                    if (_rollForVariant > .8)
                        cell = new Cell_Voyager(column, row, true);
                    else if (_rollForVariant > .5)
                        cell = new Cell_Explorer(column, row, true);
                    else
                        cell = new Cell_Traveler(column, row, true);
                    break;
                default: //this is case E_CellType.Cell_Dead
                    cell = new Cell_Basic(column, row, false);
                    break;
            }

            return cell;
        }
    }

}