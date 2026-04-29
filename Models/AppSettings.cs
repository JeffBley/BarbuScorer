using System.IO;
using System.Text.Json;

namespace CardGameScorer.Models;

/// <summary>
/// Application settings that persist between sessions
/// </summary>
public class AppSettings
{
    public TextSize TextSize { get; set; } = TextSize.Medium;
    public bool DealerAllowedToDouble { get; set; } = false;
    public bool AllowNonDealerDoublesInPositive { get; set; } = false;
    public bool SkipNegativeContractOnNoDoubles { get; set; } = true;
    public GameMode GameMode { get; set; } = GameMode.Standard;
    public BarbuVersion BarbuVersion { get; set; } = BarbuVersion.Classic;
    public bool RavageCityEnabled { get; set; } = false;
    public bool ChinesePokerEnabled { get; set; } = false;
    public int FanTanScore1st { get; set; } = 40;
    public int FanTanScore2nd { get; set; } = 25;
    public int FanTanScore3rd { get; set; } = 10;
    public int FanTanScore4th { get; set; } = -10;
    // Fan Tan / Domino scores used when only Ravage City is enabled (no Chinese Poker).
    // Default total is 85 to keep the game zero-sum with the extra Ravage City contract.
    public int FanTanRcScore1st { get; set; } = 50;
    public int FanTanRcScore2nd { get; set; } = 25;
    public int FanTanRcScore3rd { get; set; } = 10;
    public int FanTanRcScore4th { get; set; } = 0;
    
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Barbu",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // If loading fails, return default settings
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Silently fail if we can't save settings
        }
    }
}
