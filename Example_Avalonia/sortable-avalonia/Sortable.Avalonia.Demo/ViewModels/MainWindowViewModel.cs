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

using Sortable.Avalonia.Demo.ViewModels.Demos;

namespace Sortable.Avalonia.Demo.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public KanbanBoardDemoViewModel KanbanBoardDemo { get; } = new();
    public VerticalListDemoViewModel VerticalListDemo { get; } = new();
    public DragHandleDemoViewModel DragHandleDemo { get; } = new();
    public GridLayoutDemoViewModel GridLayoutDemo { get; } = new();
    public HorizontalStackDemoViewModel HorizontalStackDemo { get; } = new();
    public MultipleGroupsDemoViewModel MultipleGroupsDemo { get; } = new();
    public DisabledItemsDemoViewModel DisabledItemsDemo { get; } = new();
    public SortableOnlyDemoViewModel SortableOnlyDemo { get; } = new();
    public DroppableOnlyDemoViewModel DroppableOnlyDemo { get; } = new();
    public CrossCollectionInteractionDemoViewModel CrossCollectionInteractionDemo { get; } = new();
    public UniformGridInteractionDemoViewModel UniformGridInteractionDemo { get; } = new();
    public CopyModeDemoViewModel CopyModeDemo { get; } = new();
    public ConditionalAcceptanceDemoViewModel ConditionalAcceptanceDemo { get; } = new();
    public SortModeDemoViewModel SortModeDemo { get; } = new();
    public SwapModeDemoViewModel SwapModeDemo { get; } = new();
    public CrossCollectionSwapDemoViewModel CrossCollectionSwapDemo { get; } = new();
    public UniformGridSwapDemoViewModel UniformGridSwapDemo { get; } = new();
}
