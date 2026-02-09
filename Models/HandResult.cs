namespace CardGameScorer.Models;

public class HandResult
{
    public int HandNumber { get; set; }
    public Player Dealer { get; set; } = null!;
    public ContractType Contract { get; set; }
    public int[] PlayerScores { get; set; } = new int[4];
    public int[] RawInputs { get; set; } = new int[4];
    public List<DoubleBid> Doubles { get; set; } = new();

    public string ContractName => new Contract { Type = Contract }.Name;

    public string DoublesDescription
    {
        get
        {
            if (Doubles.Count == 0) return "None";
            return string.Join(", ", Doubles.Select(d =>
                d.IsRedoubled 
                    ? $"{d.Doubler.Name[0]}→{d.Target.Name[0]}(R)"
                    : $"{d.Doubler.Name[0]}→{d.Target.Name[0]}"));
        }
    }
}
