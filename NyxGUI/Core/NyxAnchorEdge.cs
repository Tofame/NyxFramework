namespace NyxGui;

/// <summary>Which edge (or center line) of a target widget an anchor attaches to.</summary>
public enum NyxAnchorEdge
{
    Left,
    Right,
    Top,
    Bottom,
    /// <summary>Horizontal center line of the target (<c>X + Width/2</c>).</summary>
    CenterX,
    /// <summary>Vertical center line of the target (<c>Y + Height/2</c>).</summary>
    CenterY,
}
