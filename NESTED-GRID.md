# Nested Grid Layout Specification

## Overview

Current limitation: The monitoring dashboard supports only a single-row layout with col-span support. This makes it impossible to:
- Create multi-row layouts
- Span items across multiple rows (row-span)
- Group related metrics visually with optional titles
- Create reusable metric groups

**Solution**: Introduce nested grid definitions that allow compositional, multi-row layouts.

## Problem Statement

The original problem is the lack of **row-span** support. Row-span is computationally hard because items create layout dependencies across rows (cells need to track occupation state from items above them).

Nested grids provide a workaround: an item can contain a sub-grid with multiple rows, effectively allowing that item to have more vertical space while maintaining simpler layout mathematics.

**Trade-off**: Nested grids don't provide true row-span (an item explicitly spanning rows 0-2), but they enable compositional layouts that solve 90% of row-span use cases.

## Design Decisions

### 1. Grid References Use `@` Prefix

Grid references are distinguished from metric sources using the `@` prefix:
- `"@Compute"` → References a grid definition named "Compute"
- `"CPU/8"` → Metric source (existing format)

**Rationale**: Explicit syntax prevents naming conflicts and makes intent clear. Fail-fast validation on undefined references.

### 2. Grid Definitions Are Flat (But Usage Is Nested)

Grid definitions themselves are simple 2D arrays:
```json
"GridDefinitions": {
  "Compute": {
    "Layout": [
      ["CPU/8", "RAM/1"],
      ["GPU/1"]
    ]
  }
}
```

But grids can **reference** other grids, creating nesting at usage time:
```json
"GridDefinitions": {
  "FullNode": {
    "Layout": [
      ["@Compute", "Docker/1"]
    ]
  }
}
```

**Rationale**: Keeps definition syntax simple while allowing composition.

### 3. Arbitrary Nesting Depth Allowed

Grids can reference other grids with no depth limit (except validation prevents circular references).

**Rationale**: Enables powerful composition (e.g., "Cluster" → "Node" → "Compute" → individual metrics). Real-world configs are unlikely to exceed 3-4 levels deep.

### 4. Optional Titles With No-Padding Default

Grids can have an optional `Title` property:
- If `Title` is null or empty: no padding, margins, or borders
- If `Title` is present: render title bar + padding around internal layout

**Rationale**: Titles enable visual grouping when needed, but don't force visual overhead when not needed.

### 5. Fail-Fast Validation at Startup

All configuration errors are detected and reported at application startup:
- Undefined grid references
- Circular references
- Invalid metric source formats
- Empty layouts

**Rationale**: Prevents runtime rendering issues. If validation passes, rendering should always work.

### 6. No Backward Compatibility

The `Sources` property is replaced by `Layout`. No migration support.

**Rationale**: Keeps implementation simple. Users can manually migrate configs.

## Configuration Format

### Type Definitions (C#)

```csharp
// In ModelingEvolution.BlazorPerfMon.Shared

[MessagePackObject]
public class GridDefinition
{
    /// <summary>
    /// Optional title displayed above the grid.
    /// When null/empty, no padding or margins are applied.
    /// </summary>
    [Key(0)]
    public string? Title { get; init; }

    /// <summary>
    /// 2D array representing rows and columns.
    /// Each element is either:
    /// - A metric source string (e.g., "CPU/8", "Network:eth0/2|col-span:3")
    /// - A grid reference (e.g., "@Compute", "@NetworkGroup|col-span:2")
    /// </summary>
    [Key(1)]
    public string[][] Layout { get; init; }
}

[MessagePackObject]
public class MonitorSettings
{
    /// <summary>
    /// Named grid definitions that can be referenced in layouts.
    /// Keys are grid names (used with @ prefix in layouts).
    /// </summary>
    [Key(0)]
    public Dictionary<string, GridDefinition>? GridDefinitions { get; init; }

    /// <summary>
    /// Root layout - 2D array of rows and columns.
    /// Replaces the old "Sources" property.
    /// </summary>
    [Key(1)]
    public string[][] Layout { get; init; }
}
```

### Example Configuration (appsettings.json)

```json
{
  "Monitoring": {
    "GridDefinitions": {
      "Compute": {
        "Title": "Compute Resources",
        "Layout": [
          ["CPU/8", "RAM/1"],
          ["GPU/1"]
        ]
      },
      "NetworkInterfaces": {
        "Layout": [
          ["Network:eth0/2", "Network:eth1/2", "Network:wlan0/2"]
        ]
      },
      "FullNode": {
        "Title": "Node Overview",
        "Layout": [
          ["@Compute"],
          ["@NetworkInterfaces", "Docker/1"]
        ]
      }
    },
    "Layout": [
      ["@FullNode|col-span:2", "Disk:sda/4"],
      ["@NetworkInterfaces|col-span:3"]
    ]
  }
}
```

## Validation Rules

Implement validation in `MonitorSettings.Validate()` method, called at startup:

### 1. Undefined Grid References
```csharp
foreach (var gridRef in GetAllGridReferences())
{
    var gridName = gridRef.TrimStart('@').Split('|')[0];
    if (!GridDefinitions.ContainsKey(gridName))
        throw new InvalidOperationException($"Undefined grid reference: {gridRef}");
}
```

### 2. Circular References
```csharp
void DetectCycle(string gridName, HashSet<string> visited, List<string> path)
{
    if (!visited.Add(gridName))
    {
        path.Add(gridName);
        throw new InvalidOperationException(
            $"Circular reference detected: {string.Join(" -> ", path)}");
    }

    path.Add(gridName);

    foreach (var item in GridDefinitions[gridName].Layout.SelectMany(row => row))
    {
        if (item.StartsWith("@"))
        {
            var childGrid = item.TrimStart('@').Split('|')[0];
            DetectCycle(childGrid, new HashSet<string>(visited), new List<string>(path));
        }
    }
}
```

### 3. Invalid Metric Sources
```csharp
foreach (var item in GetAllLayoutItems())
{
    if (!item.StartsWith("@"))
    {
        if (!MetricSource.TryParse(item, null, out _))
            throw new FormatException($"Invalid metric source format: {item}");
    }
}
```

### 4. Empty Layouts
```csharp
if (Layout == null || Layout.Length == 0 || Layout.Any(row => row.Length == 0))
    throw new InvalidOperationException("Root layout cannot be null or contain empty rows");

foreach (var (name, grid) in GridDefinitions)
{
    if (grid.Layout == null || grid.Layout.Length == 0 ||
        grid.Layout.Any(row => row.Length == 0))
        throw new InvalidOperationException(
            $"Grid '{name}' layout cannot be null or contain empty rows");
}
```

### 5. Maximum Nesting Depth (Optional)
```csharp
const int MAX_DEPTH = 10;

int GetMaxDepth(string gridName, int currentDepth = 0)
{
    if (currentDepth > MAX_DEPTH)
        throw new InvalidOperationException(
            $"Maximum nesting depth ({MAX_DEPTH}) exceeded at grid: {gridName}");

    var maxChildDepth = 0;
    foreach (var item in GridDefinitions[gridName].Layout.SelectMany(row => row))
    {
        if (item.StartsWith("@"))
        {
            var childGrid = item.TrimStart('@').Split('|')[0];
            maxChildDepth = Math.Max(maxChildDepth, GetMaxDepth(childGrid, currentDepth + 1));
        }
    }

    return currentDepth + maxChildDepth;
}
```

## Rendering Algorithm

### Layout Item Representation

```csharp
public abstract class LayoutItem
{
    public uint ColSpan { get; init; }
}

public class MetricSourceItem : LayoutItem
{
    public MetricSource Source { get; init; }
}

public class GridReferenceItem : LayoutItem
{
    public string GridName { get; init; }
    public GridDefinition Grid { get; init; }
}
```

### Parsing Layout Items

```csharp
LayoutItem ParseLayoutItem(string item)
{
    // Extract col-span if present
    var parts = item.Split('|');
    var name = parts[0];
    var colSpan = 1u;

    if (parts.Length > 1)
    {
        var colSpanPart = parts[1];
        if (colSpanPart.StartsWith("col-span:", StringComparison.OrdinalIgnoreCase))
        {
            colSpan = uint.Parse(colSpanPart.Substring("col-span:".Length));
        }
    }

    if (name.StartsWith("@"))
    {
        var gridName = name.Substring(1);
        return new GridReferenceItem
        {
            GridName = gridName,
            Grid = GridDefinitions[gridName],
            ColSpan = colSpan
        };
    }
    else
    {
        return new MetricSourceItem
        {
            Source = MetricSource.Parse(item, null),
            ColSpan = colSpan
        };
    }
}
```

### Recursive Rendering

```csharp
void RenderLayout(string[][] layout, SKRect bounds, GridDefinition? gridDef = null)
{
    // Apply title padding if grid has title
    if (gridDef != null && !string.IsNullOrEmpty(gridDef.Title))
    {
        var titleHeight = 30f; // Or calculate based on font size
        RenderTitle(gridDef.Title, bounds);
        bounds = new SKRect(
            bounds.Left + 8,
            bounds.Top + titleHeight + 8,
            bounds.Right - 8,
            bounds.Bottom - 8
        );
    }

    var rowHeight = bounds.Height / layout.Length;

    for (int rowIndex = 0; rowIndex < layout.Length; rowIndex++)
    {
        var row = layout[rowIndex];
        var rowBounds = new SKRect(
            bounds.Left,
            bounds.Top + rowIndex * rowHeight,
            bounds.Right,
            bounds.Top + (rowIndex + 1) * rowHeight
        );

        // Calculate total col units for this row
        uint totalColUnits = 0;
        var items = row.Select(ParseLayoutItem).ToArray();
        foreach (var item in items)
            totalColUnits += item.ColSpan;

        if (totalColUnits == 0) continue;

        // Render items in this row
        uint colOffset = 0;
        float unitWidth = rowBounds.Width / totalColUnits;

        foreach (var item in items)
        {
            var itemBounds = new SKRect(
                rowBounds.Left + colOffset * unitWidth,
                rowBounds.Top,
                rowBounds.Left + (colOffset + item.ColSpan) * unitWidth,
                rowBounds.Bottom
            );

            if (item is GridReferenceItem gridRef)
            {
                // Recursively render nested grid
                RenderLayout(gridRef.Grid.Layout, itemBounds, gridRef.Grid);
            }
            else if (item is MetricSourceItem metricItem)
            {
                // Render chart for metric source
                RenderChart(metricItem.Source, itemBounds);
            }

            colOffset += item.ColSpan;
        }
    }
}
```

### Row Height Calculation

All rows within a layout have **equal height**:
```csharp
var rowHeight = availableHeight / layout.Length;
```

**Rationale**: Simple, predictable, avoids complex height calculation logic.

### Column Width Calculation

Each row independently calculates column widths based on its col-span units:
```csharp
uint totalColUnits = row.Sum(item => item.ColSpan);
float unitWidth = availableWidth / totalColUnits;
```

**Rationale**: Same as current system, proven to work well.

## Data Transmission to WASM

Update `PerformanceConfigurationSnapshot` to include grid definitions:

```csharp
[MessagePackObject]
public readonly record struct PerformanceConfigurationSnapshot
{
    [Key(0)]
    public ImmutableArray<MetricSource> AvailableMetrics { get; init; }

    [Key(1)]
    public ImmutableDictionary<string, GridDefinition>? GridDefinitions { get; init; }

    [Key(2)]
    public ImmutableArray<ImmutableArray<string>> RootLayout { get; init; }
}
```

**Note**: Convert `string[][]` to `ImmutableArray<ImmutableArray<string>>` for MessagePack compatibility.

## Example Use Cases

### 1. Multi-Row Layout (No Nesting)

```json
{
  "Layout": [
    ["CPU/8", "RAM/1", "GPU/1"],
    ["Network:eth0/2", "Network:wlan0/2", "Disk:sda/4"],
    ["Docker/1|col-span:3"]
  ]
}
```

Visual:
```
┌─────────┬─────────┬─────────┐
│  CPU    │  RAM    │  GPU    │
├─────────┼─────────┼─────────┤
│ eth0    │ wlan0   │  sda    │
├─────────┴─────────┴─────────┤
│         Docker              │
└─────────────────────────────┘
```

### 2. Grouped Metrics With Title

```json
{
  "GridDefinitions": {
    "NetworkInterfaces": {
      "Title": "Network Interfaces",
      "Layout": [
        ["Network:eth0/2", "Network:eth1/2", "Network:wlan0/2"]
      ]
    }
  },
  "Layout": [
    ["CPU/8", "RAM/1"],
    ["@NetworkInterfaces|col-span:2"]
  ]
}
```

Visual:
```
┌─────────┬─────────┐
│  CPU    │  RAM    │
├─────────┴─────────┤
│ Network Interfaces│
│ ┌─────┬─────┬───┐ │
│ │eth0 │eth1 │wl0│ │
│ └─────┴─────┴───┘ │
└───────────────────┘
```

### 3. Reusable Compute Node

```json
{
  "GridDefinitions": {
    "ComputeNode": {
      "Title": "Compute",
      "Layout": [
        ["CPU/8", "RAM/1"],
        ["GPU/1"]
      ]
    }
  },
  "Layout": [
    ["@ComputeNode", "@ComputeNode"],
    ["@ComputeNode", "@ComputeNode"]
  ]
}
```

Visual (4x the same compute node):
```
┌──────────────┬──────────────┐
│ Compute      │ Compute      │
│ ┌────┬─────┐ │ ┌────┬─────┐ │
│ │CPU │ RAM │ │ │CPU │ RAM │ │
│ ├────┴─────┤ │ ├────┴─────┤ │
│ │   GPU    │ │ │   GPU    │ │
│ └──────────┘ │ └──────────┘ │
├──────────────┼──────────────┤
│ Compute      │ Compute      │
│ ┌────┬─────┐ │ ┌────┬─────┐ │
│ │CPU │ RAM │ │ │CPU │ RAM │ │
│ ├────┴─────┤ │ ├────┴─────┤ │
│ │   GPU    │ │ │   GPU    │ │
│ └──────────┘ │ └──────────┘ │
└──────────────┴──────────────┘
```

### 4. Nested Composition

```json
{
  "GridDefinitions": {
    "Compute": {
      "Layout": [["CPU/8", "RAM/1"], ["GPU/1"]]
    },
    "Network": {
      "Layout": [["Network:eth0/2", "Network:eth1/2"]]
    },
    "FullNode": {
      "Title": "Node Overview",
      "Layout": [
        ["@Compute", "Docker/1"],
        ["@Network|col-span:2"]
      ]
    }
  },
  "Layout": [
    ["@FullNode"]
  ]
}
```

Visual:
```
┌────────────────────────────────┐
│ Node Overview                  │
│ ┌──────────────┬─────────────┐ │
│ │ ┌─────┬────┐ │             │ │
│ │ │ CPU │RAM │ │   Docker    │ │
│ │ ├─────┴────┤ │             │ │
│ │ │   GPU    │ │             │ │
│ │ └──────────┘ │             │ │
│ ├──────────────┴─────────────┤ │
│ │ ┌──────────┬──────────┐    │ │
│ │ │  eth0    │  eth1    │    │ │
│ │ └──────────┴──────────┘    │ │
│ └────────────────────────────┘ │
└────────────────────────────────┘
```

## Edge Cases

### 1. Grid References Itself
```json
{
  "GridDefinitions": {
    "A": { "Layout": [["@A"]] }
  }
}
```
**Result**: Fail-fast validation detects cycle: `A -> A`

### 2. Mutual Recursion
```json
{
  "GridDefinitions": {
    "A": { "Layout": [["@B"]] },
    "B": { "Layout": [["@A"]] }
  }
}
```
**Result**: Fail-fast validation detects cycle: `A -> B -> A`

### 3. Undefined Grid Reference
```json
{
  "Layout": [["@NonExistent"]]
}
```
**Result**: Fail-fast validation throws: `Undefined grid reference: @NonExistent`

### 4. Empty Grid Definition
```json
{
  "GridDefinitions": {
    "Empty": { "Layout": [[]] }
  }
}
```
**Result**: Fail-fast validation throws: `Grid 'Empty' layout cannot contain empty rows`

### 5. Mixed Syntax in Same Item
```json
{
  "Layout": [["CPU/8@NetworkGroup"]]  // Invalid: mixing metric and grid reference
}
```
**Result**: Fail-fast validation throws: `Invalid metric source format: CPU/8@NetworkGroup`
**Note**: `@` must be at the start. If item starts with `@`, it's a grid ref; otherwise it must be valid MetricSource.

### 6. Title Without Content
```json
{
  "GridDefinitions": {
    "Empty": {
      "Title": "Empty Section",
      "Layout": [["CPU/8"]]
    }
  }
}
```
**Result**: Valid. Renders title with single CPU chart inside.

### 7. Grid With No Title But Padding Requested
Not possible - padding is only applied when `Title` is non-empty.

### 8. Very Deep Nesting (10+ levels)
```json
"A": { "Layout": [["@B"]] },
"B": { "Layout": [["@C"]] },
...
"K": { "Layout": [["CPU/8"]] }
```
**Result**: If `MAX_DEPTH` validation is implemented, fails at startup. Otherwise, allowed but may impact performance.

## Implementation Checklist

### Phase 1: Types and Configuration
- [ ] Add `GridDefinition` class to `Shared` project
- [ ] Update `MonitorSettings` with `GridDefinitions` and `Layout` properties
- [ ] Add MessagePack attributes to new types
- [ ] Update configuration loading in `Program.cs`

### Phase 2: Validation
- [ ] Implement `MonitorSettings.Validate()` method
- [ ] Add undefined reference detection
- [ ] Add circular reference detection
- [ ] Add invalid metric source detection
- [ ] Add empty layout detection
- [ ] Optional: Add max depth validation
- [ ] Call validation at application startup
- [ ] Add comprehensive unit tests for all validation rules

### Phase 3: Transmission
- [ ] Update `PerformanceConfigurationSnapshot` to include grid definitions and layout
- [ ] Update server-side snapshot creation
- [ ] Update client-side snapshot handling
- [ ] Test MessagePack serialization/deserialization

### Phase 4: Rendering (Client)
- [ ] Create `LayoutItem`, `MetricSourceItem`, `GridReferenceItem` types
- [ ] Implement `ParseLayoutItem()` method
- [ ] Update main render loop to use recursive `RenderLayout()`
- [ ] Implement title rendering with padding
- [ ] Ensure no padding/margins when title is null/empty
- [ ] Test with nested grids of various depths

### Phase 5: Testing
- [ ] Unit tests for parsing layout items
- [ ] Unit tests for col-span with grid references
- [ ] Integration tests for multi-row layouts
- [ ] Integration tests for nested grids
- [ ] Integration tests for titled vs. untitled grids
- [ ] Visual tests with real charts
- [ ] Performance tests with deep nesting

### Phase 6: Documentation
- [ ] Update README with nested grid examples
- [ ] Add migration guide from old `Sources` format
- [ ] Document grid definition best practices
- [ ] Add troubleshooting section

## Future Considerations

### 1. Grid Templates
Allow grids to accept parameters for reusability:
```json
"GridDefinitions": {
  "NodeTemplate": {
    "Parameters": ["nodeId"],
    "Layout": [["CPU:${nodeId}/8", "RAM:${nodeId}/1"]]
  }
}
```
Usage: `"@NodeTemplate(node1)"`

**Complexity**: High. Parameter parsing, substitution, validation.

### 2. Responsive Breakpoints
Different layouts for different screen sizes:
```json
"GridDefinitions": {
  "Compute": {
    "Layout": {
      "default": [["CPU/8", "RAM/1"]],
      "mobile": [["CPU/8"], ["RAM/1"]]
    }
  }
}
```

**Complexity**: Medium. Requires screen size detection and layout switching.

### 3. True Row-Span Support
Add `row-span` property to items:
```json
"Layout": [
  ["GPU|row-span:2", "CPU", "RAM"],
  ["Network", "Disk"]
]
```

**Complexity**: High. Requires cell occupation tracking and complex layout logic.

### 4. Dynamic Grid Definitions
Allow server to send grid definitions at runtime based on available hardware:
```csharp
// Server detects 3 network interfaces, generates grid dynamically
var networkGrid = new GridDefinition
{
    Layout = networkInterfaces.Select(iface =>
        new[] { $"Network:{iface}/2" }).ToArray()
};
```

**Complexity**: Medium. Requires server-side grid generation logic.

### 5. Grid Alignment Options
Control how grids align within their bounds:
```json
{
  "Title": "Compute",
  "Alignment": "center",  // or "stretch", "top", "bottom"
  "Layout": [...]
}
```

**Complexity**: Low-Medium. Requires bounds calculation changes.

## Open Questions

1. **Should grid names be case-sensitive?**
   - Recommendation: Yes (simpler, matches C# conventions)

2. **Should we support grid references with identifiers?**
   - Example: `@Network:eth0` where `Network` is a parameterized grid
   - Recommendation: No, not in initial implementation (can add later)

3. **Should empty grids be allowed if they have titles?**
   - Use case: Visual separator with just a title
   - Recommendation: No, requires at least one item

4. **Should we warn on unused grid definitions?**
   - Example: `"Compute"` defined but never referenced
   - Recommendation: Optional lint/warning, not an error

5. **Should col-span validation happen?**
   - Example: `col-span:15` exceeds typical 12-column grid
   - Recommendation: No validation, col-span is relative to siblings

## Summary

Nested grids provide a compositional layout system that:
- Solves 90% of row-span use cases through hierarchical layouts
- Enables visual grouping with optional titles
- Supports reusable metric groups
- Maintains simple layout mathematics (no cell occupation tracking)
- Fails fast on configuration errors

Trade-offs:
- More complex than flat layouts
- Requires recursive rendering
- Doesn't provide true row-span within a single grid
- Adds learning curve for configuration

**Recommendation**: Implement as a nice-to-have feature after core functionality is stable.
