using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CardGameScorer.Models;

/// <summary>
/// A single cell in the doubling matrix representing whether the row's player
/// (Doubler) has doubled the column's player (Target), and whether the Target
/// has redoubled in response.
/// </summary>
public class DoublingMatrixCell : INotifyPropertyChanged
{
    private bool _isDouble;
    private bool _isRedouble;
    private bool _isDoubleEnabled = true;

    public Player Doubler { get; set; } = null!;
    public Player Target { get; set; } = null!;

    /// <summary>True for the diagonal cell (Doubler == Target). Renders as empty.</summary>
    public bool IsEmpty => Doubler != null && Target != null && Doubler.Index == Target.Index;

    public bool IsNotEmpty => !IsEmpty;

    /// <summary>This double is required by the rule that each player must double each dealer twice.</summary>
    public bool IsMandatory { get; set; }

    public bool IsDouble
    {
        get => _isDouble;
        set
        {
            if (_isDouble == value) return;
            _isDouble = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRedoubleEnabled));
            // If the double is unchecked, the redouble must also be cleared.
            if (!_isDouble && _isRedouble)
            {
                _isRedouble = false;
                OnPropertyChanged(nameof(IsRedouble));
            }
        }
    }

    public bool IsRedouble
    {
        get => _isRedouble;
        set
        {
            if (_isRedouble == value) return;
            // Cannot redouble unless the cell is doubled.
            if (value && !_isDouble) return;
            _isRedouble = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Whether the double checkbox can be toggled (false for dealer's row when dealer can't double).</summary>
    public bool IsDoubleEnabled
    {
        get => _isDoubleEnabled;
        set { _isDoubleEnabled = value; OnPropertyChanged(); }
    }

    /// <summary>Redouble checkbox is enabled only after the double box is checked.</summary>
    public bool IsRedoubleEnabled => IsDouble;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// A row in the doubling matrix: one player as the doubler with cells against each target.
/// </summary>
public class DoublingMatrixRow : INotifyPropertyChanged
{
    public Player Doubler { get; set; } = null!;
    public string DoublerLabel => $"{Doubler?.Name} doubles";
    public List<DoublingMatrixCell> Cells { get; set; } = new();

    public ICommand? MaxCommand { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Header column descriptor for the matrix.</summary>
public class DoublingMatrixHeader
{
    public Player Player { get; set; } = null!;
    public string Label => Player?.Name ?? "";
}
