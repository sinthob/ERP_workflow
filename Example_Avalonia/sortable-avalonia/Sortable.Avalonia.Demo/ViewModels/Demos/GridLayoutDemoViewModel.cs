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

public partial class GridLayoutDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _gridItems =
    [
        new SortableItem("Analytics")     { Tag = "📊", Note = "7 dashboards" },
        new SortableItem("Inbox")         { Tag = "📧", Note = "12 unread" },
        new SortableItem("Calendar")      { Tag = "📅", Note = "3 upcoming" },
        new SortableItem("Team")          { Tag = "👥", Note = "18 online" },
        new SortableItem("Files")         { Tag = "📁", Note = "1.2 GB used" },
        new SortableItem("Notifications") { Tag = "🔔", Note = "5 new alerts" },
        new SortableItem("Billing")       { Tag = "💳", Note = "$4,200 MTD" },
        new SortableItem("Security")      { Tag = "🔒", Note = "2 open issues" },
        new SortableItem("Integrations")  { Tag = "🔗", Note = "8 connected" },
        new SortableItem("Reports")       { Tag = "📈", Note = "Updated today" },
        new SortableItem("Settings")      { Tag = "⚙",  Note = "4 pending" },
        new SortableItem("Activity")      { Tag = "⚡", Note = "Live feed" },
    ];

    [RelayCommand]
    private void OnSortGridItemsProgrammatically()
    {
        if (GridItems.Count > 0)
            LogEvent("⬆", $"Moved '{GridItems[^1].Name}' widget to first position");
        RotateLastItemToFront(GridItems);
    }

    [RelayCommand]
    private void OnRandomizeGridItems()
    {
        LogEvent("🔀", "Dashboard layout shuffled");
        ShuffleCollection(GridItems);
    }
}
