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

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sortable.Avalonia.Demo.Models;

namespace Sortable.Avalonia.Demo.ViewModels.Demos;

public partial class CopyModeDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _copySourceItems =
    [
        new SortableItem("Welcome Onboarding")     { Tag = "EMAIL", Note = "New user first-day journey" },
        new SortableItem("Subscription Renewal")   { Tag = "EMAIL", Note = "30-day advance notice" },
        new SortableItem("Incident Resolved")      { Tag = "EMAIL", Note = "Post-incident customer summary" },
        new SortableItem("Survey Request")         { Tag = "SMS",   Note = "CSAT feedback collection" },
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _copyTargetItems =
    [
        new SortableItem("April Retention Campaign") { Tag = "DRAFT", Note = "Started 3 days ago" },
    ];

    [RelayCommand]
    private void OnCopyModeItemDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem item) return;
        e.IsAccepted = true;
        e.TransferMode = SortableTransferMode.Copy;
        if (TryApplyDropMutation(e))
            LogEvent("📋", $"Template '{item.Name}' copied to campaign draft");
    }
}
