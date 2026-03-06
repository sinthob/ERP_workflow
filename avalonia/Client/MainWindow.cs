using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Client;

public sealed class MainWindow : Window
{
    private static readonly HttpClient Http = new();
    private const string ApiBaseUrl = "http://localhost:8003";

    private readonly TextBlock _statusText;

    private readonly ObservableCollection<TaskItem> _openTasks = new();
    private readonly ObservableCollection<TaskItem> _workingTasks = new();
    private readonly ObservableCollection<TaskItem> _pendingReviewTasks = new();
    private readonly ObservableCollection<TaskItem> _completedTasks = new();

    private readonly ObservableCollection<TaskItem> _timelineTasks = new();

    private readonly Dictionary<string, ObservableCollection<TaskItem>> _tasksByStatus;

    public MainWindow()
    {
        Width = 520;
        Height = 520;
        Title = "ERP Operations (Team 3)";

        _tasksByStatus = new Dictionary<string, ObservableCollection<TaskItem>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Open"] = _openTasks,
            ["Working"] = _workingTasks,
            ["Pending Review"] = _pendingReviewTasks,
            ["Completed"] = _completedTasks,
        };

        var title = new TextBlock
        {
            FontSize = 18,
            Text = "ERP Operations (Team 3)",
        };

        var hint = new TextBlock
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Text = "operations-service Base URL (แก้ได้ในโค้ด): http://localhost:8003",
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

        var diagnosticsTab = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12,
            Children = { title, hint, buttons, _statusText },
        };

        var kanbanTab = BuildKanbanTab();
        var timelineTab = BuildTimelineTab();

        Content = new TabControl
        {
            Margin = new Avalonia.Thickness(0),
            Items =
            {
                new TabItem { Header = "Diagnostics", Content = diagnosticsTab },
                new TabItem { Header = "Kanban", Content = kanbanTab },
                new TabItem { Header = "Timeline", Content = timelineTab },
            },
        };
    }

    private Control BuildKanbanTab()
    {
        var refreshButton = new Button { Content = "Refresh Board", Width = 140 };
        refreshButton.Click += async (_, _) => await RefreshBoardAsync();

        var topBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                refreshButton,
                new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.Gray,
                    Text = "Columns: Open → Working → Pending Review → Completed",
                },
            },
        };

        var openPanel = BuildStatusPanel("Open", _openTasks, "Working");
        var workingPanel = BuildStatusPanel("Working", _workingTasks, "Pending Review");
        var pendingPanel = BuildStatusPanel("Pending Review", _pendingReviewTasks, "Completed");
        var completedPanel = BuildStatusPanel("Completed", _completedTasks, null);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,*"),
            Margin = new Avalonia.Thickness(16),
        };

        Grid.SetColumnSpan(topBar, 4);
        Grid.SetRow(topBar, 0);
        grid.Children.Add(topBar);

        Grid.SetRow(openPanel, 1);
        Grid.SetColumn(openPanel, 0);
        grid.Children.Add(openPanel);

        Grid.SetRow(workingPanel, 1);
        Grid.SetColumn(workingPanel, 1);
        grid.Children.Add(workingPanel);

        Grid.SetRow(pendingPanel, 1);
        Grid.SetColumn(pendingPanel, 2);
        grid.Children.Add(pendingPanel);

        Grid.SetRow(completedPanel, 1);
        Grid.SetColumn(completedPanel, 3);
        grid.Children.Add(completedPanel);

        return grid;
    }

    private Control BuildStatusPanel(string status, ObservableCollection<TaskItem> tasks, string? nextStatus)
    {
        var list = new ListBox
        {
            ItemsSource = tasks,
            Height = 360,
        };

        var moveButton = new Button
        {
            Content = nextStatus is null ? "No Next" : $"Move → {nextStatus}",
            IsEnabled = nextStatus is not null,
        };

        moveButton.Click += async (_, _) =>
        {
            if (nextStatus is null)
                return;

            if (list.SelectedItem is not TaskItem selected)
                return;

            await TransitionTaskAsync(selected.Name, nextStatus);
            await RefreshBoardAsync();
        };

        return new StackPanel
        {
            Spacing = 8,
            Margin = new Avalonia.Thickness(8, 12, 8, 0),
            Children =
            {
                new TextBlock { Text = status, FontSize = 14 },
                list,
                moveButton,
            },
        };
    }

    private Control BuildTimelineTab()
    {
        var refreshButton = new Button { Content = "Refresh Timeline", Width = 140 };
        refreshButton.Click += async (_, _) => await RefreshTimelineAsync();

        var list = new ListBox
        {
            ItemsSource = _timelineTasks,
            Height = 420,
        };

        return new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Task Timeline (sorted by modified)", FontSize = 14 },
                refreshButton,
                list,
            },
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

    private async Task RefreshBoardAsync()
    {
        _statusText.Text = "Loading kanban board...";

        try
        {
            var url = ApiBaseUrl.TrimEnd('/') + "/kanban/board";
            var board = await Http.GetFromJsonAsync<KanbanBoardDto>(url);

            ClearAllBoardCollections();

            if (board?.TasksByStatus is not null)
            {
                foreach (var (status, tasks) in board.TasksByStatus)
                {
                    if (!_tasksByStatus.TryGetValue(status, out var target))
                        continue;

                    if (tasks is null)
                        continue;

                    foreach (var t in tasks)
                    {
                        target.Add(new TaskItem(t.Name ?? "", t.Subject, t.Status ?? status, t.Modified));
                    }
                }
            }

            _statusText.Text = "Kanban refreshed.";
        }
        catch (Exception ex)
        {
            _statusText.Text = ex.Message;
        }
    }

    private void ClearAllBoardCollections()
    {
        _openTasks.Clear();
        _workingTasks.Clear();
        _pendingReviewTasks.Clear();
        _completedTasks.Clear();
    }

    private async Task TransitionTaskAsync(string taskName, string toStatus)
    {
        _statusText.Text = $"Transitioning {taskName} -> {toStatus}...";

        var url = ApiBaseUrl.TrimEnd('/') + $"/tasks/{Uri.EscapeDataString(taskName)}/transition";
        var body = JsonSerializer.Serialize(new { to_status = toStatus });

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var resp = await Http.PostAsync(url, content);
        var text = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _statusText.Text = $"{(int)resp.StatusCode} {resp.ReasonPhrase}\n{text}";
            return;
        }

        _statusText.Text = "Transition applied.";
    }

    private async Task RefreshTimelineAsync()
    {
        _statusText.Text = "Loading timeline...";

        try
        {
            var url = ApiBaseUrl.TrimEnd('/') + "/timeline/tasks";
            var items = await Http.GetFromJsonAsync<List<TaskDto>>(url);
            _timelineTasks.Clear();

            if (items is not null)
            {
                foreach (var t in items)
                {
                    _timelineTasks.Add(new TaskItem(t.Name ?? "", t.Subject, t.Status ?? "", t.Modified));
                }
            }

            _statusText.Text = "Timeline refreshed.";
        }
        catch (Exception ex)
        {
            _statusText.Text = ex.Message;
        }
    }

    private sealed record TaskItem(string Name, string? Subject, string Status, DateTimeOffset? Modified)
    {
        public override string ToString()
        {
            var subject = string.IsNullOrWhiteSpace(Subject) ? "(no subject)" : Subject;
            var modified = Modified is null ? "" : $"[{Modified:yyyy-MM-dd HH:mm}] ";
            return $"{modified}{Name} | {Status} | {subject}";
        }
    }

    private sealed class KanbanBoardDto
    {
        [JsonPropertyName("columns")]
        public List<KanbanColumnDto>? Columns { get; set; }

        [JsonPropertyName("tasks_by_status")]
        public Dictionary<string, List<TaskDto>?>? TasksByStatus { get; set; }
    }

    private sealed class KanbanColumnDto
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    private sealed class TaskDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("modified")]
        public DateTimeOffset? Modified { get; set; }
    }
}
