// MIT License
// 
// Copyright (c) 2026 Russell Camo (russkyc)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sortable.Avalonia.Demo.Models;
using Sortable.Avalonia.Demo.Services;

namespace Sortable.Avalonia.Demo.ViewModels.Demos;

public partial class KanbanBoardDemoViewModel : DemoViewModelBase
{
    private readonly OperationsServiceClient _ops;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    [ObservableProperty]
    private ObservableCollection<KanbanColumn> _columns = new();

    public KanbanBoardDemoViewModel()
    {
        var baseUrl = Environment.GetEnvironmentVariable("OPERATIONS_BASE_URL") ?? "http://localhost:8003";
        _ops = new OperationsServiceClient(baseUrl);

        _ = LoadBoardAsync();
    }

    private static string StatusEmoji(string status) => status switch
    {
        "Open" => "🟦",
        "Working" => "🟨",
        "Pending Review" => "🟪",
        "Completed" => "🟩",
        "Cancelled" => "⬛",
        _ => "📌"
    };

    private static SortableItem ToSortableItem(TaskSummaryDto t)
    {
        var display = string.IsNullOrWhiteSpace(t.Subject) ? t.Name : t.Subject;
        var item = new SortableItem(display)
        {
            TaskName = t.Name,
            Tag = StatusEmoji(t.Status),
            Note = t.Name
        };

        return item;
    }

    private async Task LoadBoardAsync(CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            var board = await _ops.GetBoardAsync(cancellationToken);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Columns.Clear();

                foreach (var col in board.Columns)
                {
                    var column = new KanbanColumn(col.Key, col.Title);

                    if (board.TasksByStatus.TryGetValue(col.Key, out var tasks))
                    {
                        foreach (var t in tasks)
                            column.Items.Add(ToSortableItem(t));
                    }

                    Columns.Add(column);
                }
            });

            LogEvent("✅", "Loaded board from operations-service");
        }
        catch (Exception ex)
        {
            LogEvent("⚠", $"Failed to load board: {ex.Message}");
        }
        finally
        {
            _loadLock.Release();
        }
    }

    [RelayCommand]
    private async Task OnItemDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem item) return;

        KanbanColumn? sourceColumn = null;
        KanbanColumn? targetColumn = null;

        foreach (var column in Columns)
        {
            if (ReferenceEquals(column.Items, e.SourceCollection)) sourceColumn = column;
            if (ReferenceEquals(column.Items, e.TargetCollection)) targetColumn = column;
        }

        if (sourceColumn is null || targetColumn is null) return;

        if (ReferenceEquals(sourceColumn, targetColumn))
        {
            e.IsAccepted = true;
            e.TransferMode = SortableTransferMode.Move;
            _ = TryApplyDropMutation(e);
        }
        else
        {
            if (targetColumn.Items.Contains(item)) { e.IsAccepted = false; return; }
            e.IsAccepted = true;
            e.TransferMode = SortableTransferMode.Move;
            if (!TryApplyDropMutation(e)) return;

            LogEvent("📋", $"'{item.Name}' → '{targetColumn.Title}'");

            try
            {
                var result = await _ops.TransitionTaskAsync(item.TaskName, targetColumn.Key);
                if (result.Ok)
                    LogEvent("🔁", $"Transitioned: {result.FromStatus} → {result.ToStatus}");
            }
            catch (Exception ex)
            {
                LogEvent("⚠", $"Transition failed: {ex.Message}");
            }

            await LoadBoardAsync();
        }
    }
}
