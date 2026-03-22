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

using CommunityToolkit.Mvvm.ComponentModel;

namespace Sortable.Avalonia.Demo.Models;

public partial class SortableItem : ObservableObject
{
    [ObservableProperty]
    private string _taskName = string.Empty;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _sortable;

    [ObservableProperty]
    private bool _droppable;

    [ObservableProperty]
    private bool _reviewed;

    [ObservableProperty]
    private string _note = string.Empty;

    /// <summary>Short label displayed as a badge (emoji, priority, type, duration, initials, etc.)</summary>
    [ObservableProperty]
    private string _tag = string.Empty;

    public SortableItem(string name, bool sortable = true, bool droppable = true)
    {
        Name = name;
        Sortable = sortable;
        Droppable = droppable;
    }
}