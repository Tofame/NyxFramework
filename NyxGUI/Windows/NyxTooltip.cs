namespace NyxGui;

/// <summary>
/// Hover tooltip popup. Shown near a target widget after a delay.
/// Replaces the old NyxTooltipPaint static approach with a proper widget.
/// </summary>
public sealed class NyxTooltip : NyxWidget
{
    private const int LineHeight = 14;
    private const int Pad = 6;
    private const int DefaultDelayMs = 500;

    private string _text = string.Empty;
    private int _delayMs = DefaultDelayMs;
    private long _hoverSinceMs = -1;
    private bool _isActive;

    public NyxTooltip(string? id = null) : base(0)
    {
        Id = id;
        Visible = false;
    }

    /// <summary>Tooltip text content. Supports \n for multi-line.</summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            InvalidateRender();
        }
    }

    /// <summary>Delay in milliseconds before the tooltip appears.</summary>
    public int DelayMs
    {
        get => _delayMs;
        set => _delayMs = Math.Max(0, value);
    }

    /// <summary>True when the tooltip is currently visible.</summary>
    public bool IsActive => _isActive;

    /// <summary>Call when the pointer enters the target widget.</summary>
    public void OnPointerEnter()
    {
        if (string.IsNullOrEmpty(_text)) return;
        _hoverSinceMs = Environment.TickCount64;
    }

    /// <summary>Call when the pointer leaves the target widget.</summary>
    public void OnPointerLeave()
    {
        _hoverSinceMs = -1;
        if (_isActive)
        {
            _isActive = false;
            Visible = false;
        }
    }

    /// <summary>Updates tooltip state and visibility. Call once per frame from the host.</summary>
    public void Update()
    {
        if (_hoverSinceMs < 0 || string.IsNullOrEmpty(_text))
        {
            if (_isActive)
            {
                _isActive = false;
                Visible = false;
            }
            return;
        }

        if (!_isActive && Environment.TickCount64 - _hoverSinceMs >= _delayMs)
        {
            _isActive = true;
            Visible = true;
        }
    }

    /// <summary>Positions the tooltip near the target bounds.</summary>
    public void PositionNear(NyxRect targetBounds, int viewportWidth, int viewportHeight)
    {
        if (string.IsNullOrEmpty(_text)) return;

        var lines = _text.Split('\n');
        var maxW = lines.Max(l => l.Length) * 7;
        var tipW = Math.Max(80, maxW + Pad * 2);
        var tipH = lines.Length * LineHeight + Pad * 2;

        var tipX = targetBounds.X + (targetBounds.Width - tipW) / 2;
        var tipY = targetBounds.Y - tipH - 4;

        // Clamp to viewport
        tipX = Math.Max(0, Math.Min(tipX, viewportWidth - tipW));
        if (tipY < 0)
            tipY = targetBounds.Bottom + 4;

        SetBounds(new NyxRect(tipX, tipY, tipW, tipH));
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!Visible || string.IsNullOrEmpty(_text)) return;

        var bg = theme.TooltipBackground;
        var border = theme.TooltipBorder;
        var textColor = theme.TextPrimary;

        painter.FillRect(Bounds, bg);
        painter.DrawRect(Bounds, border, 1);

        var lines = _text.Split('\n');
        var y = Bounds.Y + Pad;
        foreach (var line in lines)
        {
            var textRect = new NyxRect(Bounds.X + Pad, y, Bounds.Width - Pad * 2, LineHeight);
            painter.DrawText(textRect, line, NyxTextAlign.TopLeft, textColor, GetPaintFont());
            y += LineHeight;
        }
    }

    /// <summary>Legacy static paint for backward compatibility with NyxElement.PaintTooltipPopup.</summary>
    internal static void PaintPopup(
        INyxGuiPainter painter,
        NyxGuiTheme theme,
        NyxRect anchor,
        string text,
        NyxFontStyle? font,
        int delayMs,
        long hoverSinceMs,
        bool pointerInside)
    {
        if (string.IsNullOrEmpty(text) || !pointerInside || hoverSinceMs < 0)
            return;
        if (Environment.TickCount64 - hoverSinceMs < delayMs)
            return;

        var lines = text.Split('\n');
        var maxW = lines.Max(l => l.Length) * 7;
        var tipW = Math.Max(80, maxW + Pad * 2);
        var tipH = lines.Length * LineHeight + Pad * 2;
        var tipX = anchor.X + (anchor.Width - tipW) / 2;
        var tipY = anchor.Y - tipH - 4;

        var bg = theme.TooltipBackground;
        var border = theme.TooltipBorder;
        var textColor = theme.TextPrimary;

        painter.FillRect(new NyxRect(tipX, tipY, tipW, tipH), bg);
        painter.DrawRect(new NyxRect(tipX, tipY, tipW, tipH), border, 1);

        var y = tipY + Pad;
        foreach (var line in lines)
        {
            var textRect = new NyxRect(tipX + Pad, y, tipW - Pad * 2, LineHeight);
            painter.DrawText(textRect, line, NyxTextAlign.TopLeft, textColor, font);
            y += LineHeight;
        }
    }
}
