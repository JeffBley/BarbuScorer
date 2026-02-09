namespace CardGameScorer.Models;

public enum ContractType
{
    Nullo,          // -2 per trick (or -3 with Ravage City/Chinese Poker)
    NoQueens,       // -6 per queen (or -8 with Ravage City, -12 with Chinese Poker)
    Hearts,         // -2 per heart (or -3 with Chinese Poker)
    NoLastTwo,      // -10 last trick, -20 second to last (or -15/-25 with Chinese Poker)
    Barbu,          // -20 for King of Hearts (or -21 with Ravage City, -30 with Chinese Poker)
    Trumps,         // +5 per trick (or +7 with Ravage City)
    FanTan,         // Points based on finish order
    RavageCity,     // Most cards in any suit scores negative
    ChinesePoker    // 6 points per beat (requires Ravage City)
}

public class Contract
{
    public static BarbuVersion CurrentVersion { get; set; } = BarbuVersion.Classic;
    public static bool RavageCityModeEnabled { get; set; } = false;
    public static bool ChinesePokerModeEnabled { get; set; } = false;
    
    // Fan Tan scoring values for dynamic descriptions
    public static int FanTanScore1st { get; set; } = 40;
    public static int FanTanScore2nd { get; set; } = 25;
    public static int FanTanScore3rd { get; set; } = 10;
    public static int FanTanScore4th { get; set; } = -10;
    public static string FanTanScoringDisplay => $"{FanTanScore1st}/{FanTanScore2nd}/{FanTanScore3rd}/{FanTanScore4th}";
    public static int FanTanScoringTotal => FanTanScore1st + FanTanScore2nd + FanTanScore3rd + FanTanScore4th;
    
    public ContractType Type { get; set; }
    public string Name => GetDisplayName(Type);

    public string Description => GetDescription(Type);

    public static List<Contract> GetAllContracts() =>
        Enum.GetValues<ContractType>().Select(t => new Contract { Type = t }).ToList();

    public static string GetDisplayName(ContractType type) => CurrentVersion switch
    {
        BarbuVersion.Modern => type switch
        {
            ContractType.Nullo => "No Tricks",
            ContractType.NoQueens => "No Queens",
            ContractType.Hearts => "No Hearts",
            ContractType.NoLastTwo => "No Last Two",
            ContractType.Barbu => "No King of Hearts",
            ContractType.Trumps => "Trumps",
            ContractType.FanTan => "Domino",
            ContractType.RavageCity => "Ravage City",
            ContractType.ChinesePoker => "Chinese Poker",
            _ => "Unknown"
        },
        _ => type switch
        {
            ContractType.Nullo => "Nullo",
            ContractType.NoQueens => "No Queens",
            ContractType.Hearts => "Hearts",
            ContractType.NoLastTwo => "No Last Two",
            ContractType.Barbu => "Barbu",
            ContractType.Trumps => "Trumps",
            ContractType.FanTan => "Fan Tan",
            ContractType.RavageCity => "Ravage City",
            ContractType.ChinesePoker => "Chinese Poker",
            _ => "Unknown"
        }
    };

    public static string GetDescription(ContractType type)
    {
        // Chinese Poker mode takes precedence (it requires Ravage City)
        if (ChinesePokerModeEnabled)
        {
            return CurrentVersion switch
            {
                BarbuVersion.Modern => type switch
                {
                    ContractType.Nullo => "-3 points per trick taken (-39 total)",
                    ContractType.NoQueens => "-12 points per queen taken (-48 total)",
                    ContractType.Hearts => "-3 points per heart taken. -9 points for the Ace of Hearts (-45 total)",
                    ContractType.NoLastTwo => "-25 points for the last trick. -15 points for the 2nd to last trick (-40 total)",
                    ContractType.Barbu => "-30 points for taking the King of Hearts",
                    ContractType.Trumps => "+5 points per trick won (+65 total)",
                    ContractType.FanTan => $"{FanTanScoringDisplay} based on finish order (+{FanTanScoringTotal} total)",
                    ContractType.RavageCity => "Most cards in any suit: -36 (ties: -18/-12/-9 each)",
                    ContractType.ChinesePoker => "+6 points per beat (+108 total)",
                    _ => ""
                },
                _ => type switch
                {
                    ContractType.Nullo => "-3 points per trick taken (-39 total)",
                    ContractType.NoQueens => "-12 points per queen taken (-48 total)",
                    ContractType.Hearts => "-3 points per heart taken. -9 points for the Ace of Hearts (-45 total)",
                    ContractType.NoLastTwo => "-25 points for the last trick. -15 points for the 2nd to last trick (-40 total)",
                    ContractType.Barbu => "-30 points for taking the King of Hearts",
                    ContractType.Trumps => "+5 points per trick won (+65 total)",
                    ContractType.FanTan => $"{FanTanScoringDisplay} based on finish order (+{FanTanScoringTotal} total)",
                    ContractType.RavageCity => "Most cards in any suit: -36 (ties: -18/-12/-9 each)",
                    ContractType.ChinesePoker => "+6 points per beat (+108 total)",
                    _ => ""
                }
            };
        }
        
        if (RavageCityModeEnabled)
        {
            return CurrentVersion switch
            {
                BarbuVersion.Modern => type switch
                {
                    ContractType.Nullo => "-3 points per trick taken (-39 total)",
                    ContractType.NoQueens => "-8 points per queen taken (-32 total)",
                    ContractType.Hearts => "-2 points per heart taken. -6 points for the Ace of Hearts (-30 total)",
                    ContractType.NoLastTwo => "-20 points for the last trick. -10 points for the 2nd to last trick (-30 total)",
                    ContractType.Barbu => "-21 points for taking the King of Hearts",
                    ContractType.Trumps => "+7 points per trick won (+91 total)",
                    ContractType.FanTan => "50/25/10/0 based on finish order (+85 total)",
                    ContractType.RavageCity => "Most cards in any suit: -24 (ties: -12/-8/-6 each)",
                    ContractType.ChinesePoker => "+6 points per beat (+108 total)",
                    _ => ""
                },
                _ => type switch
                {
                    ContractType.Nullo => "-3 points per trick taken (-39 total)",
                    ContractType.NoQueens => "-8 points per queen taken (-32 total)",
                    ContractType.Hearts => "-2 points per heart taken. -6 points for the Ace of Hearts (-30 total)",
                    ContractType.NoLastTwo => "-20 points for the last trick. -10 points for the 2nd to last trick (-30 total)",
                    ContractType.Barbu => "-21 points for taking the King of Hearts",
                    ContractType.Trumps => "+7 points per trick won (+91 total)",
                    ContractType.FanTan => "50/25/10/0 based on finish order (+85 total)",
                    ContractType.RavageCity => "Most cards in any suit: -24 (ties: -12/-8/-6 each)",
                    ContractType.ChinesePoker => "+6 points per beat (+108 total)",
                    _ => ""
                }
            };
        }
        
        return CurrentVersion switch
        {
            BarbuVersion.Modern => type switch
            {
                ContractType.Nullo => "-2 points per trick taken",
                ContractType.NoQueens => "-6 points per queen taken",
                ContractType.Hearts => "-2 points per heart taken. -6 points for the Ace of Hearts",
                ContractType.NoLastTwo => "-20 points for the last trick. -10 points for the 2nd to last trick",
                ContractType.Barbu => "-20 points for taking the King of Hearts",
                ContractType.Trumps => "+5 points per trick won",
                ContractType.FanTan => "45/20/5/-5 based on finish order",
                ContractType.RavageCity => "Most cards in any suit: -24 (ties: -12/-8/-6 each)",
                ContractType.ChinesePoker => "+6 points per beat (+108 total)",
                _ => ""
            },
            _ => type switch
            {
                ContractType.Nullo => "-2 points per trick taken",
                ContractType.NoQueens => "-6 points per queen taken",
                ContractType.Hearts => "-2 points per heart taken. -6 points for the Ace of Hearts",
                ContractType.NoLastTwo => "-20 points for the last trick. -10 points for the 2nd to last trick",
                ContractType.Barbu => "-20 points for taking the King of Hearts",
                ContractType.Trumps => "+5 points per trick won",
                ContractType.FanTan => "40/25/10/-10 based on finish order",
                ContractType.RavageCity => "Most cards in any suit: -24 (ties: -12/-8/-6 each)",
                ContractType.ChinesePoker => "+6 points per beat (+108 total)",
                _ => ""
            }
        };
    }
}

public class ContractOption : System.ComponentModel.INotifyPropertyChanged
{
    public ContractType Type { get; set; }
    public string Name => Contract.GetDisplayName(Type);
    
    public string Description
    {
        get
        {
            // Chinese Poker mode takes precedence (requires Ravage City)
            if (Contract.ChinesePokerModeEnabled)
            {
                return Contract.CurrentVersion switch
                {
                    BarbuVersion.Modern => Type switch
                    {
                        ContractType.Nullo => "-3 per trick (-39)",
                        ContractType.NoQueens => "-12 per queen (-48)",
                        ContractType.Hearts => "-3 per heart, -9 Ace (-45)",
                        ContractType.NoLastTwo => "-25 last, -15 2nd last (-40)",
                        ContractType.Barbu => "-30 for King of Hearts",
                        ContractType.Trumps => "+5 per trick (+65)",
                        ContractType.FanTan => $"{Contract.FanTanScoringDisplay} (+{Contract.FanTanScoringTotal})",
                        ContractType.RavageCity => "-36 most cards in suit",
                        ContractType.ChinesePoker => "+6 per beat (+108)",
                        _ => ""
                    },
                    _ => Type switch
                    {
                        ContractType.Nullo => "-3 per trick (-39)",
                        ContractType.NoQueens => "-12 per queen (-48)",
                        ContractType.Hearts => "-3 per heart, -9 Ace (-45)",
                        ContractType.NoLastTwo => "-25 last, -15 2nd last (-40)",
                        ContractType.Barbu => "-30 for King of Hearts",
                        ContractType.Trumps => "+5 per trick (+65)",
                        ContractType.FanTan => $"{Contract.FanTanScoringDisplay} (+{Contract.FanTanScoringTotal})",
                        ContractType.RavageCity => "-36 most cards in suit",
                        ContractType.ChinesePoker => "+6 per beat (+108)",
                        _ => ""
                    }
                };
            }
            
            if (Contract.RavageCityModeEnabled)
            {
                return Contract.CurrentVersion switch
                {
                    BarbuVersion.Modern => Type switch
                    {
                        ContractType.Nullo => "-3 per trick (-39)",
                        ContractType.NoQueens => "-8 per queen (-32)",
                        ContractType.Hearts => "-2 per heart, -6 Ace (-30)",
                        ContractType.NoLastTwo => "-20 last, -10 2nd last (-30)",
                        ContractType.Barbu => "-21 for King of Hearts",
                        ContractType.Trumps => "+7 per trick (+91)",
                        ContractType.FanTan => "50/25/10/0 finish (+85)",
                        ContractType.RavageCity => "-24 most cards in suit",
                        ContractType.ChinesePoker => "+6 per beat (+108)",
                        _ => ""
                    },
                    _ => Type switch
                    {
                        ContractType.Nullo => "-3 per trick (-39)",
                        ContractType.NoQueens => "-8 per queen (-32)",
                        ContractType.Hearts => "-2 per heart, -6 Ace (-30)",
                        ContractType.NoLastTwo => "-20 last, -10 2nd last (-30)",
                        ContractType.Barbu => "-21 for King of Hearts",
                        ContractType.Trumps => "+7 per trick (+91)",
                        ContractType.FanTan => "50/25/10/0 finish (+85)",
                        ContractType.RavageCity => "-24 most cards in suit",
                        ContractType.ChinesePoker => "+6 per beat (+108)",
                        _ => ""
                    }
                };
            }
            
            return Contract.CurrentVersion switch
            {
                BarbuVersion.Modern => Type switch
                {
                    ContractType.Nullo => "-2 per trick",
                    ContractType.NoQueens => "-6 per queen",
                    ContractType.Hearts => "-2 per heart, -6 for Ace of Hearts",
                    ContractType.NoLastTwo => "-20 last, -10 2nd last",
                    ContractType.Barbu => "-20 for King of Hearts",
                    ContractType.Trumps => "+5 per trick",
                    ContractType.FanTan => "45/20/5/-5 finish order",
                    ContractType.RavageCity => "-24 most cards in suit",
                    ContractType.ChinesePoker => "+6 per beat (+108)",
                    _ => ""
                },
                _ => Type switch
                {
                    ContractType.Nullo => "-2 per trick",
                    ContractType.NoQueens => "-6 per queen",
                    ContractType.Hearts => "-2 per heart, -6 for Ace of Hearts",
                    ContractType.NoLastTwo => "-20 last, -10 2nd last",
                    ContractType.Barbu => "-20 for King of Hearts",
                    ContractType.Trumps => "+5 per trick",
                    ContractType.FanTan => "40/25/10/-10 finish order",
                    ContractType.RavageCity => "-24 most cards in suit",
                    ContractType.ChinesePoker => "+6 per beat (+108)",
                    _ => ""
                }
            };
        }
    }
    
    public void RefreshName()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
    }

    private bool _isAvailable = true;
    public bool IsAvailable
    {
        get => _isAvailable;
        set { _isAvailable = value; OnPropertyChanged(nameof(IsAvailable)); }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => 
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
