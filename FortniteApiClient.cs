using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fortnite_Replay_Parser_GUI.Services
{
    /// <summary>
    /// Fortnite API への簡易 HTTP GET ヘルパー。
    /// - /v2/cosmetics?language={language}
    /// - /v2/cosmetics/br/search/ids?language={language}&id={id}&id={id}...
    /// </summary>
    public class FortniteApiClient : IDisposable
    {
        private static readonly Uri BaseUri = new Uri("https://fortnite-api.com/v2/");
        private static readonly HttpClient DefaultClient = CreateDefaultHttpClient();

        private readonly HttpClient _httpClient;
        private readonly bool _disposeHttpClient;
        private bool _disposed;

        public FortniteApiClient() : this(DefaultClient, disposeHttpClient: false) { }

        // テスト用に HttpClient を注入できるようにオーバーロードを追加
        public FortniteApiClient(HttpClient httpClient, bool disposeHttpClient = false)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeHttpClient = disposeHttpClient;
        }

        private static HttpClient CreateDefaultHttpClient()
        {
            var client = new HttpClient
            {
                BaseAddress = BaseUri,
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<string> GetCosmeticsRawAsync(string language, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(language)) throw new ArgumentException("language を指定してください。", nameof(language));

            var query = $"cosmetics?language={Uri.EscapeDataString(language)}";
            using var resp = await _httpClient.GetAsync(query, cancellationToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<T?> GetCosmeticsAsync<T>(string language, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var json = await GetCosmeticsRawAsync(language, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, options);
        }

        // ids を List<string> で受け取るように変更
        public async Task<string> SearchCosmeticsByIdsAsync(List<string> ids, string language, CancellationToken cancellationToken = default)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            var idList = ids.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            if (idList.Count == 0) throw new ArgumentException("少なくとも1つの id を指定してください。", nameof(ids));
            if (string.IsNullOrWhiteSpace(language)) throw new ArgumentException("language を指定してください。", nameof(language));

            var sb = new StringBuilder();
            sb.Append("cosmetics/br/search/ids?");
            sb.Append("language=").Append(Uri.EscapeDataString(language));
            foreach (var id in idList)
            {
                sb.Append("&id=").Append(Uri.EscapeDataString(id));
            }

            var requestUri = sb.ToString();
            using var resp = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        // ジェネリック版も List<string> に合わせて変更
        public async Task<T?> SearchCosmeticsByIdsAsync<T>(List<string> ids, string language, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var json = await SearchCosmeticsByIdsAsync(ids, language, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_disposeHttpClient)
            {
                _httpClient.Dispose();
            }
            _disposed = true;
        }
    }
}