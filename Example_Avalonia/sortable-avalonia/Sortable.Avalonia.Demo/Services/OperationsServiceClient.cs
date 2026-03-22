using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sortable.Avalonia.Demo.Services;

public sealed class OperationsServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public OperationsServiceClient(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required", nameof(baseUrl));

        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            baseUrl += "/";

        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<KanbanBoardDto> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        var board = await _http.GetFromJsonAsync<KanbanBoardDto>("kanban/board", JsonOptions, cancellationToken);
        return board ?? throw new InvalidOperationException("Empty response from operations-service");
    }

    public async Task<TransitionResultDto> TransitionTaskAsync(string taskName, string toStatus, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(taskName))
            throw new ArgumentException("Task name is required", nameof(taskName));
        if (string.IsNullOrWhiteSpace(toStatus))
            throw new ArgumentException("To status is required", nameof(toStatus));

        var payload = new TransitionRequestDto { ToStatus = toStatus };

        using var response = await _http.PostAsJsonAsync($"tasks/{Uri.EscapeDataString(taskName)}/transition", payload, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TransitionResultDto>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Empty transition response from operations-service");
    }
}

public sealed record KanbanBoardDto(
    [property: JsonPropertyName("columns")] List<KanbanColumnDto> Columns,
    [property: JsonPropertyName("tasks_by_status")] Dictionary<string, List<TaskSummaryDto>> TasksByStatus
);

public sealed record KanbanColumnDto(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("title")] string Title
);

public sealed record TaskSummaryDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("subject")] string? Subject,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("modified")] DateTimeOffset? Modified
);

public sealed class TransitionRequestDto
{
    [JsonPropertyName("to_status")]
    public string ToStatus { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public sealed record TransitionResultDto(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("from_status")] string FromStatus,
    [property: JsonPropertyName("to_status")] string ToStatus
);
