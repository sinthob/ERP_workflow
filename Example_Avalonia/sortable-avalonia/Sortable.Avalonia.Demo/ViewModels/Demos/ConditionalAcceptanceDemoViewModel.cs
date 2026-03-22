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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sortable.Avalonia.Demo.Models;

namespace Sortable.Avalonia.Demo.ViewModels.Demos;

public partial class ConditionalAcceptanceDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _conditionalSourceItems =
    [
        new SortableItem("URGENT: Payment gateway down")      { Tag = "URGENT", Note = "P0 · Production · SLA breached" },
        new SortableItem("Normal: Update API documentation")  { Tag = "NORMAL", Note = "P3 · Docs team · No rush" },
        new SortableItem("URGENT: Data sync delay over SLA")  { Tag = "URGENT", Note = "P0 · EU region · 3 customers affected" },
        new SortableItem("Normal: Refactor report endpoint")  { Tag = "NORMAL", Note = "P3 · Tech debt · Backlog" },
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _conditionalTargetItems =
    [
        new SortableItem("URGENT: Existing production fire") { Tag = "URGENT", Note = "Active incident · War-room open" },
    ];

    [RelayCommand]
    private void OnConditionalItemDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem item) return;

        var movingIntoHotfix = ReferenceEquals(e.TargetCollection, ConditionalTargetItems);
        if (movingIntoHotfix && !item.Name.StartsWith("URGENT:", StringComparison.OrdinalIgnoreCase))
        {
            e.IsAccepted = false;
            LogEvent("🚫", $"'{item.Name}' rejected — not URGENT");
            return;
        }

        e.IsAccepted = true;
        e.TransferMode = SortableTransferMode.Move;
        if (TryApplyDropMutation(e))
            LogEvent(movingIntoHotfix ? "🔥" : "📥",
                $"'{item.Name}' moved to {(movingIntoHotfix ? "Hotfix Queue" : "Inbox")}");
    }
}
