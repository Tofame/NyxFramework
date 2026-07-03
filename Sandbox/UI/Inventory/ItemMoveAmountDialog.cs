using NyxGui;
using Silk.NET.Input;

namespace Sandbox.UI.Inventory;

/// <summary>OT-style prompt: how many stackable items to drop when releasing on another slot.</summary>
public sealed class ItemMoveAmountDialog
{
    private const int PanelW = 280;
    private const int PanelH = 132;

    private readonly NyxContainer _root;
    private readonly NyxContainer _panel;
    private readonly NyxLabel _countLabel;
    private readonly NyxSlider _slider;
    private readonly NyxButton _ok;
    private readonly NyxButton _cancel;
    private Action<ushort?>? _callback;
    private ushort _maxCount;
    private readonly StackAmountTyping _typing = new();
    private bool _enterWasDown;
    private bool _escapeWasDown;

    public ItemMoveAmountDialog(int viewportWidth, int viewportHeight)
    {
        _root = new NyxContainer(new NyxRect(0, 0, viewportWidth, viewportHeight))
        {
            Visible = false,
        };

        var panelBounds = new NyxRect(0, 0, PanelW, PanelH);
        _panel = new NyxContainer(panelBounds);
        _panel.States.Normal.BackgroundColor = NyxColor.FromRgb(45, 45, 48);
        _panel.States.Normal.BorderWidth = 1;
        _panel.States.Normal.BorderColor = NyxColor.FromRgb(100, 100, 110);

        var title = new NyxLabel { Align = NyxTextAlign.TopCenter }
            .AnchorTop()
            .AnchorLeft()
            .AnchorRight()
            .FixedHeight(18)
            .Margin(12, 10, 12, 0);
        title.Text = "How many to move?";
        title.SetBounds(NyxLayoutResolver.Resolve(panelBounds, title));

        _countLabel = new NyxLabel { Align = NyxTextAlign.TopCenter }
            .AnchorTop()
            .AnchorLeft()
            .AnchorRight()
            .FixedHeight(18)
            .Margin(12, 32, 12, 0);
        _countLabel.Text = "1";
        _countLabel.SetBounds(NyxLayoutResolver.Resolve(panelBounds, _countLabel));

        _slider = new NyxSlider { Minimum = 1, Maximum = 1, Value = 1 }
            .AnchorTop()
            .AnchorLeft()
            .AnchorRight()
            .FixedHeight(22)
            .Margin(16, 54, 16, 0);
        _slider.SetBounds(NyxLayoutResolver.Resolve(panelBounds, _slider));

        _ok = new NyxButton { Label = "OK" }
            .AnchorRight()
            .AnchorBottom()
            .FixedSize(68, 26)
            .Margin(0, 0, 84, 10);
        _ok.Click += (_, _) => Confirm();
        _ok.SetBounds(NyxLayoutResolver.Resolve(panelBounds, _ok));

        _cancel = new NyxButton { Label = "Cancel" }
            .AnchorRight()
            .AnchorBottom()
            .FixedSize(64, 26)
            .Margin(0, 0, 12, 10);
        _cancel.Click += (_, _) => Cancel();
        _cancel.SetBounds(NyxLayoutResolver.Resolve(panelBounds, _cancel));

        _panel.AddChild(title);
        _panel.AddChild(_countLabel);
        _panel.AddChild(_slider);
        _panel.AddChild(_ok);
        _panel.AddChild(_cancel);
        _root.AddChild(_panel);
        _panel.SetBounds(CenterPanel(viewportWidth, viewportHeight));
    }

    public NyxContainer Root => _root;

    public bool IsOpen => _root.Visible;

    public void UpdateViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _root.SetBounds(new NyxRect(0, 0, width, height));
        _panel.SetBounds(CenterPanel(width, height));
    }

    public void Show(ushort maxCount, ushort? initialAmount, Action<ushort?> callback)
    {
        if (maxCount <= 1)
        {
            callback(maxCount);
            return;
        }

        _maxCount = maxCount;
        _callback = callback;
        _typing.Clear();
        if (initialAmount is ushort seed && seed >= 1 && seed <= maxCount)
            _typing.AppendDigitSequence(seed, maxCount);

        _slider.Minimum = 1;
        _slider.Maximum = maxCount;
        SyncSliderFromTyping(defaultToMax: initialAmount is null);
        _enterWasDown = false;
        _escapeWasDown = false;
        _root.Visible = true;
    }

    public void HandleKeyboard(IKeyboard keyboard)
    {
        if (!IsOpen)
            return;

        var enterDown = keyboard.IsKeyPressed(Key.Enter) || keyboard.IsKeyPressed(Key.KeypadEnter);
        if (enterDown && !_enterWasDown)
            Confirm();
        _enterWasDown = enterDown;

        var escapeDown = keyboard.IsKeyPressed(Key.Escape);
        if (escapeDown && !_escapeWasDown)
            Cancel();
        _escapeWasDown = escapeDown;

        if (StackDigitInput.TryPoll(keyboard, out var digit))
        {
            _typing.AppendDigit(digit, _maxCount);
            SyncSliderFromTyping(defaultToMax: false);
        }
    }

    public void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!IsOpen)
            return;

        painter.FillRect(_root.Bounds, new NyxColor(0, 0, 0, 140));
        _root.Paint(painter, theme);
    }

    private void OnSliderChanged(object? sender, NyxSliderValueChangedEventArgs e)
    {
        var i = (int)MathF.Round(Math.Clamp(e.Value, 1, _maxCount));
        if (Math.Abs(_slider.Value - i) > 0.01f)
            _slider.Value = i;
        UpdateCountLabel();
    }

    private void SyncSliderFromTyping(bool defaultToMax)
    {
        if (_typing.TryGetAmount(_maxCount) is ushort typed)
            _slider.Value = typed;
        else if (defaultToMax)
            _slider.Value = _maxCount;

        UpdateCountLabel();
    }

    private void UpdateCountLabel() =>
        _countLabel.Text = $"{(int)MathF.Round(_slider.Value)} / {_maxCount}";

    private void Confirm()
    {
        if (!IsOpen)
            return;

        var amount = (ushort)Math.Clamp((int)MathF.Round(_slider.Value), 1, _maxCount);
        _root.Visible = false;
        var cb = _callback;
        _callback = null;
        cb?.Invoke(amount);
    }

    private void Cancel()
    {
        if (!IsOpen)
            return;

        _root.Visible = false;
        var cb = _callback;
        _callback = null;
        cb?.Invoke(null);
    }

    private static NyxRect CenterPanel(int viewportWidth, int viewportHeight)
    {
        var x = Math.Max(0, (viewportWidth - PanelW) / 2);
        var y = Math.Max(0, (viewportHeight - PanelH) / 2);
        return new NyxRect(x, y, PanelW, PanelH);
    }
}
