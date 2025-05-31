using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Fortnite_Replay_Parser_GUI
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String fnReplayDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\FortniteGame\\Saved\\Demos";
        String fnReplayFilePath;

        FortniteReplayHelper fortniteReplayHelper;
        FortniteReplayHelper.ComboBoxItem_Player fnSelectedPlayer;

        int fnTimingOffset = 0; // Time adjustment in seconds, default is 0.


        public MainWindow()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Opens a file dialog to allow the user to select a replay file interactively.
        /// </summary>
        /// <remarks>The method displays an open file dialog with a filter for files with the ".replay"
        /// extension. If the user selects a file and confirms, the full path to the selected file is returned. If the
        /// user cancels the dialog, the method returns <see langword="null"/>.</remarks>
        /// <returns>The full path of the selected replay file, or <see langword="null"/> if the user cancels the dialog.</returns>
        protected string getReplayFileInteractive()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Replay Files (.replay)|*.replay";
            ofd.InitialDirectory = this.fnReplayDirectory;

            if (ofd.ShowDialog() == true)
            {
                return ofd.FileName;
            }
            return null;    
        }

        /// <summary>
        /// Handles the click event of the button to load and display player data from a Fortnite replay file.
        /// </summary>
        /// <remarks>This method clears the current player list, prompts the user to select a replay file,
        /// and populates  the ComboBox with player information extracted from the selected replay file. The player list
        /// is  sorted by player name before being displayed.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private void Button_Click(object sender, RoutedEventArgs e) 
        {
            // Clear the data
            cmb_Players_In_Replay.Items.Clear();

            // Show File Dialog for selecting a replay file
            this.fnReplayFilePath = getReplayFileInteractive();
            if (this.fnReplayFilePath != null)
            {
                lbl_replayFilePath.Text = this.fnReplayFilePath;
            }
            else
            {
                return;
            }

            // Replay ファイルからプレイヤーリストを取得し Deserialize
            this.fortniteReplayHelper = new FortniteReplayHelper();
            var players = fortniteReplayHelper.GetAllPlayersInReplay(this.fnReplayFilePath);

            // プレイヤー名でソートする
            var players_sorted = players.OrderBy(player => player.PlayerName);


            // ComboBoxにプレイヤーを追加する
            foreach (var item in players_sorted)
            {
                var label = String.Format("{0}: {1} - {2}", item.PlayerName, item.PlayerId, item.IsBot ? "bot" : "human");
                var obj_comboItem = new FortniteReplayHelper.ComboBoxItem_Player(label, item);
                cmb_Players_In_Replay.Items.Add(obj_comboItem);
            }

            // 基本情報を表示させる
            UpdateMatchResult();

        }

        /// <summary>
        /// Handles the selection change event for the player combo box.
        /// </summary>
        /// <remarks>This method updates the selected player data and adjusts the match result based on
        /// the current time offset. Ensure that the combo box contains valid player items and that the time adjustment
        /// input is a valid integer.</remarks>
        /// <param name="sender">The source of the event, typically the combo box.</param>
        /// <param name="e">The event data containing information about the selection change.</param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get PlayerData
           this.fnSelectedPlayer = (FortniteReplayHelper.ComboBoxItem_Player)cmb_Players_In_Replay.SelectedItem;

            // Get Offset
            int offset = Int32.Parse(TimeAdjustment.Text);

            // Parse Data and update 
            UpdateMatchResult();
        }

        /// <summary>
        /// Updates the match result for the currently selected player.
        /// </summary>
        /// <remarks>This method retrieves match data for the selected player and updates the parsed
        /// result text. The selected player must not be null, and the player's ID must be valid.</remarks>
        private void UpdateMatchResult()
        {
            if (this.fortniteReplayHelper == null)
            {
                // FortniteReplayHelperがnullの場合は何もしない
                return;
            }

            if (this.fnSelectedPlayer != null && this.fnSelectedPlayer.getPlayer().PlayerId != null)
            {
                // 指定されたプレイヤーのマッチデータ取得
                tb_Parseed_Result.Text = this.fortniteReplayHelper.GetMatchData(fnSelectedPlayer.getPlayer(), this.fnTimingOffset);
            }
            else
            {
                // プレイヤーが選択されていない場合は基本のマッチデータを取得
                tb_Parseed_Result.Text = this.fortniteReplayHelper.GetMatchData(null, this.fnTimingOffset);
            }
        }

        /// <summary>
        /// Handles the text change event for the time adjustment input. Updates the timing offset if the input is a
        /// valid integer.
        /// </summary>
        /// <remarks>If the input text can be parsed as an integer, the timing offset is updated and the
        /// event is marked as handled. Otherwise, the event is not handled, and the timing offset remains
        /// unchanged.</remarks>
        /// <param name="sender">The source of the event, typically the control where the text change occurred.</param>
        /// <param name="e">Provides data for the text change event, including information about the change.</param>
        private void TimeAdjustment_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Int32.TryParse(TimeAdjustment.Text, out int offset)) {
                this.fnTimingOffset = offset;
                e.Handled = true;
                UpdateMatchResult();
            }
            else
            {
                e.Handled = false;
            }
        }

    }
}