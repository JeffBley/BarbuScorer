using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CardGameScorer.Models;

public class DoubleBid : INotifyPropertyChanged
{
    private bool _isRedoubled;

    public Player Doubler { get; set; } = null!;
    public Player Target { get; set; } = null!;
    
    public bool IsRedoubled
    {
        get => _isRedoubled;
        set { _isRedoubled = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public enum DoubleTargetStatus
{
    Available,      // Can be doubled
    AlreadyDoubled, // This player already doubled current bidder
    Redoubled       // This player already doubled current bidder and was redoubled
}

public class DoubleTargetInfo : INotifyPropertyChanged
{
    private bool _isSelected;

    public Player Player { get; set; } = null!;
    public DoubleTargetStatus Status { get; set; } = DoubleTargetStatus.Available;
    public bool IsMandatory { get; set; } = false;
    public bool IsDealer { get; set; } = false;

    public bool IsAvailable => Status == DoubleTargetStatus.Available;
    public bool IsClickable => IsAvailable && !IsMandatory;
    public string StatusText => Status switch
    {
        DoubleTargetStatus.AlreadyDoubled => "Doubled",
        DoubleTargetStatus.Redoubled => "Redoubled",
        _ => IsMandatory ? "(Required)" : ""
    };

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class BiddingState : INotifyPropertyChanged
{
    private Player? _currentBidder;
    private BiddingPhase _phase = BiddingPhase.NotStarted;
    private string _biddingMessage = "";

    public BiddingPhase Phase
    {
        get => _phase;
        set { _phase = value; OnPropertyChanged(); }
    }

    public Player? CurrentBidder
    {
        get => _currentBidder;
        set { _currentBidder = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentBidderName)); }
    }

    public string CurrentBidderName => CurrentBidder?.DisplayName ?? "";

    public string BiddingMessage
    {
        get => _biddingMessage;
        set { _biddingMessage = value; OnPropertyChanged(); }
    }

    // All doubles placed during this round
    private List<DoubleBid> _doubles = new();
    public List<DoubleBid> Doubles 
    { 
        get => _doubles;
        set { _doubles = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoDoubles)); }
    }
    
    public bool HasNoDoubles => Doubles.Count == 0;
    
    public void NotifyDoublesChanged()
    {
        OnPropertyChanged(nameof(Doubles));
        OnPropertyChanged(nameof(HasNoDoubles));
    }

    // Track which players have completed their doubling turn
    public List<Player> PlayersWhoHaveDoubled { get; set; } = new();

    // Queue of doubles pending redouble response (immediate response after each double)
    public Queue<DoubleBid> PendingRedoubleResponses { get; set; } = new();

    // Track players who need to respond to redoubles
    public List<Player> PlayersNeedingRedoubleResponse { get; set; } = new();

    // Current doubles targeting the current responder (for redouble phase)
    public List<DoubleBid> DoublesAgainstCurrentPlayer { get; set; } = new();

    // History stack for going back during bidding
    public Stack<BiddingHistoryEntry> History { get; set; } = new();

    public void SaveState(Player currentBidder)
    {
        var entry = new BiddingHistoryEntry
        {
            Bidder = currentBidder,
            DoublesCopy = Doubles.Select(d => new DoubleBid 
            { 
                Doubler = d.Doubler, 
                Target = d.Target, 
                IsRedoubled = d.IsRedoubled 
            }).ToList(),
            PlayersWhoHaveBidCopy = PlayersWhoHaveDoubled.ToList()
        };
        History.Push(entry);
    }

    public BiddingHistoryEntry? PopState()
    {
        if (History.Count > 0)
            return History.Pop();
        return null;
    }

    public bool CanGoBack => History.Count > 0;

    public void Reset()
    {
        Phase = BiddingPhase.NotStarted;
        CurrentBidder = null;
        BiddingMessage = "";
        Doubles.Clear();
        NotifyDoublesChanged();
        PlayersWhoHaveDoubled.Clear();
        PendingRedoubleResponses.Clear();
        PlayersNeedingRedoubleResponse.Clear();
        DoublesAgainstCurrentPlayer.Clear();
        History.Clear();
    }

    // Check if playerA has already doubled playerB
    public DoubleBid? GetExistingDouble(Player doubler, Player target)
    {
        return Doubles.FirstOrDefault(d => d.Doubler == doubler && d.Target == target);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public enum BiddingPhase
{
    NotStarted,
    Doubling,      // Players are choosing who to double
    Redoubling,    // Doubled players are responding
    Complete       // Bidding is finished
}

public class BiddingHistoryEntry
{
    public Player Bidder { get; set; } = null!;
    public List<DoubleBid> DoublesCopy { get; set; } = new();
    public List<Player> PlayersWhoHaveBidCopy { get; set; } = new();
}
