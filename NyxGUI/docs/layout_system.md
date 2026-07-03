# Layout & Coordinate System

NyxGUI uses a retained-mode hierarchical layout tree. Understanding how coordinates propagate and how the layout passes execute is essential for creating correctly positioned, responsive user interfaces.

---

## 1. Absolute Screen Space

Unlike some GUI systems that store coordinates relative to their parent's top-left corner, **NyxGUI stores all widget `Bounds` in absolute screen space**. 
- A widget at `Bounds = (250, 150, 100, 30)` will draw exactly at `x = 250` and `y = 150` on the screen, regardless of where its parent is.
- This simplifies drawing commands and hit-testing since coordinates do not need to be transformed recursively on every frame.

---

## 2. Parent-Child Propagation in `SetBounds`

To ensure that children move together with their parent, `NyxContainer` overrides `SetBounds(NyxRect newBounds)`. Whenever a container's position changes, it calculates the translation delta (`dx`, `dy`) and offsets the bounds of all its children:

```csharp
public override void SetBounds(NyxRect newBounds)
{
    var old = Bounds;
    var dx = newBounds.X - old.X;
    var dy = newBounds.Y - old.Y;
    if (dx != 0 || dy != 0)
    {
        foreach (var c in _children)
        {
            var b = c.Bounds;
            c.SetBounds(new NyxRect(b.X + dx, b.Y + dy, b.Width, b.Height));
        }
    }
    base.SetBounds(newBounds);
}
```

---

## 3. The Initialization Pitfall (And How to Avoid It)

Because adding a child via `AddChild()` does **not** translate the child's coordinates automatically, creating and placing containers programmatically requires a specific initialization sequence.

### 🚫 The Incorrect Way (Causes Misalignment)
```csharp
// 1. Panel is created directly at (200, 200) screen space
var panel = new NyxContainer(new NyxRect(200, 200, 280, 120));

// 2. Button is created relative to (0, 0) of the panel
var button = new NyxButton();
button.SetBounds(new NyxRect(10, 10, 80, 26)); // interpreted as (10, 10) screen space!

// 3. Button is added
panel.AddChild(button);
// BUG: The panel is drawn at (200, 200), but the button is drawn at (10, 10)!
```

###  The Correct Way
Initialize the container at `(0, 0)`, define the children relative to `(0, 0)`, and then move the container to its final screen coordinates at the end of setup:

```csharp
// 1. Create panel at (0, 0)
var panel = new NyxContainer(new NyxRect(0, 0, 280, 120));

// 2. Create child relative to (0, 0)
var button = new NyxButton();
button.SetBounds(new NyxRect(10, 10, 80, 26));

// 3. Add child
panel.AddChild(button);

// 4. Position the panel. SetBounds calculates dx/dy and shifts the button automatically!
panel.SetBounds(new NyxRect(200, 200, 280, 120)); // Button is translated to (210, 210)
```

---

## 4. Layout Resolution Phases

When documents are loaded or when parent size constraints change, NyxGUI performs a two-pass layout pass:

1. **Measure Pass (`Measure`)**:
   - Each widget calculates its desired size (`DesiredSize`) based on its contents (e.g. text length, font size) and constraints.
   - For example, `label.Measure(availableSize)` determines the minimum bounding box needed to render the label text.
   
2. **Arrange Pass (`Arrange`)**:
   - The parent container assigns the final bounds (`Bounds`) to each child based on the layout rules (such as anchors or panel orientations).
   - Once arranged, `SetBounds` is called on the child, locking in its screen-space coordinates.

---

## 5. Invalidation Model

Layouts are not calculated every frame. Instead, NyxGUI uses an invalidation loop coalesced by the document's `Flush()` routine:

- **`InvalidateLayout()`**: Bubbled up to parent containers when children are added, removed, or changed. Triggers a `Measure` and `Arrange` pass on the next update.
- **`InvalidateStyle()`**: Bubbles down the subtree. Force-recalculates properties (colors, borders) from the active theme.
- **`InvalidateRender()`**: Queues a repaint commands rebuild without changing layout or styling.

---

## 6. Layout Engines

To simplify UI building and eliminate manual positioning calculations, NyxGUI supports a layout engine framework. Instead of defining absolute bounds or edge anchors for every child, a container can define a `Layout` strategy.

During the layout resolution phases:
1. `Measure` checks if a container has a `Layout` strategy. If it does, the strategy measures all visible children and computes the aggregate desired size.
2. `Arrange` runs the layout strategy to position and size the visible children inside the final allocated bounding box of the container.

### Supported Layout Strategies

#### 📐 Stack Layout (`NyxStackLayout`)
Stacks child elements sequentially in a single column or row.
- **Orientation**: Stacks vertically (default) or horizontally.
- **Spacing**: Adds uniform pixel spacing between children.
- **Alignment**: Aligns children along the cross-axis (`Start` (default), `Center`, `End`, or `Stretch`).

#### 🧮 Grid Layout (`NyxGridLayout`)
Arranges child elements in columns and rows.
- **Columns & Rows**: If `Columns > 0` (default), flows horizontally (row-by-row) with a fixed column count. If `Rows > 0` and `Columns = 0`, flows vertically (column-by-column) with a fixed row count.
- **Spacing**: Adds uniform spacing between cells.
- **Auto-fit & Cell Size**: When `FitChildren = true` (the default), any dimension **not** constrained by an explicit `CellWidth` / `CellHeight` is automatically stretched to fill the available space in that axis — width is divided equally across columns, height across rows. If both dimensions are explicit, `FitChildren` has no effect. Set `fit_children = false` to always use the children's own desired size.

#### ⚓ Dock Layout (`NyxDockLayout`)
Arranges child elements by docking them to specific outer edges of the container.
- **Dock Edges**: Children can set `Dock` (`Left`, `Right`, `Top`, `Bottom`, `Fill`).
- **Layout Order**: Children are laid out sequentially. The last child with `Dock = Fill` fills the remaining center area.

#### 🔄 Wrap Layout (`NyxWrapLayout`)
Positions child elements sequentially (horizontally or vertically) and automatically wraps them to the next line or row when they exceed the container's bounds.
- **Orientation**: Wrap horizontally (default) or vertically.
- **Spacing**: Adds uniform spacing between items.

