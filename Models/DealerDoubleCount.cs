using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CardGameScorer.Models;

/// <summary>
/// Tracks how many times a doubler has doubled a specific dealer when they were the dealer.
/// Max is 2 (once per each of their 7 games, but limited to 2 per rule).
/// </summary>
public class DealerDoubleCount : INotifyPropertyChanged
{
    private int _count;

    public Player Doubler { get; set; } = null!;
    public Player Dealer { get; set; } = null!;
    public bool IsSelf { get; set; }
    
    public int Count
    {
        get => _count;
        set { _count = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); OnPropertyChanged(nameof(IsComplete)); OnPropertyChanged(nameof(ColorCategory)); }
    }

    public bool IsComplete => IsSelf || Count >= 2;
    public string DisplayText => IsSelf ? "-" : (Count >= 2 ? "2+" : Count.ToString());
    /// <summary>0 = red, 1 = yellow, 2+ = green</summary>
    public int ColorCategory => IsSelf ? 2 : (Count >= 2 ? 2 : Count);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// Summary row for dealer doubles matrix display.
/// </summary>
public class DealerDoubleRow
{
    public Player Doubler { get; set; } = null!;
    public List<DealerDoubleCount> DoubleCounts { get; set; } = new();
}
