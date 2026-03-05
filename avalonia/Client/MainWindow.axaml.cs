using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Client;

public partial class MainWindow : Window
{
    private static readonly HttpClient Http = new();
    private const string ApiBaseUrl = "http://localhost:8000";

    public MainWindow()
    {
        InitializeComponent();

        var healthButton = this.FindControl<Button>("HealthButton");
        var pingButton = this.FindControl<Button>("PingButton");

        healthButton.Click += async (_, _) => await CallEndpointAsync("/health");
        pingButton.Click += async (_, _) => await CallEndpointAsync("/erp/ping");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private async Task CallEndpointAsync(string path)
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        var url = ApiBaseUrl.TrimEnd('/') + path;

        statusText.Text = $"Calling {url}...";

        try
        {
            using var resp = await Http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            statusText.Text = $"{(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}";
        }
        catch (Exception ex)
        {
            statusText.Text = ex.Message;
        }
    }
}
