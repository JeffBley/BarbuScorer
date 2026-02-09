using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CardGameScorer.Models;

/// <summary>
/// Represents a cell in the scorecard for a specific game and player
/// </summary>
public class ScorecardCell : INotifyPropertyChanged
{
    private int? _score;
    private string _doublesInfo = "";
    private List<string> _doubledTargets = new();
    private List<string> _redoubledTargets = new();

    public int? Score
    {
        get => _score;
        set { _score = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayScore)); OnPropertyChanged(nameof(HasScore)); }
    }

    public string DoublesInfo
    {
        get => _doublesInfo;
        set { _doublesInfo = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Position initials of players this player doubled (e.g., "S", "E")
    /// </summary>
    public List<string> DoubledTargets
    {
        get => _doubledTargets;
        set { _doubledTargets = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDoubles)); OnPropertyChanged(nameof(DoublesDisplay)); }
    }

    /// <summary>
    /// Position initials of players this player redoubled (e.g., "W")
    /// </summary>
    public List<string> RedoubledTargets
    {
        get => _redoubledTargets;
        set { _redoubledTargets = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasRedoubles)); OnPropertyChanged(nameof(RedoublesDisplay)); }
    }

    public bool HasDoubles => DoubledTargets.Count > 0;
    public bool HasRedoubles => RedoubledTargets.Count > 0;
    public string DoublesDisplay => string.Join(", ", DoubledTargets);
    public string RedoublesDisplay => string.Join(", ", RedoubledTargets);

    public string DisplayScore => Score?.ToString() ?? "";
    public bool HasScore => Score.HasValue;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// Represents a row in the scorecard (one game for one dealer)
/// </summary>
public class ScorecardRow : INotifyPropertyChanged
{
    private bool _isPlayed;
    private bool _hasBiddingComplete;
    private string _gameName = "";

    public ContractType Contract { get; set; }
    public string GameName 
    { 
        get => _gameName; 
        set { _gameName = value; OnPropertyChanged(); } 
    }
    public Player Dealer { get; set; } = null!;
    public ScorecardCell[] PlayerCells { get; set; } = new ScorecardCell[4];
    public string DoublesDescription { get; set; } = "";
    
    public bool IsPlayed 
    { 
        get => _isPlayed; 
        set { _isPlayed = value; OnPropertyChanged(); OnPropertyChanged(nameof(CheckSum)); OnPropertyChanged(nameof(CheckSumDisplay)); }
    }

    public bool HasBiddingComplete 
    { 
        get => _hasBiddingComplete; 
        set { _hasBiddingComplete = value; OnPropertyChanged(); }
    }

    public int? CheckSum => IsPlayed ? PlayerCells.Sum(c => c.Score ?? 0) : null;
    public string CheckSumDisplay => CheckSum?.ToString() ?? "";

    public ScorecardRow()
    {
        for (int i = 0; i < 4; i++)
            PlayerCells[i] = new ScorecardCell();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// Represents a dealer section in the scorecard (7 games for one dealer)
/// </summary>
public class DealerSection
{
    public Player Dealer { get; set; } = null!;
    public List<ScorecardRow> Rows { get; set; } = new();
}
