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
using Sortable.Avalonia.Demo.Models;

namespace Sortable.Avalonia.Demo.ViewModels.Demos;

public partial class SortableOnlyDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _simpleList =
    [
        new SortableItem("Product backlog grooming")     { Tag = "📋", Note = "Product · Wednesday 10am" },
        new SortableItem("Customer interview synthesis") { Tag = "🗣", Note = "Research · 6 sessions" },
        new SortableItem("Experiment scoring")           { Tag = "🧪", Note = "Growth · 3 active tests" },
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _horizontalItems =
    [
        new SortableItem("Finance week close")    { Tag = "💰", Note = "Finance · Friday EOD" },
        new SortableItem("Board deck updates")    { Tag = "📊", Note = "Exec · Due Thursday" },
        new SortableItem("Hiring pipeline review") { Tag = "👥", Note = "People · 8 candidates" },
    ];
}
