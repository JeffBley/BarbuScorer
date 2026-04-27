using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using CardGameScorer.Models;

namespace CardGameScorer.ViewModels;

public enum GameScreen
{
    PlayerSetup,
    GamePlay
}

public enum GamePhase
{
    SelectingContract,
    Bidding,
    EnteringScores,
    ScoreSummary
}

public class SavedGameInfo : INotifyPropertyChanged
{
    private bool _isShowingDeleteOption;
    
    public string FilePath { get; set; } = "";
    public string GameName { get; set; } = "";
    public int CurrentHandNumber { get; set; }
    public DateTime SavedAt { get; set; }
    
    public string DisplayName => string.IsNullOrEmpty(GameName) ? System.IO.Path.GetFileNameWithoutExtension(FilePath) : GameName;
    public string RoundInfo => $"Round {CurrentHandNumber} of 28";
    public string SavedAtDisplay => SavedAt.ToString("MMM d, yyyy h:mm tt");
    
    public bool IsShowingDeleteOption
    {
        get => _isShowingDeleteOption;
        set { _isShowingDeleteOption = value; OnPropertyChanged(); }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class MainViewModel : INotifyPropertyChanged
{
    private GameScreen _currentScreen = GameScreen.PlayerSetup;
    private GamePhase _currentPhase = GamePhase.SelectingContract;
    private Player? _currentDealer;
    private ContractType? _selectedContract;
    private int _currentHandNumber;
    private string _statusMessage = "Enter player names to begin";
    private string _errorMessage = "";
    private Player? _currentBiddingPlayer;
    private bool _isInRedoublePhase;
    private bool _isWaitingForImmediateRedouble;
    private DoubleBid? _currentPendingRedouble;

    // Player setup names
    private string _gameName = "";
    private string _westName = "";
    private string _northName = "";
    private string _eastName = "";
    private string _southName = "";
    private string? _autoSaveFilePath;

    public ObservableCollection<Player> Players { get; } = new();
    public ObservableCollection<HandResult> HandHistory { get; } = new();
    public ObservableCollection<Contract> AvailableContracts { get; } = new();
    public ObservableCollection<ContractOption> AllContractOptions { get; } = new();
    public BiddingState BiddingState { get; } = new();

    // Saved games for load picker
    public ObservableCollection<SavedGameInfo> SavedGames { get; } = new();

    // Double targets with status info (for current bidder) - legacy wizard, retained for save/load compatibility
    public ObservableCollection<DoubleTargetInfo> DoubleTargets { get; } = new();

    // Single-view doubling matrix (replaces the wizard).
    public ObservableCollection<DoublingMatrixRow> DoublingMatrix { get; } = new();
    public ObservableCollection<DoublingMatrixHeader> DoublingMatrixHeaders { get; } = new();

    // Redouble options for current player (legacy, kept for compatibility)
    public ObservableCollection<DoubleBid> RedoubleOptions { get; } = new();

    // Dealer doubles tracking - each player must double each other player twice when they're dealer
    public ObservableCollection<DealerDoubleRow> DealerDoubleMatrix { get; } = new();

    // Scorecard for history view - organized by dealer sections
    public ObservableCollection<DealerSection> Scorecard { get; } = new();

    // Settings menu
    private bool _isSettingsMenuOpen;
    public bool IsSettingsMenuOpen
    {
        get => _isSettingsMenuOpen;
        set { _isSettingsMenuOpen = value; OnPropertyChanged(); }
    }

    // Game Settings Panel
    private bool _isSettingsOpen;
    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set { _isSettingsOpen = value; OnPropertyChanged(); }
    }

    private int _selectedSettingsTab;
    public int SelectedSettingsTab
    {
        get => _selectedSettingsTab;
        set { _selectedSettingsTab = value; OnPropertyChanged(); }
    }

    private TextSize _selectedTextSize = TextSize.Small;
    public TextSize SelectedTextSize
    {
        get => _selectedTextSize;
        set 
        { 
            if (_selectedTextSize == value) return;
            _selectedTextSize = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(BaseFontSize));
            OnPropertyChanged(nameof(SmallFontSize));
            OnPropertyChanged(nameof(MediumFontSize));
            OnPropertyChanged(nameof(LargeFontSize));
            OnPropertyChanged(nameof(XLargeFontSize));
            OnPropertyChanged(nameof(XXLargeFontSize));
            OnPropertyChanged(nameof(HugeFontSize));
            OnPropertyChanged(nameof(TitleFontSize));
            OnPropertyChanged(nameof(CircleSize));
            OnPropertyChanged(nameof(CircleCornerRadius));
            OnPropertyChanged(nameof(CircleFontSize));
            OnPropertyChanged(nameof(DiamondOuterSize));
            OnPropertyChanged(nameof(DiamondInnerSize));
            OnPropertyChanged(nameof(DiamondFontSize));
            SaveSettings();
        }
    }

    private bool _dealerAllowedToDouble = false;
    public bool DealerAllowedToDouble
    {
        get => _dealerAllowedToDouble;
        set 
        { 
            if (_dealerAllowedToDouble == value) return;
            _dealerAllowedToDouble = value; 
            OnPropertyChanged();
            SaveSettings();
        }
    }

    private GameMode _gameMode = GameMode.Standard;
    public GameMode GameMode
    {
        get => _gameMode;
        set 
        { 
            if (_gameMode == value) return;
            var previousValue = _gameMode;
            _gameMode = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsStandardMode));
            OnPropertyChanged(nameof(IsSaladeMode));
            OnPropertyChanged(nameof(NotSaladeMode));
            // Disable optional games when Salade is selected
            if (value == GameMode.Salade)
            {
                RavageCityEnabled = false;
                ChinesePokerEnabled = false;
            }
            SaveSettings();
            // Prompt to start new game if a game is in progress
            if (IsGameScreen)
            {
                _pendingRevertGameMode = previousValue;
                ShowSettingsNewGamePrompt = true;
            }
        }
    }
    
    public bool IsStandardMode
    {
        get => GameMode == GameMode.Standard;
        set { if (value) GameMode = GameMode.Standard; }
    }
    
    public bool IsSaladeMode
    {
        get => GameMode == GameMode.Salade;
        set { if (value) GameMode = GameMode.Salade; }
    }

    public bool NotSaladeMode => GameMode != GameMode.Salade;

    private BarbuVersion _barbuVersion = BarbuVersion.Classic;
    public BarbuVersion BarbuVersion
    {
        get => _barbuVersion;
        set
        {
            if (_barbuVersion == value) return;
            _barbuVersion = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsClassicNames));
            OnPropertyChanged(nameof(IsModernNames));
            Contract.CurrentVersion = value;
            // Refresh contract display names
            foreach (var option in AllContractOptions)
            {
                option.RefreshName();
            }
            RefreshScorecardNames();
            SaveSettings();
        }
    }

    public bool IsClassicNames
    {
        get => BarbuVersion == BarbuVersion.Classic;
        set { if (value) BarbuVersion = BarbuVersion.Classic; }
    }

    public bool IsModernNames
    {
        get => BarbuVersion == BarbuVersion.Modern;
        set { if (value) BarbuVersion = BarbuVersion.Modern; }
    }

    private bool _ravageCityEnabled;
    public bool RavageCityEnabled
    {
        get => _ravageCityEnabled;
        set 
        { 
            if (_ravageCityEnabled == value) return;
            var previousValue = _ravageCityEnabled;
            _ravageCityEnabled = value; 
            Contract.RavageCityModeEnabled = value;
            OnPropertyChanged();
            // If Ravage City is disabled, Chinese Poker must also be disabled
            if (!value && ChinesePokerEnabled)
            {
                ChinesePokerEnabled = false;
            }
            // Refresh contract options to update descriptions and availability
            RefreshContractOptions();
            SaveSettings();
            // Prompt to start new game if a game is in progress
            if (IsGameScreen)
            {
                _pendingRevertRavageCityEnabled = previousValue;
                ShowSettingsNewGamePrompt = true;
            }
        }
    }

    private bool _chinesePokerEnabled;
    public bool ChinesePokerEnabled
    {
        get => _chinesePokerEnabled;
        set 
        { 
            if (_chinesePokerEnabled == value) return;
            var previousValue = _chinesePokerEnabled;
            _chinesePokerEnabled = value;
            Contract.ChinesePokerModeEnabled = value;
            OnPropertyChanged();
            // If Chinese Poker is enabled, Ravage City must also be enabled
            if (value && !RavageCityEnabled)
            {
                RavageCityEnabled = true;
            }
            // Refresh contract options to update descriptions and availability
            RefreshContractOptions();
            SaveSettings();
            // Prompt to start new game if a game is in progress
            if (IsGameScreen)
            {
                _pendingRevertChinesePokerEnabled = previousValue;
                ShowSettingsNewGamePrompt = true;
            }
        }
    }

    // Chinese Poker scoring mode
    private bool _chinesePokerScoreBySetting = true;  // true = by setting, false = by total beats
    public bool ChinesePokerScoreBySetting
    {
        get => _chinesePokerScoreBySetting;
        set { _chinesePokerScoreBySetting = value; OnPropertyChanged(); OnPropertyChanged(nameof(ChinesePokerScoreByTotalBeats)); }
    }
    public bool ChinesePokerScoreByTotalBeats
    {
        get => !_chinesePokerScoreBySetting;
        set { ChinesePokerScoreBySetting = !value; }
    }

    // Chinese Poker inputs - by setting (4 players × 3 settings)
    public int[,] ChinesePokerSettingInputs { get; } = new int[4, 3]; // [player, setting] where 0=Front, 1=Middle, 2=Back
    
    // Chinese Poker inputs - by total beats (4 players)
    public int[] ChinesePokerTotalInputs { get; } = new int[4];

    // Fan Tan/Domino Scoring
    private int _fanTanScore1st = 40;
    public int FanTanScore1st
    {
        get => _fanTanScore1st;
        set { _fanTanScore1st = value; Contract.FanTanScore1st = value; OnPropertyChanged(); OnPropertyChanged(nameof(FanTanScoringDisplay)); OnPropertyChanged(nameof(FanTanScoringTotal)); OnPropertyChanged(nameof(IsFanTanScoringValid)); }
    }

    private int _fanTanScore2nd = 25;
    public int FanTanScore2nd
    {
        get => _fanTanScore2nd;
        set { _fanTanScore2nd = value; Contract.FanTanScore2nd = value; OnPropertyChanged(); OnPropertyChanged(nameof(FanTanScoringDisplay)); OnPropertyChanged(nameof(FanTanScoringTotal)); OnPropertyChanged(nameof(IsFanTanScoringValid)); }
    }

    private int _fanTanScore3rd = 10;
    public int FanTanScore3rd
    {
        get => _fanTanScore3rd;
        set { _fanTanScore3rd = value; Contract.FanTanScore3rd = value; OnPropertyChanged(); OnPropertyChanged(nameof(FanTanScoringDisplay)); OnPropertyChanged(nameof(FanTanScoringTotal)); OnPropertyChanged(nameof(IsFanTanScoringValid)); }
    }

    private int _fanTanScore4th = -10;
    public int FanTanScore4th
    {
        get => _fanTanScore4th;
        set { _fanTanScore4th = value; Contract.FanTanScore4th = value; OnPropertyChanged(); OnPropertyChanged(nameof(FanTanScoringDisplay)); OnPropertyChanged(nameof(FanTanScoringTotal)); OnPropertyChanged(nameof(IsFanTanScoringValid)); }
    }

    public string FanTanScoringDisplay => $"{FanTanScore1st}/{FanTanScore2nd}/{FanTanScore3rd}/{FanTanScore4th}";
    public int FanTanScoringTotal => FanTanScore1st + FanTanScore2nd + FanTanScore3rd + FanTanScore4th;
    public bool IsFanTanScoringValid => FanTanScoringTotal == 65;

    // Temp values for editing
    private int _tempFanTanScore1st;
    private int _tempFanTanScore2nd;
    private int _tempFanTanScore3rd;
    private int _tempFanTanScore4th;

    public int TempFanTanScore1st
    {
        get => _tempFanTanScore1st;
        set { _tempFanTanScore1st = value; OnPropertyChanged(); OnPropertyChanged(nameof(TempFanTanScoringTotal)); OnPropertyChanged(nameof(IsTempFanTanScoringValid)); }
    }

    public int TempFanTanScore2nd
    {
        get => _tempFanTanScore2nd;
        set { _tempFanTanScore2nd = value; OnPropertyChanged(); OnPropertyChanged(nameof(TempFanTanScoringTotal)); OnPropertyChanged(nameof(IsTempFanTanScoringValid)); }
    }

    public int TempFanTanScore3rd
    {
        get => _tempFanTanScore3rd;
        set { _tempFanTanScore3rd = value; OnPropertyChanged(); OnPropertyChanged(nameof(TempFanTanScoringTotal)); OnPropertyChanged(nameof(IsTempFanTanScoringValid)); }
    }

    public int TempFanTanScore4th
    {
        get => _tempFanTanScore4th;
        set { _tempFanTanScore4th = value; OnPropertyChanged(); OnPropertyChanged(nameof(TempFanTanScoringTotal)); OnPropertyChanged(nameof(IsTempFanTanScoringValid)); }
    }

    public int TempFanTanScoringTotal => TempFanTanScore1st + TempFanTanScore2nd + TempFanTanScore3rd + TempFanTanScore4th;
    public bool IsTempFanTanScoringValid => TempFanTanScoringTotal == 65;

    private bool _showFanTanScoringEditor;
    public bool ShowFanTanScoringEditor
    {
        get => _showFanTanScoringEditor;
        set { _showFanTanScoringEditor = value; OnPropertyChanged(); }
    }

    public ICommand OpenFanTanScoringEditorCommand { get; }
    public ICommand SaveFanTanScoringCommand { get; }
    public ICommand DiscardFanTanScoringCommand { get; }

    private void OpenFanTanScoringEditor()
    {
        TempFanTanScore1st = FanTanScore1st;
        TempFanTanScore2nd = FanTanScore2nd;
        TempFanTanScore3rd = FanTanScore3rd;
        TempFanTanScore4th = FanTanScore4th;
        ShowFanTanScoringEditor = true;
    }

    private void SaveFanTanScoring()
    {
        if (IsTempFanTanScoringValid)
        {
            FanTanScore1st = TempFanTanScore1st;
            FanTanScore2nd = TempFanTanScore2nd;
            FanTanScore3rd = TempFanTanScore3rd;
            FanTanScore4th = TempFanTanScore4th;
            ShowFanTanScoringEditor = false;
            SaveSettings();
            // Refresh contract options to update descriptions with new Fan Tan values
            RefreshContractOptions();
        }
    }

    private void DiscardFanTanScoring()
    {
        ShowFanTanScoringEditor = false;
    }

    public double BaseFontSize => SelectedTextSize switch
    {
        TextSize.Small => 14,
        TextSize.Medium => 16,
        TextSize.Large => 18,
        _ => 14
    };

    // Scaled font sizes
    public double SmallFontSize => BaseFontSize * 0.78;     // ~11
    public double MediumFontSize => BaseFontSize * 0.86;    // ~12
    public double LargeFontSize => BaseFontSize * 1.14;     // ~16
    public double XLargeFontSize => BaseFontSize * 1.28;    // ~18
    public double XXLargeFontSize => BaseFontSize * 1.43;   // ~20
    public double HugeFontSize => BaseFontSize * 2.0;       // ~28
    public double TitleFontSize => BaseFontSize * 2.57;     // ~36

    // Icon sizes for doubling indicators (circles and diamonds)
    public double CircleSize => BaseFontSize;               // 14/16/18 based on setting
    public double CircleCornerRadius => BaseFontSize / 2;   // Half of size for circle
    public double CircleFontSize => BaseFontSize * 0.64;    // ~9 at small
    public double DiamondOuterSize => BaseFontSize * 1.14;  // ~16 at small
    public double DiamondInnerSize => BaseFontSize * 0.71;  // ~10 at small
    public double DiamondFontSize => BaseFontSize * 0.57;   // ~8 at small

    // Score editing
    private bool _isEditingScore;
    public bool IsEditingScore
    {
        get => _isEditingScore;
        set { _isEditingScore = value; OnPropertyChanged(); }
    }

    private ScorecardRow? _rowBeingEdited;
    public ScorecardRow? RowBeingEdited
    {
        get => _rowBeingEdited;
        set { _rowBeingEdited = value; OnPropertyChanged(); OnPropertyChanged(nameof(EditingGameDescription)); }
    }

    public string EditingGameDescription => RowBeingEdited != null 
        ? $"{RowBeingEdited.GameName} ({RowBeingEdited.Dealer.Name}'s deal)"
        : "";

    public int[] EditInputs { get; } = new int[4];

    private string? _editErrorMessage;
    public string? EditErrorMessage
    {
        get => _editErrorMessage;
        set { _editErrorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEditError)); }
    }

    public bool HasEditError => !string.IsNullOrEmpty(EditErrorMessage);

    // Score Summary properties
    public int[] SummaryBaseScores { get; } = new int[4];
    public int[] SummaryFinalScores { get; } = new int[4];
    public List<string> ScoringExplanation { get; } = new();
    
    private bool _showScoringExplanation;
    public bool ShowScoringExplanation
    {
        get => _showScoringExplanation;
        set { _showScoringExplanation = value; OnPropertyChanged(); }
    }
    
    private bool _showRestartBiddingConfirm;
    public bool ShowRestartBiddingConfirm
    {
        get => _showRestartBiddingConfirm;
        set { _showRestartBiddingConfirm = value; OnPropertyChanged(); }
    }
    
    private bool _showNewGameConfirm;
    public bool ShowNewGameConfirm
    {
        get => _showNewGameConfirm;
        set { _showNewGameConfirm = value; OnPropertyChanged(); }
    }
    
    private bool _showSettingsNewGamePrompt;
    public bool ShowSettingsNewGamePrompt
    {
        get => _showSettingsNewGamePrompt;
        set { _showSettingsNewGamePrompt = value; OnPropertyChanged(); }
    }
    
    // Store previous settings for reverting when user cancels
    private GameMode? _pendingRevertGameMode;
    private bool? _pendingRevertRavageCityEnabled;
    private bool? _pendingRevertChinesePokerEnabled;
    
    private bool _showSaveConfirmation;
    public bool ShowSaveConfirmation
    {
        get => _showSaveConfirmation;
        set { _showSaveConfirmation = value; OnPropertyChanged(); }
    }
    
    private bool _showLoadGamePicker;
    public bool ShowLoadGamePicker
    {
        get => _showLoadGamePicker;
        set { _showLoadGamePicker = value; OnPropertyChanged(); }
    }

    // Score inputs
    public int[] CurrentInputs { get; } = new int[4];

    // Ace of Hearts holder for Hearts contract
    private Player? _aceOfHeartsPlayer;
    public Player? AceOfHeartsPlayer
    {
        get => _aceOfHeartsPlayer;
        set { _aceOfHeartsPlayer = value; OnPropertyChanged(); }
    }

    // King of Hearts holder for Barbu contract
    private Player? _kingOfHeartsPlayer;
    public Player? KingOfHeartsPlayer
    {
        get => _kingOfHeartsPlayer;
        set { _kingOfHeartsPlayer = value; OnPropertyChanged(); }
    }

    // No Last Two trick winners
    private Player? _secondToLastTrickPlayer;
    public Player? SecondToLastTrickPlayer
    {
        get => _secondToLastTrickPlayer;
        set { _secondToLastTrickPlayer = value; OnPropertyChanged(); }
    }

    private Player? _lastTrickPlayer;
    public Player? LastTrickPlayer
    {
        get => _lastTrickPlayer;
        set { _lastTrickPlayer = value; OnPropertyChanged(); }
    }

    // Ravage City - players who took most cards in any suit (multiple can be selected for ties)
    public ObservableCollection<RavageCityPlayerSelection> RavageCityPlayerSelections { get; } = new();
    
    public List<Player> SelectedRavageCityPlayers => 
        RavageCityPlayerSelections.Where(s => s.IsSelected).Select(s => s.Player).ToList();

    public bool IsHeartsContract => SelectedContract == ContractType.Hearts;
    public bool IsBarbuContract => SelectedContract == ContractType.Barbu;
    public bool IsNoLastTwoContract => SelectedContract == ContractType.NoLastTwo;
    public bool IsRavageCityContract => SelectedContract == ContractType.RavageCity;
    public bool IsChinesePokerContract => SelectedContract == ContractType.ChinesePoker;
    
    public string RavageCityScoringDescription => ChinesePokerEnabled 
        ? "Scoring: 1 player = -36, 2-way tie = -18 each, 3-way = -12 each, 4-way = -9 each"
        : "Scoring: 1 player = -24, 2-way tie = -12 each, 3-way = -8 each, 4-way = -6 each";
    
    public bool ShowScoreInputs => SelectedContract != ContractType.Barbu && 
                                   SelectedContract != ContractType.NoLastTwo && 
                                   SelectedContract != ContractType.RavageCity &&
                                   SelectedContract != ContractType.ChinesePoker;

    #region Properties

    public GameScreen CurrentScreen
    {
        get => _currentScreen;
        set { _currentScreen = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSetupScreen)); OnPropertyChanged(nameof(IsGameScreen)); }
    }

    public bool IsSetupScreen => CurrentScreen == GameScreen.PlayerSetup;
    public bool IsGameScreen => CurrentScreen == GameScreen.GamePlay;

    public GamePhase CurrentPhase
    {
        get => _currentPhase;
        set 
        { 
            _currentPhase = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(IsSelectingContract));
            OnPropertyChanged(nameof(IsBidding));
            OnPropertyChanged(nameof(IsEnteringScores));
            OnPropertyChanged(nameof(IsScoreSummary));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsSelectingContract => CurrentPhase == GamePhase.SelectingContract;
    public bool IsBidding => CurrentPhase == GamePhase.Bidding;
    public bool IsEnteringScores => CurrentPhase == GamePhase.EnteringScores;
    public bool IsScoreSummary => CurrentPhase == GamePhase.ScoreSummary;

    public string GameName
    {
        get => _gameName;
        set { _gameName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanStartGame)); }
    }

    public string WestName
    {
        get => _westName;
        set { _westName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanStartGame)); }
    }

    public string NorthName
    {
        get => _northName;
        set { _northName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanStartGame)); }
    }

    public string EastName
    {
        get => _eastName;
        set { _eastName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanStartGame)); }
    }

    public string SouthName
    {
        get => _southName;
        set { _southName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanStartGame)); }
    }

    public bool CanStartGame => 
        !string.IsNullOrWhiteSpace(GameName) &&
        !string.IsNullOrWhiteSpace(WestName) &&
        !string.IsNullOrWhiteSpace(NorthName) &&
        !string.IsNullOrWhiteSpace(EastName) &&
        !string.IsNullOrWhiteSpace(SouthName);

    public Player? CurrentDealer
    {
        get => _currentDealer;
        set { _currentDealer = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentDealerDisplay)); OnPropertyChanged(nameof(DealerNamePossessive)); OnPropertyChanged(nameof(DealerTurnSuffix)); UpdateAvailableContracts(); }
    }

    public string CurrentDealerDisplay => CurrentDealer != null 
        ? $"{CurrentDealer.Name}'s turn to choose a game ({CurrentDealer.PositionName})" 
        : "";

    public string DealerNamePossessive => CurrentDealer != null 
        ? $"{CurrentDealer.Name}'s" 
        : "";

    public string DealerTurnSuffix => CurrentDealer != null 
        ? $"turn to choose a contract ({CurrentDealer.PositionName})" 
        : "";

    public ContractType? SelectedContract
    {
        get => _selectedContract;
        set 
        { 
            _selectedContract = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(ContractDescription)); 
            OnPropertyChanged(nameof(CanStartBid));
            OnPropertyChanged(nameof(InputLabel));
            OnPropertyChanged(nameof(ContractInstructions));
            OnPropertyChanged(nameof(IsHeartsContract));
            OnPropertyChanged(nameof(IsBarbuContract));
            OnPropertyChanged(nameof(IsNoLastTwoContract));
            OnPropertyChanged(nameof(IsRavageCityContract));
            OnPropertyChanged(nameof(IsChinesePokerContract));
            OnPropertyChanged(nameof(ShowScoreInputs));
            OnPropertyChanged(nameof(SelectedContractName));
            AceOfHeartsPlayer = null;  // Reset when contract changes
            KingOfHeartsPlayer = null;
            SecondToLastTrickPlayer = null;
            LastTrickPlayer = null;
            RavageCityPlayerSelections.Clear();  // Reset Ravage City selections
            // Reset Chinese Poker inputs
            Array.Clear(ChinesePokerSettingInputs);
            Array.Clear(ChinesePokerTotalInputs);
            ChinesePokerScoreBySetting = true;  // Reset to default
        }
    }

    public string ContractDescription => SelectedContract.HasValue
        ? new Contract { Type = SelectedContract.Value }.Description
        : "";

    public string SelectedContractName => SelectedContract.HasValue
        ? Contract.GetDisplayName(SelectedContract.Value)
        : "";

    public string InputLabel => SelectedContract switch
    {
        ContractType.Nullo => "Tricks taken:",
        ContractType.NoQueens => "Queens taken:",
        ContractType.Hearts => "Hearts taken (not including Ace):",
        ContractType.NoLastTwo => "Last tricks (0=none, 1=2nd last, 2=last, 3=both):",
        ContractType.Barbu => "Took King? (1=Yes, 0=No):",
        ContractType.Trumps => "Tricks won:",
        ContractType.FanTan => "Finish position (1-4):",
        ContractType.RavageCity => "Most cards in suit:",
        ContractType.ChinesePoker => "Beats won:",
        _ => "Value:"
    };

    public string ContractInstructions => SelectedContract switch
    {
        ContractType.Nullo => "Enter the number of tricks each player took",
        ContractType.NoQueens => "Enter the number of Queens each player took",
        ContractType.Hearts => "Enter the number of Hearts each player took (not counting the Ace of Hearts)",
        ContractType.NoLastTwo => "Enter who won the last two tricks",
        ContractType.Barbu => "Select who took the King of Hearts",
        ContractType.Trumps => "Enter the number of tricks each player took",
        ContractType.FanTan => "Enter the order each player went out (1/2/3/4)",
        ContractType.RavageCity => "Select the player(s) who took the most cards in any one suit",
        ContractType.ChinesePoker => "Enter the number of Beats per player per Setting",
        _ => "Enter the values"
    };

    public bool CanStartBid => SelectedContract.HasValue && CurrentPhase == GamePhase.SelectingContract;

    public int CurrentHandNumber
    {
        get => _currentHandNumber;
        set { _currentHandNumber = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private DispatcherTimer? _errorClearTimer;

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
            // Auto-dismiss after 5 seconds when an error is shown.
            _errorClearTimer?.Stop();
            if (!string.IsNullOrEmpty(value))
            {
                _errorClearTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _errorClearTimer.Tick -= OnErrorClearTick;
                _errorClearTimer.Tick += OnErrorClearTick;
                _errorClearTimer.Start();
            }
        }
    }

    private void OnErrorClearTick(object? sender, EventArgs e)
    {
        _errorClearTimer?.Stop();
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            _errorMessage = "";
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public Player? CurrentBiddingPlayer
    {
        get => _currentBiddingPlayer;
        set { _currentBiddingPlayer = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentBiddingPlayerDisplay)); }
    }

    public string CurrentBiddingPlayerDisplay => CurrentBiddingPlayer != null
        ? $"{CurrentBiddingPlayer.Name}'s turn to double"
        : "";

    public bool IsInRedoublePhase
    {
        get => _isInRedoublePhase;
        set { _isInRedoublePhase = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsInDoublingPhase)); }
    }

    public bool IsInDoublingPhase => !IsInRedoublePhase && !IsWaitingForImmediateRedouble;

    public bool IsWaitingForImmediateRedouble
    {
        get => _isWaitingForImmediateRedouble;
        set { _isWaitingForImmediateRedouble = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsInDoublingPhase)); }
    }

    public DoubleBid? CurrentPendingRedouble
    {
        get => _currentPendingRedouble;
        set { _currentPendingRedouble = value; OnPropertyChanged(); OnPropertyChanged(nameof(ImmediateRedoubleDisplay)); }
    }

    public string ImmediateRedoubleDisplay => CurrentPendingRedouble != null
        ? $"{CurrentPendingRedouble.Target.Name}: {CurrentPendingRedouble.Doubler.Name} doubled you. Redouble?"
        : "";

    // Total rounds: 28 standard, 32 with Ravage City (8 contracts), 36 with Chinese Poker (9 contracts)
    public int TotalRounds => ChinesePokerEnabled ? 36 : (RavageCityEnabled ? 32 : 28);
    
    public bool IsGameComplete => HandHistory.Count >= TotalRounds;

    #endregion

    #region Commands

    public ICommand StartGameCommand { get; }
    public ICommand StartBidCommand { get; }
    public ICommand ConfirmDoublesCommand { get; }
    public ICommand SkipDoubleCommand { get; }
    public ICommand MaxDoublesCommand { get; }
    public ICommand AcceptRedoubleCommand { get; }
    public ICommand DeclineRedoubleCommand { get; }
    public ICommand ConfirmBiddingMatrixCommand { get; }
    public ICommand RecordHandCommand { get; }
    public ICommand NewGameCommand { get; }
    public ICommand ToggleDoubleTargetCommand { get; }
    public ICommand ToggleSettingsMenuCommand { get; }
    public ICommand SaveGameCommand { get; }
    public ICommand LoadGameCommand { get; }
    public ICommand OpenGameSettingsCommand { get; }
    public ICommand CloseSettingsCommand { get; }
    public ICommand SetTextSizeCommand { get; }
    public ICommand SelectSettingsTabCommand { get; }
    public ICommand CloseSettingsMenuCommand { get; }
    public ICommand StartEditScoreCommand { get; }
    public ICommand ConfirmEditScoreCommand { get; }
    public ICommand CancelEditScoreCommand { get; }
    public ICommand ConfirmSummaryCommand { get; }
    public ICommand BackToScoreInputCommand { get; }
    public ICommand BackToBiddingCommand { get; }
    public ICommand ConfirmRestartBiddingCommand { get; }
    public ICommand CancelRestartBiddingCommand { get; }
    public ICommand ToggleScoringExplanationCommand { get; }
    public ICommand BackBiddingStepCommand { get; }
    public ICommand SelectContractCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand ConfirmNewGameCommand { get; }
    public ICommand CancelNewGameCommand { get; }
    public ICommand ConfirmSettingsNewGameCommand { get; }
    public ICommand CancelSettingsNewGameCommand { get; }
    public ICommand SelectSavedGameCommand { get; }
    public ICommand CancelLoadGameCommand { get; }
    public ICommand ToggleSaveDeleteMenuCommand { get; }
    public ICommand DeleteSavedGameCommand { get; }
    public ICommand DismissSaveConfirmationCommand { get; }

    #endregion

    public MainViewModel()
    {
        StartGameCommand = new RelayCommand(StartGame, () => CanStartGame);
        StartBidCommand = new RelayCommand(StartBidding, () => CanStartBid);
        ConfirmDoublesCommand = new RelayCommand(ConfirmDoubles);
        SkipDoubleCommand = new RelayCommand(SkipDouble);
        MaxDoublesCommand = new RelayCommand(MaxDoubles);
        AcceptRedoubleCommand = new RelayCommand(AcceptRedouble);
        DeclineRedoubleCommand = new RelayCommand(DeclineRedouble);
        ConfirmBiddingMatrixCommand = new RelayCommand(ConfirmBiddingMatrix);
        RecordHandCommand = new RelayCommand(RecordHand);
        NewGameCommand = new RelayCommand(() => { IsSettingsMenuOpen = false; ShowNewGameConfirm = true; });
        ConfirmNewGameCommand = new RelayCommand(ConfirmNewGame);
        CancelNewGameCommand = new RelayCommand(() => ShowNewGameConfirm = false);
        ConfirmSettingsNewGameCommand = new RelayCommand(ConfirmSettingsNewGame);
        CancelSettingsNewGameCommand = new RelayCommand(CancelSettingsNewGame);
        DismissSaveConfirmationCommand = new RelayCommand(() => ShowSaveConfirmation = false);
        SelectSavedGameCommand = new RelayCommand<SavedGameInfo>(LoadSelectedGame);
        CancelLoadGameCommand = new RelayCommand(() => ShowLoadGamePicker = false);
        ToggleSaveDeleteMenuCommand = new RelayCommand<SavedGameInfo>(ToggleSaveDeleteMenu);
        DeleteSavedGameCommand = new RelayCommand<SavedGameInfo>(DeleteSavedGame);
        ToggleDoubleTargetCommand = new RelayCommand<DoubleTargetInfo>(ToggleDoubleTarget);
        ToggleSettingsMenuCommand = new RelayCommand(() => IsSettingsMenuOpen = !IsSettingsMenuOpen);
        SaveGameCommand = new RelayCommand(SaveGame);
        LoadGameCommand = new RelayCommand(LoadGame);
        OpenGameSettingsCommand = new RelayCommand(OpenGameSettings);
        CloseSettingsCommand = new RelayCommand(() => IsSettingsOpen = false);
        SetTextSizeCommand = new RelayCommand<TextSize>(size => SelectedTextSize = size);
        SelectSettingsTabCommand = new RelayCommand<string>(tabStr => { if (int.TryParse(tabStr, out int tab)) SelectedSettingsTab = tab; });
        CloseSettingsMenuCommand = new RelayCommand(() => IsSettingsMenuOpen = false);
        StartEditScoreCommand = new RelayCommand<ScorecardRow>(StartEditScore);
        ConfirmEditScoreCommand = new RelayCommand(ConfirmEditScore);
        CancelEditScoreCommand = new RelayCommand(() => { IsEditingScore = false; RowBeingEdited = null; });
        ConfirmSummaryCommand = new RelayCommand(ConfirmSummary);
        BackToScoreInputCommand = new RelayCommand(BackToScoreInput);
        BackToBiddingCommand = new RelayCommand(BackToBiddingMatrix);
        ConfirmRestartBiddingCommand = new RelayCommand(ConfirmRestartBidding);
        CancelRestartBiddingCommand = new RelayCommand(() => ShowRestartBiddingConfirm = false);
        ToggleScoringExplanationCommand = new RelayCommand(() => ShowScoringExplanation = !ShowScoringExplanation);
        BackBiddingStepCommand = new RelayCommand(BackBiddingStep);
        SelectContractCommand = new RelayCommand<ContractOption>(SelectContract);
        BackCommand = new RelayCommand(BackForCurrentPhase, CanBackForCurrentPhase);
        OpenFanTanScoringEditorCommand = new RelayCommand(OpenFanTanScoringEditor);
        SaveFanTanScoringCommand = new RelayCommand(SaveFanTanScoring);
        DiscardFanTanScoringCommand = new RelayCommand(DiscardFanTanScoring);
        
        // Initialize all contract options (excluding RavageCity/ChinesePoker initially, will be added by RefreshContractOptions if enabled)
        foreach (var contractType in Enum.GetValues<ContractType>().Where(c => c != ContractType.RavageCity && c != ContractType.ChinesePoker))
        {
            AllContractOptions.Add(new ContractOption { Type = contractType });
        }
        
        // Load saved settings
        LoadSettings();
    }
    
    private void RefreshContractOptions()
    {
        // Add or remove RavageCity option based on setting
        var hasRavageCity = AllContractOptions.Any(o => o.Type == ContractType.RavageCity);
        
        if (RavageCityEnabled && !hasRavageCity)
        {
            AllContractOptions.Add(new ContractOption { Type = ContractType.RavageCity });
        }
        else if (!RavageCityEnabled && hasRavageCity)
        {
            var toRemove = AllContractOptions.FirstOrDefault(o => o.Type == ContractType.RavageCity);
            if (toRemove != null)
                AllContractOptions.Remove(toRemove);
        }
        
        // Add or remove ChinesePoker option based on setting
        var hasChinesePoker = AllContractOptions.Any(o => o.Type == ContractType.ChinesePoker);
        
        if (ChinesePokerEnabled && !hasChinesePoker)
        {
            AllContractOptions.Add(new ContractOption { Type = ContractType.ChinesePoker });
        }
        else if (!ChinesePokerEnabled && hasChinesePoker)
        {
            var toRemove = AllContractOptions.FirstOrDefault(o => o.Type == ContractType.ChinesePoker);
            if (toRemove != null)
                AllContractOptions.Remove(toRemove);
        }
        
        // Refresh all option descriptions (scoring changes with Ravage City/Chinese Poker mode)
        foreach (var option in AllContractOptions)
        {
            option.RefreshName();
        }
    }
    
    private void LoadSettings()
    {
        var settings = AppSettings.Load();
        _selectedTextSize = settings.TextSize;
        _dealerAllowedToDouble = settings.DealerAllowedToDouble;
        _gameMode = settings.GameMode;
        _barbuVersion = settings.BarbuVersion;
        _ravageCityEnabled = settings.RavageCityEnabled;
        _chinesePokerEnabled = settings.ChinesePokerEnabled;
        _fanTanScore1st = settings.FanTanScore1st;
        _fanTanScore2nd = settings.FanTanScore2nd;
        _fanTanScore3rd = settings.FanTanScore3rd;
        _fanTanScore4th = settings.FanTanScore4th;
        Contract.CurrentVersion = _barbuVersion;
        Contract.RavageCityModeEnabled = _ravageCityEnabled;
        Contract.ChinesePokerModeEnabled = _chinesePokerEnabled;
        Contract.FanTanScore1st = _fanTanScore1st;
        Contract.FanTanScore2nd = _fanTanScore2nd;
        Contract.FanTanScore3rd = _fanTanScore3rd;
        Contract.FanTanScore4th = _fanTanScore4th;
        OnPropertyChanged(nameof(SelectedTextSize));
        OnPropertyChanged(nameof(DealerAllowedToDouble));
        OnPropertyChanged(nameof(RavageCityEnabled));
        OnPropertyChanged(nameof(ChinesePokerEnabled));
        OnPropertyChanged(nameof(FanTanScore1st));
        OnPropertyChanged(nameof(FanTanScore2nd));
        OnPropertyChanged(nameof(FanTanScore3rd));
        OnPropertyChanged(nameof(FanTanScore4th));
        OnPropertyChanged(nameof(FanTanScoringDisplay));
        OnPropertyChanged(nameof(GameMode));
        OnPropertyChanged(nameof(BarbuVersion));
        OnPropertyChanged(nameof(IsStandardMode));
        OnPropertyChanged(nameof(IsSaladeMode));
        OnPropertyChanged(nameof(NotSaladeMode));
        OnPropertyChanged(nameof(IsClassicNames));
        OnPropertyChanged(nameof(IsModernNames));
        OnPropertyChanged(nameof(BaseFontSize));
        OnPropertyChanged(nameof(SmallFontSize));
        OnPropertyChanged(nameof(MediumFontSize));
        OnPropertyChanged(nameof(LargeFontSize));
        OnPropertyChanged(nameof(XLargeFontSize));
        OnPropertyChanged(nameof(XXLargeFontSize));
        OnPropertyChanged(nameof(HugeFontSize));
        OnPropertyChanged(nameof(TitleFontSize));
        OnPropertyChanged(nameof(CircleSize));
        OnPropertyChanged(nameof(CircleCornerRadius));
        OnPropertyChanged(nameof(CircleFontSize));
        OnPropertyChanged(nameof(DiamondOuterSize));
        OnPropertyChanged(nameof(DiamondInnerSize));
        OnPropertyChanged(nameof(DiamondFontSize));
        // Refresh contract options (adds/removes RavageCity and updates descriptions)
        RefreshContractOptions();
    }
    
    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            TextSize = SelectedTextSize,
            DealerAllowedToDouble = DealerAllowedToDouble,
            GameMode = GameMode,
            BarbuVersion = BarbuVersion,
            RavageCityEnabled = RavageCityEnabled,
            ChinesePokerEnabled = ChinesePokerEnabled,
            FanTanScore1st = FanTanScore1st,
            FanTanScore2nd = FanTanScore2nd,
            FanTanScore3rd = FanTanScore3rd,
            FanTanScore4th = FanTanScore4th
        };
        settings.Save();
    }

    // Property for bidding back button visibility
    public bool CanGoBackInBidding => BiddingState.CanGoBack || (CurrentPhase == GamePhase.Bidding && !BiddingState.CanGoBack);

    // Actions for file dialogs (set by View)
    public Action<Action<string?>>? RequestSaveFilePath { get; set; }
    public Action<Action<string?>>? RequestLoadFilePath { get; set; }

    private void SaveGame()
    {
        IsSettingsMenuOpen = false;
        
        // Auto-save to the current game file if it exists, otherwise create one
        if (string.IsNullOrEmpty(_autoSaveFilePath))
        {
            InitializeAutoSave();
        }
        
        if (string.IsNullOrEmpty(_autoSaveFilePath))
        {
            ErrorMessage = "Cannot save: No game name set.";
            return;
        }
        
        try
        {
            var saveData = CreateSaveData();
            var json = GameSaveData.Serialize(saveData);
            System.IO.File.WriteAllText(_autoSaveFilePath, json);
            ShowSaveConfirmation = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
    }

    private void LoadGame()
    {
        IsSettingsMenuOpen = false;
        
        // Scan for saved games in app directory
        SavedGames.Clear();
        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var saveFiles = System.IO.Directory.GetFiles(appDir, "*.barbu");
            
            foreach (var filePath in saveFiles)
            {
                try
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    var saveData = GameSaveData.Deserialize(json);
                    if (saveData != null)
                    {
                        SavedGames.Add(new SavedGameInfo
                        {
                            FilePath = filePath,
                            GameName = saveData.GameName,
                            CurrentHandNumber = saveData.CurrentHandNumber,
                            SavedAt = saveData.SavedAt
                        });
                    }
                }
                catch
                {
                    // Skip corrupted files
                }
            }
            
            // Sort by most recently saved
            var sorted = SavedGames.OrderByDescending(g => g.SavedAt).ToList();
            SavedGames.Clear();
            foreach (var game in sorted)
            {
                SavedGames.Add(game);
            }
        }
        catch
        {
            // Failed to scan directory
        }
        
        if (SavedGames.Count == 0)
        {
            ErrorMessage = "No saved games found.";
            return;
        }
        
        ShowLoadGamePicker = true;
    }

    private void LoadSelectedGame(SavedGameInfo? savedGame)
    {
        if (savedGame == null) return;
        
        ShowLoadGamePicker = false;
        
        try
        {
            var json = System.IO.File.ReadAllText(savedGame.FilePath);
            var saveData = GameSaveData.Deserialize(json);
            if (saveData != null)
            {
                RestoreFromSaveData(saveData);
                StatusMessage = "Game loaded successfully!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
    }

    private void ToggleSaveDeleteMenu(SavedGameInfo? savedGame)
    {
        if (savedGame == null) return;
        
        // Close any other open delete menus
        foreach (var game in SavedGames)
        {
            if (game != savedGame)
                game.IsShowingDeleteOption = false;
        }
        
        // Toggle this one
        savedGame.IsShowingDeleteOption = !savedGame.IsShowingDeleteOption;
    }

    private void DeleteSavedGame(SavedGameInfo? savedGame)
    {
        if (savedGame == null) return;
        
        try
        {
            if (System.IO.File.Exists(savedGame.FilePath))
            {
                System.IO.File.Delete(savedGame.FilePath);
            }
            SavedGames.Remove(savedGame);
            
            if (SavedGames.Count == 0)
            {
                ShowLoadGamePicker = false;
                StatusMessage = "All saved games deleted.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message}";
        }
    }

    private GameSaveData CreateSaveData()
    {
        var data = new GameSaveData
        {
            GameName = GameName,
            CurrentDealerIndex = CurrentDealer?.Index ?? 0,
            CurrentHandNumber = _currentHandNumber,
            Phase = CurrentPhase.ToString(),
            SelectedContract = SelectedContract?.ToString(),
            SavedAt = DateTime.Now,
            // Save settings with game
            TextSize = SelectedTextSize.ToString(),
            DealerAllowedToDouble = DealerAllowedToDouble,
            GameMode = GameMode.ToString(),
            BarbuVersion = BarbuVersion.ToString(),
            RavageCityEnabled = RavageCityEnabled,
            ChinesePokerEnabled = ChinesePokerEnabled,
            FanTanScore1st = FanTanScore1st,
            FanTanScore2nd = FanTanScore2nd,
            FanTanScore3rd = FanTanScore3rd,
            FanTanScore4th = FanTanScore4th
        };

        // Save current doubles if in bidding or entering scores phase
        foreach (var d in BiddingState.Doubles)
        {
            data.CurrentDoubles.Add(new DoubleBidSaveData
            {
                DoublerIndex = d.Doubler.Index,
                TargetIndex = d.Target.Index,
                IsRedoubled = d.IsRedoubled
            });
        }

        foreach (var player in Players)
        {
            data.Players.Add(new PlayerSaveData
            {
                Index = player.Index,
                Name = player.Name,
                Position = player.Position.ToString(),
                TotalScore = player.TotalScore,
                DealtContracts = player.DealtContracts.Select(c => c.ToString()).ToList()
            });
        }

        foreach (var hand in HandHistory)
        {
            var handData = new HandResultSaveData
            {
                HandNumber = hand.HandNumber,
                Contract = hand.Contract.ToString(),
                DealerIndex = hand.Dealer.Index,
                Scores = hand.PlayerScores.ToList()
            };
            foreach (var d in hand.Doubles)
            {
                handData.Doubles.Add(new DoubleBidSaveData
                {
                    DoublerIndex = d.Doubler.Index,
                    TargetIndex = d.Target.Index,
                    IsRedoubled = d.IsRedoubled
                });
            }
            data.HandHistory.Add(handData);
        }

        return data;
    }

    private void AutoSave()
    {
        if (string.IsNullOrEmpty(_autoSaveFilePath)) return;
        
        try
        {
            var saveData = CreateSaveData();
            var json = GameSaveData.Serialize(saveData);
            System.IO.File.WriteAllText(_autoSaveFilePath, json);
        }
        catch
        {
            // Silent fail for auto-save
        }
    }

    private void InitializeAutoSave()
    {
        if (string.IsNullOrWhiteSpace(GameName)) return;
        
        try
        {
            // Get the app directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Sanitize the game name for use as a filename
            var sanitizedName = string.Join("_", GameName.Split(System.IO.Path.GetInvalidFileNameChars()));
            
            _autoSaveFilePath = System.IO.Path.Combine(appDir, $"{sanitizedName}.barbu");
            
            // Create initial save file
            AutoSave();
        }
        catch
        {
            _autoSaveFilePath = null;
        }
    }

    private void RestoreFromSaveData(GameSaveData data)
    {
        // Restore game name
        GameName = data.GameName ?? "";
        
        // Set player names from save data
        foreach (var pd in data.Players)
        {
            switch (pd.Position)
            {
                case "West": WestName = pd.Name; break;
                case "North": NorthName = pd.Name; break;
                case "East": EastName = pd.Name; break;
                case "South": SouthName = pd.Name; break;
            }
        }

        // Initialize players
        Players.Clear();
        foreach (var pd in data.Players)
        {
            var player = new Player
            {
                Index = pd.Index,
                Name = pd.Name,
                Position = Enum.Parse<Position>(pd.Position),
                TotalScore = pd.TotalScore,
                DealtContracts = pd.DealtContracts.Select(c => Enum.Parse<ContractType>(c)).ToList()
            };
            Players.Add(player);
        }

        // Restore hand history
        HandHistory.Clear();
        foreach (var hd in data.HandHistory)
        {
            var dealer = Players.First(p => p.Index == hd.DealerIndex);
            var hand = new HandResult
            {
                HandNumber = hd.HandNumber,
                Contract = Enum.Parse<ContractType>(hd.Contract),
                Dealer = dealer,
                PlayerScores = hd.Scores.ToArray()
            };
            foreach (var dd in hd.Doubles)
            {
                hand.Doubles.Add(new DoubleBid
                {
                    Doubler = Players.First(p => p.Index == dd.DoublerIndex),
                    Target = Players.First(p => p.Index == dd.TargetIndex),
                    IsRedoubled = dd.IsRedoubled
                });
            }
            HandHistory.Add(hand);
        }

        // Restore game state
        CurrentDealer = Players.FirstOrDefault(p => p.Index == data.CurrentDealerIndex);
        _currentHandNumber = data.CurrentHandNumber;
        CurrentPhase = Enum.Parse<GamePhase>(data.Phase);
        CurrentScreen = GameScreen.GamePlay;

        // Restore settings from saved game
        if (Enum.TryParse<TextSize>(data.TextSize, out var textSize))
            _selectedTextSize = textSize;
        _dealerAllowedToDouble = data.DealerAllowedToDouble;
        if (Enum.TryParse<GameMode>(data.GameMode, out var gameMode))
            _gameMode = gameMode;
        if (Enum.TryParse<BarbuVersion>(data.BarbuVersion, out var barbuVersion))
            _barbuVersion = barbuVersion;
        _ravageCityEnabled = data.RavageCityEnabled;
        _chinesePokerEnabled = data.ChinesePokerEnabled;
        _fanTanScore1st = data.FanTanScore1st;
        _fanTanScore2nd = data.FanTanScore2nd;
        _fanTanScore3rd = data.FanTanScore3rd;
        _fanTanScore4th = data.FanTanScore4th;
        
        // Sync static Contract settings
        Contract.CurrentVersion = _barbuVersion;
        Contract.RavageCityModeEnabled = _ravageCityEnabled;
        Contract.ChinesePokerModeEnabled = _chinesePokerEnabled;
        Contract.FanTanScore1st = _fanTanScore1st;
        Contract.FanTanScore2nd = _fanTanScore2nd;
        Contract.FanTanScore3rd = _fanTanScore3rd;
        Contract.FanTanScore4th = _fanTanScore4th;
        
        // Notify UI of settings changes
        OnPropertyChanged(nameof(SelectedTextSize));
        OnPropertyChanged(nameof(DealerAllowedToDouble));
        OnPropertyChanged(nameof(RavageCityEnabled));
        OnPropertyChanged(nameof(ChinesePokerEnabled));
        OnPropertyChanged(nameof(FanTanScore1st));
        OnPropertyChanged(nameof(FanTanScore2nd));
        OnPropertyChanged(nameof(FanTanScore3rd));
        OnPropertyChanged(nameof(FanTanScore4th));
        OnPropertyChanged(nameof(FanTanScoringDisplay));
        OnPropertyChanged(nameof(GameMode));
        OnPropertyChanged(nameof(BarbuVersion));
        OnPropertyChanged(nameof(IsStandardMode));
        OnPropertyChanged(nameof(IsSaladeMode));
        OnPropertyChanged(nameof(NotSaladeMode));
        OnPropertyChanged(nameof(IsClassicNames));
        OnPropertyChanged(nameof(IsModernNames));
        OnPropertyChanged(nameof(BaseFontSize));
        OnPropertyChanged(nameof(SmallFontSize));
        OnPropertyChanged(nameof(MediumFontSize));
        OnPropertyChanged(nameof(LargeFontSize));
        OnPropertyChanged(nameof(XLargeFontSize));
        OnPropertyChanged(nameof(XXLargeFontSize));
        OnPropertyChanged(nameof(HugeFontSize));

        // Rebuild scorecard and dealer double matrix
        InitializeScorecard();
        InitializeDealerDoubleMatrix();
        
        // Replay history to rebuild scorecard and matrix
        foreach (var hand in HandHistory)
        {
            UpdateScorecardFromHand(hand);
            UpdateDealerDoubleMatrixFromHand(hand);
        }

        // Update available contracts for current dealer
        UpdateAvailableContracts();
        
        // Restore selected contract if saved
        if (!string.IsNullOrEmpty(data.SelectedContract) && Enum.TryParse<ContractType>(data.SelectedContract, out var contract))
        {
            SelectedContract = contract;
        }
        
        // Restore current doubles if in bidding or entering scores phase
        BiddingState.Doubles.Clear();
        foreach (var dd in data.CurrentDoubles)
        {
            BiddingState.Doubles.Add(new DoubleBid
            {
                Doubler = Players.First(p => p.Index == dd.DoublerIndex),
                Target = Players.First(p => p.Index == dd.TargetIndex),
                IsRedoubled = dd.IsRedoubled
            });
        }
        BiddingState.NotifyDoublesChanged();
        
        // If we're in EnteringScores phase, update scorecard with doubles
        if (CurrentPhase == GamePhase.EnteringScores && SelectedContract.HasValue)
        {
            UpdateScorecardDoublesOnly();
        }
        
        // Save settings so they persist
        SaveSettings();
        
        // Setup auto-save for loaded game
        InitializeAutoSave();
    }

    private void UpdateScorecardFromHand(HandResult hand)
    {
        var section = Scorecard.FirstOrDefault(s => s.Dealer.Index == hand.Dealer.Index);
        if (section == null) return;
        
        var row = section.Rows.FirstOrDefault(r => r.Contract == hand.Contract);
        if (row == null) return;

        for (int i = 0; i < 4; i++)
        {
            var player = Players[i];
            var cell = row.PlayerCells[i];
            
            cell.Score = hand.PlayerScores[i];
            
            // Find doubles made by this player (not redoubled) - use Index for comparison after load
            var doubles = hand.Doubles
                .Where(d => d.Doubler.Index == player.Index && !d.IsRedoubled)
                .Select(d => d.Target.Position.ToInitial())
                .ToList();
            cell.DoubledTargets = doubles;
            
            // Find redoubles made by this player
            var redoubles = hand.Doubles
                .Where(d => d.Doubler.Index == player.Index && d.IsRedoubled)
                .Select(d => d.Target.Position.ToInitial())
                .ToList();
            cell.RedoubledTargets = redoubles;
        }
        row.HasBiddingComplete = true;
        row.IsPlayed = true;
    }

    private void UpdateDealerDoubleMatrixFromHand(HandResult hand)
    {
        foreach (var doubleBid in hand.Doubles)
        {
            if (doubleBid.Target.Index == hand.Dealer.Index)
            {
                var row = DealerDoubleMatrix.FirstOrDefault(r => r.Doubler.Index == doubleBid.Doubler.Index);
                if (row != null)
                {
                    var count = row.DoubleCounts.FirstOrDefault(c => c.Dealer.Index == hand.Dealer.Index);
                    if (count != null)
                    {
                        count.Count++;
                    }
                }
            }
        }
    }

    private void OpenGameSettings()
    {
        IsSettingsMenuOpen = false;
        SelectedSettingsTab = 0;
        IsSettingsOpen = true;
    }

    private void StartEditScore(ScorecardRow? row)
    {
        if (row == null || !row.IsPlayed) return;
        
        RowBeingEdited = row;
        EditErrorMessage = null;
        
        // Pre-populate edit inputs with current scores
        for (int i = 0; i < 4; i++)
        {
            EditInputs[i] = row.PlayerCells[i].Score ?? 0;
        }
        
        IsEditingScore = true;
    }

    private void ConfirmEditScore()
    {
        if (RowBeingEdited == null) return;
        
        // Find the matching hand in history
        var hand = HandHistory.FirstOrDefault(h => 
            h.Dealer == RowBeingEdited.Dealer && 
            h.Contract == RowBeingEdited.Contract);
        
        if (hand == null)
        {
            EditErrorMessage = "Could not find matching hand in history.";
            return;
        }
        
        // Get expected checksum for this contract
        int expectedSum = RowBeingEdited.Contract switch
        {
            ContractType.Nullo => -26,
            ContractType.NoQueens => -24,
            ContractType.Hearts => -30,
            ContractType.NoLastTwo => -30,
            ContractType.Barbu => -20,
            ContractType.Trumps => 65,
            ContractType.FanTan => 65,
            _ => 0
        };
        
        // Validate the sum of entered scores
        int actualSum = EditInputs.Sum();
        if (actualSum != expectedSum)
        {
            EditErrorMessage = $"Scores must sum to {expectedSum}. Currently: {actualSum}";
            return;
        }
        
        // Calculate old total score for each player (to adjust)
        int[] oldScores = hand.PlayerScores.ToArray();
        
        // Use EditInputs directly as final scores (user enters the final adjusted values)
        int[] newFinalScores = EditInputs.ToArray();
        
        // Update player totals
        for (int i = 0; i < 4; i++)
        {
            Players[i].TotalScore -= oldScores[i];
            Players[i].TotalScore += newFinalScores[i];
        }
        
        // Update hand history
        hand.PlayerScores = newFinalScores;
        hand.RawInputs = EditInputs.ToArray();
        
        // Update scorecard display
        for (int i = 0; i < 4; i++)
        {
            RowBeingEdited.PlayerCells[i].Score = newFinalScores[i];
        }
        
        // Clear error and close edit mode
        EditErrorMessage = null;
        IsEditingScore = false;
        RowBeingEdited = null;
        StatusMessage = "Score updated successfully!";
        
        // Auto-save after manual score edit
        AutoSave();
    }

    private void StartGame()
    {
        // Initialize players with positions
        Players.Clear();
        Players.Add(new Player { Index = 0, Position = Position.West, Name = WestName });
        Players.Add(new Player { Index = 1, Position = Position.North, Name = NorthName });
        Players.Add(new Player { Index = 2, Position = Position.East, Name = EastName });
        Players.Add(new Player { Index = 3, Position = Position.South, Name = SouthName });

        // Initialize dealer doubles matrix
        InitializeDealerDoubleMatrix();
        
        // Initialize scorecard for history tab
        InitializeScorecard();

        CurrentHandNumber = 1;
        CurrentDealer = Players[0]; // West starts
        CurrentScreen = GameScreen.GamePlay;
        CurrentPhase = GamePhase.SelectingContract;
        StatusMessage = $"Round {CurrentHandNumber}: {CurrentDealer.Name}'s turn to choose a game";
        
        // Initialize auto-save with game name
        InitializeAutoSave();
    }

    private void InitializeDealerDoubleMatrix()
    {
        DealerDoubleMatrix.Clear();
        
        // For each player (as doubler), create a row with counts for each other player (as dealer)
        foreach (var doubler in Players)
        {
            var row = new DealerDoubleRow { Doubler = doubler };
            foreach (var dealer in Players)
            {
                row.DoubleCounts.Add(new DealerDoubleCount
                {
                    Doubler = doubler,
                    Dealer = dealer,
                    Count = 0,
                    IsSelf = dealer == doubler
                });
            }
            DealerDoubleMatrix.Add(row);
        }
    }

    private void UpdateDealerDoubleMatrix(Player dealer, List<DoubleBid> doubles)
    {
        // For each player who doubled the DEALER this round, increment their count once
        // (we only care about doubles where the target is the dealer)
        var playersWhoDoubledDealer = doubles
            .Where(d => d.Target == dealer)  // Only count doubles targeting the dealer
            .Select(d => d.Doubler)
            .Distinct()  // Each player only gets counted once per game
            .ToList();
        
        foreach (var doubler in playersWhoDoubledDealer)
        {
            var row = DealerDoubleMatrix.FirstOrDefault(r => r.Doubler == doubler);
            if (row != null)
            {
                var count = row.DoubleCounts.FirstOrDefault(c => c.Dealer == dealer);
                if (count != null)
                {
                    count.Count++;
                }
            }
        }
        OnPropertyChanged(nameof(DealerDoubleMatrix));
    }

    private void InitializeScorecard()
    {
        Scorecard.Clear();
        
        var contractTypes = new List<ContractType> 
        { 
            ContractType.Nullo, 
            ContractType.NoQueens, 
            ContractType.Hearts, 
            ContractType.NoLastTwo, 
            ContractType.Barbu, 
            ContractType.Trumps, 
            ContractType.FanTan 
        };
        
        // Add RavageCity when enabled
        if (RavageCityEnabled)
        {
            contractTypes.Add(ContractType.RavageCity);
        }
        
        // Add ChinesePoker when enabled
        if (ChinesePokerEnabled)
        {
            contractTypes.Add(ContractType.ChinesePoker);
        }
        
        // Create a section for each dealer (in position order)
        foreach (var dealer in Players)
        {
            var section = new DealerSection { Dealer = dealer };
            
            foreach (var contract in contractTypes)
            {
                var row = new ScorecardRow 
                { 
                    Contract = contract,
                    GameName = Contract.GetDisplayName(contract),
                    Dealer = dealer
                };
                // PlayerCells are auto-initialized by constructor
                
                section.Rows.Add(row);
            }
            
            Scorecard.Add(section);
        }
    }

    private void UpdateScorecard(HandResult result)
    {
        // Find the dealer's section
        var section = Scorecard.FirstOrDefault(s => s.Dealer == result.Dealer);
        if (section == null) return;
        
        // Find the row for this contract
        var row = section.Rows.FirstOrDefault(r => r.Contract == result.Contract);
        if (row == null) return;
        
        // Update each player's cell with their score and doubles info
        for (int i = 0; i < Players.Count && i < row.PlayerCells.Length; i++)
        {
            var player = Players[i];
            var cell = row.PlayerCells[i];
            
            cell.Score = result.PlayerScores[i];
            
            // Find doubles made by this player (not redoubled)
            var doubles = result.Doubles
                .Where(d => d.Doubler == player && !d.IsRedoubled)
                .Select(d => d.Target.Position.ToInitial())
                .ToList();
            cell.DoubledTargets = doubles;
            
            // Find redoubles made by this player
            var redoubles = result.Doubles
                .Where(d => d.Doubler == player && d.IsRedoubled)
                .Select(d => d.Target.Position.ToInitial())
                .ToList();
            cell.RedoubledTargets = redoubles;
        }
        
        row.IsPlayed = true;
    }

    private void RefreshScorecardNames()
    {
        // Update all scorecard row names based on current version
        foreach (var section in Scorecard)
        {
            foreach (var row in section.Rows)
            {
                row.GameName = Contract.GetDisplayName(row.Contract);
            }
        }
    }

    private void UpdateScorecardDoublesOnly()
    {
        if (CurrentDealer == null || !SelectedContract.HasValue) return;

        // Find the dealer's section - use Index for comparison after load
        var section = Scorecard.FirstOrDefault(s => s.Dealer.Index == CurrentDealer.Index);
        if (section == null) return;
        
        // Find the row for this contract
        var row = section.Rows.FirstOrDefault(r => r.Contract == SelectedContract.Value);
        if (row == null) return;
        
        // Update each player's cell with doubles info only (no scores yet)
        for (int i = 0; i < Players.Count && i < row.PlayerCells.Length; i++)
        {
            var player = Players[i];
            var cell = row.PlayerCells[i];
            
            // Find doubles made by this player (not redoubled) - use Index for comparison
            var doubles = BiddingState.Doubles
                .Where(d => d.Doubler.Index == player.Index && !d.IsRedoubled)
                .Select(d => d.Target.Position.ToInitial())
                .ToList();
            cell.DoubledTargets = doubles;
            
            // Find redoubles made by this player
            var redoubles = BiddingState.Doubles
                .Where(d => d.Doubler.Index == player.Index && d.IsRedoubled)
                .Select(d => d.Target.Position.ToInitial())
                .ToList();
            cell.RedoubledTargets = redoubles;
        }
        
        row.HasBiddingComplete = true;
    }

    /// <summary>
    /// Clears any doubles/redoubles previously written to the scorecard row
    /// for the currently selected contract (used when the user backs out of
    /// bidding and chooses a different contract).
    /// </summary>
    private void ClearScorecardDoublesForCurrentContract()
    {
        if (CurrentDealer == null || !SelectedContract.HasValue) return;
        var section = Scorecard.FirstOrDefault(s => s.Dealer.Index == CurrentDealer.Index);
        if (section == null) return;
        var row = section.Rows.FirstOrDefault(r => r.Contract == SelectedContract.Value);
        if (row == null) return;
        foreach (var cell in row.PlayerCells)
        {
            cell.DoubledTargets = new List<string>();
            cell.RedoubledTargets = new List<string>();
        }
        row.HasBiddingComplete = false;
    }

    private void StartBidding()
    {
        if (!SelectedContract.HasValue || CurrentDealer == null) return;

        BiddingState.Reset();
        CurrentPhase = GamePhase.Bidding;
        IsInRedoublePhase = false;
        IsWaitingForImmediateRedouble = false;
        CurrentBiddingPlayer = null;

        BuildDoublingMatrix();
        StatusMessage = "Bidding: select doubles and redoubles, then confirm.";
    }

    /// <summary>
    /// Builds the single-view doubling matrix. Rows are doublers, columns are targets.
    /// Diagonal cells are empty. Mandatory doubles (non-dealer must double dealer twice
    /// across the dealer's 7 contracts) are pre-selected. If the dealer is not allowed
    /// to initiate doubles, the dealer's row is disabled.
    /// </summary>
    private void BuildDoublingMatrix()
    {
        DoublingMatrix.Clear();
        DoublingMatrixHeaders.Clear();
        if (CurrentDealer == null || Players.Count < 4) return;

        // Order players clockwise starting from the player after the dealer.
        // Top-to-bottom (and left-to-right) follows the clockwise turn order.
        var ordered = new List<Player>();
        var pos = CurrentDealer.Position.NextClockwise();
        for (int i = 0; i < 4; i++)
        {
            ordered.Add(GetPlayerByPosition(pos));
            pos = pos.NextClockwise();
        }

        foreach (var p in ordered)
        {
            DoublingMatrixHeaders.Add(new DoublingMatrixHeader { Player = p });
        }

        var dealer = CurrentDealer;

        foreach (var doubler in ordered)
        {
            var row = new DoublingMatrixRow { Doubler = doubler };

            foreach (var target in ordered)
            {
                var cell = new DoublingMatrixCell { Doubler = doubler, Target = target };

                if (cell.IsEmpty)
                {
                    cell.IsDoubleEnabled = false;
                    row.Cells.Add(cell);
                    continue;
                }

                // Mandatory: non-dealer doubling the dealer when remaining games == doubles still needed.
                bool isDealerRow = doubler.Index == dealer.Index;
                if (!isDealerRow && target.Index == dealer.Index)
                {
                    int currentDoubleCount = GetDoubleCountForDealerPair(doubler, dealer);
                    int doublesNeeded = 2 - currentDoubleCount;
                    int gamesRemainingForDealer = 7 - dealer.DealtContracts.Count;
                    if (gamesRemainingForDealer <= doublesNeeded && doublesNeeded > 0)
                    {
                        cell.IsMandatory = true;
                        cell.IsDouble = true;
                    }
                }

                row.Cells.Add(cell);
            }

            row.MaxCommand = new RelayCommand(() => MaxDoublesForRow(row));
            DoublingMatrix.Add(row);
        }
    }

    private void MaxDoublesForRow(DoublingMatrixRow row)
    {
        if (row == null) return;
        foreach (var cell in row.Cells)
        {
            if (cell.IsEmpty || !cell.IsDoubleEnabled) continue;
            cell.IsDouble = true;
        }
    }

    private void ConfirmBiddingMatrix()
    {
        if (CurrentDealer == null) return;

        // Validate any mandatory cells were not unchecked.
        foreach (var row in DoublingMatrix)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.IsMandatory && !cell.IsDouble)
                {
                    ErrorMessage = $"{cell.Doubler.Name} must double {cell.Target.Name} (mandatory).";
                    return;
                }
            }
        }

        // Translate matrix selections into BiddingState.Doubles in clockwise order.
        // Reassign the underlying list so the bound ItemsControl fully rebuilds (no stale rows).
        var newDoubles = new List<DoubleBid>();
        foreach (var row in DoublingMatrix)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.IsEmpty || !cell.IsDouble) continue;
                newDoubles.Add(new DoubleBid
                {
                    Doubler = cell.Doubler,
                    Target = cell.Target,
                    IsRedoubled = cell.IsRedouble
                });
            }
        }
        BiddingState.Doubles = newDoubles;
        BiddingState.NotifyDoublesChanged();

        // Mark all players as having completed their turn so any legacy logic remains consistent.
        BiddingState.PlayersWhoHaveDoubled.Clear();
        foreach (var p in Players) BiddingState.PlayersWhoHaveDoubled.Add(p);

        ErrorMessage = "";
        CompleteBidding();
    }

    private void SetupDoubleTargetsForCurrentBidder()
    {
        DoubleTargets.Clear();

        if (CurrentBiddingPlayer == null || CurrentDealer == null) return;
        
        // Capture in local variables to avoid null reference warnings
        var bidder = CurrentBiddingPlayer;
        var dealer = CurrentDealer;

        // Check if dealer is bidding and not allowed to double
        bool isDealerBidding = bidder.Index == dealer.Index;
        bool dealerCanOnlyRedouble = isDealerBidding && !DealerAllowedToDouble;

        // Get players in clockwise order starting from the player to the left of the current bidder
        var playersInOrder = GetPlayersClockwiseFromBidder(bidder);

        // Can double any other player, but check if they already doubled this player
        foreach (var player in playersInOrder)
        {
            if (player != bidder)
            {
                var targetInfo = new DoubleTargetInfo 
                { 
                    Player = player,
                    IsDealer = (player.Index == dealer.Index)
                };

                // Check if this player has already doubled the current bidder
                var existingDouble = BiddingState.GetExistingDouble(player, bidder);
                if (existingDouble != null)
                {
                    targetInfo.Status = existingDouble.IsRedoubled 
                        ? DoubleTargetStatus.Redoubled 
                        : DoubleTargetStatus.AlreadyDoubled;
                    // Dealer can always respond to doubles (redouble)
                    DoubleTargets.Add(targetInfo);
                }
                else
                {
                    // If dealer can only redouble, skip targets they haven't been doubled by
                    if (dealerCanOnlyRedouble)
                    {
                        continue; // Dealer can't initiate doubles on players who haven't doubled them
                    }
                    
                    targetInfo.Status = DoubleTargetStatus.Available;
                    
                    // Check if doubling is mandatory (for dealer only)
                    // Each player must double the dealer twice by end of game
                    if (player.Index == dealer.Index)
                    {
                        int currentDoubleCount = GetDoubleCountForDealerPair(bidder, dealer);
                        int doublesNeeded = 2 - currentDoubleCount;
                        int gamesRemainingForDealer = 7 - dealer.DealtContracts.Count;  // Current game not yet added
                        
                        // If remaining games equals doubles needed, this one is mandatory
                        if (gamesRemainingForDealer <= doublesNeeded && doublesNeeded > 0)
                        {
                            targetInfo.IsMandatory = true;
                            targetInfo.IsSelected = true;  // Auto-select mandatory doubles
                        }
                    }
                    
                    DoubleTargets.Add(targetInfo);
                }
            }
        }
    }

    private List<Player> GetPlayersClockwiseFromBidder(Player bidder)
    {
        // Return players in clockwise order starting from the player to the bidder's left
        var result = new List<Player>();
        var startPosition = bidder.Position.NextClockwise();
        
        for (int i = 0; i < 3; i++)
        {
            var position = (Position)(((int)startPosition + i) % 4);
            var player = Players.FirstOrDefault(p => p.Position == position);
            if (player != null)
            {
                result.Add(player);
            }
        }
        
        return result;
    }

    private int GetDoubleCountForDealerPair(Player doubler, Player dealer)
    {
        // Find the double count from the matrix - use Index for comparison
        var row = DealerDoubleMatrix.FirstOrDefault(r => r.Doubler.Index == doubler.Index);
        if (row == null) return 0;
        var count = row.DoubleCounts.FirstOrDefault(c => c.Dealer.Index == dealer.Index);
        return count?.Count ?? 0;
    }

    private void ToggleDoubleTarget(DoubleTargetInfo? targetInfo)
    {
        if (targetInfo == null || !targetInfo.IsClickable) return;

        targetInfo.IsSelected = !targetInfo.IsSelected;
    }

    private void ConfirmDoubles()
    {
        if (CurrentBiddingPlayer == null) return;

        // Save state before proceeding (for back button)
        BiddingState.SaveState(CurrentBiddingPlayer);

        // Get all selected targets
        var selectedTargets = DoubleTargets.Where(t => t.IsSelected && t.IsAvailable).ToList();

        // Create doubles and queue redouble responses
        foreach (var targetInfo in selectedTargets)
        {
            var doubleBid = new DoubleBid
            {
                Doubler = CurrentBiddingPlayer,
                Target = targetInfo.Player,
                IsRedoubled = false
            };
            BiddingState.Doubles.Add(doubleBid);
            
            // Always queue redouble response - anyone who is doubled can redouble
            // (DealerAllowedToDouble only affects INITIATING doubles, not responding)
            BiddingState.PendingRedoubleResponses.Enqueue(doubleBid);
        }
        BiddingState.NotifyDoublesChanged();

        BiddingState.PlayersWhoHaveDoubled.Add(CurrentBiddingPlayer);

        OnPropertyChanged(nameof(CanGoBackInBidding));
        
        // Process immediate redouble responses before moving to next bidder
        ProcessNextRedoubleResponse();
    }

    private void SkipDouble()
    {
        if (CurrentBiddingPlayer == null) return;
        
        // Check for mandatory doubles that weren't selected
        var mandatoryNotSelected = DoubleTargets.Where(t => t.IsMandatory && !t.IsSelected).ToList();
        if (mandatoryNotSelected.Any())
        {
            ErrorMessage = $"You must double {mandatoryNotSelected.First().Player.Name} (mandatory double).";
            return;
        }
        
        // Save state before proceeding (for back button)
        BiddingState.SaveState(CurrentBiddingPlayer);
        
        BiddingState.PlayersWhoHaveDoubled.Add(CurrentBiddingPlayer);
        
        OnPropertyChanged(nameof(CanGoBackInBidding));
        ProcessNextRedoubleResponse();
    }

    private void MaxDoubles()
    {
        // Select all available targets that can be doubled
        foreach (var target in DoubleTargets)
        {
            // Select if available (not already doubled by them, not redoubled)
            // IsAvailable means Status == Available, which means they haven't doubled the current bidder
            if (target.IsAvailable)
            {
                target.IsSelected = true;
            }
        }
    }

    private void BackBiddingStep()
    {
        // If no history, go back to contract selection
        if (!BiddingState.CanGoBack)
        {
            // Clear any doubles that were already written to the scorecard for the
            // currently-selected contract — otherwise switching to a different game
            // leaves stale doubles on the abandoned row.
            ClearScorecardDoublesForCurrentContract();
            BiddingState.Reset();
            SelectedContract = null;
            foreach (var opt in AllContractOptions)
            {
                opt.IsSelected = false;
            }
            CurrentPhase = GamePhase.SelectingContract;
            StatusMessage = $"Round {CurrentHandNumber}: {CurrentDealer?.Name}'s turn to choose a game";
            OnPropertyChanged(nameof(CanGoBackInBidding));
            return;
        }

        // Pop the last state
        var previousState = BiddingState.PopState();
        if (previousState == null) return;

        // Restore the doubles and who has bid
        BiddingState.Doubles.Clear();
        BiddingState.Doubles.AddRange(previousState.DoublesCopy);
        BiddingState.NotifyDoublesChanged();
        
        BiddingState.PlayersWhoHaveDoubled.Clear();
        BiddingState.PlayersWhoHaveDoubled.AddRange(previousState.PlayersWhoHaveBidCopy);
        
        // Clear pending redoubles
        BiddingState.PendingRedoubleResponses.Clear();
        
        // Return to that bidder's turn
        CurrentBiddingPlayer = previousState.Bidder;
        IsWaitingForImmediateRedouble = false;
        CurrentPendingRedouble = null;

        SetupDoubleTargetsForCurrentBidder();
        StatusMessage = $"Bidding: {CurrentBiddingPlayer.Name}'s turn to double";
        
        OnPropertyChanged(nameof(CanGoBackInBidding));
    }

    private void ProcessNextRedoubleResponse()
    {
        if (BiddingState.PendingRedoubleResponses.Count > 0)
        {
            // Show immediate redouble prompt
            CurrentPendingRedouble = BiddingState.PendingRedoubleResponses.Dequeue();
            IsWaitingForImmediateRedouble = true;
            StatusMessage = $"{CurrentPendingRedouble.Target.Name}: {CurrentPendingRedouble.Doubler.Name} doubled you!";
        }
        else
        {
            // No more redoubles pending, move to next bidder
            IsWaitingForImmediateRedouble = false;
            CurrentPendingRedouble = null;
            MoveToNextBidder();
        }
    }

    private void AcceptRedouble()
    {
        if (CurrentPendingRedouble != null)
        {
            CurrentPendingRedouble.IsRedoubled = true;
        }
        ProcessNextRedoubleResponse();
    }

    private void DeclineRedouble()
    {
        // Just move on, don't set redouble
        ProcessNextRedoubleResponse();
    }

    private void MoveToNextBidder()
    {
        if (CurrentBiddingPlayer == null) return;

        // Find next player who hasn't doubled yet
        var nextPosition = CurrentBiddingPlayer.Position.NextClockwise();
        
        for (int i = 0; i < 4; i++)
        {
            var nextPlayer = GetPlayerByPosition(nextPosition);
            
            // Skip dealer entirely if they're not allowed to double
            if (nextPlayer.Index == CurrentDealer?.Index && !DealerAllowedToDouble)
            {
                // Mark dealer as having "doubled" (skipped) so bidding can complete
                if (!BiddingState.PlayersWhoHaveDoubled.Contains(nextPlayer))
                {
                    BiddingState.PlayersWhoHaveDoubled.Add(nextPlayer);
                }
                nextPosition = nextPosition.NextClockwise();
                continue;
            }
            
            if (!BiddingState.PlayersWhoHaveDoubled.Contains(nextPlayer))
            {
                CurrentBiddingPlayer = nextPlayer;
                SetupDoubleTargetsForCurrentBidder();
                StatusMessage = $"Bidding: {CurrentBiddingPlayer.Name}'s turn to double";
                return;
            }
            nextPosition = nextPosition.NextClockwise();
        }

        // All players have doubled, complete bidding
        CompleteBidding();
    }

    private void CompleteBidding()
    {
        IsInRedoublePhase = false;
        IsWaitingForImmediateRedouble = false;
        CurrentBiddingPlayer = null;
        CurrentPendingRedouble = null;
        CurrentPhase = GamePhase.EnteringScores;
        
        StatusMessage = "";
        ErrorMessage = "";
        
        // Initialize Ravage City player selections if applicable
        if (SelectedContract == ContractType.RavageCity)
        {
            RavageCityPlayerSelections.Clear();
            foreach (var player in Players)
            {
                RavageCityPlayerSelections.Add(new RavageCityPlayerSelection { Player = player });
            }
        }
        
        // Update scorecard with doubles info immediately (before score entry)
        UpdateScorecardDoublesOnly();
        
        // Auto-save after bidding complete
        AutoSave();
    }

    private Player GetPlayerByPosition(Position position)
    {
        return Players.First(p => p.Position == position);
    }

    private void UpdateAvailableContracts()
    {
        AvailableContracts.Clear();
        if (CurrentDealer != null)
        {
            foreach (var contractType in CurrentDealer.RemainingContracts)
            {
                AvailableContracts.Add(new Contract { Type = contractType });
            }
            
            // Update AllContractOptions availability
            foreach (var option in AllContractOptions)
            {
                option.IsAvailable = CurrentDealer.RemainingContracts.Contains(option.Type);
                option.IsSelected = false;
            }
        }
    }

    private void SelectContract(ContractOption? option)
    {
        if (option == null || !option.IsAvailable) return;
        
        // Deselect all, select this one
        foreach (var opt in AllContractOptions)
        {
            opt.IsSelected = false;
        }
        option.IsSelected = true;
        
        SelectedContract = option.Type;

        // Single-click: jump straight into bidding.
        StartBidding();
    }

    private void RecordHand()
    {
        if (CurrentDealer == null || !SelectedContract.HasValue) return;

        ErrorMessage = "";

        // Validate scores
        var validationError = ValidateScores(SelectedContract.Value, CurrentInputs);
        if (!string.IsNullOrEmpty(validationError))
        {
            ErrorMessage = validationError;
            return;
        }

        // Calculate scores based on contract type and inputs
        int[] baseScores = CalculateBaseScores(SelectedContract.Value, CurrentInputs);
        
        // Apply doubles and redoubles
        int[] finalScores = ApplyDoubles(baseScores, BiddingState.Doubles);
        
        // Store in summary properties
        for (int i = 0; i < 4; i++)
        {
            SummaryBaseScores[i] = baseScores[i];
            SummaryFinalScores[i] = finalScores[i];
        }
        
        // Build scoring explanation
        BuildScoringExplanation(baseScores, finalScores);
        
        // Reset explanation visibility
        ShowScoringExplanation = false;
        
        // Go to summary phase
        OnPropertyChanged(nameof(SummaryBaseScores));
        OnPropertyChanged(nameof(SummaryFinalScores));
        OnPropertyChanged(nameof(ScoringExplanation));
        CurrentPhase = GamePhase.ScoreSummary;
        StatusMessage = "Review the scores before confirming";
    }

    private void BuildScoringExplanation(int[] baseScores, int[] finalScores)
    {
        ScoringExplanation.Clear();
        
        // First, show base scores for all players
        ScoringExplanation.Add("Base Scores:");
        for (int i = 0; i < 4; i++)
        {
            ScoringExplanation.Add($"  {Players[i].Name}: {baseScores[i]}");
        }
        
        // Then explain each double's effect
        if (BiddingState.Doubles.Count > 0)
        {
            ScoringExplanation.Add("");
            ScoringExplanation.Add("Doubles Applied:");
            
            foreach (var dbl in BiddingState.Doubles)
            {
                int doublerIdx = dbl.Doubler.Index;
                int targetIdx = dbl.Target.Index;
                int diff = baseScores[targetIdx] - baseScores[doublerIdx];
                int multiplier = dbl.IsRedoubled ? 2 : 1;
                int transfer = Math.Abs(diff) * multiplier;
                string doubleType = dbl.IsRedoubled ? "redoubled" : "doubled";
                
                if (diff > 0)
                {
                    // Target scored higher, so target wins the double
                    ScoringExplanation.Add($"  {dbl.Doubler.Name} {doubleType} {dbl.Target.Name}:");
                    ScoringExplanation.Add($"    Diff = {baseScores[targetIdx]} - ({baseScores[doublerIdx]}) = {diff}");
                    if (dbl.IsRedoubled)
                        ScoringExplanation.Add($"    ×2 for redouble = {transfer}");
                    ScoringExplanation.Add($"    {dbl.Target.Name} wins: +{transfer}, {dbl.Doubler.Name} loses: -{transfer}");
                }
                else if (diff < 0)
                {
                    // Doubler scored higher, so doubler wins the double
                    ScoringExplanation.Add($"  {dbl.Doubler.Name} {doubleType} {dbl.Target.Name}:");
                    ScoringExplanation.Add($"    Diff = {baseScores[targetIdx]} - ({baseScores[doublerIdx]}) = {diff}");
                    if (dbl.IsRedoubled)
                        ScoringExplanation.Add($"    ×2 for redouble = {transfer}");
                    ScoringExplanation.Add($"    {dbl.Doubler.Name} wins: +{transfer}, {dbl.Target.Name} loses: -{transfer}");
                }
                else
                {
                    ScoringExplanation.Add($"  {dbl.Doubler.Name} {doubleType} {dbl.Target.Name}: No effect (tied scores)");
                }
            }
        }
        else
        {
            ScoringExplanation.Add("");
            ScoringExplanation.Add("No doubles this round.");
        }
        
        // Show final scores
        ScoringExplanation.Add("");
        ScoringExplanation.Add("Final Scores:");
        for (int i = 0; i < 4; i++)
        {
            ScoringExplanation.Add($"  {Players[i].Name}: {finalScores[i]}");
        }
    }

    private void ConfirmSummary()
    {
        if (CurrentDealer == null || !SelectedContract.HasValue) return;

        var result = new HandResult
        {
            HandNumber = CurrentHandNumber,
            Dealer = CurrentDealer,
            Contract = SelectedContract.Value,
            RawInputs = (int[])CurrentInputs.Clone(),
            Doubles = BiddingState.Doubles.ToList()
        };
        
        result.PlayerScores = SummaryFinalScores.ToArray();

        // Update player totals
        for (int i = 0; i < 4; i++)
        {
            Players[i].TotalScore += SummaryFinalScores[i];
        }

        // Mark contract as dealt by current dealer
        CurrentDealer.DealtContracts.Add(SelectedContract.Value);
        CurrentDealer.NotifyContractsChanged();

        // Update dealer doubles matrix
        UpdateDealerDoubleMatrix(CurrentDealer, BiddingState.Doubles.ToList());

        HandHistory.Add(result);
        
        // Update history scorecard
        UpdateScorecard(result);
        
        // Clear inputs for next hand
        Array.Clear(CurrentInputs);
        BiddingState.Reset();
        
        // Reset special card holders
        AceOfHeartsPlayer = null;
        KingOfHeartsPlayer = null;
        SecondToLastTrickPlayer = null;
        LastTrickPlayer = null;

        // Move to next hand
        CurrentHandNumber++;
        MoveToNextDealer();

        OnPropertyChanged(nameof(IsGameComplete));
        if (IsGameComplete)
        {
            var winner = Players.OrderByDescending(p => p.TotalScore).First();
            StatusMessage = $"Game Over! {winner.Name} wins with {winner.TotalScore} points!";
        }
        
        // Auto-save after confirming scores
        AutoSave();
    }

    private void BackToScoreInput()
    {
        CurrentPhase = GamePhase.EnteringScores;
        StatusMessage = "Enter scores for each player";
    }

    private bool CanBackForCurrentPhase()
    {
        return CurrentPhase == GamePhase.Bidding
            || CurrentPhase == GamePhase.EnteringScores
            || CurrentPhase == GamePhase.ScoreSummary;
    }

    private void BackForCurrentPhase()
    {
        switch (CurrentPhase)
        {
            case GamePhase.Bidding:
                BackBiddingStep();
                break;
            case GamePhase.EnteringScores:
                BackToBiddingMatrix();
                break;
            case GamePhase.ScoreSummary:
                BackToScoreInput();
                break;
        }
    }

    private void BackToBiddingMatrix()
    {
        if (CurrentDealer == null) return;
        // Return straight to the bidding matrix while keeping the current
        // matrix selections so the user can simply tweak them.
        ShowRestartBiddingConfirm = false;
        ErrorMessage = "";
        CurrentPhase = GamePhase.Bidding;
        IsInRedoublePhase = false;
        IsWaitingForImmediateRedouble = false;
        CurrentBiddingPlayer = null;
        StatusMessage = "Bidding: select doubles and redoubles, then confirm.";
        if (DoublingMatrix.Count == 0)
        {
            BuildDoublingMatrix();
        }
    }

    private void ConfirmRestartBidding()
    {
        ShowRestartBiddingConfirm = false;
        
        // Reset bidding state
        BiddingState.Reset();
        Array.Clear(CurrentInputs);
        AceOfHeartsPlayer = null;
        KingOfHeartsPlayer = null;
        SecondToLastTrickPlayer = null;
        LastTrickPlayer = null;
        
        // Go back to contract selection
        SelectedContract = null;
        foreach (var opt in AllContractOptions)
        {
            opt.IsSelected = false;
        }
        CurrentPhase = GamePhase.SelectingContract;
        StatusMessage = $"Round {CurrentHandNumber}: {CurrentDealer?.Name}'s turn to choose a game";
    }

    private string? ValidateScores(ContractType contract, int[] inputs)
    {
        int total = inputs.Sum();
        int expectedTotal;
        string itemName;

        switch (contract)
        {
            case ContractType.Nullo:
                expectedTotal = 13;
                itemName = "tricks";
                if (total != expectedTotal)
                    return $"Error: Total tricks must equal {expectedTotal}. You entered {total} {itemName}. Please fix the scoring.";
                break;

            case ContractType.NoQueens:
                expectedTotal = 4;
                itemName = "queens";
                if (total != expectedTotal)
                    return $"Error: Total queens must equal {expectedTotal}. You entered {total} {itemName}. Please fix the scoring. (Total points should be -24)";
                break;

            case ContractType.Hearts:
                expectedTotal = 12;  // 13 hearts minus the Ace which is tracked separately
                itemName = "hearts (not including Ace)";
                if (total != expectedTotal)
                    return $"Error: Total hearts must equal {expectedTotal}. You entered {total} {itemName}. Please fix the scoring.";
                if (AceOfHeartsPlayer == null)
                    return "Error: Please select which player won the Ace of Hearts.";
                break;

            case ContractType.NoLastTwo:
                if (SecondToLastTrickPlayer == null)
                    return "Error: Please select which player won the 2nd to last trick.";
                if (LastTrickPlayer == null)
                    return "Error: Please select which player won the last trick.";
                break;

            case ContractType.Barbu:
                if (KingOfHeartsPlayer == null)
                    return "Error: Please select which player won the King of Hearts.";
                break;

            case ContractType.Trumps:
                expectedTotal = 13;
                itemName = "tricks";
                if (total != expectedTotal)
                    return $"Error: Total tricks must equal {expectedTotal}. You entered {total} {itemName}. Please fix the scoring.";
                break;

            case ContractType.FanTan:
                // Check that positions 1-4 are each used exactly once
                var positions = inputs.OrderBy(x => x).ToArray();
                if (!positions.SequenceEqual(new[] { 1, 2, 3, 4 }))
                    return $"Error: Each player must have a unique finish position (1-4). Please fix the scoring.";
                break;
                
            case ContractType.RavageCity:
                // At least one player must be selected
                if (!SelectedRavageCityPlayers.Any())
                    return "Error: Please select at least one player who took the most cards in any suit.";
                break;
                
            case ContractType.ChinesePoker:
                if (ChinesePokerScoreBySetting)
                {
                    // Each setting (Front/Middle/Back) must sum to 6 beats
                    for (int setting = 0; setting < 3; setting++)
                    {
                        int settingTotal = 0;
                        for (int player = 0; player < 4; player++)
                        {
                            settingTotal += ChinesePokerSettingInputs[player, setting];
                        }
                        string settingName = setting switch { 0 => "Front", 1 => "Middle", _ => "Back" };
                        if (settingTotal != 6)
                            return $"Error: {settingName} beats must sum to 6. You entered {settingTotal}. Please fix the scoring.";
                    }
                }
                else
                {
                    // Total beats across all players must equal 18
                    int totalBeats = ChinesePokerTotalInputs.Sum();
                    if (totalBeats != 18)
                        return $"Error: Total beats must equal 18. You entered {totalBeats}. Please fix the scoring.";
                }
                break;
        }

        return null; // Valid
    }

    private void MoveToNextDealer()
    {
        if (CurrentDealer == null) return;

        // Move clockwise to next dealer who still has contracts
        var nextPosition = CurrentDealer.Position.NextClockwise();
        
        for (int i = 0; i < 4; i++)
        {
            var nextPlayer = GetPlayerByPosition(nextPosition);
            if (nextPlayer.RemainingContracts.Count > 0)
            {
                CurrentDealer = nextPlayer;
                SelectedContract = null;
                CurrentPhase = GamePhase.SelectingContract;
                StatusMessage = $"Round {CurrentHandNumber}: {CurrentDealer.Name}'s turn to choose a game";
                return;
            }
            nextPosition = nextPosition.NextClockwise();
        }
    }

    private void ConfirmNewGame()
    {
        ShowNewGameConfirm = false;
        HandHistory.Clear();
        Players.Clear();
        DealerDoubleMatrix.Clear();
        Scorecard.Clear();
        CurrentHandNumber = 0;
        CurrentDealer = null;
        SelectedContract = null;
        BiddingState.Reset();
        Array.Clear(CurrentInputs);
        ErrorMessage = "";
        CurrentScreen = GameScreen.PlayerSetup;
        CurrentPhase = GamePhase.SelectingContract;
        StatusMessage = "Enter player names to begin";
        GameName = "";
        WestName = "";
        NorthName = "";
        EastName = "";
        SouthName = "";
        _autoSaveFilePath = null;
        OnPropertyChanged(nameof(IsGameComplete));
        OnPropertyChanged(nameof(CanStartGame));
    }

    private void ConfirmSettingsNewGame()
    {
        ShowSettingsNewGamePrompt = false;
        // Clear pending reverts since we're starting a new game
        _pendingRevertGameMode = null;
        _pendingRevertRavageCityEnabled = null;
        _pendingRevertChinesePokerEnabled = null;
        IsSettingsOpen = false;
        ConfirmNewGame();
    }

    private void CancelSettingsNewGame()
    {
        ShowSettingsNewGamePrompt = false;
        
        // Revert any pending setting changes (in reverse order to handle dependencies)
        if (_pendingRevertChinesePokerEnabled.HasValue)
        {
            _chinesePokerEnabled = _pendingRevertChinesePokerEnabled.Value;
            Contract.ChinesePokerModeEnabled = _chinesePokerEnabled;
            OnPropertyChanged(nameof(ChinesePokerEnabled));
            _pendingRevertChinesePokerEnabled = null;
        }
        
        if (_pendingRevertRavageCityEnabled.HasValue)
        {
            _ravageCityEnabled = _pendingRevertRavageCityEnabled.Value;
            Contract.RavageCityModeEnabled = _ravageCityEnabled;
            OnPropertyChanged(nameof(RavageCityEnabled));
            _pendingRevertRavageCityEnabled = null;
        }
        
        if (_pendingRevertGameMode.HasValue)
        {
            _gameMode = _pendingRevertGameMode.Value;
            OnPropertyChanged(nameof(GameMode));
            OnPropertyChanged(nameof(IsStandardMode));
            OnPropertyChanged(nameof(IsSaladeMode));
            OnPropertyChanged(nameof(NotSaladeMode));
            _pendingRevertGameMode = null;
        }
        
        // Refresh contract options to reflect reverted settings
        RefreshContractOptions();
        SaveSettings();
    }

    private int[] CalculateBaseScores(ContractType contract, int[] inputs)
    {
        int[] scores = new int[4];

        switch (contract)
        {
            case ContractType.Nullo:
                // -3 per trick with Ravage City or Chinese Poker, -2 otherwise
                int nulloMultiplier = (ChinesePokerEnabled || RavageCityEnabled) ? -3 : -2;
                for (int i = 0; i < 4; i++)
                    scores[i] = nulloMultiplier * inputs[i];
                break;

            case ContractType.NoQueens:
                // -12 per queen with Chinese Poker, -8 with Ravage City, -6 otherwise
                int queenMultiplier = ChinesePokerEnabled ? -12 : (RavageCityEnabled ? -8 : -6);
                for (int i = 0; i < 4; i++)
                    scores[i] = queenMultiplier * inputs[i];
                break;

            case ContractType.Hearts:
                // -3 per heart, -9 for Ace with Chinese Poker; -2 per heart, -6 for Ace otherwise
                int heartMultiplier = ChinesePokerEnabled ? -3 : -2;
                int aceValue = ChinesePokerEnabled ? -9 : -6;
                for (int i = 0; i < 4; i++)
                {
                    scores[i] = heartMultiplier * inputs[i];  // per heart (not including Ace)
                    if (AceOfHeartsPlayer != null && Players[i] == AceOfHeartsPlayer)
                        scores[i] += aceValue;  // Ace of Hearts
                }
                break;

            case ContractType.NoLastTwo:
                // -25 for last, -15 for 2nd last with Chinese Poker; -20/-10 otherwise
                int lastPenalty = ChinesePokerEnabled ? -25 : -20;
                int secondLastPenalty = ChinesePokerEnabled ? -15 : -10;
                for (int i = 0; i < 4; i++)
                {
                    scores[i] = 0;
                    if (SecondToLastTrickPlayer != null && Players[i] == SecondToLastTrickPlayer)
                        scores[i] += secondLastPenalty;
                    if (LastTrickPlayer != null && Players[i] == LastTrickPlayer)
                        scores[i] += lastPenalty;
                }
                break;

            case ContractType.Barbu:
                // -30 for King of Hearts with Chinese Poker, -21 with Ravage City, -20 otherwise
                int barbuScore = ChinesePokerEnabled ? -30 : (RavageCityEnabled ? -21 : -20);
                for (int i = 0; i < 4; i++)
                    scores[i] = (KingOfHeartsPlayer != null && Players[i] == KingOfHeartsPlayer) ? barbuScore : 0;
                break;

            case ContractType.Trumps:
                // +7 per trick with Ravage City only, +5 otherwise (including Chinese Poker)
                int trumpsMultiplier = (RavageCityEnabled && !ChinesePokerEnabled) ? 7 : 5;
                for (int i = 0; i < 4; i++)
                    scores[i] = trumpsMultiplier * inputs[i];
                break;

            case ContractType.FanTan:
                // With Ravage City only: fixed 50/25/10/0, otherwise use configured values
                int[] fanTanPoints;
                if (RavageCityEnabled && !ChinesePokerEnabled)
                {
                    fanTanPoints = new[] { 50, 25, 10, 0 };
                }
                else
                {
                    fanTanPoints = new[] { FanTanScore1st, FanTanScore2nd, FanTanScore3rd, FanTanScore4th };
                }
                for (int i = 0; i < 4; i++)
                {
                    int position = inputs[i];
                    if (position >= 1 && position <= 4)
                        scores[i] = fanTanPoints[position - 1];
                }
                break;
                
            case ContractType.RavageCity:
                // Player(s) who took most cards in any suit score negative
                // With Chinese Poker: -36 for 1 player, -18 for 2-way, -12 for 3-way, -9 for 4-way
                // Without Chinese Poker: -24 for 1 player, -12 for 2-way, -8 for 3-way, -6 for 4-way
                var selectedPlayers = SelectedRavageCityPlayers;
                int penaltyPerPlayer;
                if (ChinesePokerEnabled)
                {
                    penaltyPerPlayer = selectedPlayers.Count switch
                    {
                        1 => -36,
                        2 => -18,
                        3 => -12,
                        4 => -9,
                        _ => 0
                    };
                }
                else
                {
                    penaltyPerPlayer = selectedPlayers.Count switch
                    {
                        1 => -24,
                        2 => -12,
                        3 => -8,
                        4 => -6,
                        _ => 0
                    };
                }
                for (int i = 0; i < 4; i++)
                {
                    scores[i] = selectedPlayers.Contains(Players[i]) ? penaltyPerPlayer : 0;
                }
                break;
                
            case ContractType.ChinesePoker:
                // +6 per beat, scored by setting (Front/Middle/Back) or total beats
                if (ChinesePokerScoreBySetting)
                {
                    // Sum beats from all three settings (Front, Middle, Back)
                    for (int i = 0; i < 4; i++)
                    {
                        int totalBeats = ChinesePokerSettingInputs[i, 0] + ChinesePokerSettingInputs[i, 1] + ChinesePokerSettingInputs[i, 2];
                        scores[i] = 6 * totalBeats;
                    }
                }
                else
                {
                    // Score by total beats
                    for (int i = 0; i < 4; i++)
                    {
                        scores[i] = 6 * ChinesePokerTotalInputs[i];
                    }
                }
                break;
        }

        return scores;
    }

    private int[] ApplyDoubles(int[] baseScores, List<DoubleBid> doubles)
    {
        int[] finalScores = new int[4];
        Array.Copy(baseScores, finalScores, 4);

        foreach (var dbl in doubles)
        {
            int doublerIdx = dbl.Doubler.Index;
            int targetIdx = dbl.Target.Index;

            // Calculate the difference
            int diff = baseScores[targetIdx] - baseScores[doublerIdx];
            
            // Multiplier: 2x for double, 4x for redouble
            int multiplier = dbl.IsRedoubled ? 2 : 1;

            // Transfer points based on outcome
            finalScores[doublerIdx] -= diff * multiplier;
            finalScores[targetIdx] += diff * multiplier;
        }

        return finalScores;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class RavageCityPlayerSelection : INotifyPropertyChanged
{
    private bool _isSelected;
    
    public Player Player { get; set; } = null!;
    
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

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
