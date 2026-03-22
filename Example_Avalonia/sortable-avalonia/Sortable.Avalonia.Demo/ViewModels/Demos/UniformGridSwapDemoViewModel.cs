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

public partial class UniformGridSwapDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _gridSwapItems =
    [
        new SortableItem("Auth Service")         { Tag = "🔑", Note = "Handles SSO / OAuth" },
        new SortableItem("Payment API")          { Tag = "💳", Note = "Stripe / billing gateway" },
        new SortableItem("Search Engine")        { Tag = "🔍", Note = "Full-text · Elasticsearch" },
        new SortableItem("Notification Worker")  { Tag = "🔔", Note = "Email, SMS & push" },
        new SortableItem("Data Warehouse")       { Tag = "🗄",  Note = "Analytics pipeline" },
        new SortableItem("Feature Flags")        { Tag = "🚩", Note = "Runtime config service" },
        new SortableItem("File Storage")         { Tag = "📁", Note = "S3-compatible blob store" },
        new SortableItem("Admin Portal")         { Tag = "🖥",  Note = "Internal back-office" },
    ];

    [RelayCommand]
    private void OnSwapGridItems()
    {
        if (GridSwapItems.Count >= 2)
            LogEvent("🔄", $"Swapped '{GridSwapItems[0].Name}' ↔ '{GridSwapItems[^1].Name}' (programmatic)");
        SwapFirstAndLast(GridSwapItems);
    }

    [RelayCommand]
    private void OnRandomizeGridSwap()
    {
        LogEvent("🔀", "Service grid randomized");
        SwapRandomItems(GridSwapItems);
    }
}
