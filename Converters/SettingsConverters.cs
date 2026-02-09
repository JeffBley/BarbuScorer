using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CardGameScorer.Models;

namespace CardGameScorer.Converters;

/// <summary>
/// Converts true to Collapsed and false to Visible.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts SelectedSettingsTab to a background color based on whether the tab is selected.
/// </summary>
public class TabSelectedConverter : IValueConverter
{
    private static readonly SolidColorBrush SelectedBrush = new(Color.FromRgb(0x45, 0x47, 0x5a));
    private static readonly SolidColorBrush UnselectedBrush = Brushes.Transparent;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int selectedTab && parameter is string paramStr && int.TryParse(paramStr, out int tabIndex))
        {
            return selectedTab == tabIndex ? SelectedBrush : UnselectedBrush;
        }
        return UnselectedBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts SelectedSettingsTab to Visibility based on whether the tab is selected.
/// </summary>
public class TabVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int selectedTab && parameter is string paramStr && int.TryParse(paramStr, out int tabIndex))
        {
            return selectedTab == tabIndex ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts enum values to bool for radio button binding. Two-way support.
/// </summary>
public class EnumBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        
        string enumValue = value.ToString()!;
        string targetValue = parameter.ToString()!;
        return enumValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string enumString)
        {
            if (Enum.TryParse(typeof(TextSize), enumString, true, out object? result))
            {
                return result;
            }
        }
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts bool to color: true = green (#a6e3a1), false = red (#f38ba8)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush ValidBrush = new(Color.FromRgb(0xa6, 0xe3, 0xa1)); // green
    private static readonly SolidColorBrush InvalidBrush = new(Color.FromRgb(0xf3, 0x8b, 0xa8)); // red/pink

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isValid)
        {
            return isValid ? ValidBrush : InvalidBrush;
        }
        return InvalidBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
