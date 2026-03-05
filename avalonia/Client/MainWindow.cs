using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Client;

public sealed class MainWindow : Window
{
    private static readonly HttpClient Http = new();
    private const string ApiBaseUrl = "http://localhost:8000";

    private readonly TextBlock _statusText;

    public MainWindow()
    {
        Width = 520;
        Height = 240;
        Title = "ERP Workflow Client";

        var title = new TextBlock
        {
            FontSize = 18,
            Text = "ERP Workflow Client",
        };

        var hint = new TextBlock
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Text = "FastAPI Base URL (แก้ได้ในโค้ด): http://localhost:8000",
        };

        var healthButton = new Button { Content = "Check /health", Width = 140 };
        var pingButton = new Button { Content = "Ping ERP (/erp/ping)", Width = 170 };
        healthButton.Click += async (_, _) => await CallEndpointAsync("/health");
        pingButton.Click += async (_, _) => await CallEndpointAsync("/erp/ping");

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { healthButton, pingButton },
        };

        _statusText = new TextBlock
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };

        Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12,
            Children = { title, hint, buttons, _statusText },
        };
    }

    private async Task CallEndpointAsync(string path)
    {
        var url = ApiBaseUrl.TrimEnd('/') + path;
        _statusText.Text = $"Calling {url}...";

        try
        {
            using var resp = await Http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            _statusText.Text = $"{(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}";
        }
        catch (Exception ex)
        {
            _statusText.Text = ex.Message;
        }
    }
}
