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

public partial class DisabledItemsDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _mixedSortableList =
    [
        new SortableItem("Deploy API hotfix to staging")        { Tag = "⚡", Note = "Platform · Ready to deploy" },
        new SortableItem("Approve vendor invoice #12931")       { Tag = "📋", Note = "Finance · Awaiting review" },
        new SortableItem("CFO sign-off pending", false)         { Tag = "🔒", Note = "Finance · Change freeze" },
        new SortableItem("Rotate database credentials")         { Tag = "🔑", Note = "Security · Overdue 2 days" },
        new SortableItem("Change freeze window active", false)  { Tag = "🔒", Note = "Ops · Until Dec 31" },
        new SortableItem("Update support playbook")             { Tag = "📘", Note = "CS · Q4 refresh due" },
    ];
}
