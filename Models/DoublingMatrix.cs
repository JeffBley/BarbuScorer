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
    private DoublingMatrixCell? _inverseCell;

    public Player Doubler { get; set; } = null!;
    public Player Target { get; set; } = null!;

    /// <summary>True for the diagonal cell (Doubler == Target). Renders as empty.</summary>
    public bool IsEmpty => Doubler != null && Target != null && Doubler.Index == Target.Index;

    public bool IsNotEmpty => !IsEmpty;

    /// <summary>This double is required by the rule that each player must double each dealer twice.</summary>
    public bool IsMandatory { get; set; }

    /// <summary>True when this cell belongs to the dealer's row and dealers aren't allowed
    /// to double. The Dbl checkbox is hidden, leaving only Re-Dbl available.</summary>
    private bool _isDealerRowLocked;
    public bool IsDealerRowLocked
    {
        get => _isDealerRowLocked;
        set
        {
            if (_isDealerRowLocked == value) return;
            _isDealerRowLocked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotDealerRowLocked));
            OnPropertyChanged(nameof(IsDoubleVisible));
            OnPropertyChanged(nameof(IsDoubleEnabled));
        }
    }
    public bool IsNotDealerRowLocked => !_isDealerRowLocked;

    /// <summary>True when the Dbl checkbox should be hidden because the rule
    /// "Double non-dealers for positive contracts" is disabled and this cell
    /// is a non-dealer doubling another non-dealer in a positive contract.</summary>
    private bool _isRestricted;
    public bool IsRestricted
    {
        get => _isRestricted;
        set
        {
            if (_isRestricted == value) return;
            _isRestricted = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDoubleVisible));
            OnPropertyChanged(nameof(IsDoubleEnabled));
        }
    }

    /// <summary>The Dbl checkbox is visible only when the cell isn't locked
    /// for any reason (dealer-row-locked or contract-rule-restricted).</summary>
    public bool IsDoubleVisible => !_isDealerRowLocked && !_isRestricted;

    /// <summary>The mirror cell where the row/column players are swapped. When this cell's
    /// inverse is doubled, this cell's Dbl is disabled and Re-Dbl becomes the only option,
    /// representing this player redoubling the other player's original double.</summary>
    public DoublingMatrixCell? InverseCell
    {
        get => _inverseCell;
        set
        {
            if (_inverseCell != null)
                _inverseCell.PropertyChanged -= OnInversePropertyChanged;
            _inverseCell = value;
            if (_inverseCell != null)
                _inverseCell.PropertyChanged += OnInversePropertyChanged;
            OnPropertyChanged(nameof(IsDoubleEnabled));
            OnPropertyChanged(nameof(IsRedoubleEnabled));
            OnPropertyChanged(nameof(EffectiveRedouble));
        }
    }

    private void OnInversePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsDouble))
        {
            OnPropertyChanged(nameof(IsDoubleEnabled));
            OnPropertyChanged(nameof(IsRedoubleEnabled));
            OnPropertyChanged(nameof(EffectiveRedouble));
        }
        else if (e.PropertyName == nameof(IsRedouble))
        {
            OnPropertyChanged(nameof(EffectiveRedouble));
        }
    }

    public bool IsDouble
    {
        get => _isDouble;
        set
        {
            if (_isDouble == value) return;
            // Mandatory doubles cannot be unchecked.
            if (IsMandatory && !value)
            {
                // Notify so the bound UI reverts to checked.
                OnPropertyChanged();
                return;
            }
            _isDouble = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRedoubleEnabled));
            OnPropertyChanged(nameof(EffectiveRedouble));
            // If the double is unchecked, the redouble must also be cleared.
            if (!_isDouble && _isRedouble)
            {
                _isRedouble = false;
                OnPropertyChanged(nameof(IsRedouble));
                OnPropertyChanged(nameof(EffectiveRedouble));
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
            OnPropertyChanged(nameof(EffectiveRedouble));
        }
    }

    /// <summary>Whether the double checkbox can be toggled. Disabled when the inverse cell
    /// is already doubled (in which case only a redouble is possible from this side) or when
    /// the row is the dealer's and dealers aren't allowed to double.</summary>
    public bool IsDoubleEnabled
    {
        get => _isDoubleEnabled && !IsDealerRowLocked && !IsRestricted && !(InverseCell?.IsDouble ?? false);
        set { _isDoubleEnabled = value; OnPropertyChanged(); }
    }

    /// <summary>Redouble checkbox is enabled only when the inverse cell is doubled
    /// (i.e., the other player has doubled this player) — never alongside this cell's own Dbl.</summary>
    public bool IsRedoubleEnabled => !IsDouble && (InverseCell?.IsDouble ?? false);

    /// <summary>The Re-Dbl checkbox in the UI binds here. The redouble is always stored
    /// on the inverse cell so that scoring (which looks at the original DoubleBid) sees
    /// IsRedoubled=true on the doubler's entry.</summary>
    public bool EffectiveRedouble
    {
        get
        {
            if (InverseCell != null && InverseCell.IsDouble) return InverseCell.IsRedouble;
            return false;
        }
        set
        {
            if (InverseCell != null && InverseCell.IsDouble)
            {
                InverseCell.IsRedouble = value;
            }
        }
    }

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

    private bool _isDealer;
    public bool IsDealer
    {
        get => _isDealer;
        set { _isDealer = value; OnPropertyChanged(); }
    }

    /// <summary>True when this row represents the current dealer AND dealers are not allowed to double.</summary>
    private bool _isDealerLocked;
    public bool IsDealerLocked
    {
        get => _isDealerLocked;
        set
        {
            _isDealerLocked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotDealerLocked));
            foreach (var cell in Cells)
                cell.IsDealerRowLocked = value;
        }
    }
    public bool IsNotDealerLocked => !_isDealerLocked;

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
