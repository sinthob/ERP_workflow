# Sortable.Avalonia

An attached-behavior library that adds drag-and-drop sorting, cross-collection transfers, and animated item movements to any Avalonia `ItemsControl` — all with full MVVM support and no code-behind.

## Key Features

✅ **Same-collection sorting** – Reorder items within a list  
✅ **Cross-collection transfers** – Move/copy/swap items between lists  
✅ **Reversible drops** – Accept/reject drops in your ViewModel before commit  
✅ **Transfer modes** – Move, Copy, or Swap  
✅ **Sortable modes** – Sort (shift) or Swap (exchange)  
✅ **Drag handles** – Restrict drag start to specific controls  
✅ **Smooth animations** – For both interactive and programmatic changes  
✅ **Groups** – Isolate interactions by group name  
✅ **Touch + Mouse** – Unified pointer input

## Quick Example

```xml
<ItemsControl xmlns:sortable="clr-namespace:Sortable.Avalonia;assembly=Sortable.Avalonia"
              sortable:Sortable.Sortable="True"
              sortable:Sortable.UpdateCommand="{Binding UpdateCmd}"
              ItemsSource="{Binding Items}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border sortable:Sortable.IsSortable="True" Cursor="Hand">
                <TextBlock Text="{Binding Name}" />
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

```csharp
[RelayCommand]
void Update(SortableUpdateEventArgs e)
{
    if (e.ApplyUpdateMutation())
    {
        Console.WriteLine($"Moved from {e.OldIndex} to {e.NewIndex}");
    }
}
```

## Installation

```powershell
dotnet add package Sortable.Avalonia
```

## Why Use This?

- **Zero code-behind** – Pure XAML + ViewModel commands
- **Works with any panel** – `StackPanel`, `UniformGrid`, `WrapPanel`, etc.
- **Conditional acceptance** – Validate business rules before applying mutations
- **Helper extensions** – `ApplyUpdateMutation()` and `ApplyDropMutation()` handle collection changes for you

## Common Scenarios

| Use Case | Description |
|----------|-------------|
| **Kanban board** | Drag tasks between columns (Triage → Engineering → Release) |
| **Priority queue** | Reorder items by drag-and-drop |
| **Drag handles** | Only drag from specific icons, keep buttons clickable |
| **Copy mode** | Duplicate templates instead of moving them |
| **Swap mode** | Exchange positions without shifting other items |
| **Cross-collection swap** | Trade items between two lists in one gesture |

### Visit the repository for the full documentation

📖 **[Sortable.Avalonia Repo - Full Documentation & Interactive Demos](https://github.com/russkyc/sortable-avalonia)**

The repository includes:
- Detailed property reference
- 18 runnable demo scenarios (Kanban, grid layouts, drag handles, etc.)
- Advanced patterns (conditional acceptance, copy mode, groups)
- Animation control and customization guide

## License

MIT License – See [LICENSE](https://github.com/russkyc/sortable-avalonia/blob/main/LICENSE) for details.

---

**Need help?** [Open an issue](https://github.com/russkyc/sortable-avalonia/issues) or check out the [demo app](https://github.com/russkyc/sortable-avalonia/tree/main/Sortable.Avalonia.Demo) for working examples.

