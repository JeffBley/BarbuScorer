namespace CardGameScorer.Models;

public class HandResult
{
    public int HandNumber { get; set; }
    public Player Dealer { get; set; } = null!;
    public ContractType Contract { get; set; }
    public int[] PlayerScores { get; set; } = new int[4];
    public int[] RawInputs { get; set; } = new int[4];
    /// <summary>False when this hand was scored without ever entering raw inputs
    /// (e.g. a negative contract auto-skipped because no one doubled). Causes the
    /// Edit popup to display blank text boxes instead of zeros.</summary>
    public bool HasRawInputs { get; set; } = true;
    public List<DoubleBid> Doubles { get; set; } = new();

    // --- Per-contract raw inputs needed to faithfully re-edit a hand later ---
    // Player indices for special-card holders (null if not applicable / not recorded).
    public int? AceOfHeartsPlayerIndex { get; set; }
    public int? KingOfHeartsPlayerIndex { get; set; }
    public int? LastTrickPlayerIndex { get; set; }
    public int? SecondToLastTrickPlayerIndex { get; set; }

    // Salade per-player counts.
    public int[]? SaladeTricks { get; set; }
    public int[]? SaladeQueens { get; set; }
    public int[]? SaladeHearts { get; set; }

    // Ravage City: list of player indices who took the most cards in any suit.
    public List<int>? RavageCityPlayerIndices { get; set; }

    // Chinese Poker raw inputs.
    public bool? ChinesePokerScoreBySetting { get; set; }
    // Flat 12-element [player*3 + setting] (0=Front,1=Middle,2=Back) when scoring by setting.
    public int[]? ChinesePokerSettingInputs { get; set; }
    public int[]? ChinesePokerTotalInputs { get; set; }

    public string ContractName => new Contract { Type = Contract }.Name;

    public string DoublesDescription
    {
        get
        {
            if (Doubles.Count == 0) return "None";
            return string.Join(", ", Doubles.Select(d =>
                d.IsRedoubled 
                    ? $"{d.Doubler.Name[0]}→{d.Target.Name[0]}(R)"
                    : $"{d.Doubler.Name[0]}→{d.Target.Name[0]}"));
        }
    }

    /// <summary>
    /// Apply doubles/redoubles to a base-score array. Pure function: no VM state.
    /// </summary>
    public static int[] ApplyDoubles(int[] baseScores, IList<DoubleBid> doubles)
    {
        int[] finalScores = new int[4];
        Array.Copy(baseScores, finalScores, 4);

        foreach (var dbl in doubles)
        {
            int doublerIdx = dbl.Doubler.Index;
            int targetIdx = dbl.Target.Index;
            int diff = baseScores[targetIdx] - baseScores[doublerIdx];
            int multiplier = dbl.IsRedoubled ? 2 : 1;
            finalScores[doublerIdx] -= diff * multiplier;
            finalScores[targetIdx] += diff * multiplier;
        }

        return finalScores;
    }

    /// <summary>
    /// Recompute base scores from this hand's stored raw inputs and contract type,
    /// using the supplied scoring context (game-mode flags + Fan Tan values).
    /// Returns null if the hand lacks the required raw inputs (legacy save).
    /// </summary>
    public int[]? RecomputeBaseScores(ScoringContext ctx)
    {
        int[] scores = new int[4];

        switch (Contract)
        {
            case ContractType.Nullo:
            {
                int mult = ctx.IsSaladeMode ? -5
                    : ((ctx.ChinesePokerEnabled || ctx.RavageCityEnabled) ? -3 : -2);
                for (int i = 0; i < 4; i++) scores[i] = mult * RawInputs[i];
                break;
            }
            case ContractType.NoQueens:
            {
                int mult = ctx.IsSaladeMode ? -20
                    : (ctx.ChinesePokerEnabled ? -12 : (ctx.RavageCityEnabled ? -8 : -6));
                for (int i = 0; i < 4; i++) scores[i] = mult * RawInputs[i];
                break;
            }
            case ContractType.Hearts:
                if (ctx.IsSaladeMode)
                {
                    for (int i = 0; i < 4; i++) scores[i] = -10 * RawInputs[i];
                }
                else
                {
                    int mult = ctx.ChinesePokerEnabled ? -3 : -2;
                    int aceVal = ctx.ChinesePokerEnabled ? -9 : -6;
                    for (int i = 0; i < 4; i++)
                    {
                        scores[i] = mult * RawInputs[i];
                        if (AceOfHeartsPlayerIndex == i) scores[i] += aceVal;
                    }
                }
                break;
            case ContractType.NoLastTwo:
                if (ctx.IsSaladeMode)
                {
                    for (int i = 0; i < 4; i++)
                        scores[i] = (LastTrickPlayerIndex == i) ? -30 : 0;
                }
                else
                {
                    int last = ctx.ChinesePokerEnabled ? -25 : -20;
                    int second = ctx.ChinesePokerEnabled ? -15 : -10;
                    for (int i = 0; i < 4; i++)
                    {
                        scores[i] = 0;
                        if (SecondToLastTrickPlayerIndex == i) scores[i] += second;
                        if (LastTrickPlayerIndex == i) scores[i] += last;
                    }
                }
                break;
            case ContractType.Barbu:
            {
                int barbu = ctx.IsSaladeMode ? -50
                    : (ctx.ChinesePokerEnabled ? -30 : (ctx.RavageCityEnabled ? -21 : -20));
                for (int i = 0; i < 4; i++)
                    scores[i] = (KingOfHeartsPlayerIndex == i) ? barbu : 0;
                break;
            }
            case ContractType.Salade:
                if (SaladeTricks == null || SaladeQueens == null || SaladeHearts == null)
                    return null;
                for (int i = 0; i < 4; i++)
                {
                    int s = -5 * SaladeTricks[i] + -20 * SaladeQueens[i] + -10 * SaladeHearts[i];
                    if (LastTrickPlayerIndex == i) s += -30;
                    if (KingOfHeartsPlayerIndex == i) s += -50;
                    scores[i] = s;
                }
                break;
            case ContractType.Trumps:
            {
                int mult = (ctx.RavageCityEnabled && !ctx.ChinesePokerEnabled) ? 7 : 5;
                for (int i = 0; i < 4; i++) scores[i] = mult * RawInputs[i];
                break;
            }
            case ContractType.FanTan:
            {
                int[] points = (ctx.RavageCityEnabled && !ctx.ChinesePokerEnabled)
                    ? new[] { 50, 25, 10, 0 }
                    : new[] { ctx.FanTanScore1st, ctx.FanTanScore2nd, ctx.FanTanScore3rd, ctx.FanTanScore4th };
                for (int i = 0; i < 4; i++)
                {
                    int pos = RawInputs[i];
                    if (pos >= 1 && pos <= 4) scores[i] = points[pos - 1];
                }
                break;
            }
            case ContractType.RavageCity:
            {
                if (RavageCityPlayerIndices == null) return null;
                int n = RavageCityPlayerIndices.Count;
                int penalty;
                if (ctx.ChinesePokerEnabled)
                    penalty = n switch { 1 => -36, 2 => -18, 3 => -12, 4 => -9, _ => 0 };
                else
                    penalty = n switch { 1 => -24, 2 => -12, 3 => -8, 4 => -6, _ => 0 };
                for (int i = 0; i < 4; i++)
                    scores[i] = RavageCityPlayerIndices.Contains(i) ? penalty : 0;
                break;
            }
            case ContractType.ChinesePoker:
                if (ChinesePokerScoreBySetting == true && ChinesePokerSettingInputs != null)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int beats = ChinesePokerSettingInputs[i * 3]
                                  + ChinesePokerSettingInputs[i * 3 + 1]
                                  + ChinesePokerSettingInputs[i * 3 + 2];
                        scores[i] = 6 * beats;
                    }
                }
                else if (ChinesePokerTotalInputs != null)
                {
                    for (int i = 0; i < 4; i++) scores[i] = 6 * ChinesePokerTotalInputs[i];
                }
                else return null;
                break;
        }

        return scores;
    }
}

/// <summary>
/// Context needed to recompute base scores for a given hand.
/// Captures game-mode flags and Fan Tan scoring values active for that hand.
/// </summary>
public struct ScoringContext
{
    public bool IsSaladeMode;
    public bool RavageCityEnabled;
    public bool ChinesePokerEnabled;
    public int FanTanScore1st;
    public int FanTanScore2nd;
    public int FanTanScore3rd;
    public int FanTanScore4th;
}
