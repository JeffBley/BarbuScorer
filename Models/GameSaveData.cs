using System.Text.Json;

namespace CardGameScorer.Models;

/// <summary>
/// Data structure for saving/loading game state
/// </summary>
public class GameSaveData
{
    public string GameName { get; set; } = "";
    public List<PlayerSaveData> Players { get; set; } = new();
    public int CurrentDealerIndex { get; set; }
    public int CurrentHandNumber { get; set; }
    public List<HandResultSaveData> HandHistory { get; set; } = new();
    public string Phase { get; set; } = "SelectingContract";
    public string? SelectedContract { get; set; }
    public List<DoubleBidSaveData> CurrentDoubles { get; set; } = new();
    public DateTime SavedAt { get; set; } = DateTime.Now;
    
    // Settings saved with game
    public string TextSize { get; set; } = "Medium";
    public bool DealerAllowedToDouble { get; set; } = false;
    public string GameMode { get; set; } = "Standard";
    public string BarbuVersion { get; set; } = "Classic";
    public bool RavageCityEnabled { get; set; } = false;
    public bool ChinesePokerEnabled { get; set; } = false;
    public int FanTanScore1st { get; set; } = 40;
    public int FanTanScore2nd { get; set; } = 25;
    public int FanTanScore3rd { get; set; } = 10;
    public int FanTanScore4th { get; set; } = -10;
    public int FanTanRcScore1st { get; set; } = 50;
    public int FanTanRcScore2nd { get; set; } = 25;
    public int FanTanRcScore3rd { get; set; } = 10;
    public int FanTanRcScore4th { get; set; } = 0;

    public static string Serialize(GameSaveData data)
    {
        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    public static GameSaveData? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<GameSaveData>(json);
    }
}

public class PlayerSaveData
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public int TotalScore { get; set; }
    public List<string> DealtContracts { get; set; } = new();
}

public class HandResultSaveData
{
    public int HandNumber { get; set; }
    public string Contract { get; set; } = "";
    public int DealerIndex { get; set; }
    public List<int> Scores { get; set; } = new();
    public List<DoubleBidSaveData> Doubles { get; set; } = new();

    // Per-contract raw inputs (optional; older saves may omit these).
    public List<int>? RawInputs { get; set; }
    public int? AceOfHeartsPlayerIndex { get; set; }
    public int? KingOfHeartsPlayerIndex { get; set; }
    public int? LastTrickPlayerIndex { get; set; }
    public int? SecondToLastTrickPlayerIndex { get; set; }
    public List<int>? SaladeTricks { get; set; }
    public List<int>? SaladeQueens { get; set; }
    public List<int>? SaladeHearts { get; set; }
    public List<int>? RavageCityPlayerIndices { get; set; }
    public bool? ChinesePokerScoreBySetting { get; set; }
    public List<int>? ChinesePokerSettingInputs { get; set; }
    public List<int>? ChinesePokerTotalInputs { get; set; }
}

public class DoubleBidSaveData
{
    public int DoublerIndex { get; set; }
    public int TargetIndex { get; set; }
    public bool IsRedoubled { get; set; }
}
