using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TodayProglinesCounter;

public partial class MainWindow : Window
{
    private int _proglines;

    private int Proglines
    {
        get => _proglines;
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

    private async Task<int> GetProglinesAsync()
    {
        if (GithubApi.IsAuthorized == false) return -1;

        var commitUrls = await GithubApi.FetchDayAllCommitsAsync(DateTime.UtcNow.Date);
        var tasks = commitUrls.Select(GithubApi.FetchCommitChangeLineCountsAsync);

        var results = await Task.WhenAll(tasks);
        var proglines = results.Sum();

        return proglines;
    }

    private async void UpdateProglinesAsync()
    {
        while (true)
        {
            try
            {
                Proglines = await GetProglinesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            await Task.Delay(10000);
        }
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        // トークンを読み込み
        GithubApi.LoadToken();
        await GithubApi.TryAuthorizeAsync();

        // トークンが読み込めなかった場合、ログイン画面を表示
        if (GithubApi.IsAuthorized == false)
        {
            var loginWindow = new GithubLogin();
            loginWindow.Show();
        }

        UpdateProglinesAsync();
    }
}
