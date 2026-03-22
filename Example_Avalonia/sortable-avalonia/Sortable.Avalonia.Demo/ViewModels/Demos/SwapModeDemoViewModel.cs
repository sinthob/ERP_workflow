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

public partial class SwapModeDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _swapModeItems =
    [
        new SortableItem("Alex Chen")    { Tag = "AC", Note = "Backend · Mon–Wed · @alexc" },
        new SortableItem("Jordan Lee")   { Tag = "JL", Note = "Frontend · Tue–Thu · @jlee" },
        new SortableItem("Sam Rivera")   { Tag = "SR", Note = "Infra · Wed–Fri · @srivera" },
        new SortableItem("Morgan Kim")   { Tag = "MK", Note = "Data · Thu–Sat · @mkim" },
        new SortableItem("Casey Park")   { Tag = "CP", Note = "Security · Fri–Sun · @cpark" },
        new SortableItem("Drew Taylor")  { Tag = "DT", Note = "Platform · Sat–Mon · @dtaylor" },
    ];

    [RelayCommand]
    private void OnSwapModeItemsProgrammatically()
    {
        if (SwapModeItems.Count >= 2)
            LogEvent("🔄", $"Swapped '{SwapModeItems[0].Name}' ↔ '{SwapModeItems[^1].Name}' on-call slots");
        SwapFirstAndLast(SwapModeItems);
    }

    [RelayCommand]
    private void OnRandomizeSwapMode()
    {
        LogEvent("🔀", "On-call roster randomized");
        SwapRandomItems(SwapModeItems);
    }
}
