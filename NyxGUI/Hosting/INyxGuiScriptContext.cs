using NyxGui.Definitions;

namespace NyxGui.Hosting;

/// <summary>Lua module API surface (plan §4). No batch API exposed to scripts.</summary>
public interface INyxGuiScriptContext
{
    NyxGuiLoadOptions GuiOptions();

    void IncludeLayout(string relativePath);

    NyxGuiBuiltDocument LoadTree(IReadOnlyDictionary<string, object?> definitionTable);

    void RegisterModule(string moduleId, NyxGuiBuiltDocument document);

    /// <summary>Registers the document produced by the last <see cref="LoadTree"/>.</summary>
    void RegisterModule(string moduleId);

    NyxElement? Get(string widgetId);

    void SetChildren(string parentId, IReadOnlyDictionary<string, object?> childDefinitions);
}
