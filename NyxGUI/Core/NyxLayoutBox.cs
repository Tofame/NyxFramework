namespace NyxGui;

/// <summary>
/// Edge-anchor layout spec for free-floating panels.
/// Containers (StackPanel, DockPanel, etc.) do not use this — they use Measure/Arrange.
/// </summary>
public sealed class NyxLayoutBox
{
    /// <summary>Edge anchor for the left side (e.g. parent.left, otherWidget.right).</summary>
    public NyxLayoutAnchor? Left { get; set; }

    /// <summary>Edge anchor for the right side.</summary>
    public NyxLayoutAnchor? Right { get; set; }

    /// <summary>Edge anchor for the top side.</summary>
    public NyxLayoutAnchor? Top { get; set; }

    /// <summary>Edge anchor for the bottom side.</summary>
    public NyxLayoutAnchor? Bottom { get; set; }

    /// <summary>Margin applied after anchor resolution: left, top, right, bottom.</summary>
    public NyxThickness Margin { get; set; }

	/// <summary>Padding applied to the content area: left, top, right, bottom.</summary>
	public NyxThickness Padding { get; set; }

	/// <summary>Which edge this child docks to inside a Dock panel layout.</summary>
	public Dock? Dock { get; set; }


    /// <summary>Fixed width used when left and right anchor to the same point (center alignment),
    /// or as the initial/layout width.</summary>
    public int FixedWidth { get; set; }

    /// <summary>Fixed height used when top and bottom anchor to the same point (center alignment),
    /// or as the initial/layout height.</summary>
    public int FixedHeight { get; set; }

    /// <summary>Minimum width constraint. 0 = unconstrained.</summary>
    public int MinWidth { get; set; }

    /// <summary>Minimum height constraint. 0 = unconstrained.</summary>
    public int MinHeight { get; set; }

    /// <summary>Maximum width constraint. 0 = unconstrained.</summary>
    public int MaxWidth { get; set; }

    /// <summary>Maximum height constraint. 0 = unconstrained.</summary>
    public int MaxHeight { get; set; }

    /// <summary>When true, width/height are locked and cannot be resized.</summary>
    public bool FixedSize { get; set; }

    public bool HasAnyAnchor => Left is not null || Right is not null || Top is not null || Bottom is not null;

    /// <summary>Yields widget IDs that must be laid out before this element (excludes "parent" and root window).</summary>
    public IEnumerable<string> GetWidgetDependencies(string rootWindowAnchorId = NyxGuiRootWindow.DefaultAnchorId)
    {
        foreach (var a in new[] { Left, Right, Top, Bottom })
        {
            if (a is null) continue;
            foreach (var dep in a.Value.GetWidgetDependencies(rootWindowAnchorId))
                yield return dep;
        }
    }
}
