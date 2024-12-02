using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TodayProglinesCounter;

public static class GithubApi
{
    private const string GITHUB_APP_NAME = "TodayProglinesCounter";

    private static readonly HttpClient _httpClient = new();

    private static readonly string _tokenFilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "token.txt");

    private static string _username = string.Empty;

    public static string Token { get; set; } = string.Empty;
    public static bool IsAuthorized { get; private set; }

    public static async Task TryAuthorizeAsync()
    {
        if (string.IsNullOrWhiteSpace(Token)) return;

        // GitHub APIを使用してトークンの有効性を確認
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Add("User-Agent", GITHUB_APP_NAME);
        request.Headers.Add("Authorization", $"token {Token}");

        Console.WriteLine("トークンの有効性を確認中...");

        var result = await _httpClient.SendAsync(request);

        Console.WriteLine($"トークンの有効性を確認: {result.StatusCode}");

        IsAuthorized = result.IsSuccessStatusCode;

        if (IsAuthorized)
        {
            var json = await result.Content.ReadAsStringAsync();
            dynamic user = JObject.Parse(json);
            _username = user.login;

            Console.WriteLine($"ユーザー名: {_username}");
        }
    }

    public static void SaveToken()
    {
        if (IsAuthorized == false) return;

        // トークンを保存
        File.WriteAllText(_tokenFilePath, Token);

        Console.WriteLine($"{_tokenFilePath}にトークンを保存しました: {Token}");

        // パーミッションを自身以外が読めないように変更
        File.SetAttributes(_tokenFilePath, File.GetAttributes(_tokenFilePath) | FileAttributes.Hidden);
    }

    public static void LoadToken()
    {
        if (File.Exists(_tokenFilePath) == false) return;

        // トークンを読み込み
        var fileToken = File.ReadAllText(_tokenFilePath);

        Console.WriteLine($"{_tokenFilePath}からトークンを読み込みました: {fileToken}");

        // トークンが変更されている場合、認証フラグをリセット
        if (Token != fileToken) IsAuthorized = false;

        Token = fileToken;
    }

    public static async Task<string[]> FetchDayAllCommitsAsync(DateTime day)
    {
        if (IsAuthorized == false || string.IsNullOrEmpty(_username)) return [];

        // GitHub APIを使用して今日のコミット数を取得
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/users/{_username}/events");
        request.Headers.Add("User-Agent", GITHUB_APP_NAME);
        request.Headers.Add("Authorization", $"token {Token}");

        Console.WriteLine("今日のコミット数を取得中...");

        var result = await _httpClient.SendAsync(request);
        var json = await result.Content.ReadAsStringAsync();
        var events = JArray.Parse(json);

        var pushEvents = events
            .Where(e => e["type"]?.ToString() == "PushEvent")
            // githubのevent APIは最大6時間の遅延があるため、今日と昨日の18時までのイベントを取得
            // 6時間ってデカくないですか
            .Where(e => e["created_at"]?.Value<DateTime>() >= day.Date.AddHours(-6));

        var commitUrls =
            (from pushEvent in pushEvents
                from commit in pushEvent["payload"]?["commits"]
                select commit["url"]?.ToString()).ToList();

        Console.WriteLine($"今日のコミット数を取得: {commitUrls.Count}");

        return commitUrls.ToArray();
    }

    public static async Task<int> FetchCommitChangeLineCountsAsync(string commitUrl)
    {
        if (IsAuthorized == false) return 0;

        // GitHub APIを使用してコミットの変更行数を取得
        using var request = new HttpRequestMessage(HttpMethod.Get, commitUrl);
        request.Headers.Add("User-Agent", GITHUB_APP_NAME);
        request.Headers.Add("Authorization", $"token {Token}");

        Console.WriteLine($"コミットの変更行数を取得中: {commitUrl}");

        var result = await _httpClient.SendAsync(request);
        var json = await result.Content.ReadAsStringAsync();
        var commit = JObject.Parse(json);

        var changes = commit["files"]?.Select(f => f["changes"]?.Value<int>() ?? 0).Sum() ?? -1;

        Console.WriteLine($"コミットの変更行数を取得: {changes}");

        return changes;
    }
}
