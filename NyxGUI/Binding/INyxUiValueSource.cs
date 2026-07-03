namespace NyxGui.Binding;

/// <summary>Supplies bound UI values (plan §6.6). Register on <see cref="NyxUiState"/>.</summary>
public interface INyxUiValueSource
{
    bool TryGetString(string bindKey, out string value);
}
