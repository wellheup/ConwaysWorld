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
            Cell_King,
            Cell_Dead,
        }

        private struct CellSpawnFrequency
        {
            public E_CellType Type;
            public float TypeSpawnFrequency;
        }

        private List<CellSpawnFrequency> _spawnFrequencies;
        private int BasePercentLiving;

        public Cell_Generator(int basePercentLiving)
        {
            BasePercentLiving = basePercentLiving;
            _spawnFrequencies = new List<CellSpawnFrequency>();
            InitializeFrequencies();
        }

        private void InitializeFrequencies()
        {
            // This assumes a 0 - 1 range, with TypeSpawnFrequency being the percent chance of a CellType occuring.
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Immortal, TypeSpawnFrequency = 0.025f * BasePercentLiving / 100 });
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Diseased, TypeSpawnFrequency = 0.2f * BasePercentLiving / 100 });
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Traveler, TypeSpawnFrequency = 0.05f * BasePercentLiving / 100 });
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Doctor, TypeSpawnFrequency = 0.05f * BasePercentLiving / 100 });
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Hunter, TypeSpawnFrequency = 0.05f * BasePercentLiving / 100 });
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Bomber, TypeSpawnFrequency = 0.1f * BasePercentLiving / 100 });

            float remainingPercentOfLiving = BasePercentLiving / 100f;
            for (int i = 0; i < _spawnFrequencies.Count; i++)
            {
                remainingPercentOfLiving -= _spawnFrequencies[i].TypeSpawnFrequency;
            }

            // Fill in the remaining amount of the living cells with basic living
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Basic, TypeSpawnFrequency = remainingPercentOfLiving });

            // Everything left is dead cells
            _spawnFrequencies.Add(new CellSpawnFrequency() { Type = E_CellType.Cell_Dead, TypeSpawnFrequency = (100f - BasePercentLiving) / 100 });

            float testSpawnFrequency = 0;
            for (int k = 0; k < _spawnFrequencies.Count; k++)
            {
                testSpawnFrequency += _spawnFrequencies[k].TypeSpawnFrequency;

                if (testSpawnFrequency > 1.0f)
                {
                    Debug.Log("Cell Generator is misconfigured, total TypeSpawnFrequency is: " + testSpawnFrequency);
                }
            }
        }

        private E_CellType GetRandomCellType()
        {
            float value = UnityEngine.Random.value;
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

        public Cell InitializeRandomCell(int column, int row)
        {
            Cell cell;
            float _rollForVariant = UnityEngine.Random.value;
            switch (GetRandomCellType())
            {
                case E_CellType.Cell_Basic:
                    cell = new Cell_Basic(column, row, true);
                    if (UnityEngine.Random.Range(1, 5) == 1)// 1/4 chance disease immunity
                        cell.Conditions.Add("immune");
                    if (UnityEngine.Random.Range(1, 101) == 1) //1/100 chance immaculate
                        cell.Conditions.Add("immaculate");
                    break;
                case E_CellType.Cell_Immortal:
                    cell = new Cell_Immortal(column, row, true);
                    break;
                case E_CellType.Cell_Diseased:
                    if (_rollForVariant > .2)
                        cell = new Cell_Diseased(column, row, true);
                    else
                        cell = new Cell_Plague(column, row, true);
                    break;
                case E_CellType.Cell_Traveler:
                    if (_rollForVariant > .4)
                        cell = new Cell_Traveler(column, row, true);
                    else
                        cell = new Cell_Explorer(column, row, true);
                    break;
                case E_CellType.Cell_Doctor:
                    cell = new Cell_Doctor(column, row, true);
                    break;
                // Diplomats and Kings should never be spawned on their own
                // case E_CellType.Cell_Diplomat:
                //     cell = new Cell_Diplomat(column, row, true);
                //     break;
                // case E_CellType.Cell_King:
                //     cell = new Cell_King(column, row, true);
                //     break;
                case E_CellType.Cell_Hunter:
                    cell = new Cell_Hunter(column, row, true);
                    break;
                case E_CellType.Cell_Bomber:
                    cell = new Cell_Bomber(column, row, true);
                    break;
                // warriors should never be spawned on their own
                // case E_CellType.Cell_Warrior:
                //     cell = new Cell_Warrior(column, row, true);
                //     break;
                default: //this is case E_CellType.Cell_Dead
                    cell = new Cell_Basic(column, row, false);
                    break;
            }

            return cell;
        }
    }

}