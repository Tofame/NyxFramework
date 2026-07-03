namespace NyxGui;

/// <summary>
/// Default palette for flat UI when no <see cref="NyxImageStyle"/> is used.
/// </summary>
public sealed class NyxGuiTheme
{
    public NyxColor PanelBackground { get; init; } = NyxColor.FromRgb(45, 45, 48);
    public NyxColor PanelBorder { get; init; } = NyxColor.FromRgb(80, 80, 88);
    public NyxColor ButtonFace { get; init; } = NyxColor.FromRgb(70, 70, 78);
    public NyxColor ButtonFaceHover { get; init; } = NyxColor.FromRgb(90, 90, 100);
    public NyxColor ButtonFacePressed { get; init; } = NyxColor.FromRgb(55, 55, 62);
    public NyxColor ButtonBorder { get; init; } = NyxColor.FromRgb(120, 120, 130);
    public NyxColor TextPrimary { get; init; } = NyxColor.FromRgb(220, 220, 225);
    public NyxColor TextMuted { get; init; } = NyxColor.FromRgb(160, 160, 170);
    public NyxColor ScrollTrack { get; init; } = NyxColor.FromRgb(35, 35, 38);
    public NyxColor ScrollTrackDisabled { get; init; } = NyxColor.FromRgb(28, 28, 30);
    public NyxColor ScrollThumb { get; init; } = NyxColor.FromRgb(100, 100, 110);
    public NyxColor ScrollThumbDisabled { get; init; } = NyxColor.FromRgb(58, 58, 64);
    public NyxColor ScrollThumbHover { get; init; } = NyxColor.FromRgb(130, 130, 145);
    public NyxColor CheckOn { get; init; } = NyxColor.FromRgb(90, 140, 220);
    public NyxColor Separator { get; init; } = NyxColor.FromRgb(60, 60, 66);
    public NyxColor InputBackground { get; init; } = NyxColor.FromRgb(30, 30, 34);
    public NyxColor InputBorder { get; init; } = NyxColor.FromRgb(100, 100, 110);
    public NyxColor InputBorderFocused { get; init; } = NyxColor.FromRgb(90, 140, 220);
    public NyxColor Caret { get; init; } = NyxColor.FromRgb(220, 220, 230);
    public NyxColor SliderTrack { get; init; } = NyxColor.FromRgb(40, 40, 44);
    public NyxColor SliderFill { get; init; } = NyxColor.FromRgb(90, 140, 220);
    public NyxColor SliderThumb { get; init; } = NyxColor.FromRgb(180, 180, 190);
    public NyxColor ProgressTrack { get; init; } = NyxColor.FromRgb(35, 35, 40);
    public NyxColor ProgressFill { get; init; } = NyxColor.FromRgb(70, 160, 90);
    public NyxColor TooltipBackground { get; init; } = NyxColor.FromRgb(20, 20, 24);
    public NyxColor TooltipBorder { get; init; } = NyxColor.FromRgb(140, 140, 150);
    public NyxColor TableHeader { get; init; } = NyxColor.FromRgb(55, 55, 62);
    public NyxColor TableRowAlt { get; init; } = NyxColor.FromRgb(38, 38, 42);
    public NyxColor TableGrid { get; init; } = NyxColor.FromRgb(70, 70, 78);
    public NyxColor GraphAxis { get; init; } = NyxColor.FromRgb(90, 90, 100);
    public NyxColor GraphLine { get; init; } = NyxColor.FromRgb(90, 180, 120);
    public NyxColor GraphLineSecondary { get; init; } = NyxColor.FromRgb(220, 140, 80);
    public NyxColor GraphLineTertiary { get; init; } = NyxColor.FromRgb(140, 180, 230);
    public NyxColor ComboDropdown { get; init; } = NyxColor.FromRgb(40, 40, 46);
}
