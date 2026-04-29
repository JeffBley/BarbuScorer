using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CardGameScorer.Models;

public class Player : INotifyPropertyChanged
{
    private string _name = "";
    private int _totalScore;

    public int Index { get; set; }
    public Position Position { get; set; }

    public string PositionName => Position.ToDisplayString();
    public string PositionInitial => Position.ToInitial();
    public string NameWithPosition => $"{Name} ({PositionInitial})";

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? PositionName : $"{Name} ({PositionName})";

    public int TotalScore
    {
        get => _totalScore;
        set { _totalScore = value; OnPropertyChanged(); }
    }

    // Track which contracts this player has dealt (each must be dealt once)
    public List<ContractType> DealtContracts { get; set; } = new();

    public bool HasDealtContract(ContractType type) => DealtContracts.Contains(type);

    public List<ContractType> RemainingContracts =>
        Enum.GetValues<ContractType>()
            .Where(c => !DealtContracts.Contains(c))
            .Where(c => c != ContractType.RavageCity || Contract.RavageCityModeEnabled)
            .Where(c => c != ContractType.ChinesePoker || Contract.ChinesePokerModeEnabled)
            .Where(c => c != ContractType.Salade || Contract.SaladeModeEnabled)
            .Where(c => !Contract.SaladeModeEnabled || (c != ContractType.Trumps && c != ContractType.FanTan))
            .ToList();

    public void NotifyContractsChanged()
    {
        OnPropertyChanged(nameof(RemainingContracts));
        OnPropertyChanged(nameof(DealtContracts));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
