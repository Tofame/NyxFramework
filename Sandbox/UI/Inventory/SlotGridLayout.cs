using NyxGui;

namespace Sandbox.UI.Inventory;

/// <summary>Positions manual slot children under a host panel (survives <see cref="NyxContainer.SetBoundsSilently"/> on the host).</summary>
internal static class SlotGridLayout
{
    /// <summary>Insets from the host panel's <see cref="NyxElement.LayoutBox"/> margin (set in TOML <c>[id.anchors]</c>).</summary>
    public static void GetHostMargins(NyxContainer host, out int top, out int bottom, out int left, out int right)
    {
        if (host.LayoutBox is { } box)
        {
            top = box.Margin.Top;
            bottom = box.Margin.Bottom;
            left = box.Margin.Left;
            right = box.Margin.Right;
            return;
        }

        top = bottom = left = right = 0;
    }

    public static NyxRect PlaceInHost(NyxContainer host, NyxElement child, int relX, int relY, int width, int height)
    {
        var bounds = new NyxRect(host.Bounds.X + relX, host.Bounds.Y + relY, width, height);
        child.SetBounds(bounds);
        return bounds;
    }

    /// <summary>Positions slot chrome and the interactive <see cref="UISlot"/> at the same screen rect.</summary>
    public static void PlaceSlotPair(
        NyxContainer host,
        NyxContainer frame,
        UISlot itemSlot,
        int relX,
        int relY,
        int width,
        int height)
    {
        var bounds = PlaceInHost(host, frame, relX, relY, width, height);
        itemSlot.SetBounds(bounds);
    }
}
