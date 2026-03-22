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

using System.Collections;

namespace Sortable.Avalonia;

/// <summary>
/// Event args for reversible drop operations. The handler can accept/reject the operation
/// and choose whether to duplicate or transfer the item.
/// </summary>
public class SortableDropEventArgs
{
    /// <summary>
    /// The item being transferred.
    /// </summary>
    public object? Item { get; set; }

    /// <summary>
    /// The source collection where the item was dragged from.
    /// </summary>
    public IList? SourceCollection { get; set; }

    /// <summary>
    /// The target collection where the item is being dropped.
    /// </summary>
    public IList? TargetCollection { get; set; }

    /// <summary>
    /// The index of the item in the source collection.
    /// </summary>
    public int OldIndex { get; set; }

    /// <summary>
    /// The index where the item will be inserted in the target collection.
    /// </summary>
    public int NewIndex { get; set; }

    /// <summary>
    /// Whether this drop operation should be accepted. Set to false to reject the drop.
    /// Default is true.
    /// </summary>
    public bool IsAccepted { get; set; } = true;

    /// <summary>
    /// How the item should be transferred.
    /// Move removes from source, Copy keeps original, Swap exchanges source/target items.
    /// Default is Move.
    /// </summary>
    public SortableTransferMode TransferMode { get; set; } = SortableTransferMode.Move;

    /// <summary>
    /// The actual item that will be added to the target collection.
    /// This can be modified by the handler (e.g., to clone the item or modify properties).
    /// If null, the original Item will be used.
    /// </summary>
    public object? ModifiedItem { get; set; }

    /// <summary>
    /// Gets the item that should actually be inserted into the target collection.
    /// </summary>
    public object? GetItemToInsert() => ModifiedItem ?? Item;
}
