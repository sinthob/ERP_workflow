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

public partial class CrossCollectionSwapDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _triageLane =
    [
        new SortableItem("Alex Chen")   { Tag = "AC", Note = "Backend Lead · On-call this week" },
        new SortableItem("Jordan Lee")  { Tag = "JL", Note = "Frontend Lead · Escalation backup" },
        new SortableItem("Sam Rivera")  { Tag = "SR", Note = "Infra Lead · Database expert" },
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _responseLane =
    [
        new SortableItem("Morgan Kim")  { Tag = "MK", Note = "Data Lead · Analytics specialist" },
        new SortableItem("Casey Park")  { Tag = "CP", Note = "Security Lead · Compliance owner" },
        new SortableItem("Drew Taylor") { Tag = "DT", Note = "Platform Lead · Release manager" },
    ];

    [RelayCommand]
    private void OnCrossCollectionSwapDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem item) return;
        e.IsAccepted = true;
        e.TransferMode = SortableTransferMode.Swap;
        if (TryApplyDropMutation(e))
            LogEvent("🔄", $"'{item.Name}' swapped teams (drag)");
    }

    [RelayCommand]
    private void SwapFirstItems()
    {
        if (TriageLane.Count > 0 && ResponseLane.Count > 0)
        {
            LogEvent("🔄", $"'{TriageLane[0].Name}' ↔ '{ResponseLane[0].Name}' (team leads)");
            var triageItem = TriageLane[0];
            var responseItem = ResponseLane[0];
            TriageLane.RemoveAt(0);
            ResponseLane.RemoveAt(0);
            ResponseLane.Insert(0, triageItem);
            TriageLane.Insert(0, responseItem);
        }
    }

    [RelayCommand]
    private void SwapLastItems()
    {
        if (TriageLane.Count > 0 && ResponseLane.Count > 0)
        {
            var ti = TriageLane.Count - 1;
            var ri = ResponseLane.Count - 1;
            LogEvent("🔄", $"'{TriageLane[ti].Name}' ↔ '{ResponseLane[ri].Name}' (last members)");
            var triageItem = TriageLane[ti];
            var responseItem = ResponseLane[ri];
            TriageLane.RemoveAt(ti);
            ResponseLane.RemoveAt(ri);
            ResponseLane.Insert(ri, triageItem);
            TriageLane.Insert(ti, responseItem);
        }
    }

    [RelayCommand]
    private void RandomSwap()
    {
        if (TriageLane.Count > 0 && ResponseLane.Count > 0)
        {
            var random = new Random();
            var triageIndex = random.Next(TriageLane.Count);
            var responseIndex = random.Next(ResponseLane.Count);
            LogEvent("��", $"'{TriageLane[triageIndex].Name}' ↔ '{ResponseLane[responseIndex].Name}' (random)");
            var triageItem = TriageLane[triageIndex];
            var responseItem = ResponseLane[responseIndex];
            TriageLane.RemoveAt(triageIndex);
            ResponseLane.RemoveAt(responseIndex);
            ResponseLane.Insert(responseIndex, triageItem);
            TriageLane.Insert(triageIndex, responseItem);
        }
    }
}
