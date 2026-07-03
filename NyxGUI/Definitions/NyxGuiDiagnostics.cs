namespace NyxGui.Definitions;

/// <summary>v1 diagnostics: <c>[NyxGUI] ERROR</c> + file:line (plan §8).</summary>
public static class NyxGuiDiagnostics
{
    public static void Error(string file, int line, string message, string? widgetId = null)
    {
        var idPart = widgetId is null ? string.Empty : $" widget=\"{widgetId}\"";
        Console.Error.WriteLine($"[NyxGUI] ERROR {file}:{line}{idPart} — {message}");
    }

    public static void Error(Exception ex, string file, int line, string? widgetId = null)
    {
        var idPart = widgetId is null ? string.Empty : $" widget=\"{widgetId}\"";
        Console.Error.WriteLine($"[NyxGUI] ERROR {file}:{line}{idPart} — {ex.Message}");
    }
}
