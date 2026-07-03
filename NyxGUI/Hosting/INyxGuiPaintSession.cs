namespace NyxGui.Hosting;

/// <summary>Per-frame GUI paint entry (plan §4).</summary>
public interface INyxGuiPaintSession
{
    void Paint(INyxGuiPainter painter, NyxGuiTheme theme);
}
