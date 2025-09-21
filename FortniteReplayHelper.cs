using FortniteReplayReader;
using FortniteReplayReader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Unreal.Core.Models;
using Unreal.Core.Models.Enums;
using Scriban;
using System.Management;
using Fortnite_Replay_Parser_GUI.Templates;
using Scriban.Runtime;

namespace Fortnite_Replay_Parser_GUI
{
    public class FortniteReplayHelper
    {

        // Looking at player data, NPC has TeamIndex 2 and players have 3 or more.
        const int MINIMUM_TEAM_INDEX_FOR_PLAYERS = 3;

        private FortniteReplayReader.Models.FortniteReplay fnReplayData;


        /// <summary>
        /// 指定したリプレイファイルパスからリプレイデータを読み込み、fnReplayDataに格納します。
        /// </summary>
        public FortniteReplayHelper(string fnReplayFilePath)
        {
            var reader = new ReplayReader(parseMode: ParseMode.Full);
            this.fnReplayData = reader.ReadReplay(fnReplayFilePath);
        }

        /// <summary>
        /// 数値を順位表記（1st, 2nd, 3rd, ...）の文字列に変換します。
        /// </summary>
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

        /// <summary>
        /// NPCを除外した全プレイヤーのリストを取得します。
        /// </summary>
        public IEnumerable<PlayerData> GetAllPlayersInReplay()
        {
            return GetAllPlayersInReplay_Without_NPCs();
        }

        /// <summary>
        /// NPCを除外した全プレイヤーのリストを取得します（内部用）。
        /// </summary>
        private IEnumerable<PlayerData> GetAllPlayersInReplay_Without_NPCs()
        {
            // Parse Replay File and store it to local member.
            return this.fnReplayData.PlayerData.Where(o => o.TeamIndex >= MINIMUM_TEAM_INDEX_FOR_PLAYERS);
        }


        /// <summary>
        /// 指定したプレイヤーのマッチデータ（戦績や統計情報）を文字列として取得します。
        /// </summary>
        public string GetMatchData(PlayerData player, int offset)
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
                if (player == null)
                {
                    ret = $"======== Game Stats =========\n{match_date_time}\n{players_total}\n{players_counts}";
                    return ret;
                }

                // Player exists. continue to parse player data.
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


        /// <summary>
        /// リプレイデータをJSON形式で保存します。
        /// </summary>
        public void SaveReplayAsJSON(string replayData_json_path)
        {
            if (string.IsNullOrEmpty(replayData_json_path))
            {
                return;
            }

            try
            {
                using (var sw = new StreamWriter(replayData_json_path, false, System.Text.Encoding.UTF8))
                {
                    var json_options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
                        WriteIndented = true
                    };
                    var jsonString = JsonSerializer.Serialize(this.fnReplayData, json_options);

                    // JSON データをファイルに書き込み
                    sw.Write(jsonString);
                }
            }
            catch (Exception ex)
            {
                // 必要に応じてログ出力や例外の再スローを行う
                // 例: Console.WriteLine($"JSON保存エラー: {ex.Message}");
                throw new IOException("リプレイデータのJSON保存中にエラーが発生しました。", ex);
            }
        }

        /// <summary>
        /// ComboBox用のプレイヤー選択アイテムを表します。
        /// </summary>
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

        /// <summary>
        /// Scribanテンプレートを使用して、マッチ結果をレンダリングし文字列として返します。
        /// </summary>
        public string RenderMatchResultFromTemplate(PlayerData player, int offset)
        {
            var replayData = this.fnReplayData;
            if (replayData == null || !replayData.GameData.UtcTimeStartedMatch.HasValue) return "";


            // 開始・終了時刻
            var start_time = replayData.GameData.UtcTimeStartedMatch.Value.ToLocalTime();
            var started_at = $"{start_time}";
            var ended_at = $"{start_time.AddMilliseconds(Convert.ToInt32(replayData.Info.LengthInMs))}";
            var duration = $"{replayData.Info.LengthInMs / 1000 / 60}:{replayData.Info.LengthInMs / 1000 % 60}";


            // プレイヤー集計
            var playerData_except_NPCs = GetAllPlayersInReplay_Without_NPCs();
            var total_players = playerData_except_NPCs.Count();
            var human_players = playerData_except_NPCs.Count(o => !o.IsBot);
            var bot_players = total_players - human_players;

            // Scriban テンプレートを使用
            var template = Template.Parse(MatchResult.MatchStatTemplate);

            var model = new
            {
                started_at = started_at,
                ended_at = ended_at,
                duration = duration,
                total_players = total_players,
                human_players = human_players,
                bot_players = bot_players,
                player_name = player == null ? "" : player.PlayerName,
                // game_result = game_result,
                player_result = player == null ? "": RenderPlayerResultFromTemplate(player, offset),
                system_info = SystemInfoHelper.GetSystemInfoText()
            };

            return template.Render(model, member => member.Name);
        }

        /// <summary>
        /// Scribanテンプレートを使用して、指定プレイヤーの戦績結果をレンダリングし文字列として返します。
        /// </summary>
        public string RenderPlayerResultFromTemplate(PlayerData player, int offset)
        {
            var replayData = this.fnReplayData;
            if (replayData == null || player == null) return "";

            // eliminations: プレイヤーが倒した相手
            var eliminations = replayData.Eliminations
                .Where(c => c.Eliminator == player.PlayerId.ToUpper())
                .Select((elim, idx) =>
                {
                    var killed = replayData.PlayerData.FirstOrDefault(d => d.PlayerId == elim.EliminatedInfo.Id.ToUpper());
                    return new
                    {
                        time = DateTime.ParseExact(elim.Time, "mm:ss", null).AddSeconds(offset).ToString("mm:ss"),
                        player_name = killed?.PlayerName ?? "Unknown",
                        is_bot = killed?.IsBot ?? false,
                        index = idx + 1
                    };
                }).ToList();

            // eliminated: プレイヤーが倒された場合
            var eliminated = replayData.Eliminations
                .Where(c => c.Eliminated == player.PlayerId.ToUpper())
                .Select(elim =>
                {
                    var eliminator = replayData.PlayerData.FirstOrDefault(d => d.PlayerId == elim.EliminatorInfo.Id.ToUpper());
                    return new
                    {
                        time = DateTime.ParseExact(elim.Time, "mm:ss", null).AddSeconds(offset).ToString("mm:ss"),
                        player_name = eliminator?.PlayerName ?? "Unknown",
                        is_bot = eliminator?.IsBot ?? false
                    };
                }).FirstOrDefault();

            // Scriban テンプレート
            var template = Template.Parse(MatchResult.PlayerResultTemplate);

            // FormNumber関数を Scriban に渡す
            var scriptObj = new Scriban.Runtime.ScriptObject();
            scriptObj.Import("fn_form_number", new Func<int, string>(FormNumber));

            // プロパティをcontextに追加
            scriptObj.SetValue("player_name", player.PlayerName, false);
            scriptObj.SetValue("eliminations", eliminations, false);
            scriptObj.SetValue("eliminated", eliminated, false);

            var context = new Scriban.TemplateContext();
            context.PushGlobal(scriptObj);

            return template.Render(context);
        }
    }
}