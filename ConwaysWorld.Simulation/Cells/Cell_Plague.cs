namespace ConwaysWorld.Simulation;

public class Cell_Plague : Cell_Diseased
{
    public Cell_Plague(int column, int row, bool isAlive)
        : base(column, row, isAlive)
    {
        TransmissionRate = (int)Math.Round(10 * 1.4);
        CellType = CellType.Plague;
        Disease = RandomCondition('p');
    }

    public override void Live()
    {
        IsAlive = true;
        Age++;
        if (Age > MatureAge) Conditions.Add("mature");
        CellType = CellType.Plague;
        ChooseNation();
    }

    public override void Die()
    {
        IsAlive = false;
        Conditions.Remove(Disease);
        Nationality = -1;
    }

    public override void SpecialActions(Cell[,] cellGrid) => SpreadDisease(cellGrid);
}
