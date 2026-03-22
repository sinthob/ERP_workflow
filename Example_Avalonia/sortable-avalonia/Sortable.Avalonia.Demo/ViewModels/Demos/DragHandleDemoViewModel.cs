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

public partial class DragHandleDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _dragHandleItems =
    [
        new SortableItem("Appearance")    { Tag = "🎨", Note = "Themes, fonts and display density" },
        new SortableItem("Notifications") { Tag = "🔔", Note = "Alerts, badges and sound settings" },
        new SortableItem("Privacy")       { Tag = "🔒", Note = "Permissions and data sharing" },
        new SortableItem("Accessibility") { Tag = "♿", Note = "Screen reader and contrast options" },
        new SortableItem("Accounts")      { Tag = "👤", Note = "Login, SSO and connected services" },
    ];

    [RelayCommand]
    private void OnMoveLastHandleItemToTop()
    {
        if (DragHandleItems.Count > 0)
            LogEvent("⬆", $"Moved '{DragHandleItems[^1].Name}' section to top");
        RotateLastItemToFront(DragHandleItems);
    }
}
