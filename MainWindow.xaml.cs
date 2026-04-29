using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using CardGameScorer.Models;
using CardGameScorer.ViewModels;
using Microsoft.Win32;

namespace CardGameScorer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Auto-select TextBox contents on focus so users can type over a leading
        // "0" without first having to clear it. Applies to all TextBoxes in the
        // window (keyboard tab focus and mouse click).
        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.GotKeyboardFocusEvent,
            new RoutedEventHandler((s, e) => { if (s is TextBox tb) tb.SelectAll(); }));
        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler((s, e) =>
            {
                if (s is TextBox tb && !tb.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    tb.Focus(); // triggers GotKeyboardFocus -> SelectAll
                }
            }));
        
        // Wire up file dialog actions after DataContext is set
        Loaded += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.RequestSaveFilePath = callback =>
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = "Barbu Save Files (*.barbu)|*.barbu|All Files (*.*)|*.*",
                        DefaultExt = ".barbu",
                        Title = "Save Game"
                    };
                    if (dialog.ShowDialog() == true)
                    {
                        callback(dialog.FileName);
                    }
                    else
                    {
                        callback(null);
                    }
                };

                vm.RequestLoadFilePath = callback =>
                {
                    var dialog = new OpenFileDialog
                    {
                        Filter = "Barbu Save Files (*.barbu)|*.barbu|All Files (*.*)|*.*",
                        DefaultExt = ".barbu",
                        Title = "Load Game"
                    };
                    if (dialog.ShowDialog() == true)
                    {
                        callback(dialog.FileName);
                    }
                    else
                    {
                        callback(null);
                    }
                };

                // Seed initial window width so adaptive sizing kicks in on first paint.
                vm.WindowWidth = ActualWidth;
            }
        };

        // Wire up responsive behavior for score tiles grid
        SizeChanged += MainWindow_SizeChanged;

        // Dispose VM resources (timers, etc.) when the window closes.
        Closed += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Dispose();
            }
        };
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Feed window width to the VM so font sizes / panel widths can adapt.
        if (DataContext is MainViewModel vm)
        {
            vm.WindowWidth = ActualWidth;
        }

        // Switch score tiles between 4 columns and 2 columns based on window width
        if (ScoreTilesGrid != null)
        {
            // Use 4 columns when window is wide enough (700px+), otherwise 2 columns for 2x2 layout
            ScoreTilesGrid.Columns = ActualWidth >= 700 ? 4 : 2;
        }
    }

    private void RecordHand_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            // Local helper: read an integer from a TextBox by name. Returns false (no-op) if the
            // control isn't currently in the visual tree (e.g. removed during a XAML refactor).
            void ReadInt(string name, Action<int> assign)
            {
                if (FindName(name) is TextBox tb && int.TryParse(tb.Text, out int v))
                    assign(v);
            }

            // Copy input values to ViewModel
            ReadInt("ScoreInput0", v => vm.CurrentInputs[0] = v);
            ReadInt("ScoreInput1", v => vm.CurrentInputs[1] = v);
            ReadInt("ScoreInput2", v => vm.CurrentInputs[2] = v);
            ReadInt("ScoreInput3", v => vm.CurrentInputs[3] = v);

            // Copy Chinese Poker setting inputs (4 players × 3 settings)
            ReadInt("CPSetting00", v => vm.ChinesePokerSettingInputs[0, 0] = v);
            ReadInt("CPSetting01", v => vm.ChinesePokerSettingInputs[0, 1] = v);
            ReadInt("CPSetting02", v => vm.ChinesePokerSettingInputs[0, 2] = v);
            ReadInt("CPSetting10", v => vm.ChinesePokerSettingInputs[1, 0] = v);
            ReadInt("CPSetting11", v => vm.ChinesePokerSettingInputs[1, 1] = v);
            ReadInt("CPSetting12", v => vm.ChinesePokerSettingInputs[1, 2] = v);
            ReadInt("CPSetting20", v => vm.ChinesePokerSettingInputs[2, 0] = v);
            ReadInt("CPSetting21", v => vm.ChinesePokerSettingInputs[2, 1] = v);
            ReadInt("CPSetting22", v => vm.ChinesePokerSettingInputs[2, 2] = v);
            ReadInt("CPSetting30", v => vm.ChinesePokerSettingInputs[3, 0] = v);
            ReadInt("CPSetting31", v => vm.ChinesePokerSettingInputs[3, 1] = v);
            ReadInt("CPSetting32", v => vm.ChinesePokerSettingInputs[3, 2] = v);

            // Copy Chinese Poker total inputs
            ReadInt("CPTotal0", v => vm.ChinesePokerTotalInputs[0] = v);
            ReadInt("CPTotal1", v => vm.ChinesePokerTotalInputs[1] = v);
            ReadInt("CPTotal2", v => vm.ChinesePokerTotalInputs[2] = v);
            ReadInt("CPTotal3", v => vm.ChinesePokerTotalInputs[3] = v);

            // Copy Salade inputs (tricks/queens/hearts per player)
            ReadInt("SaladeTricks0", v => vm.SaladeTricksInputs[0] = v);
            ReadInt("SaladeTricks1", v => vm.SaladeTricksInputs[1] = v);
            ReadInt("SaladeTricks2", v => vm.SaladeTricksInputs[2] = v);
            ReadInt("SaladeTricks3", v => vm.SaladeTricksInputs[3] = v);
            ReadInt("SaladeQueens0", v => vm.SaladeQueensInputs[0] = v);
            ReadInt("SaladeQueens1", v => vm.SaladeQueensInputs[1] = v);
            ReadInt("SaladeQueens2", v => vm.SaladeQueensInputs[2] = v);
            ReadInt("SaladeQueens3", v => vm.SaladeQueensInputs[3] = v);
            ReadInt("SaladeHearts0", v => vm.SaladeHeartsInputs[0] = v);
            ReadInt("SaladeHearts1", v => vm.SaladeHeartsInputs[1] = v);
            ReadInt("SaladeHearts2", v => vm.SaladeHeartsInputs[2] = v);
            ReadInt("SaladeHearts3", v => vm.SaladeHeartsInputs[3] = v);

            // Call RecordHand directly so we can check HasError after validation
            vm.RecordHandCommand.Execute(null);

            // Clear inputs only if no validation error
            if (!vm.HasError)
            {
                void Clear(string name) { if (FindName(name) is TextBox tb) tb.Text = ""; }
                Clear("ScoreInput0"); Clear("ScoreInput1"); Clear("ScoreInput2"); Clear("ScoreInput3");
                Clear("CPSetting00"); Clear("CPSetting01"); Clear("CPSetting02");
                Clear("CPSetting10"); Clear("CPSetting11"); Clear("CPSetting12");
                Clear("CPSetting20"); Clear("CPSetting21"); Clear("CPSetting22");
                Clear("CPSetting30"); Clear("CPSetting31"); Clear("CPSetting32");
                Clear("CPTotal0"); Clear("CPTotal1"); Clear("CPTotal2"); Clear("CPTotal3");
                Clear("SaladeTricks0"); Clear("SaladeTricks1"); Clear("SaladeTricks2"); Clear("SaladeTricks3");
                Clear("SaladeQueens0"); Clear("SaladeQueens1"); Clear("SaladeQueens2"); Clear("SaladeQueens3");
                Clear("SaladeHearts0"); Clear("SaladeHearts1"); Clear("SaladeHearts2"); Clear("SaladeHearts3");
            }
        }
    }

    private void DoubleTarget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle && toggle.Tag is DoubleTargetInfo targetInfo)
        {
            if (!targetInfo.IsClickable)
            {
                return;
            }
            
            targetInfo.IsSelected = toggle.IsChecked == true;
        }
    }

    private void EditScore_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            // Copy input values to ViewModel
            if (int.TryParse(EditInput1.Text, out int val0)) vm.EditInputs[0] = val0;
            else vm.EditInputs[0] = 0;
            if (int.TryParse(EditInput2.Text, out int val1)) vm.EditInputs[1] = val1;
            else vm.EditInputs[1] = 0;
            if (int.TryParse(EditInput3.Text, out int val2)) vm.EditInputs[2] = val2;
            else vm.EditInputs[2] = 0;
            if (int.TryParse(EditInput4.Text, out int val3)) vm.EditInputs[3] = val3;
            else vm.EditInputs[3] = 0;

            vm.ConfirmEditScoreCommand.Execute(null);
        }
    }

    private void EditScorePopup_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is MainViewModel vm)
        {
            // Pre-populate TextBoxes with current values
            EditInput1.Text = vm.EditInputs[0].ToString();
            EditInput2.Text = vm.EditInputs[1].ToString();
            EditInput3.Text = vm.EditInputs[2].ToString();
            EditInput4.Text = vm.EditInputs[3].ToString();
        }
    }

    private void EditInputsPopup_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || DataContext is not MainViewModel vm) return;

        // When the hand has no real raw inputs (auto-skipped no-doubles negative
        // contract), display blank text boxes so the user can type without first
        // having to clear leading zeros.
        bool blankZeros = !vm.EditHandHasRawInputs;

        void Set(string name, int value)
        {
            if (FindName(name) is TextBox tb)
                tb.Text = (blankZeros && value == 0) ? "" : value.ToString();
        }

        Set("EditNum0", vm.EditInputs[0]);
        Set("EditNum1", vm.EditInputs[1]);
        Set("EditNum2", vm.EditInputs[2]);
        Set("EditNum3", vm.EditInputs[3]);

        Set("EditSalT0", vm.EditSaladeTricks[0]); Set("EditSalT1", vm.EditSaladeTricks[1]);
        Set("EditSalT2", vm.EditSaladeTricks[2]); Set("EditSalT3", vm.EditSaladeTricks[3]);
        Set("EditSalQ0", vm.EditSaladeQueens[0]); Set("EditSalQ1", vm.EditSaladeQueens[1]);
        Set("EditSalQ2", vm.EditSaladeQueens[2]); Set("EditSalQ3", vm.EditSaladeQueens[3]);
        Set("EditSalH0", vm.EditSaladeHearts[0]); Set("EditSalH1", vm.EditSaladeHearts[1]);
        Set("EditSalH2", vm.EditSaladeHearts[2]); Set("EditSalH3", vm.EditSaladeHearts[3]);

        Set("EditCP00", vm.EditChinesePokerSetting[0, 0]); Set("EditCP01", vm.EditChinesePokerSetting[0, 1]); Set("EditCP02", vm.EditChinesePokerSetting[0, 2]);
        Set("EditCP10", vm.EditChinesePokerSetting[1, 0]); Set("EditCP11", vm.EditChinesePokerSetting[1, 1]); Set("EditCP12", vm.EditChinesePokerSetting[1, 2]);
        Set("EditCP20", vm.EditChinesePokerSetting[2, 0]); Set("EditCP21", vm.EditChinesePokerSetting[2, 1]); Set("EditCP22", vm.EditChinesePokerSetting[2, 2]);
        Set("EditCP30", vm.EditChinesePokerSetting[3, 0]); Set("EditCP31", vm.EditChinesePokerSetting[3, 1]); Set("EditCP32", vm.EditChinesePokerSetting[3, 2]);

        Set("EditCPTot0", vm.EditChinesePokerTotal[0]); Set("EditCPTot1", vm.EditChinesePokerTotal[1]);
        Set("EditCPTot2", vm.EditChinesePokerTotal[2]); Set("EditCPTot3", vm.EditChinesePokerTotal[3]);
    }

    private void EditInputsSave_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        void ReadInt(string name, Action<int> assign)
        {
            if (FindName(name) is TextBox tb && int.TryParse(tb.Text, out int v)) assign(v);
            else if (FindName(name) is TextBox tb2 && string.IsNullOrWhiteSpace(tb2.Text)) assign(0);
        }

        ReadInt("EditNum0", v => vm.EditInputs[0] = v);
        ReadInt("EditNum1", v => vm.EditInputs[1] = v);
        ReadInt("EditNum2", v => vm.EditInputs[2] = v);
        ReadInt("EditNum3", v => vm.EditInputs[3] = v);

        ReadInt("EditSalT0", v => vm.EditSaladeTricks[0] = v); ReadInt("EditSalT1", v => vm.EditSaladeTricks[1] = v);
        ReadInt("EditSalT2", v => vm.EditSaladeTricks[2] = v); ReadInt("EditSalT3", v => vm.EditSaladeTricks[3] = v);
        ReadInt("EditSalQ0", v => vm.EditSaladeQueens[0] = v); ReadInt("EditSalQ1", v => vm.EditSaladeQueens[1] = v);
        ReadInt("EditSalQ2", v => vm.EditSaladeQueens[2] = v); ReadInt("EditSalQ3", v => vm.EditSaladeQueens[3] = v);
        ReadInt("EditSalH0", v => vm.EditSaladeHearts[0] = v); ReadInt("EditSalH1", v => vm.EditSaladeHearts[1] = v);
        ReadInt("EditSalH2", v => vm.EditSaladeHearts[2] = v); ReadInt("EditSalH3", v => vm.EditSaladeHearts[3] = v);

        ReadInt("EditCP00", v => vm.EditChinesePokerSetting[0, 0] = v); ReadInt("EditCP01", v => vm.EditChinesePokerSetting[0, 1] = v); ReadInt("EditCP02", v => vm.EditChinesePokerSetting[0, 2] = v);
        ReadInt("EditCP10", v => vm.EditChinesePokerSetting[1, 0] = v); ReadInt("EditCP11", v => vm.EditChinesePokerSetting[1, 1] = v); ReadInt("EditCP12", v => vm.EditChinesePokerSetting[1, 2] = v);
        ReadInt("EditCP20", v => vm.EditChinesePokerSetting[2, 0] = v); ReadInt("EditCP21", v => vm.EditChinesePokerSetting[2, 1] = v); ReadInt("EditCP22", v => vm.EditChinesePokerSetting[2, 2] = v);
        ReadInt("EditCP30", v => vm.EditChinesePokerSetting[3, 0] = v); ReadInt("EditCP31", v => vm.EditChinesePokerSetting[3, 1] = v); ReadInt("EditCP32", v => vm.EditChinesePokerSetting[3, 2] = v);

        ReadInt("EditCPTot0", v => vm.EditChinesePokerTotal[0] = v); ReadInt("EditCPTot1", v => vm.EditChinesePokerTotal[1] = v);
        ReadInt("EditCPTot2", v => vm.EditChinesePokerTotal[2] = v); ReadInt("EditCPTot3", v => vm.EditChinesePokerTotal[3] = v);

        vm.ConfirmEditInputsCommand.Execute(null);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter && e.Key != Key.Return)
            return;

        if (DataContext is not MainViewModel vm)
            return;

        // Setup screen: Start Game
        if (vm.IsSetupScreen && vm.CanStartGame)
        {
            vm.StartGameCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Contract selection: Start Bid
        if (vm.CanStartBid)
        {
            vm.StartBidCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Bidding phase (doubling matrix): Confirm Bidding
        if (vm.IsBidding)
        {
            vm.ConfirmBiddingMatrixCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Entering scores: Submit Score
        if (vm.IsEnteringScores)
        {
            RecordHand_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        // Score summary: Confirm Scores
        if (vm.IsScoreSummary)
        {
            vm.ConfirmSummaryCommand.Execute(null);
            e.Handled = true;
            return;
        }
    }

    private void MandatoryDouble_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb) return;
        if (cb.DataContext is not DoublingMatrixCell cell) return;
        if (!cell.IsMandatory) return;

        // The model rejects the unchecked state for mandatory cells, but the visual
        // can still flicker; force it back to checked and show an info popup.
        if (cb.IsChecked != true)
        {
            cb.IsChecked = true;
        }

        ShowMandatoryDoublePopup(cb, $"{cell.Doubler?.Name} must double the dealer.");
    }

    private void ShowMandatoryDoublePopup(UIElement target, string message)
    {
        var converter = new BrushConverter();
        var border = new Border
        {
            Background = (Brush)converter.ConvertFromString("#1e1e2e")!,
            BorderBrush = (Brush)converter.ConvertFromString("#89b4fa")!,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            MaxWidth = 280,
            Margin = new Thickness(0, 4, 0, 0),
            Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.5 },
            Child = new TextBlock
            {
                Text = message,
                Foreground = (Brush)converter.ConvertFromString("#cdd6f4")!,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            }
        };

        var popup = new Popup
        {
            PlacementTarget = target,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            Child = border,
            IsOpen = true
        };

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, _) =>
        {
            popup.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }
}