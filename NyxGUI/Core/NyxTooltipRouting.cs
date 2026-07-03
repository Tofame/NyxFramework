namespace NyxGui;

/// <summary>Picks the deepest hovered widget with <see cref="NyxElement.Tooltip"/> and paints its popup.</summary>
internal static class NyxTooltipRouting
{
    public static void PaintActiveTooltip(NyxElement scope, INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (FindDeepestHovered(scope) is { } target)
            target.PaintTooltipPopup(painter, theme);
    }

    public static NyxElement? FindDeepestHovered(NyxElement scope)
    {
        if (!scope.Visible)
            return null;

        switch (scope)
        {
            case NyxMiniWindow mini:
            {
                for (var i = mini.Children.Count - 1; i >= 0; i--)
                {
                    var chrome = FindDeepestHovered(mini.Children[i]);
                    if (chrome is not null)
                        return chrome;
                }

                if (!mini.Minimized)
                {
                    var body = FindDeepestHovered(mini.Body);
                    if (body is not null)
                        return body;
                }

                break;
            }
            case NyxScrollablePanel scroll:
            {
                var body = FindDeepestHovered(scroll.Body);
                if (body is not null)
                    return body;
                break;
            }

            case NyxContainer content:
            {
                for (var i = content.Children.Count - 1; i >= 0; i--)
                {
                    var hit = FindDeepestHovered(content.Children[i]);
                    if (hit is not null)
                        return hit;
                }

                break;
            }
        }

        if (scope.IsTooltipHovered)
            return scope;

        return null;
    }
}
