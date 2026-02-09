using System.Globalization;
using System.Windows.Data;

namespace CardGameScorer;

public class ScoreSignConverter : IValueConverter
{
    public static ScoreSignConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int score)
        {
            return score >= 0;
        }
        if (value is int?)
        {
            var nullableScore = (int?)value;
            if (nullableScore.HasValue)
                return nullableScore.Value >= 0;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
