using System;
using System.Collections.Generic;
using System.Linq;
using FortniteReplayReader.Models;
using FortniteReplayReader;

namespace Fortnite_Replay_Parser_GUI
{
    public class FortniteReplayHelper
    {

        // Looking at player data, NPC has TeamIndex 2 and players have 3 or more.
        const int MINIMUM_TEAM_INDEX_FOR_PLAYERS = 3;

        FortniteReplayReader.Models.FortniteReplay fnReplayData;

        /// <summary>
        /// Formats a number as an ordinal string representation.
        /// </summary>
        /// <remarks>This method handles special cases for numbers ending in 11, 12, or 13, which always
        /// use the "th" suffix.</remarks>
        /// <param name="num">The number to format. Must be a non-negative integer.</param>
        /// <returns>A string representing the ordinal form of the number. For example: <list type="bullet">
        /// <item><description>"1st" for 1</description></item> <item><description>"2nd" for 2</description></item>
        /// <item><description>"3rd" for 3</description></item> <item><description>"4th" for 4</description></item>
        /// </list> If the number is less than or equal to zero, the method returns the number as a string without an
        /// ordinal suffix.</returns>
        public static string FormNumber(int num)
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

        // プレイヤーリスト取得
        /// <summary>
        /// Retrieves a collection of all players present in the specified replay file.
        /// </summary>
        /// <remarks>This method processes the replay file to extract player data, excluding NPCs. Ensure
        /// the file path points to a valid replay file.</remarks>
        /// <param name="fnReplayFilePath">The file path of the replay file to be parsed. Must be a valid, non-null, and non-empty string.</param>
        /// <returns>An <see cref="IEnumerable{PlayerData}"/> containing the player data extracted from the replay file.  The
        /// collection excludes non-player characters (NPCs) and will be empty if no players are found.</returns>
        public IEnumerable<PlayerData> GetAllPlayersInReplay(string fnReplayFilePath)
        {
            // Parse Replay File and store it to local member.
            var reader = new ReplayReader();
            this.fnReplayData = reader.ReadReplay(fnReplayFilePath);
            return GetAllPlayersInReplay_Without_NPCs();
        }

        // NPCを除外したプレイヤーリスト取得
        /// <summary>
        /// Retrieves all player data from the replay, excluding non-player characters (NPCs).
        /// </summary>
        /// <remarks>This method filters the player data based on the team index, ensuring that only
        /// actual players are included in the result. NPCs are excluded by applying a minimum team index
        /// threshold.</remarks>
        /// <returns>An enumerable collection of <see cref="PlayerData"/> objects representing all players in the replay,
        /// excluding NPCs. The collection will be empty if no players meet the criteria.</returns>
        private IEnumerable<PlayerData> GetAllPlayersInReplay_Without_NPCs()
        {
            // Parse Replay File and store it to local member.
            return this.fnReplayData.PlayerData.Where(o => o.TeamIndex >= MINIMUM_TEAM_INDEX_FOR_PLAYERS);
        }


        // マッチデータ取得
        /// <summary>
        /// Retrieves match data for a specified player, including game statistics and elimination details.
        /// </summary>
        /// <remarks>This method provides detailed information about the match, including the start and
        /// end times, the number of human and bot players, and elimination details for the specified player. If the
        /// player was eliminated, the method includes information about the eliminator. If the player achieved victory,
        /// the result is noted as "Victory Royale".</remarks>
        /// <param name="player">The player whose match data is to be retrieved. If <paramref name="player"/> is <see langword="null"/>,
        /// general match statistics are returned instead.</param>
        /// <param name="offset">The time offset, in seconds, to adjust elimination timestamps.</param>
        /// <returns>A string containing match statistics, including start and end times, total player counts, and elimination
        /// details. If <paramref name="player"/> is <see langword="null"/>, the returned string contains general match
        /// statistics.</returns>
        public string GetMatchData( PlayerData player, int offset)
        {
            var replayData = this.fnReplayData;

            // Check if replayData is null
            if (replayData == null) return "";

            string ret = "";
            if (replayData.GameData.UtcTimeStartedMatch.HasValue)
            {
                var started_at = replayData.GameData.UtcTimeStartedMatch.Value.ToLocalTime();
                var match_date_time = $"Started : {started_at}\nEnded :{started_at.AddMilliseconds(Convert.ToInt32(replayData.Info.LengthInMs))}\n";

                var playerData_except_NPCs = GetAllPlayersInReplay_Without_NPCs();
                var players_total = $"Total Players: {playerData_except_NPCs.Count()}";

                var human_players = playerData_except_NPCs.Where(o => o.IsBot == false);
                var players_counts = $"Humans : {human_players.Count()} / Bots : {playerData_except_NPCs.Count() - human_players.Count()}";

                // at this point, if player is null, return a basic information.
                if(player == null)
                {
                    ret = $"======== Game Stats =========\n{match_date_time}\n{players_total}\n{players_counts}";
                    return ret;
                }

                // Player should exists. continue to parse player data.
                var eliminations = replayData.Eliminations.Where(c => c.Eliminator == player.PlayerId.ToUpper()).ToList();

                string game_result = "================\n";
                if (eliminations.Count > 0)
                {
                    for (var i = 0; i < eliminations.Count(); i++)
                    {
                        var killedOn = DateTime.ParseExact(eliminations[i].Time, "mm:ss", null);

                        var killedByBot = false;
                        var playerKilled = replayData.PlayerData.Where(d => d.PlayerId == eliminations[i].EliminatedInfo.Id.ToUpper()).ToList();
                        if (playerKilled.Count > 0 && playerKilled[0].IsBot)
                        {
                            killedByBot = true;
                        }
                        game_result += $"{FormNumber(i + 1)}: {killedOn.AddSeconds(offset):mm\\:ss} - {playerKilled[0].PlayerName}({(killedByBot ? "bot" : "human")})\n";
                    }
                }

                var eliminated = replayData.Eliminations.Where(c => c.Eliminated == player.PlayerId.ToUpper()).ToList();
                if (eliminated.Count > 0)
                {
                    var eliminator_data = replayData.PlayerData.Where(d => d.PlayerId == eliminated[0].EliminatorInfo.Id.ToUpper()).ToList();
                    game_result += $"Eliminated by {eliminator_data[0].PlayerName}({(eliminator_data[0].IsBot ? "bot" : "human")}) at {eliminated[0].Time} ";
                }
                else
                {
                    game_result += "==== Victory Royale!! ====";
                }
                ret = $"======== Game Stats for {player.PlayerName} =========\n{match_date_time}\n{players_total}\n{players_counts}\nGame Results\n{game_result}";
            }
            return ret;
        }

        // ComboBoxItem_Player も移動
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
    }
}