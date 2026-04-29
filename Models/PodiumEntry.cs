namespace CardGameScorer.Models;

public class PodiumEntry
{
    public int Place { get; set; }
    public string PlaceLabel { get; set; } = string.Empty;
    public string Medal { get; set; } = string.Empty;
    public string MedalColor { get; set; } = "#cdd6f4";
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public double BlockHeight { get; set; }
}
