using NyxGui;

namespace NyxGui.Definitions;

/// <summary>Loads named state groups from a Lua build spec (e.g. <c>quest-entry</c>).</summary>
public static class NyxGuiStateTemplateLoader
{
    public static NyxWidgetStates LoadFromStateGroup(
        IReadOnlyDictionary<string, object?> stateGroup,
        NyxGuiLoadOptions? options = null)
    {
        options ??= new NyxGuiLoadOptions();
        var states = new NyxWidgetStates();
        NyxGuiWidgetStateApplicator.ApplyStateGroup(states, NyxGuiPropertyBag.From(stateGroup), options);
        return states;
    }
}
