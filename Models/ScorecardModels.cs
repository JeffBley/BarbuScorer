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
    private bool _isMostRecent;
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
        set
        {
            _isPlayed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CheckSum));
            OnPropertyChanged(nameof(CheckSumDisplay));
            OnPropertyChanged(nameof(IsCheckBalanced));
            OnPropertyChanged(nameof(IsCheckUnbalanced));
        }
    }

    public bool HasBiddingComplete 
    { 
        get => _hasBiddingComplete; 
        set { _hasBiddingComplete = value; OnPropertyChanged(); }
    }

    /// <summary>True if this row corresponds to the most recently completed contract in the game.</summary>
    public bool IsMostRecent
    {
        get => _isMostRecent;
        set { _isMostRecent = value; OnPropertyChanged(); }
    }

    public int? CheckSum => IsPlayed ? PlayerCells.Sum(c => c.Score ?? 0) : null;
    public string CheckSumDisplay => CheckSum?.ToString() ?? "";

    /// <summary>Expected total of all four player scores for this contract (assuming
    /// the standard, undoubled point distribution). Doubles cancel out between players
    /// so the sum should always equal this value when scoring is correct.</summary>
    public int? ExpectedCheckSum
    {
        get
        {
            if (CardGameScorer.Models.Contract.SaladeModeEnabled)
            {
                return Contract switch
                {
                    ContractType.Nullo => -65,
                    ContractType.NoQueens => -80,
                    ContractType.Hearts => -130,
                    ContractType.NoLastTwo => -30,
                    ContractType.Barbu => -50,
                    // -5*13 (tricks) + -20*4 (queens) + -10*13 (hearts) + -30 (last) + -50 (K\u2665) = -355
                    ContractType.Salade => -355,
                    _ => null
                };
            }
            if (CardGameScorer.Models.Contract.ChinesePokerModeEnabled)
            {
                return Contract switch
                {
                    ContractType.Nullo => -39,
                    ContractType.NoQueens => -48,
                    ContractType.Hearts => -45,
                    ContractType.NoLastTwo => -40,
                    ContractType.Barbu => -30,
                    ContractType.Trumps => 65,
                    ContractType.FanTan => CardGameScorer.Models.Contract.FanTanScoringTotal,
                    ContractType.RavageCity => -36,
                    ContractType.ChinesePoker => 108,
                    _ => null
                };
            }
            if (CardGameScorer.Models.Contract.RavageCityModeEnabled)
            {
                return Contract switch
                {
                    ContractType.Nullo => -39,
                    ContractType.NoQueens => -32,
                    ContractType.Hearts => -30,
                    ContractType.NoLastTwo => -30,
                    ContractType.Barbu => -21,
                    ContractType.Trumps => 91,
                    ContractType.FanTan => 85,
                    ContractType.RavageCity => -24,
                    _ => null
                };
            }
            return Contract switch
            {
                ContractType.Nullo => -26,
                ContractType.NoQueens => -24,
                ContractType.Hearts => -30,
                ContractType.NoLastTwo => -30,
                ContractType.Barbu => -20,
                ContractType.Trumps => 65,
                ContractType.FanTan => CardGameScorer.Models.Contract.FanTanScoringTotal,
                _ => null
            };
        }
    }

    public bool IsCheckBalanced => IsPlayed && ExpectedCheckSum.HasValue && CheckSum == ExpectedCheckSum;
    public bool IsCheckUnbalanced => IsPlayed && ExpectedCheckSum.HasValue && CheckSum != ExpectedCheckSum;

    public ScorecardRow()
    {
        for (int i = 0; i < 4; i++)
        {
            PlayerCells[i] = new ScorecardCell();
            PlayerCells[i].PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ScorecardCell.Score))
                {
                    OnPropertyChanged(nameof(CheckSum));
                    OnPropertyChanged(nameof(CheckSumDisplay));
                    OnPropertyChanged(nameof(IsCheckBalanced));
                    OnPropertyChanged(nameof(IsCheckUnbalanced));
                }
            };
        }
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

    /// <summary>
    /// Transposed view: one entry per player, each holding that player's cells across all
    /// contracts in this section (in row order). Built by the view-model after Rows are populated.
    /// </summary>
    public List<PlayerRowView> PlayerRows { get; set; } = new();
}

/// <summary>
/// One player's row across a dealer section's contracts (used for the transposed scorecard layout).
/// </summary>
public class PlayerRowView
{
    public Player Player { get; set; } = null!;
    public string PlayerName => Player.NameWithPosition;
    public List<ScorecardCell> Cells { get; set; } = new();
}
