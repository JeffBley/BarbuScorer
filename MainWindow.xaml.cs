using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
            }
        };

        // Wire up responsive behavior for score tiles grid
        SizeChanged += MainWindow_SizeChanged;
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
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
            // Copy input values to ViewModel
            if (int.TryParse(ScoreInput0.Text, out int val0)) vm.CurrentInputs[0] = val0;
            if (int.TryParse(ScoreInput1.Text, out int val1)) vm.CurrentInputs[1] = val1;
            if (int.TryParse(ScoreInput2.Text, out int val2)) vm.CurrentInputs[2] = val2;
            if (int.TryParse(ScoreInput3.Text, out int val3)) vm.CurrentInputs[3] = val3;

            // Copy Chinese Poker setting inputs (4 players × 3 settings)
            if (int.TryParse(CPSetting00.Text, out int cp00)) vm.ChinesePokerSettingInputs[0, 0] = cp00;
            if (int.TryParse(CPSetting01.Text, out int cp01)) vm.ChinesePokerSettingInputs[0, 1] = cp01;
            if (int.TryParse(CPSetting02.Text, out int cp02)) vm.ChinesePokerSettingInputs[0, 2] = cp02;
            if (int.TryParse(CPSetting10.Text, out int cp10)) vm.ChinesePokerSettingInputs[1, 0] = cp10;
            if (int.TryParse(CPSetting11.Text, out int cp11)) vm.ChinesePokerSettingInputs[1, 1] = cp11;
            if (int.TryParse(CPSetting12.Text, out int cp12)) vm.ChinesePokerSettingInputs[1, 2] = cp12;
            if (int.TryParse(CPSetting20.Text, out int cp20)) vm.ChinesePokerSettingInputs[2, 0] = cp20;
            if (int.TryParse(CPSetting21.Text, out int cp21)) vm.ChinesePokerSettingInputs[2, 1] = cp21;
            if (int.TryParse(CPSetting22.Text, out int cp22)) vm.ChinesePokerSettingInputs[2, 2] = cp22;
            if (int.TryParse(CPSetting30.Text, out int cp30)) vm.ChinesePokerSettingInputs[3, 0] = cp30;
            if (int.TryParse(CPSetting31.Text, out int cp31)) vm.ChinesePokerSettingInputs[3, 1] = cp31;
            if (int.TryParse(CPSetting32.Text, out int cp32)) vm.ChinesePokerSettingInputs[3, 2] = cp32;

            // Copy Chinese Poker total inputs
            if (int.TryParse(CPTotal0.Text, out int cpt0)) vm.ChinesePokerTotalInputs[0] = cpt0;
            if (int.TryParse(CPTotal1.Text, out int cpt1)) vm.ChinesePokerTotalInputs[1] = cpt1;
            if (int.TryParse(CPTotal2.Text, out int cpt2)) vm.ChinesePokerTotalInputs[2] = cpt2;
            if (int.TryParse(CPTotal3.Text, out int cpt3)) vm.ChinesePokerTotalInputs[3] = cpt3;

            // Call RecordHand directly so we can check HasError after validation
            vm.RecordHandCommand.Execute(null);

            // Clear inputs only if no validation error
            if (!vm.HasError)
            {
                ScoreInput0.Text = "";
                ScoreInput1.Text = "";
                ScoreInput2.Text = "";
                ScoreInput3.Text = "";
                
                // Clear Chinese Poker inputs
                CPSetting00.Text = ""; CPSetting01.Text = ""; CPSetting02.Text = "";
                CPSetting10.Text = ""; CPSetting11.Text = ""; CPSetting12.Text = "";
                CPSetting20.Text = ""; CPSetting21.Text = ""; CPSetting22.Text = "";
                CPSetting30.Text = ""; CPSetting31.Text = ""; CPSetting32.Text = "";
                CPTotal0.Text = ""; CPTotal1.Text = ""; CPTotal2.Text = ""; CPTotal3.Text = "";
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

        // Bidding phase (doubling): Confirm Doubles
        if (vm.IsBidding && vm.IsInDoublingPhase)
        {
            vm.ConfirmDoublesCommand.Execute(null);
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
}