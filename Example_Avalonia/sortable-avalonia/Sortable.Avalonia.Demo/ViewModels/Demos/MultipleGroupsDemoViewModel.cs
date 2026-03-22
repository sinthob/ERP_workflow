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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sortable.Avalonia.Demo.Models;

namespace Sortable.Avalonia.Demo.ViewModels.Demos;

public partial class MultipleGroupsDemoViewModel : DemoViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SortableItem> _groupA1 =
    [
        new SortableItem("UI bug triage")             { Tag = "🎨", Note = "Design · High priority" },
        new SortableItem("Checkout conversion test")   { Tag = "🎨", Note = "Design · A/B running" },
        new SortableItem("Header nav redesign")        { Tag = "🎨", Note = "Design · Figma ready" },
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _groupA2 =
    [
        new SortableItem("Design QA pass")       { Tag = "🎨", Note = "Design · Review needed" },
        new SortableItem("Accessibility fixes")  { Tag = "🎨", Note = "Design · WCAG 2.1 AA" },
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _groupB1 =
    [
        new SortableItem("Data retention policy") { Tag = "🔒", Note = "Security · Compliance" },
        new SortableItem("Audit log exports")     { Tag = "🔒", Note = "Security · GDPR req." },
        new SortableItem("SSO provisioning flow") { Tag = "🔒", Note = "Security · Enterprise" },
    ];

    [ObservableProperty]
    private ObservableCollection<SortableItem> _groupB2 =
    [
        new SortableItem("IAM threat model")      { Tag = "🔒", Note = "Security · Quarterly" },
        new SortableItem("Risk register update")  { Tag = "🔒", Note = "Security · Due Friday" },
    ];

    [RelayCommand]
    private void OnItemDropped(SortableDropEventArgs e)
    {
        if (e.Item is not SortableItem item) return;

        var collectionMap = new Dictionary<System.Collections.IList, string>
        {
            { GroupA1, "Design Sprint A" },
            { GroupA2, "Design Sprint B" },
            { GroupB1, "Security Queue A" },
            { GroupB2, "Security Queue B" },
        };

        if (e.SourceCollection == null || e.TargetCollection == null ||
            !collectionMap.ContainsKey(e.SourceCollection) ||
            !collectionMap.ContainsKey(e.TargetCollection))
            return;

        e.IsAccepted = true;
        e.TransferMode = SortableTransferMode.Move;
        if (TryApplyDropMutation(e))
            LogEvent("🔄", $"'{item.Name}' → {collectionMap[e.TargetCollection]}");
    }
}
