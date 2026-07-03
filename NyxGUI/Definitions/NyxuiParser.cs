namespace NyxGui.Definitions;

/// <summary>
/// Parses .nyxui files (indentation-based, otui-style) into a widget tree.
///
/// Format:
///   WidgetType [id]
///     key = value
///     $hover:
///       key = value
///     ChildWidget childId
///       key = value
///
/// <b>Indent-based tree stack:</b> the parser uses a <c>parentStack</c> to track the
/// current nesting level.  Lines at a deeper indent than the current stack top become
/// children of the top widget; lines at the same or shallower indent pop the stack
/// until the correct parent is found.
///
/// <b>Compound values:</b> properties with inline <c>{ … }</c> are parsed as inline tables;
/// multi-line compound values start a virtual table block (indented lines under a
/// non-indented property line).
/// </summary>
public static class NyxuiParser
{
    private static readonly HashSet<string> StatePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "$hover:", "$pressed:", "$focused:", "$disabled:", "$on:",
        "$selected:", "$checked:", "$active:",
    };

    /// <summary>Parses a .nyxui file and returns the widget definitions.</summary>
    public static NyxGuiBuildSpec Parse(string source, string? sourcePath = null)
    {
        var lines = source.Split('\n');
        var widgets = new List<NyxGuiWidgetDef>();
        var documentRoot = string.Empty;
        var rootWindowAnchor = NyxGuiRootWindow.DefaultAnchorId;
        var settings = NyxGuiSettings.Default;

        var parentStack = new List<(int Indent, int WidgetIndex)>();
        var definitionOrder = 0;
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmed = line.TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.TrimStart().StartsWith('#'))
            {
                i++;
                continue;
            }

            var indent = GetIndent(line);
            var content = trimmed.Trim();

            if (indent == 0 && content.StartsWith("[document]", StringComparison.Ordinal))
            {
                var result = ParseDocumentBlock(lines, i);
                documentRoot = result.Root;
                rootWindowAnchor = result.RootWindowAnchor;
                i = result.NextLine;
                continue;
            }

            var widgetIndent = GetIndent(lines[i]);
            var (kind, id) = ParseWidgetHeader(content);

            var props = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var states = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            var j = i + 1;
            while (j < lines.Length)
            {
                var propLine = lines[j].TrimEnd('\r').Trim();
                if (string.IsNullOrWhiteSpace(propLine) || propLine.StartsWith('#'))
                {
                    j++;
                    continue;
                }

                var propIndent = GetIndent(lines[j]);
                if (propIndent <= widgetIndent)
                    break;

                // Check for state block header like $hover:
                if (StatePrefixes.Contains(propLine))
                {
                    var stateName = propLine[1..^1]; // strip $ and :
                    var stateProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    var stateIndent = propIndent;
                    j++;
                    while (j < lines.Length)
                    {
                        var stateLine = lines[j].TrimEnd('\r').Trim();
                        if (string.IsNullOrWhiteSpace(stateLine) || stateLine.StartsWith('#'))
                        {
                            j++;
                            continue;
                        }

                        var stateLineIndent = GetIndent(lines[j]);
                        if (stateLineIndent <= stateIndent)
                            break;

                        if (stateLine.Contains('='))
                        {
                            var eqIdx = stateLine.IndexOf('=');
                            var key = stateLine[..eqIdx].Trim();
                            var value = ParseValue(stateLine[(eqIdx + 1)..].Trim());
                            stateProps[key] = value;
                            j++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (stateProps.Count > 0)
                        states[stateName] = stateProps;
                    continue;
                }

                if (propLine.Contains('='))
                {
                    var eqIdx = propLine.IndexOf('=');
                    var key = propLine[..eqIdx].Trim();
                    var rawValue = propLine[(eqIdx + 1)..].Trim();

                    if (rawValue == "{" || rawValue.StartsWith("{", StringComparison.Ordinal))
                    {
                        // Inline or multi-line compound value: key = { sub = val, ... } or key = {\n  sub = val\n}
                        var table = ParseCompoundValue(rawValue, lines, ref j);
                        props[key] = table;
                        j++;
                    }
                    else
                    {
                        props[key] = ParseValue(rawValue);
                        j++;
                    }
                }
                else
                {
                    break;
                }
            }

            var def = new NyxGuiWidgetDef
            {
                Kind = kind,
                Id = id,
                Properties = props,
                States = states.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyDictionary<string, object?>)kvp.Value,
                    StringComparer.OrdinalIgnoreCase),
                DefinitionOrder = definitionOrder++,
                SourceLine = i + 1,
                SourcePath = sourcePath,
            };
            widgets.Add(def);

            while (parentStack.Count > 0 && parentStack[^1].Indent >= indent)
                parentStack.RemoveAt(parentStack.Count - 1);

            if (parentStack.Count > 0)
                widgets[^1].ParentId = widgets[parentStack[^1].WidgetIndex].Id;

            parentStack.Add((indent, widgets.Count - 1));
            i = j;
        }

        return new NyxGuiBuildSpec
        {
            Widgets = widgets,
            DocumentRootId = documentRoot,
            RootWindowAnchorId = rootWindowAnchor,
            Settings = settings,
            SourcePath = sourcePath,
        };
    }

    /// <summary>Parses a single .nyxui file and builds a document.</summary>
    public static NyxGuiBuiltDocument ParseAndBuild(string source, NyxGuiLoadOptions? options = null, string? sourcePath = null)
    {
        var spec = Parse(source, sourcePath);
        return NyxGuiDefinitionBuilder.Build(spec, options);
    }

    private static int GetIndent(string line)
    {
        var count = 0;
        foreach (var c in line)
        {
            if (c == ' ') count++;
            else if (c == '\t') count += 4;
            else break;
        }
        return count;
    }

    private static (string Kind, string Id) ParseWidgetHeader(string content)
    {
        var spaceIdx = content.IndexOf(' ');
        if (spaceIdx < 0)
            return (content, string.Empty);

        var kind = content[..spaceIdx];
        var rest = content[(spaceIdx + 1)..].Trim();
        var id = rest.Length > 0 && !rest.Contains('=') ? rest : string.Empty;
        return (kind, id);
    }

    private static object? ParseValue(string raw)
    {
        if (raw.Length == 0) return string.Empty;
        if (raw.StartsWith('"') && raw.EndsWith('"'))
            return raw[1..^1].Replace("\\n", "\n", StringComparison.Ordinal);
        if (raw.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (raw.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        if (raw.Equals("none", StringComparison.OrdinalIgnoreCase)) return null;
        if (raw.StartsWith('#') && NyxColor.TryParseHex(raw, out var color))
            return color;
        if (int.TryParse(raw, out var intValue)) return intValue;
        if (float.TryParse(raw, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var floatValue))
            return floatValue;
        return raw;
    }

    /// <summary>
    /// Parses a compound value like <c>{ value = 300, min = 200 }</c>.
    /// Handles both inline (single-line) and multi-line (indented sub-properties) forms.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> ParseCompoundValue(
        string opening, string[] lines, ref int lineIndex)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Try inline: { key = val, key2 = val2 }
        var inlineContent = opening.Trim();
        if (inlineContent.StartsWith("{"))
        {
            var inner = inlineContent[1..].TrimEnd('}').Trim();
            if (inner.Length > 0 && !inlineContent.Contains('\n'))
            {
                foreach (var pair in SplitCompoundPairs(inner))
                {
                    var eqIdx = pair.IndexOf('=');
                    if (eqIdx < 0) continue;
                    var k = pair[..eqIdx].Trim();
                    var v = ParseValue(pair[(eqIdx + 1)..].Trim());
                    result[NyxGuiKeyNames.ToSnakeCase(k)] = v;
                }
                if (result.Count > 0 || inlineContent.EndsWith("}"))
                    return result;
            }
        }

        // Multi-line: opening is just "{" or "{\n...", read following lines at deeper indent
        var startIndent = GetIndent(lines[lineIndex]);
        lineIndex++;
        while (lineIndex < lines.Length)
        {
            var line = lines[lineIndex].TrimEnd('\r');
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                lineIndex++;
                continue;
            }

            // A closing brace at same or less indent ends the block
            var curIndent = GetIndent(line);
            if (curIndent <= startIndent && trimmed.StartsWith('}'))
                break;
            // Also stop if we're back at widget-level indent with no brace
            if (curIndent <= startIndent)
                break;

            if (trimmed.Contains('='))
            {
                var eqIdx = trimmed.IndexOf('=');
                var k = trimmed[..eqIdx].Trim();
                var v = ParseValue(trimmed[(eqIdx + 1)..].Trim());
                result[NyxGuiKeyNames.ToSnakeCase(k)] = v;
            }

            lineIndex++;
        }

        // Skip closing brace if present
        if (lineIndex < lines.Length && lines[lineIndex].TrimEnd('\r').Trim() == "}")
            lineIndex++;

        return result;
    }

    /// <summary>Splits a comma-separated list of <c>key = value</c> pairs, respecting quoted strings.</summary>
    private static List<string> SplitCompoundPairs(string inner)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < inner.Length; i++)
        {
            var c = inner[i];
            if (c == '"') inQuotes = !inQuotes;
            if (c == ',' && !inQuotes)
            {
                var part = current.ToString().Trim();
                if (part.Length > 0) result.Add(part);
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        var last = current.ToString().Trim();
        if (last.Length > 0) result.Add(last);
        return result;
    }

    private static (string Root, string RootWindowAnchor, int NextLine)
        ParseDocumentBlock(string[] lines, int startLine)
    {
        var root = string.Empty;
        var rootWindowAnchor = NyxGuiRootWindow.DefaultAnchorId;
        var i = startLine + 1;

        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r').Trim();
            if (string.IsNullOrEmpty(line)) break;
            if (line.StartsWith('#')) { i++; continue; }

            var indent = GetIndent(lines[i]);
            if (indent == 0 && !line.Contains('=')) break;

            if (line.Contains('='))
            {
                var eqIdx = line.IndexOf('=');
                var key = line[..eqIdx].Trim();
                var value = line[(eqIdx + 1)..].Trim().Trim('"');
                if (key == "root") root = value;
                else if (key == "root-window") rootWindowAnchor = value;
            }
            i++;
        }
        return (root, rootWindowAnchor, i);
    }
}
