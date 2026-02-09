namespace CardGameScorer.Models;

/// <summary>
/// Controls the contract/game naming style
/// </summary>
public enum BarbuVersion
{
    Classic,   // Nullo, Hearts, Barbu, Fan Tan
    Modern     // No Tricks, No Hearts, No King of Hearts, Domino
}

/// <summary>
/// Controls the game mode/rules
/// </summary>
public enum GameMode
{
    Standard,  // Normal Barbu game
    Salade     // Salade variant
}
