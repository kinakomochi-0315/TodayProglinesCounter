using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TodayProglinesCounter;

public partial class GithubLogin : Window
{
    private const string GENERATE_TOKEN_URL =
        "https://github.com/settings/tokens/new?description=TodayProglinesCounter&scopes=repo";

    public GithubLogin()
    {
        InitializeComponent();

        // エラー時テキストを非表示
        ErrorLabel.IsVisible = false;
    }

    private void OnGenerateButtonClick(object? sender, RoutedEventArgs e)
    {
        var pInfo = new ProcessStartInfo
        {
            FileName = GENERATE_TOKEN_URL,
            UseShellExecute = true
        };

        Process.Start(pInfo);
    }

    private async void OnRegisterButtonClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TokenTextBox.Text)) return;

        // Registerボタンを無効化
        RegisterButton.IsEnabled = false;

        // Tokenの有効性を確認
        GithubApi.Token = TokenTextBox.Text;
        await GithubApi.TryAuthorizeAsync();

        if (GithubApi.IsAuthorized)
        {
            ErrorLabel.IsVisible = false;

            // トークンを保存
            GithubApi.SaveToken();

            Close();
        }
        else
        {
            ErrorLabel.IsVisible = true;
            RegisterButton.IsEnabled = true;
        }
    }
}
