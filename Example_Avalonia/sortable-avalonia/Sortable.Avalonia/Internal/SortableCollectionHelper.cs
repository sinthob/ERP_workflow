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

namespace Sortable.Avalonia.Internal;

/// <summary>
/// Helper methods for safe collection mutations during drag/drop operations.
/// </summary>
internal static class SortableCollectionHelper
{
    internal static bool InsertAtIndexOrAdd(IList collection, int index, object? item)
    {
        if (index >= 0 && index <= collection.Count)
        {
            collection.Insert(index, item);
            return true;
        }

        collection.Add(item);
        return true;
    }

    internal static bool RemoveAtIfInRange(IList collection, int index)
    {
        if (index >= 0 && index < collection.Count)
        {
            collection.RemoveAt(index);
            return true;
        }

        return false;
    }

    internal static bool SwapItemsInCollection(IList collection, int index1, int index2)
    {
        if (index1 == index2)
        {
            return false;
        }

        if (index1 < 0 || index1 >= collection.Count || index2 < 0 || index2 >= collection.Count)
        {
            return false;
        }

        (collection[index1], collection[index2]) = (collection[index2], collection[index1]);
        return true;
    }
}
