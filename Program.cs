using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Fortnite_Replay_Parser_GUI;

var builder = WebApplication.CreateBuilder(args);

// SystemInfoHelper の初期化（PowerShell Get-ComputerInfo をバックグラウンドで実行）
SystemInfoHelper.InitializeAsync();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// --- セッション管理 ---
// アップロードされたリプレイデータをセッション ID で管理する
var sessions = new ConcurrentDictionary<string, ReplaySession>();

// POST /api/upload — .replay ファイルをアップロードし、プレイヤー一覧を返す
app.MapPost("/api/upload", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("replayFile");
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { error = "replayFile が指定されていません。" });
    }

    // アップロードされたファイルを一時ファイルに保存
    var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.replay");
    try
    {
        using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var helper = new FortniteReplayHelper(tempPath);
        var players = helper.GetAllPlayersInReplay()
            .OrderBy(p => p.PlayerName)
            .Select((p, idx) => new
            {
                index = idx,
                label = $"{p.PlayerName}: {p.PlayerId} - {(p.IsBot ? "bot" : "human")}",
                playerId = p.PlayerId,
                playerName = p.PlayerName,
                isBot = p.IsBot
            })
            .ToList();

        var sessionId = Guid.NewGuid().ToString("N");
        sessions[sessionId] = new ReplaySession(helper, tempPath);

        return Results.Ok(new { sessionId, players });
    }
    catch (Exception ex)
    {
        // 失敗した場合、一時ファイルを削除
        if (File.Exists(tempPath)) File.Delete(tempPath);
        return Results.BadRequest(new { error = $"リプレイファイルの読み込みに失敗しました: {ex.Message}" });
    }
});

// POST /api/result — 選択したプレイヤーとオフセットでマッチ結果を返す
app.MapPost("/api/result", async (ParseRequest req) =>
{
    if (!sessions.TryGetValue(req.SessionId, out var session))
    {
        return Results.NotFound(new { error = "セッションが見つかりません。リプレイファイルを再度アップロードしてください。" });
    }

    var players = session.Helper.GetAllPlayersInReplay()
        .OrderBy(p => p.PlayerName)
        .ToList();

    FortniteReplayReader.Models.PlayerData? selectedPlayer = null;
    if (req.PlayerIndex >= 0 && req.PlayerIndex < players.Count)
    {
        selectedPlayer = players[req.PlayerIndex];
    }

    var result = await session.Helper.RenderMatchResultFromTemplate(selectedPlayer, req.Offset);
    return Results.Ok(new { result });
});

// GET /api/export/{sessionId} — リプレイデータの JSON エクスポート（ダウンロード）
app.MapGet("/api/export/{sessionId}", (string sessionId) =>
{
    if (!sessions.TryGetValue(sessionId, out var session))
    {
        return Results.NotFound(new { error = "セッションが見つかりません。" });
    }

    var jsonOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = true
    };
    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(session.Helper.GetReplayData(), jsonOptions);

    return Results.File(jsonBytes, "application/json", "replay.json");
});

// DELETE /api/session/{sessionId} — セッションを削除してリソースを解放
app.MapDelete("/api/session/{sessionId}", (string sessionId) =>
{
    if (sessions.TryRemove(sessionId, out var session))
    {
        if (File.Exists(session.TempFilePath)) File.Delete(session.TempFilePath);
    }
    return Results.Ok();
});

app.Run($"http://localhost:12345");

// --- 内部型定義 ---

record ReplaySession(FortniteReplayHelper Helper, string TempFilePath);

record ParseRequest(string SessionId, int PlayerIndex, int Offset);
