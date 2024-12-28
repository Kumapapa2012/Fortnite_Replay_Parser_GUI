using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using FortniteReplayReader; // Fortnite Replay Reader - https://www.nuget.org/packages/FortniteReplayReader
using Unreal.Core.Models;
using FortniteReplayReader.Models;
using System.ComponentModel;
using System.Numerics;
using FortniteReplayReader.Models.NetFieldExports;

namespace Fortnite_Replay_Parser_GUI
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String fnReplayDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\FortniteGame\\Saved\\Demos";
        String fnReplayFilePath;

        ComboBoxItem_Player fnSelectedPlayer;
        int fnTimingOffset;

        FortniteReplayReader.Models.FortniteReplay fnReplayData;

        static string FormNumber(int num)
        {
            if (num <= 0) return num.ToString();
            var sp = "";
            if (num < 10) sp = " ";

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return sp + num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return sp + num + "st";
                case 2:
                    return sp + num + "nd";
                case 3:
                    return sp + num + "rd";
                default:
                    return sp + num + "th";
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

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

        protected IEnumerable<PlayerData> getAllPlayersInReplay()
        {
            var playerData_except_NPCs = this.fnReplayData.PlayerData.Where(o => o.Placement != null);
            return playerData_except_NPCs;
        }

        public class ComboBoxItem_Player
        {
            private string _label;
            private PlayerData _player;

            public ComboBoxItem_Player(string label, PlayerData player)
            {
                _label = label;
                _player = player;
            }
            public PlayerData getPlayer()
            {
                return _player;
            }

            public override string ToString()
            {
                return _label;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) 
        {
            // Clear the data
            cmb_Players_In_Replay.Items.Clear();

            // Show File Dialog
            this.fnReplayFilePath = getReplayFileInteractive();
            if (this.fnReplayFilePath != null)
            {
                lbl_replayFilePath.Text = this.fnReplayFilePath;
            }
            else
            {
                return;
            }

            // Parse Replay File and store it to local member.
            var reader = new ReplayReader();
            this.fnReplayData = reader.ReadReplay(this.fnReplayFilePath);

            // Add Epic IDs and Names to Combo Box - Sort by PlayerName 
            var players = getAllPlayersInReplay();
            var players_sorted = players.OrderBy(player => player.PlayerName);


            foreach (var item in players_sorted)
            {
                var label = String.Format("{0}: {1} - {2}", item.PlayerName, item.PlayerId, item.IsBot ? "bot" : "human");
                var obj_comboItem = new ComboBoxItem_Player(label, item);
                cmb_Players_In_Replay.Items.Add(obj_comboItem);
            }

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get PlayerData
           this.fnSelectedPlayer = (ComboBoxItem_Player)cmb_Players_In_Replay.SelectedItem;

            // Get Offset
            int offset = Int32.Parse(TimeAdjustment.Text);

            // Parse Data and update 
            UpdateMatchResult();
        }

        private void UpdateMatchResult()
        {
            if (this.fnSelectedPlayer != null && this.fnSelectedPlayer.getPlayer().PlayerId != null)
            {
                tb_Parseed_Result.Text = getMatchData(fnSelectedPlayer.getPlayer(), this.fnTimingOffset);
            }
        }

        private String getMatchData(PlayerData player, int offset)
        {
            String ret = "";
            // Match
            if (this.fnReplayData.GameData.UtcTimeStartedMatch.HasValue)
            {
                // Match Date Time
                // To do: End time should be calc from   "Info": "LengthInMs": 1290238,
                var started_at = this.fnReplayData.GameData.UtcTimeStartedMatch.Value.ToLocalTime();
                var match_date_time = String.Format("Started at : {0}\nEnded at :{1}\n",
                    started_at,
                    started_at.AddMilliseconds(Convert.ToInt32(this.fnReplayData.Info.LengthInMs)));

                // PlayerData : Placement == null : NPCs , Placement != null : Players
                var playerData_except_NPCs = fnReplayData.PlayerData.Where(o => o.Placement != null);
                var players_total = String.Format("Total Players: {0}", playerData_except_NPCs.Count());

                var human_players = playerData_except_NPCs.Where(o => o.IsBot == false);
                var players_counts = String.Format("Human Players : {0} / Bots : {1}"
                                , human_players.Count()
                                , playerData_except_NPCs.Count() - human_players.Count()
                                );

                // eliminations
                var eliminations = (this.fnReplayData.Eliminations.Where(c => c.Eliminator == player.PlayerId.ToUpper()).ToList());

                String game_result = "================\n";
                // List Any Kills
                if (eliminations.Count > 0)
                {
                    for (var i = 0; i < eliminations.Count(); i++)
                    {
                        var killedAt = DateTime.ParseExact(eliminations[i].Time, "mm:ss", null);

                        var botKill = false;
                        var killedPlayerData = this.fnReplayData.PlayerData.Where(d => d.PlayerId == eliminations[i].EliminatedInfo.Id.ToUpper()).ToList();
                        if (killedPlayerData.Count > 0 && killedPlayerData[0].IsBot)
                        {
                            botKill = true;
                        }
                        game_result += String.Format("{0}: {1} - {2}({3})\n",
                            FormNumber(i + 1),
                            killedAt.AddSeconds(offset).ToString("mm:ss"),
                            killedPlayerData[0].PlayerName,
                            botKill ? "bot" : "human");
                    }
                }

                // Ended up with...
                var eliminated = (this.fnReplayData.Eliminations.Where(c => c.Eliminated == player.PlayerId.ToUpper()).ToList());
                if (eliminated.Count > 0)
                {
                    // You lose.
                    var eliminator_data = this.fnReplayData.PlayerData.Where(d => d.PlayerId == eliminated[0].EliminatorInfo.Id.ToUpper()).ToList();
                        game_result += String.Format("Eliminated at {0} by {1}({2})",
                        eliminated[0].Time,
                        eliminator_data[0].PlayerName,
                        eliminator_data[0].IsBot ? "bot":"human"
                        );
                }
                else
                {
                    game_result += "==== Victory Royale!! ====";
                }
                ret = String.Format("======== Game Stats for {0} =========\n{1}\n{2}\n{3}\nGame Results\n{4}", 
                    player.PlayerName,
                    match_date_time,
                    players_total,
                    players_counts,
                    game_result
                    );

            }
            return ret;
        }


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