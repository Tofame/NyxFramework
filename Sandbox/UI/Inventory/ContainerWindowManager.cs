using NyxGui;
using NyxGui.Definitions;
using NyxGuiRender;
using Sandbox.Items;
using Sandbox.UI;

namespace Sandbox.UI.Inventory;

/// <summary>Tracks floating container mini windows (one per distinct <see cref="ItemStorage"/>).</summary>
internal sealed class ContainerWindowManager
{
    private readonly Dictionary<ItemStorage, UIContainerWindow> _byStorage = new();
    private readonly List<UIContainerWindow> _windows = [];
    private SandboxShell? _shell;
    private Func<NyxDockPanel>? _resolveDock;

    public void BindShell(SandboxShell shell, Func<NyxDockPanel> resolveDock)
    {
        _shell = shell;
        _resolveDock = resolveDock;
    }

    public void Open(
        ItemContainer container,
        UISlotHost slotHost,
        NyxGuiLoadOptions loadOptions,
        NyxRect? placeNear = null)
    {
        if (_byStorage.TryGetValue(container.Contents, out var existing))
        {
            if (existing.Root.Visible)
            {
                existing.Hide();
                return;
            }

            existing.Show();
            DockWindow(existing);
            return;
        }

        var useDock = _shell is not null;
        var created = UIContainerWindow.TryCreate(
            container,
            slotHost,
            loadOptions,
            placeNear: useDock ? null : placeNear);
        if (created is null)
            return;

        _byStorage[container.Contents] = created;
        _windows.Add(created);
        created.Show();
        DockWindow(created);
    }

    public void UpdateViewport(int width, int height)
    {
        foreach (var window in _windows)
            window.UpdateViewport(width, height);
    }

    private void DockWindow(UIContainerWindow window)
    {
        if (_shell is null)
            return;

        if (window.MiniWindow.Parent is NyxDockPanel)
        {
            return;
        }

        var rightDock = _shell.RightDock;
        var leftDock = _shell.LeftDock;
        int neededHeight = window.MiniWindow.Bounds.Height;

        if (rightDock is not null && DockHasSpace(rightDock, neededHeight))
        {
            _shell.Document?.Adopt(window.Document, rightDock);
        }
        else if (leftDock is not null && DockHasSpace(leftDock, neededHeight))
        {
            _shell.Document?.Adopt(window.Document, leftDock);
        }
        else
        {
            _shell.AdoptIntoShellRoot(window.Document);

            var gameBounds = _shell.GamePanel?.Bounds ?? new NyxRect(0, 0, 800, 600);
            int winW = window.MiniWindow.Bounds.Width;
            int winH = window.MiniWindow.Bounds.Height;

            int targetX = gameBounds.Right - winW - 10;
            int targetY = gameBounds.Y + 10;

            while (true)
            {
                bool collision = false;
                foreach (var other in _windows)
                {
                    if (other == window || !other.Root.Visible || other.MiniWindow.Parent is NyxDockPanel)
                        continue;

                    var ob = other.MiniWindow.Bounds;
                    if (Math.Abs(ob.X - targetX) < 10)
                    {
                        if (targetY >= ob.Y && targetY < ob.Bottom + 10)
                        {
                            targetY = ob.Bottom + 10;
                            collision = true;
                        }
                    }
                }

                if (!collision)
                    break;

                if (targetY + winH > gameBounds.Bottom - 10)
                {
                    targetX -= (winW + 10);
                    targetY = gameBounds.Y + 10;
                }
            }

            window.MiniWindow.SetBounds(new NyxRect(targetX, targetY, winW, winH));
        }
    }

    private bool DockHasSpace(NyxDockPanel dock, int neededHeight)
    {
        int totalHeight = dock.Margin * 2;
        bool hasVisible = false;
        foreach (var child in dock.Children)
        {
            if (child is NyxMiniWindow win && win.Visible)
            {
                var h = win.Minimized ? win.TitleBarHeight : win.Bounds.Height;
                totalHeight += h + dock.Gap;
                hasVisible = true;
            }
        }
        if (hasVisible)
        {
            totalHeight -= dock.Gap;
        }

        return totalHeight + neededHeight + dock.Gap <= dock.Bounds.Height;
    }
}
