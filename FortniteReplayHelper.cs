using Fortnite_Replay_Parser_GUI.Services;
using Fortnite_Replay_Parser_GUI.Templates;
using FortniteReplayReader;
using FortniteReplayReader.Models;
using Scriban;
using Scriban.Runtime;
using System.IO;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Unreal.Core.Models.Enums;

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
        /// リプレイデータオブジェクトを取得します（JSON エクスポート用）。
        /// </summary>
        public FortniteReplay GetReplayData()
        {
            return this.fnReplayData;
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
                throw new IOException("リプレイデータのJSON保存中にエラーが発生しました。", ex);
            }
        }

        /// <summary>
        /// Fortnite API の SearchCosmeticsByIds を使い、与えられた cosmetics id から表示名を取得します。
        /// （簡易なパーシングを行い、見つからない場合は id を返します）
        /// </summary>
        public async Task<string> GetCosmeticsNameAsync(string cosmeticId, string language = "en")
        {
            if (string.IsNullOrEmpty(cosmeticId)) return "Unknown";

            try
            {
                using var http = new HttpClient() { BaseAddress = new Uri("https://fortnite-api.com/v2/") };
                var api = new FortniteApiClient(http, disposeHttpClient: true);
                var json = await api.SearchCosmeticsByIdsAsync(new List<string> { cosmeticId }, language);
                if (string.IsNullOrEmpty(json)) return cosmeticId;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                {
                    var item = data[0];
                    return item.GetProperty("name").GetString()??cosmeticId;
                }
            }
            catch
            {
                // APIやパースエラーは無視して id を返す（必要ならログを追加）
            }

            return cosmeticId;
        }

        /// <summary>
        /// Scribanテンプレートを使用して、マッチ結果をレンダリングし文字列として返します。
        /// </summary>
        public async Task<string> RenderMatchResultFromTemplate(PlayerData? player, int offset)
        {
            var replayData = this.fnReplayData;
            if (replayData == null || !replayData.GameData.UtcTimeStartedMatch.HasValue) return "";


            // 開始・終了時刻
            var start_time = replayData.GameData.UtcTimeStartedMatch.Value.ToLocalTime();
            var started_at = $"{start_time}";
            var ended_at = $"{start_time.AddMilliseconds(Convert.ToInt32(replayData.Info.LengthInMs))}";

            // duration を "MM:SS" フォーマットにする（分は合計分数を表示）
            var matchLength = TimeSpan.FromMilliseconds(replayData.Info.LengthInMs);
            var duration = $"{(int)matchLength.TotalMinutes:D2}:{matchLength.Seconds:D2}";


            // プレイヤー集計
            var playerData_except_NPCs = GetAllPlayersInReplay_Without_NPCs();
            var total_players = playerData_except_NPCs.Count();
            var human_players = playerData_except_NPCs.Count(o => !o.IsBot);
            var bot_players = total_players - human_players;

            // Cosmetics名は非同期で取得して await する
            var cosmeticsName = await GetCosmeticsNameAsync(player?.Cosmetics?.Character ?? "Unknown");

            // Scriban テンプレートを使用
            var template = Template.Parse(Template_MatchResult.MatchStatTemplate);

            var model = new
            {
                started_at = started_at,
                ended_at = ended_at,
                duration = duration,
                total_players = total_players,
                human_players = human_players,
                bot_players = bot_players,
                player_name = player == null ? "" : player.PlayerName,
                cosmetics_name = cosmeticsName,
                player_result = player == null ? "" : await RenderPlayerResultFromTemplate(player, offset),
                system_info = RenderSystemInfoFromTemplate()
            };

            return template.Render(model, member => member.Name);
        }

        /// <summary>
        /// Scribanテンプレートを使用して、指定プレイヤーの戦績結果をレンダリングし文字列として返します。
        /// </summary>
        public async Task<string> RenderPlayerResultFromTemplate(PlayerData player, int offset)
        {
            var replayData = this.fnReplayData;
            if (replayData == null || player == null || player.PlayerId == null) return "";

            // eliminations: プレイヤーが倒した相手
            var eliminations = await Task.WhenAll(
                replayData.Eliminations
                .Where(c => c.Eliminator == player.PlayerId.ToUpper())
                .Select(async (elim, idx) =>
                {
                    var killed = replayData.PlayerData.FirstOrDefault(d => d.PlayerId == elim.EliminatedInfo.Id.ToUpper());
                    // Cosmetics名は非同期で取得して await する
                    var cosmeticsName_killed = await GetCosmeticsNameAsync(killed?.Cosmetics?.Character ?? "Unknown", "ja");
                    return new
                    {
                        time = DateTime.ParseExact(elim.Time, "mm:ss", null).AddSeconds(offset).ToString("mm:ss"),
                        player_name = killed?.PlayerName ?? "Unknown",
                        cosmetics_name = cosmeticsName_killed,
                        is_bot = killed?.IsBot ?? false,
                        index = idx + 1
                    };
                })
                .ToList()
            );

            // eliminated: プレイヤーが倒された場合
            var eliminatedElim = replayData.Eliminations
                .Where(c => c.Eliminated == player.PlayerId.ToUpper())
                .FirstOrDefault();

            object? eliminated = null;
            if (eliminatedElim != null)
            {
                var eliminator = replayData.PlayerData.FirstOrDefault(d => d.PlayerId == eliminatedElim.EliminatorInfo.Id.ToUpper());
                var cosmeticsName_eliminator = await GetCosmeticsNameAsync(eliminator?.Cosmetics?.Character ?? "Unknown", "ja");
                eliminated = new
                {
                    time = DateTime.ParseExact(eliminatedElim.Time, "mm:ss", null).AddSeconds(offset).ToString("mm:ss"),
                    player_name = eliminator?.PlayerName ?? "Unknown",
                    cosmetics_name = cosmeticsName_eliminator,
                    is_bot = eliminator?.IsBot ?? false
                };
            }

            // Cosmetics名は非同期で取得して await する
            var cosmeticsName = await GetCosmeticsNameAsync(player?.Cosmetics?.Character ?? "Unknown", "ja");

            // Scriban テンプレート
            var template = Template.Parse(Template_MatchResult.PlayerResultTemplate);

            // FormNumber関数を Scriban に渡す
            var scriptObj = new Scriban.Runtime.ScriptObject();
            scriptObj.Import("fn_form_number", new Func<int, string>(FormNumber));

            // プロパティをcontextに追加
            scriptObj.SetValue("player_name", player.PlayerName, false);
            scriptObj.SetValue("cosmetics_name", cosmeticsName, false);
            scriptObj.SetValue("eliminations", eliminations, false);
            scriptObj.SetValue("elimination_count", eliminations.Length, false);
            scriptObj.SetValue("eliminated", eliminated, false);
            scriptObj.SetValue("placement", player.Placement, false);

            var context = new Scriban.TemplateContext();
            context.PushGlobal(scriptObj);

            return template.Render(context);
        }

        /// <summary>
        /// Template_SystemInfo.cs と SystemInfoHelper.cs を使って、Scriban でシステム情報をレンダリングして文字列で返します。
        /// </summary>
        public string RenderSystemInfoFromTemplate()
        {
            try
            {
                var template = Template.Parse(Template_MatchResult.SystemInfoTemplate);

                var model = new
                {
                    os = SystemInfoHelper.GetOS(),
                    cpu = SystemInfoHelper.GetCPU(),
                    memory = SystemInfoHelper.GetMemory(),
                    available_memory = SystemInfoHelper.GetAvailableMemory(),
                    gpu = SystemInfoHelper.GetGPU(),
                    resolution = SystemInfoHelper.GetResolution()
                };

                return template.Render(model, member => member.Name);
            }
            catch (Exception ex)
            {
                // 呼び出し側でログを取る想定。簡潔なエラーメッセージを返す。
                return $"システム情報のレンダリングに失敗しました: {ex.Message}";
            }
        }
    }
}
