using NyxGui;
using NyxGui.Definitions;
using NyxGuiRender;
using Sandbox.Items;
using Sandbox.UI;

namespace Sandbox.UI.Inventory;

/// <summary>Extra mini window for one <see cref="ItemContainer"/> (map loot, slots, …).</summary>
internal sealed class UIContainerWindow
{
    private readonly NyxGuiBuiltDocument _document;
    private readonly UIContainer _grid;
    private bool _visible;

    private UIContainerWindow(
        NyxGuiBuiltDocument document,
        UIContainer grid,
        NyxMiniWindow window,
        ItemContainer container)
    {
        _document = document;
        Root = document.Root;
        MiniWindow = window;
        _grid = grid;
        Container = container;
    }

    public NyxElement Root { get; }

    public NyxMiniWindow MiniWindow { get; }

    public ItemContainer Container { get; }

    public ItemStorage Storage => Container.Contents;

	public NyxGuiBuiltDocument Document => _document;

    public static UIContainerWindow? TryCreate(
        ItemContainer container,
        UISlotHost slotHost,
        NyxGuiLoadOptions loadOptions,
        NyxRect? placeNear = null)
    {
        var loaded = SandboxUIDefinitions.TryLoad("backpack", loadOptions);
        if (loaded is null)
            return null;

        var doc = loaded.Document;
        if (doc.Root is not NyxMiniWindow window)
            return null;

        SandboxMiniWindowBehavior.TryAppendChrome(window, doc, loadOptions);

        var host = doc.TryGet<NyxContainer>("BackpackSlots");
        if (host is null)
            return null;

        var grid = new UIContainer(container.Contents, slotHost);
        grid.BuildInto(host);

        window.Title = ContainerWindowTitles.For(container);
        grid.ApplyMiniWindowSize(window);

        if (placeNear is { } near)
            window.SetBounds(new NyxRect(near.X + 28, near.Y + 28, window.Bounds.Width, window.Bounds.Height));

        var entry = new UIContainerWindow(doc, grid, window, container);

        window.BoundsChanged += (_, _) => grid.RelayoutSlots();
        if (doc.TryGetButton("closeButton") is { } close)
            close.Click += (_, _) => entry.Hide();

        return entry;
    }

    public void Show()
    {
        _visible = true;
        Root.Visible = true;
        _grid.SyncSlots();
        _grid.RelayoutSlots();
    }

    public void Hide()
    {
        _visible = false;
        Root.Visible = false;
    }

    public void Paint(NyxGuiRenderer renderer, NyxGuiTheme theme)
    {
        if (_visible)
            Root.Paint(renderer, theme);
    }

    public void UpdateViewport(int width, int height) => _document.SetWindowSize(width, height);
}

internal static class ContainerWindowTitles
{
    public static string For(Item item)
    {
        var t = item.GetItemType();
        if (!string.IsNullOrWhiteSpace(t.DisplayName))
            return t.DisplayName;
        return t.IsNone ? "Container" : $"Item {t.DatId}";
    }
}
