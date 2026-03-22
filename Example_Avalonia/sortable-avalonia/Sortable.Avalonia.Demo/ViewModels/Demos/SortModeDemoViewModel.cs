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

public partial class SortModeDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _sortModeItems =
    [
        new SortableItem("Blinding Lights")  { Tag = "3:20", Note = "The Weeknd · Synth-pop" },
        new SortableItem("Levitating")       { Tag = "3:23", Note = "Dua Lipa · Dance-pop" },
        new SortableItem("Industry Baby")    { Tag = "3:32", Note = "Lil Nas X · Hip-hop" },
        new SortableItem("Stay")             { Tag = "2:21", Note = "The Kid LAROI, Bieber · Pop" },
        new SortableItem("Bad Habits")       { Tag = "3:51", Note = "Ed Sheeran · Pop" },
        new SortableItem("Heat Waves")       { Tag = "3:59", Note = "Glass Animals · Indie" },
    ];

    [RelayCommand]
    private void OnSortModeItemsProgrammatically()
    {
        if (SortModeItems.Count > 0)
            LogEvent("▶", $"Queued '{SortModeItems[^1].Name}' as next track");
        RotateLastItemToFront(SortModeItems);
    }
}
