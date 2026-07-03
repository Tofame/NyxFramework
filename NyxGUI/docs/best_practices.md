# Best Practices & Pitfalls

Following these design patterns and developer guidelines will keep your UI code clean, maintainable, performant, and bug-free.

---

## 1. Always Coordinate Coordinate Spaces at Setup

As absolute coordinates propagate down to child elements during layout changes, constructing widgets requires care.

* **The Rule**: Always instantiate panels/containers at `(0, 0)`. Position all children using coordinates relative to `(0, 0)`. Finally, call `SetBounds` on the parent container to shift the whole block into position on the screen.
* **Why**: If you create a panel at `(200, 200)` and add a child at `(10, 10)`, the child will draw at `(10, 10)` in screen space (misaligned to the top-left of the screen) because `AddChild()` does not convert local spaces into absolute screen coordinates.

* **Example Code Pattern**:
  ```csharp
  // 1. Create panel at (0, 0)
  _panel = new NyxContainer(new NyxRect(0, 0, PanelW, PanelH));

  // 2. Set child bounds relative to panel
  _okButton.SetBounds(new NyxRect(PanelW - 80, PanelH - 30, 70, 26));

  // 3. Add to parent
  _panel.AddChild(_okButton);
  _root.AddChild(_panel);

  // 4. Position panel (this translates children automatically)
  _panel.SetBounds(CenterPanel(viewportWidth, viewportHeight));
  ```

---

## 2. Prefer Declarative Layouts (`.nyxui`)

Keep UI visual structure separate from logic:
- **Declarative first**: Define widget types, sizes, alignment, and anchors in `.nyxui` files.
- **Imperative second**: Write your host wrapper class in C# to retrieve widget references (`document.TryGet<T>("widgetId")`), wire event handlers, and synchronize game data to text properties.
- **Why**: Keeps UI markup clean and readable, makes structural updates simple, and facilitates UI hot-reloads without recompiling C# code.

---

## 3. Keep Logic Out of the Paint Loop

The `Paint()` method is meant purely for rendering commands:
- **Do not** modify widget values, change child hierarchy, recalculate layout constraints, or run complex game calculations inside `Paint()`.
- **Do**: Perform updates inside your component's `Update()` loop or in response to events (e.g. `Click`, `SliderValueChanged`).
- **Why**: Mutating layout structure inside rendering passes causes invalidation loops, layout recalculation lag, and visual jittering.

---

## 4. Optimize Updates with Caching

Avoid pushing updates to widgets on every frame if the underlying data has not changed.
- **The Pattern**: Keep track of a local version/signature of the displayed data. Only update the widget properties (like `.Text` or sprite textures) when this signature changes:
  ```csharp
  public void Refresh(Player player)
  {
      if (player.Health == _lastHealthValue) return;

      _lastHealthValue = player.Health;
      _healthLabel.Text = $"HP: {player.Health}";
  }
  ```
- **Why**: Changing properties like `.Text` triggers layout invalidations. Caching values avoids unnecessary layout parsing and string allocations.

---

## 5. Handle Z-Ordering Correctly

If you need overlays (such as context menus or dialog popups) to draw above other widgets:
- Ensure they are added as the **last child** in their parent container.
- Or call `BringChildToFront(child)` on the parent container when displaying the overlay.
- For modal dialogs that cover the entire viewport, use the global `NyxGuiRootStack` to register the dialog root element above normal game UI layers.

---

## 6. Use `Phantom` Sparingly

Setting `phantom = true` tells the input router to ignore the widget for click/hover collision checking.
- **When to use**: Use it on container panels that are serving purely as invisible alignment frames, allowing clicks to pass through to widgets or game fields underneath them.
- **When NOT to use**: Do not use it on a container if you expect to handle background clicks on it or if children need the container to capture cursor pointer bounds.
