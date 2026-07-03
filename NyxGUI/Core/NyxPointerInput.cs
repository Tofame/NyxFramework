namespace NyxGui;

/// <summary>
/// Provides three depth-first hit-test traversals for different purposes:
///
/// <b>1. FindFocusableAt</b> — returns the deepest <see cref="NyxElement.Focusable"/> widget
/// under the point.  Used to route keyboard focus on mouse-down.
///
/// <b>2. FindCapturingWidget</b> — returns the deepest interactive widget under the point
/// (buttons, scrollbars, text entries, combo boxes, sliders, checkboxes, radio buttons,
/// and scrollable panels).  Used to decide whether a click should start a drag or be
/// handled by the interactive widget.
///
/// <b>3. FindDeepestHit</b> — returns the deepest visible widget under the point, including
/// non-interactive elements like labels and panels.  Used for general hit-testing and
/// tooltip routing.
///
/// All three traversals handle <see cref="NyxMiniWindow"/> (chrome → body recursion),
/// <see cref="NyxScrollablePanel"/> (delegates to body), and <see cref="NyxContainer"/>
/// (reversed children for top-to-bottom z-order).
///
/// <b>CapturesPointer</b> hardcodes the set of interactive widget types.
/// A phantom or disabled widget delegates to its children without capturing itself.
/// </summary>
internal static class NyxPointerInput
{
    /// <summary>Widget types that consume pointer events (prevent drag-through).</summary>
    public static bool CapturesPointer(NyxElement element) =>
        element is ICapturesPointer
            or NyxButton
            or NyxCheckBox
            or NyxRadioButton
            or NyxTextBox
            or NyxTextArea
            or NyxComboBox
            or NyxSlider
            or NyxVScrollBar
            or NyxHScrollBar
            or NyxScrollablePanel;

    /// <summary>Deepest <see cref="NyxElement.Focusable"/> widget under the point, or null.</summary>
    public static NyxElement? FindFocusableAt(NyxElement scope, int x, int y)
    {
        if (!scope.Visible || !scope.Bounds.Contains(x, y))
            return null;

        if (scope.Phantom || !scope.Enabled)
            return FindFocusableInChildren(scope, x, y);

        switch (scope)
        {
            case NyxMiniWindow mini:
            {
                for (var i = mini.Children.Count - 1; i >= 0; i--)
                {
                    var chromeHit = FindFocusableAt(mini.Children[i], x, y);
                    if (chromeHit is not null)
                        return chromeHit;
                }

                var bodyHit = FindFocusableAt(mini.Body, x, y);
                return bodyHit ?? (mini.Focusable ? mini : null);
            }
            case NyxScrollablePanel scroll:
                return FindFocusableAt(scroll.Body, x, y) ?? (scroll.Focusable ? scroll : null);
            case NyxContainer content:
            {
                for (var i = content.Children.Count - 1; i >= 0; i--)
                {
                    var hit = FindFocusableAt(content.Children[i], x, y);
                    if (hit is not null)
                        return hit;
                }

                return content.Focusable ? content : null;
            }
        }

        return scope.Focusable ? scope : null;
    }

    /// <summary>Deepest button / scroll / checkbox under the point, or null.</summary>
    public static NyxElement? FindCapturingWidget(NyxElement scope, int x, int y)
    {
        if (!scope.Visible || !scope.Bounds.Contains(x, y))
            return null;

        if (scope.Phantom || !scope.Enabled)
            return FindCapturingInChildren(scope, x, y);

        switch (scope)
        {
            case NyxMiniWindow mini:
            {
                for (var i = mini.Children.Count - 1; i >= 0; i--)
                {
                    var chrome = FindCapturingWidget(mini.Children[i], x, y);
                    if (chrome is not null)
                        return chrome;
                }

                return FindCapturingWidget(mini.Body, x, y);
            }
            case NyxScrollablePanel scroll:
                return FindCapturingWidget(scroll.Body, x, y);
            case NyxContainer:
                return FindCapturingInChildren(scope, x, y) ?? (CapturesPointer(scope) ? scope : null);
        }

        return CapturesPointer(scope) ? scope : null;
    }

    /// <summary>Deepest visible widget under the point (labels and panels included).</summary>
    public static NyxElement? FindDeepestHit(NyxElement scope, int x, int y)
    {
        if (!scope.Visible || !scope.Bounds.Contains(x, y))
            return null;

        if (scope.Phantom || !scope.Enabled)
            return FindDeepestInChildren(scope, x, y);

        switch (scope)
        {
            case NyxMiniWindow mini:
            {
                for (var i = mini.Children.Count - 1; i >= 0; i--)
                {
                    var chromeHit = FindDeepestHit(mini.Children[i], x, y);
                    if (chromeHit is not null)
                        return chromeHit;
                }

                var bodyHit = FindDeepestHit(mini.Body, x, y);
                return bodyHit ?? mini;
            }
            case NyxScrollablePanel scroll:
                return FindDeepestHit(scroll.Body, x, y);
            case NyxContainer content:
            {
                for (var i = content.Children.Count - 1; i >= 0; i--)
                {
                    var hit = FindDeepestHit(content.Children[i], x, y);
                    if (hit is not null)
                        return hit;
                }

                return content;
            }
        }

        return scope;
    }

    /// <summary>Checks if the point is on the resize area of a <see cref="NyxMiniWindow"/>.</summary>
    public static bool IsOnMiniWindowResizeArea(NyxElement scope, int x, int y)
    {
        if (!scope.Visible) return false;

        switch (scope)
        {
            case NyxMiniWindow mini:
                return mini.IsResizingHeight || mini.Resizable && !mini.Minimized &&
                       x >= mini.Bounds.X && x < mini.Bounds.Right &&
                       y >= mini.Bounds.Bottom - mini.ResizeGripHitHeight && y < mini.Bounds.Bottom;
            case NyxScrollablePanel scroll:
                return IsOnMiniWindowResizeArea(scroll.Body, x, y);
            case NyxContainer content:
                for (var i = content.Children.Count - 1; i >= 0; i--)
                {
                    if (IsOnMiniWindowResizeArea(content.Children[i], x, y))
                        return true;
                }
                return false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Walks up the parent chain from <paramref name="deepestHit"/> looking for a
    /// draggable element.  A draggable MiniWindow only drags if the point is in the
    /// title bar (above the content area), not the body.  Stops at any element that
    /// <see cref="CapturesPointer"/> (interactive widgets consume the click).
    /// </summary>
    public static NyxElement? FindDraggablePanel(int x, int y, NyxElement? deepestHit)
    {
        for (var n = deepestHit; n is not null; n = n.Parent)
        {
            if (CapturesPointer(n))
                return null;

            if (n is NyxMiniWindow { Draggable: true } mini && mini.HitTest(x, y))
            {
                if (!mini.Minimized && y >= mini.Bounds.Y + mini.TitleBarHeight)
                    continue;

                return mini;
            }

            if (n is NyxContainer panel && panel.HitTest(x, y))
                return panel;
        }

        return null;
    }

    private static NyxElement? FindCapturingInChildren(NyxElement scope, int x, int y)
    {
        if (scope is not NyxContainer content)
            return null;

        for (var i = content.Children.Count - 1; i >= 0; i--)
        {
            var found = FindCapturingWidget(content.Children[i], x, y);
            if (found is not null)
                return found;
        }

        return null;
    }

    private static NyxElement? FindDeepestInChildren(NyxElement scope, int x, int y)
    {
        if (scope is not NyxContainer content)
            return null;

        for (var i = content.Children.Count - 1; i >= 0; i--)
        {
            var hit = FindDeepestHit(content.Children[i], x, y);
            if (hit is not null)
                return hit;
        }

        return null;
    }

    private static NyxElement? FindFocusableInChildren(NyxElement scope, int x, int y)
    {
        if (scope is not NyxContainer content)
            return null;

        for (var i = content.Children.Count - 1; i >= 0; i--)
        {
            var hit = FindFocusableAt(content.Children[i], x, y);
            if (hit is not null)
                return hit;
        }

        return null;
    }
}
