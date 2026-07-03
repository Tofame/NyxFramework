namespace NyxGui.Hosting;

/// <summary>GUI hit-test before game/world input (plan §7).</summary>
public interface INyxGuiInputRouter
{
    bool ProcessMouse(int x, int y, bool leftPressed, bool rightPressed = false, int wheelDelta = 0);

    bool HitTest(int x, int y);
}
