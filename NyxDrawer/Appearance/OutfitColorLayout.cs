using NyxRender;

namespace NyxDrawer.Appearance;

/// <summary>Nyx creature outfit colors (head / body / legs / feet) for mask-layer shaders.</summary>
public readonly struct OutfitColorLayout
{
    public OutfitColorLayout(Color head, Color body, Color legs, Color feet)
    {
        Head = head;
        Body = body;
        Legs = legs;
        Feet = feet;
    }

    public Color Head { get; }
    public Color Body { get; }
    public Color Legs { get; }
    public Color Feet { get; }
}
