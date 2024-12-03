using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace TodayProglinesCounter;

public partial class MainWindow : Window
{
    private const int UPDATE_INTERVAL_MS = 300 * 1000;

    private readonly GithubLogin _loginWindow = new();
    private int _proglines;

    private int Proglines
    {
        set
        {
            _proglines = value;
            LinesCountText.Text = _proglines == -1 ? "----" : _proglines.ToString("N0");
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        Proglines = -1;
    }

    private void ShowGithubLoginWindow()
    {
        if (!_loginWindow.IsActive)
        {
            _loginWindow.Show();
        }
    }


    private static async Task<int> GetProglinesAsync(DateTime date)
    {
        var commitUrls = await GithubApi.FetchDayAllCommitsAsync(date);
        var tasks = commitUrls.Select(GithubApi.FetchCommitChangeLineCountsAsync);

        var results = await Task.WhenAll(tasks);
        var proglines = results.Sum();

        return proglines;
    }

    private void SetDiffText(int diff)
    {
        LinesCountDiffText.Content = $"前日比: {diff:+0;-#}行";
        LinesCountDiffText.Foreground = diff >= 0 ? Brushes.MediumSeaGreen : Brushes.Crimson;
    }

    private async Task UpdateProglinesAsync()
    {
        await GithubApi.TryAuthorizeAsync();

        if (GithubApi.IsAuthorized == false)
        {
            ShowGithubLoginWindow();
            return;
        }
        
        var todayTask = GetProglinesAsync(DateTime.UtcNow.Date);
        var yesterdayTask = GetProglinesAsync(DateTime.UtcNow.Date.AddDays(-1));

        await Task.WhenAll(todayTask, yesterdayTask);

        var todayProglines = todayTask.Result;
        var yesterdayProglines = yesterdayTask.Result;

        Proglines = todayProglines;
        SetDiffText(todayProglines * 2 - yesterdayProglines);
    }

    private void UpdateProglinesTask()
    {
        UpdateProglinesAsync().ContinueWith(_ => Task.Delay(UPDATE_INTERVAL_MS).ContinueWith(_ => UpdateProglinesTask()));
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        // トークンを読み込み
        GithubApi.LoadToken();
        await GithubApi.TryAuthorizeAsync();

        // トークンが読み込めなかった場合、ログイン画面を表示
        if (GithubApi.IsAuthorized == false)
        {
            ShowGithubLoginWindow();
        }

        UpdateProglinesTask();
    }

    private void ReloadButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = UpdateProglinesAsync();
    }
}
