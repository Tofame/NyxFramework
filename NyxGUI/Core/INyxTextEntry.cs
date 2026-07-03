namespace NyxGui;

/// <summary>Widgets that accept keyboard input via <see cref="NyxGuiRootStack.ProcessKeyboard"/>.</summary>
public interface INyxTextEntry
{
    void HandleKey(NyxGuiKey key, char? character = null);
}
