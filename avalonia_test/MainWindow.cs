using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaTest;

// ---------- Lightweight DTOs for list parsing ----------

public sealed class SalesOrderRow
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("customer")] public string? Customer { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("transaction_date")] public string? TransactionDate { get; set; }
    [JsonPropertyName("delivery_date")] public string? DeliveryDate { get; set; }
}

public sealed class WorkOrderRow
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("item_name")] public string? ItemName { get; set; }
    [JsonPropertyName("production_item")] public string? ProductionItem { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("qty")] public double? Qty { get; set; }
    [JsonPropertyName("sales_order")] public string? SalesOrder { get; set; }
    [JsonPropertyName("modified")] public string? Modified { get; set; }
}

public sealed class JobCardRow
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("work_order")] public string? WorkOrder { get; set; }
    [JsonPropertyName("operation")] public string? Operation { get; set; }
    [JsonPropertyName("workstation")] public string? Workstation { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("for_quantity")] public double? ForQuantity { get; set; }
}

public sealed class MainWindow : Window
{
    private readonly ApiClient _api = new();
    private TextBox _output = null!;

    // -- Drill-down state --
    private string? _selectedSO;
    private string? _selectedWO;

    // -- Navigation generation: incremented on every navigation so stale async
    //    continuations can detect they've been superseded and abort.
    private int _navGen;

    // -- Drill-down UI elements --
    private TextBlock _breadcrumb = null!;
    private StackPanel _contentArea = null!;

    public MainWindow()
    {
        Title = "ERP Gateway Test — Team 3";
        Width = 1050;
        Height = 780;
        BuildUi();
    }

    // ====================================================
    // UI Construction
    // ====================================================

    private void BuildUi()
    {
        _output = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas,Courier New"),
            FontSize = 12,
        };

        _breadcrumb = new TextBlock
        {
            FontSize = 14,
            Margin = new Thickness(8, 4),
            Foreground = Brushes.Gray,
        };
        _contentArea = new StackPanel { Spacing = 4 };

        var tabs = new TabControl
        {
            TabStripPlacement = global::Avalonia.Controls.Dock.Top,
            Items =
            {
                BuildDrillDownTab(),
                BuildWorkflowTab(),
                BuildViewsTab(),
                BuildHealthTab(),
            },
        };

        var topBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(4),
            Children =
            {
                MakeBtn("Clear Log", (_, _) => _output.Text = ""),
            },
        };

        Content = new DockPanel
        {
            Children =
            {
                SetDock(topBar, global::Avalonia.Controls.Dock.Top),
                SetDock(new ScrollViewer { Content = _output, Height = 200 }, global::Avalonia.Controls.Dock.Bottom),
                tabs,
            },
        };

        // Load initial view
        ShowSalesOrders();
    }

    // ====================================================
    // Drill-Down Tab (Sales Orders → Work Orders → Job Cards)
    // ====================================================

    private TabItem BuildDrillDownTab()
    {
        var drillPanel = new DockPanel
        {
            Children =
            {
                SetDock(_breadcrumb, global::Avalonia.Controls.Dock.Top),
                new ScrollViewer { Content = _contentArea },
            },
        };

        return new TabItem { Header = "Sale Orders", Content = drillPanel };
    }

    // ---------- Level 1: Sales Orders ----------

    private async void ShowSalesOrders()
    {
        var gen = ++_navGen;
        _selectedSO = null;
        _selectedWO = null;
        _breadcrumb.Text = "📋 Sales Orders";
        _contentArea.Children.Clear();

        // Create form
        var createPanel = BuildSOCreateForm();
        _contentArea.Children.Add(createPanel);
        _contentArea.Children.Add(new Separator { Margin = new Thickness(0, 4) });

        // Loading indicator
        var loading = new TextBlock { Text = "Loading…", Margin = new Thickness(8) };
        _contentArea.Children.Add(loading);

        try
        {
            var rows = await _api.GetJsonAsync<List<SalesOrderRow>>("/sales-orders?limit=50");
            if (gen != _navGen) return;   // navigated away while loading
            _contentArea.Children.Remove(loading);

            if (rows == null || rows.Count == 0)
            {
                _contentArea.Children.Add(new TextBlock { Text = "No Sales Orders found.", Margin = new Thickness(8) });
                return;
            }

            // Column header
            var header = new Grid { Margin = new Thickness(8, 4) };
            header.ColumnDefinitions.Add(new ColumnDefinition(110, GridUnitType.Pixel));
            header.ColumnDefinitions.Add(new ColumnDefinition(190, GridUnitType.Pixel));
            header.ColumnDefinitions.Add(new ColumnDefinition(200, GridUnitType.Pixel));
            header.ColumnDefinitions.Add(new ColumnDefinition(160, GridUnitType.Pixel));
            header.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            void AddHeader(string t, int col)
            {
                var tb = new TextBlock { Text = t, FontWeight = FontWeight.Bold, Foreground = Brushes.Gray, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(tb, col);
                header.Children.Add(tb);
            }
            AddHeader("ORDER ID", 0); AddHeader("ORDER NAME", 1); AddHeader("TIMELINE", 2); AddHeader("CUSTOMER", 3); AddHeader("STATUS", 4);
            _contentArea.Children.Add(header);
            _contentArea.Children.Add(new Separator { Margin = new Thickness(0, 2) });

            foreach (var so in rows)
            {
                var card = MakeSOCard(so);
                _contentArea.Children.Add(card);
            }

            _contentArea.Children.Add(new TextBlock
            {
                Text = $"Showing {rows.Count} orders",
                Margin = new Thickness(8, 6),
                Foreground = Brushes.Gray,
                FontSize = 12,
            });
        }
        catch (Exception ex)
        {
            _contentArea.Children.Remove(loading);
            Log($"ERROR loading Sales Orders: {ex.Message}");
            _contentArea.Children.Add(new TextBlock { Text = $"Error: {ex.Message}", Foreground = Brushes.Red, Margin = new Thickness(8) });
        }
    }

    private StackPanel BuildSOCreateForm()
    {
        var customerBox = new AutoCompleteBox
        {
            Watermark = "Customer *",
            Width = 200,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadCustomerSuggestionsAsync(customerBox);

        // Shows "item_name (item_code)" — we parse item_code back on submit
        var itemBox = new AutoCompleteBox
        {
            Watermark = "Item (พิมพ์ชื่อ)",
            Width = 220,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadItemSuggestionsAsync(itemBox);

        var soTitleBox = new TextBox { Watermark = "SO Name (opt.)", Width = 130 };
        var qtyBox = new TextBox { Watermark = "Qty", Width = 70, Text = "1" };
        var startPicker = new DatePicker
        {
            SelectedDate = DateTimeOffset.Now,
            Width = 130,
        };
        var deliveryPicker = new DatePicker
        {
            SelectedDate = DateTimeOffset.Now.AddDays(7),
            Width = 130,
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 4),
            Children =
        {
            new TextBlock { Text = "+ New SO:", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.SemiBold },
            soTitleBox, customerBox, itemBox, qtyBox,
            new TextBlock { Text = "Start:", VerticalAlignment = VerticalAlignment.Center },
            startPicker,
            new TextBlock { Text = "End:", VerticalAlignment = VerticalAlignment.Center },
            deliveryPicker,
            MakeBtn("Create", async (_, _) =>
            {
                var startDate = (startPicker.SelectedDate ?? DateTimeOffset.Now).ToString("yyyy-MM-dd");
                var deliveryDate = (deliveryPicker.SelectedDate ?? DateTimeOffset.Now.AddDays(7)).ToString("yyyy-MM-dd");
                var body = new Dictionary<string, object?>
                {
                    ["customer"] = customerBox.Text,
                    ["transaction_date"] = startDate,
                    ["delivery_date"] = deliveryDate,
                };
                if (!string.IsNullOrWhiteSpace(soTitleBox.Text))
                    body["title"] = soTitleBox.Text.Trim();
                var itemCode = ExtractItemCode(itemBox.Text);
                if (!string.IsNullOrWhiteSpace(itemCode))
                {
                    var items = new List<object>
                    {
                        new { item_code = itemCode, qty = double.TryParse(qtyBox.Text, out var q) ? q : 1 }
                    };
                    body["items"] = items;
                }
                Log(await _api.PostAsync("/sales-orders", body));
                ShowSalesOrders();
            }),
            MakeBtn("⟳ Refresh", (_, _) => ShowSalesOrders()),
        }
        };
    }

    /// <summary>
    /// If text is "ชื่อสินค้า (ITEM-001)" extract "ITEM-001".
    /// If no parentheses (user typed raw code), return as-is.
    /// </summary>
    private static string? ExtractItemCode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var start = text.LastIndexOf('(');
        var end = text.LastIndexOf(')');
        if (start >= 0 && end > start)
            return text[(start + 1)..end].Trim();
        return text.Trim();
    }

    private async System.Threading.Tasks.Task LoadCustomerSuggestionsAsync(AutoCompleteBox box)
    {
        try
        {
            var rows = await _api.GetJsonAsync<List<System.Text.Json.JsonElement>>("/master/customers?limit=200");
            if (rows == null) return;
            var names = rows
                .Where(r => r.TryGetProperty("name", out _))
                .Select(r => r.GetProperty("name").GetString() ?? "")
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            box.ItemsSource = names;
        }
        catch { }
    }

    private async System.Threading.Tasks.Task LoadWarehouseSuggestionsAsync(AutoCompleteBox box)
    {
        try
        {
            var rows = await _api.GetJsonAsync<List<System.Text.Json.JsonElement>>("/master/warehouses?limit=100");
            if (rows == null) return;
            var names = rows
                .Select(r => r.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "")
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            box.ItemsSource = names;
        }
        catch { }
    }

    // Generic: fetch any endpoint that returns [{name: ...}, ...] and populate a box
    private async System.Threading.Tasks.Task LoadSimpleSuggestionsAsync(AutoCompleteBox box, string url)
    {
        try
        {
            var rows = await _api.GetJsonAsync<List<System.Text.Json.JsonElement>>(url);
            if (rows == null) return;
            var names = rows
                .Select(r => r.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "")
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            box.ItemsSource = names;
        }
        catch { }
    }

    private async System.Threading.Tasks.Task LoadCompanySuggestionsAsync(AutoCompleteBox box)
    {
        try
        {
            var rows = await _api.GetJsonAsync<List<System.Text.Json.JsonElement>>("/master/companies?limit=50");
            if (rows == null) return;
            var names = rows
                .Select(r => r.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "")
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            box.ItemsSource = names;
            // Pre-fill if only one company
            if (names.Count == 1) box.Text = names[0];
        }
        catch { }
    }

    private async System.Threading.Tasks.Task LoadBomSuggestionsAsync(
        AutoCompleteBox box,
        System.Collections.Generic.Dictionary<string, string> itemMap)
    {
        try
        {
            var rows = await _api.GetJsonAsync<List<System.Text.Json.JsonElement>>("/master/boms?limit=100");
            if (rows == null) return;
            var labels = new List<string>();
            foreach (var r in rows)
            {
                var bomName = r.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var itemCode = r.TryGetProperty("item", out var ic) ? ic.GetString() ?? "" : "";
                var itemName = r.TryGetProperty("item_name", out var inn) ? inn.GetString() ?? itemCode : itemCode;
                var isDraft = r.TryGetProperty("_draft", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.True;
                if (string.IsNullOrWhiteSpace(bomName)) continue;
                var label = isDraft
                    ? $"{itemName} ({bomName}) ⚠️Draft"
                    : (itemName == bomName ? bomName : $"{itemName} ({bomName})");
                labels.Add(label);
                itemMap[label] = itemCode;
            }
            box.ItemsSource = labels;
        }
        catch { }
    }

    private async System.Threading.Tasks.Task LoadItemSuggestionsAsync(AutoCompleteBox box)
    {
        try
        {
            var rows = await _api.GetJsonAsync<List<System.Text.Json.JsonElement>>("/master/items?limit=200");
            if (rows == null) return;
            // Display: "item_name (item_code)" — item_code = name field in ERPNext
            var labels = rows
                .Select(r =>
                {
                    var code = r.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var displayName = r.TryGetProperty("item_name", out var iname) ? iname.GetString() ?? code : code;
                    return displayName == code ? code : $"{displayName} ({code})";
                })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            box.ItemsSource = labels;
        }
        catch { }
    }

    // ---------- Level 2: Work Orders for a Sales Order ----------

    private async void ShowWorkOrders(string salesOrder)
    {
        var gen = ++_navGen;
        _selectedSO = salesOrder;
        _selectedWO = null;
        _breadcrumb.Text = $"📋 Sales Orders  ›  {salesOrder}  ›  Work Orders";
        _contentArea.Children.Clear();

        // Back + create
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 4),
            Children =
        {
            MakeBtn("← Back to Sales Orders", (_, _) => ShowSalesOrders()),
            MakeBtn("Delete this SO", async (_, _) =>
            {
                Log(await _api.DeleteAsync($"/sales-orders/{salesOrder}"));
                ShowSalesOrders();
            }),
        }
        };
        _contentArea.Children.Add(toolbar);

        var createPanel = BuildWOCreateForm(salesOrder);
        _contentArea.Children.Add(createPanel);
        _contentArea.Children.Add(new Separator { Margin = new Thickness(0, 4) });

        var loading = new TextBlock { Text = "Loading…", Margin = new Thickness(8) };
        _contentArea.Children.Add(loading);

        try
        {
            var rows = await _api.GetJsonAsync<List<WorkOrderRow>>($"/work-orders?sales_order={salesOrder}&limit=50");
            if (gen != _navGen) return;   // navigated away while loading
            _contentArea.Children.Remove(loading);

            if (rows == null || rows.Count == 0)
            {
                _contentArea.Children.Add(new TextBlock { Text = "No Work Orders for this Sales Order.", Margin = new Thickness(8) });
                return;
            }

            _contentArea.Children.Add(MakeWOKanbanBoard(rows, salesOrder));
        }
        catch (Exception ex)
        {
            _contentArea.Children.Remove(loading);
            Log($"ERROR loading Work Orders: {ex.Message}");
            _contentArea.Children.Add(new TextBlock { Text = $"Error: {ex.Message}", Foreground = Brushes.Red, Margin = new Thickness(8) });
        }
    }

    private StackPanel BuildWOCreateForm(string salesOrder)
    {
        var companyBox = new AutoCompleteBox
        {
            Watermark = "Company *",
            Width = 200,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadCompanySuggestionsAsync(companyBox);

        var prodItemBox = new AutoCompleteBox
        {
            Watermark = "Production Item *",
            Width = 200,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadItemSuggestionsAsync(prodItemBox);

        var woQtyBox = new TextBox { Watermark = "Qty *", Width = 60, Text = "1" };

        var woTitleBox = new TextBox { Watermark = "WO Name (opt.)", Width = 140 };

        var warehouseBox = new AutoCompleteBox
        {
            Watermark = "Target Warehouse *",
            Width = 180,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadWarehouseSuggestionsAsync(warehouseBox);

        var wipWarehouseBox = new AutoCompleteBox
        {
            Watermark = "WIP Warehouse *",
            Width = 180,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadWarehouseSuggestionsAsync(wipWarehouseBox);

        // BOM dropdown — selecting one auto-fills Production Item
        var bomItemMap = new System.Collections.Generic.Dictionary<string, string>();
        var bomBox = new AutoCompleteBox
        {
            Watermark = "BOM *",
            Width = 240,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadBomSuggestionsAsync(bomBox, bomItemMap);
        bomBox.SelectionChanged += (_, e) =>
        {
            var selected = bomBox.Text ?? "";
            if (bomItemMap.TryGetValue(selected, out var itemCode))
                prodItemBox.Text = itemCode;
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 4),
            Children =
        {
            new TextBlock { Text = "+ New WO:", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.SemiBold },
            woTitleBox, companyBox, prodItemBox, woQtyBox, warehouseBox, wipWarehouseBox, bomBox,
            MakeBtn("Create", async (_, _) =>
            {
                var body = new Dictionary<string, object?>
                {
                    ["company"] = companyBox.Text,
                    ["production_item"] = ExtractItemCode(prodItemBox.Text),
                    ["qty"] = double.TryParse(woQtyBox.Text, out var q) ? q : 1,
                    ["bom_no"] = ExtractBomNo(bomBox.Text),
                    ["fg_warehouse"] = ExtractItemCode(warehouseBox.Text),
                    ["wip_warehouse"] = ExtractItemCode(wipWarehouseBox.Text),
                    ["sales_order"] = salesOrder,
                };
                if (!string.IsNullOrWhiteSpace(woTitleBox.Text))
                    body["title"] = woTitleBox.Text.Trim();
                Log(await _api.PostAsync("/work-orders", body));
                ShowWorkOrders(salesOrder);
            }),
            MakeBtn("⟳", (_, _) => ShowWorkOrders(salesOrder)),
        }
        };
    }

    private static string? ExtractBomNo(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        // Strip "⚠️Draft" suffix if present
        var clean = text.Replace(" ⚠️Draft", "").Trim();
        // Display is "item_name (BOM-xxx)" — extract BOM-xxx
        return ExtractItemCode(clean);
    }

    // ---------- Level 3: Job Cards for a Work Order ----------

    private async void ShowJobCards(string salesOrder, string workOrder)
    {
        var gen = ++_navGen;
        _selectedSO = salesOrder;
        _selectedWO = workOrder;
        _breadcrumb.Text = $"📋 Sales Orders  ›  {salesOrder}  ›  {workOrder}  ›  Job Cards";
        _contentArea.Children.Clear();

        // Back + workflow + delete
        var toolbar = BuildJCToolbar(salesOrder, workOrder);
        _contentArea.Children.Add(toolbar);

        var createPanel = BuildJCCreateForm(salesOrder, workOrder);
        _contentArea.Children.Add(createPanel);
        _contentArea.Children.Add(new Separator { Margin = new Thickness(0, 4) });

        var loading = new TextBlock { Text = "Loading…", Margin = new Thickness(8) };
        _contentArea.Children.Add(loading);

        try
        {
            var rows = await _api.GetJsonAsync<List<JobCardRow>>($"/job-cards?work_order={workOrder}&limit=50");
            if (gen != _navGen) return;   // navigated away while loading
            _contentArea.Children.Remove(loading);

            if (rows == null || rows.Count == 0)
            {
                _contentArea.Children.Add(new TextBlock { Text = "No Job Cards for this Work Order.", Margin = new Thickness(8) });
                return;
            }

            _contentArea.Children.Add(MakeGridHeader("NAME", "OPERATION", "WORKSTATION", "STATUS"));

            foreach (var jc in rows)
            {
                var row = MakeSelectableRow(
                    jc.Name, jc.Operation ?? "", jc.Workstation ?? "", jc.Status ?? "",
                    async () =>
                    {
                        Log(await _api.GetAsync($"/job-cards/{jc.Name}"));
                    });
                _contentArea.Children.Add(row);
            }

            _contentArea.Children.Add(new TextBlock
            {
                Text = $"Showing {rows.Count} job cards",
                Margin = new Thickness(8, 6),
                Foreground = Brushes.Gray,
                FontSize = 12,
            });
        }
        catch (Exception ex)
        {
            _contentArea.Children.Remove(loading);
            Log($"ERROR loading Job Cards: {ex.Message}");
            _contentArea.Children.Add(new TextBlock { Text = $"Error: {ex.Message}", Foreground = Brushes.Red, Margin = new Thickness(8) });
        }
    }

    private StackPanel BuildJCToolbar(string salesOrder, string workOrder)
    {
        var toStatus = new ComboBox
        {
            Width = 150,
            PlaceholderText = "Transition →",
            ItemsSource = new[] { "Not Started", "In Process", "Completed", "Cancelled" },
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 4),
            Children =
        {
            MakeBtn("← Back to Work Orders", (_, _) => ShowWorkOrders(salesOrder)),
            new Separator(),
            toStatus,
            MakeBtn("Transition WO", async (_, _) =>
            {
                var sel = toStatus.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(sel)) { Log("ERROR: select a target status"); return; }
                Log(await _api.PostAsync($"/workflow/work-orders/{workOrder}/transition", new { to_status = sel }));
                ShowWorkOrders(salesOrder); // go back to kanban so updated card is visible
            }),
            new Separator(),
            MakeBtn("Delete this WO", async (_, _) =>
            {
                Log(await _api.DeleteAsync($"/work-orders/{workOrder}"));
                ShowWorkOrders(salesOrder);
            }),
        }
        };
    }

    private StackPanel BuildJCCreateForm(string salesOrder, string workOrder)
    {
        var jcCompany = new AutoCompleteBox
        {
            Watermark = "Company *",
            Width = 190,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadCompanySuggestionsAsync(jcCompany);

        var jcOp = new AutoCompleteBox
        {
            Watermark = "Operation *",
            Width = 150,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadSimpleSuggestionsAsync(jcOp, "/master/operations?limit=100");

        var jcWs = new AutoCompleteBox
        {
            Watermark = "Workstation *",
            Width = 160,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadSimpleSuggestionsAsync(jcWs, "/master/workstations?limit=100");

        var jcWip = new AutoCompleteBox
        {
            Watermark = "WIP Warehouse *",
            Width = 180,
            MinimumPrefixLength = 0,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
        };
        _ = LoadWarehouseSuggestionsAsync(jcWip);

        var jcQty = new TextBox { Watermark = "Qty", Width = 60, Text = "1" };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 4),
            Children =
        {
            new TextBlock { Text = "+ New JC:", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.SemiBold },
            jcCompany, jcOp, jcWs, jcWip, jcQty,
            MakeBtn("Create", async (_, _) =>
            {
                var body = new Dictionary<string, object?>
                {
                    ["company"] = jcCompany.Text,
                    ["work_order"] = workOrder,
                    ["operation"] = jcOp.Text,
                    ["workstation"] = jcWs.Text,
                    ["wip_warehouse"] = jcWip.Text,
                };
                if (double.TryParse(jcQty.Text, out var fq) && fq > 0)
                    body["for_quantity"] = fq;
                Log(await _api.PostAsync("/job-cards", body));
                ShowJobCards(salesOrder, workOrder);
            }),
            MakeBtn("⟳", (_, _) => ShowJobCards(salesOrder, workOrder)),
        }
        };
    }

    // ====================================================
    // Workflow Tab (standalone — for any WO by name)
    // ====================================================

    private TabItem BuildWorkflowTab()
    {
        var woName = new TextBox { Watermark = "Work Order Name *", Width = 250 };
        var toStatus = new ComboBox
        {
            Width = 200,
            PlaceholderText = "Target Status",
            ItemsSource = new[] { "Not Started", "In Process", "Completed", "Cancelled" },
        };

        var panel = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(8),
            Children =
        {
            new TextBlock { Text = "Work Order Workflow Transition", FontWeight = FontWeight.Bold, FontSize = 16 },
            new TextBlock { Text = "Allowed: Draft → Not Started → In Process → Completed (or → Cancelled)" },
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children =
            {
                woName,
                toStatus,
                MakeBtn("Transition", async (_, _) =>
                {
                    var sel = toStatus.SelectedItem?.ToString();
                    if (string.IsNullOrWhiteSpace(sel)) { Log("ERROR: select a target status"); return; }
                    var body = new { to_status = sel };
                    Log(await _api.PostAsync($"/workflow/work-orders/{woName.Text}/transition", body));
                }),
            }},
        }
        };

        return new TabItem { Header = "Workflow", Content = new ScrollViewer { Content = panel } };
    }

    // ====================================================
    // Views Tab (Kanban / Timeline)
    // ====================================================

    private TabItem BuildViewsTab()
    {
        var soForKanban = new TextBox { Watermark = "Sales Order Name", Width = 250 };
        var soForTimeline = new TextBox { Watermark = "Sales Order Name", Width = 250 };
        var woForView = new TextBox { Watermark = "Work Order Name", Width = 250 };

        var panel = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(8),
            Children =
        {
            new TextBlock { Text = "Kanban / Timeline Views", FontWeight = FontWeight.Bold, FontSize = 16 },
            Wrap("Kanban (per Sales Order)", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children =
            {
                soForKanban,
                MakeBtn("Get Kanban", async (_, _) => Log(await _api.GetAsync($"/views/orders/{soForKanban.Text}/kanban"))),
            }}),
            new Separator(),
            Wrap("Timeline (per Sales Order)", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children =
            {
                soForTimeline,
                MakeBtn("Get Timeline", async (_, _) => Log(await _api.GetAsync($"/views/orders/{soForTimeline.Text}/timeline"))),
            }}),
            new Separator(),
            Wrap("Job Cards (per Work Order)", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children =
            {
                woForView,
                MakeBtn("List JC for WO", async (_, _) => Log(await _api.GetAsync($"/views/work-orders/{woForView.Text}/job-cards"))),
            }}),
        }
        };

        return new TabItem { Header = "Views", Content = new ScrollViewer { Content = panel } };
    }

    // ====================================================
    // Health Tab
    // ====================================================

    private TabItem BuildHealthTab()
    {
        var panel = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(8),
            Children =
        {
            new TextBlock { Text = "Health / Connectivity", FontWeight = FontWeight.Bold, FontSize = 16 },
            MakeBtn("GET /health", async (_, _) => Log(await _api.GetAsync("/health"))),
            MakeBtn("GET /erp/ping", async (_, _) => Log(await _api.GetAsync("/erp/ping"))),
        }
        };

        return new TabItem { Header = "Health", Content = panel };
    }

    // ====================================================
    // Helpers
    // ====================================================

    private void Log(string message)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        _output.Text = $"[{ts}] {message}\n\n{_output.Text}";
    }

    private static Button MakeBtn(string text, EventHandler<Avalonia.Interactivity.RoutedEventArgs> handler)
    {
        var btn = new Button { Content = text, Padding = new Thickness(10, 4) };
        btn.Click += handler;
        return btn;
    }

    private static Control SetDock(Control child, global::Avalonia.Controls.Dock dock)
    {
        DockPanel.SetDock(child, dock);
        return child;
    }

    private static StackPanel Wrap(string label, Control inner) => new()
    {
        Spacing = 4,
        Children =
        {
            new TextBlock { Text = label, FontWeight = FontWeight.SemiBold },
            inner,
        },
    };

    // ---------- Grid helpers ----------

    private static Border MakeGridHeader(params string[] columns)
    {
        var grid = new Grid { Margin = new Thickness(8, 4) };
        foreach (var _ in columns)
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

        for (int i = 0; i < columns.Length; i++)
        {
            var tb = new TextBlock
            {
                Text = columns[i],
                FontWeight = FontWeight.Bold,
                FontSize = 12,
                Foreground = Brushes.Gray,
            };
            Grid.SetColumn(tb, i);
            grid.Children.Add(tb);
        }

        return new Border
        {
            Child = grid,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 2),
        };
    }

    /// <summary>Clickable row that navigates to the next drill-down level.</summary>
    private static Border MakeClickableRow(string col1, string col2, string col3, string col4, Action onClick)
    {
        var grid = new Grid { Margin = new Thickness(8, 2) };
        for (int i = 0; i < 4; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

        var texts = new[] { col1, col2, col3, col4 };
        for (int i = 0; i < texts.Length; i++)
        {
            var tb = new TextBlock { Text = texts[i], FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(tb, i);
            grid.Children.Add(tb);
        }

        var border = new Border
        {
            Child = grid,
            Background = Brushes.Transparent,
            Padding = new Thickness(0, 4),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
        };

        border.PointerPressed += (_, _) => onClick();
        border.PointerEntered += (s, _) => ((Border)s!).Background = new SolidColorBrush(Color.FromArgb(30, 100, 100, 255));
        border.PointerExited += (s, _) => ((Border)s!).Background = Brushes.Transparent;

        return border;
    }

    /// <summary>Selectable row (bottom level) — click shows detail in log.</summary>
    private static Border MakeSelectableRow(string col1, string col2, string col3, string col4, Action onClick)
    {
        return MakeClickableRow(col1, col2, col3, col4, onClick);
    }

    // ---- Work Order Kanban board ----

    private Grid MakeWOKanbanBoard(List<WorkOrderRow> rows, string salesOrder)
    {
        var inProgressSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "In Process" };
        var completedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Completed", "Closed" };

        var todo = rows.Where(r => !inProgressSet.Contains(r.Status ?? "") && !completedSet.Contains(r.Status ?? "")).ToList();
        var inProgress = rows.Where(r => inProgressSet.Contains(r.Status ?? "")).ToList();
        var completed = rows.Where(r => completedSet.Contains(r.Status ?? "")).ToList();

        var todoColor = Color.FromRgb(150, 150, 160);
        var inProgressColor = Color.FromRgb(255, 172, 50);
        var completedColor = Color.FromRgb(72, 187, 120);

        var grid = new Grid { Margin = new Thickness(8, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(10, GridUnitType.Pixel));
        grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(10, GridUnitType.Pixel));
        grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

        var col0 = MakeWOColumn("TO DO", todoColor, todo, salesOrder);
        var col1 = MakeWOColumn("IN PROGRESS", inProgressColor, inProgress, salesOrder);
        var col2 = MakeWOColumn("COMPLETED", completedColor, completed, salesOrder);

        Grid.SetColumn(col0, 0);
        Grid.SetColumn(col1, 2);
        Grid.SetColumn(col2, 4);
        grid.Children.Add(col0);
        grid.Children.Add(col1);
        grid.Children.Add(col2);
        return grid;
    }

    private StackPanel MakeWOColumn(string label, Color headerColor, List<WorkOrderRow> wos, string salesOrder)
    {
        var column = new StackPanel { Spacing = 8 };

        // Column header row
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 4),
        };

        // Colored dot
        header.Children.Add(new Border
        {
            Width = 10,
            Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(headerColor),
            VerticalAlignment = VerticalAlignment.Center,
        });

        // Label
        header.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 90)),
            VerticalAlignment = VerticalAlignment.Center,
            LetterSpacing = 1.0,
        });

        // Count badge
        header.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(50, headerColor.R, headerColor.G, headerColor.B)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = wos.Count.ToString(),
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(headerColor),
            },
        });

        column.Children.Add(header);

        // Cards
        if (wos.Count == 0)
        {
            column.Children.Add(new TextBlock
            {
                Text = "No items",
                Foreground = Brushes.LightGray,
                FontSize = 12,
                Margin = new Thickness(4, 4),
            });
        }
        else
        {
            foreach (var wo in wos)
                column.Children.Add(MakeWOCard(wo, headerColor, salesOrder));
        }

        return column;
    }

    private Border MakeWOCard(WorkOrderRow wo, Color accentColor, string salesOrder)
    {
        var content = new StackPanel { Spacing = 4, Margin = new Thickness(12, 10, 12, 10) };

        // Top row: status badge (left) + modified date (right)
        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        topRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var statusBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(35, accentColor.R, accentColor.G, accentColor.B)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(7, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = (wo.Status ?? "UNKNOWN").ToUpperInvariant(),
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(accentColor),
            },
        };
        Grid.SetColumn(statusBadge, 0);
        topRow.Children.Add(statusBadge);

        var dateText = "";
        if (!string.IsNullOrEmpty(wo.Modified) && wo.Modified.Length >= 10 &&
            DateTime.TryParse(wo.Modified.Substring(0, 10), out var modDate))
            dateText = modDate.ToString("MMM dd");
        var dateTb = new TextBlock
        {
            Text = dateText,
            FontSize = 11,
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(dateTb, 1);
        topRow.Children.Add(dateTb);
        content.Children.Add(topRow);

        // WO bold heading: custom title → item_name → system name (priority order)
        var displayTitle = !string.IsNullOrWhiteSpace(wo.Title)
            ? wo.Title
            : !string.IsNullOrWhiteSpace(wo.ItemName)
                ? wo.ItemName
                : wo.Name;

        // WO Name (bold title)
        content.Children.Add(new TextBlock
        {
            Text = displayTitle,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(25, 25, 55)),
            TextWrapping = TextWrapping.Wrap,
        });

        // Always show system name as sub-text
        content.Children.Add(new TextBlock
        {
            Text = wo.Name,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 160)),
            TextWrapping = TextWrapping.Wrap,
        });

        // Production Item (description)
        if (!string.IsNullOrWhiteSpace(wo.ProductionItem))
            content.Children.Add(new TextBlock
            {
                Text = wo.ProductionItem,
                FontSize = 12,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
            });

        // Divider
        content.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 2) });

        // Bottom row: Qty badge
        content.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(20, 100, 100, 110)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(7, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = $"Qty: {wo.Qty?.ToString("F0") ?? "—"}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 100)),
            },
        });

        // Build card: left colored strip (4px) + content, clipped to rounded corners
        var cardGrid = new Grid();
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition(4, GridUnitType.Pixel));
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        var strip = new Border
        {
            Background = new SolidColorBrush(accentColor),
            CornerRadius = new CornerRadius(8, 0, 0, 8),
        };
        Grid.SetColumn(strip, 0);
        Grid.SetColumn(content, 1);
        cardGrid.Children.Add(strip);
        cardGrid.Children.Add(content);

        var card = new Border
        {
            Child = cardGrid,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(Color.FromArgb(40, 200, 200, 210)),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
        };
        card.PointerPressed += (_, _) => ShowJobCards(salesOrder, wo.Name);
        card.PointerEntered += (s, _) => ((Border)s!).BorderBrush = new SolidColorBrush(Color.FromArgb(120, accentColor.R, accentColor.G, accentColor.B));
        card.PointerExited += (s, _) => ((Border)s!).BorderBrush = new SolidColorBrush(Color.FromArgb(40, 200, 200, 210));
        return card;
    }

    // ---- Sales Order card (new design) ----

    private Border MakeSOCard(SalesOrderRow so)
    {
        var grid = new Grid { Margin = new Thickness(8, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition(110, GridUnitType.Pixel));  // ID
        grid.ColumnDefinitions.Add(new ColumnDefinition(190, GridUnitType.Pixel));  // Name
        grid.ColumnDefinitions.Add(new ColumnDefinition(200, GridUnitType.Pixel));  // Timeline
        grid.ColumnDefinitions.Add(new ColumnDefinition(160, GridUnitType.Pixel));  // Customer
        grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));     // Status

        // Col 0 — ID (blue link style)
        var idTb = new TextBlock
        {
            Text = so.Name,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 120, 230)),
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(idTb, 0);
        grid.Children.Add(idTb);

        // Col 1 — Name (title or name as heading, customer sub-text)
        var displayTitle = string.IsNullOrWhiteSpace(so.Title) ? so.Name : so.Title;
        var namePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        namePanel.Children.Add(new TextBlock { Text = displayTitle, FontSize = 13, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        if (!string.IsNullOrWhiteSpace(so.Customer))
            namePanel.Children.Add(new TextBlock { Text = so.Customer, FontSize = 11, Foreground = Brushes.Gray, TextTrimming = TextTrimming.CharacterEllipsis });
        Grid.SetColumn(namePanel, 1);
        grid.Children.Add(namePanel);

        // Col 2 — Timeline
        var start = so.TransactionDate ?? "—";
        var end = so.DeliveryDate ?? "—";
        var timePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Spacing = 4 };
        void AddTime(string label, string date)
        {
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = date, FontSize = 12 });
            sp.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = Brushes.Gray });
            timePanel.Children.Add(sp);
        }
        AddTime("START", start);
        timePanel.Children.Add(new TextBlock { Text = "→", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray });
        AddTime("END", end);
        Grid.SetColumn(timePanel, 2);
        grid.Children.Add(timePanel);

        // Col 3 — Customer
        var custTb = new TextBlock
        {
            Text = so.Customer ?? "—",
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(custTb, 3);
        grid.Children.Add(custTb);

        // Col 4 — Status badge
        var badge = MakeStatusBadge(so.Status ?? "Draft");
        Grid.SetColumn(badge, 4);
        grid.Children.Add(badge);

        var border = new Border
        {
            Child = grid,
            Background = Brushes.Transparent,
            Padding = new Thickness(0, 6),
            BorderBrush = new SolidColorBrush(Color.FromArgb(40, 200, 200, 200)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
        };
        border.PointerPressed += (_, _) => ShowWorkOrders(so.Name);
        border.PointerEntered += (s, _) => ((Border)s!).Background = new SolidColorBrush(Color.FromArgb(20, 100, 120, 255));
        border.PointerExited += (s, _) => ((Border)s!).Background = Brushes.Transparent;
        return border;
    }

    private static Border MakeStatusBadge(string status)
    {
        // Color map matching the design reference
        var (bg, fg) = status.ToUpperInvariant() switch
        {
            "COMPLETED" or "CLOSED" => (Color.FromArgb(40, 60, 200, 80), Color.FromRgb(60, 180, 60)),
            "IN PROCESS" or "IN PROGRESS" => (Color.FromArgb(40, 240, 160, 40), Color.FromRgb(200, 130, 20)),
            "SUBMITTED" or "TO DELIVER AND BILL" or "TO BILL" or "TO DELIVER"
                                           => (Color.FromArgb(40, 80, 160, 240), Color.FromRgb(60, 120, 220)),
            "CANCELLED" or "STOPPED" => (Color.FromArgb(40, 200, 60, 60), Color.FromRgb(180, 40, 40)),
            _ => (Color.FromArgb(40, 160, 160, 160), Color.FromRgb(130, 130, 130)),
        };

        return new Border
        {
            Background = new SolidColorBrush(bg),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 3),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = status.ToUpperInvariant(),
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(fg),
            },
        };
    }
}
