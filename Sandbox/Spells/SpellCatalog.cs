using Tomlyn;
using Tomlyn.Model;

namespace Sandbox.Spells;

internal sealed class SpellCatalog
{
    public IReadOnlyDictionary<string, SpellAreaPattern> Areas { get; }
    public IReadOnlyList<SpellDefinition> Spells { get; }
    public IReadOnlyDictionary<string, SpellScript> Scripts { get; }

    private SpellCatalog(
        IReadOnlyDictionary<string, SpellAreaPattern> areas,
        IReadOnlyList<SpellDefinition> spells,
        IReadOnlyDictionary<string, SpellScript> scripts)
    {
        Areas = areas;
        Spells = spells;
        Scripts = scripts;
    }

    public static SpellCatalog Load(string spellsDirectory)
    {
        var areasPath = Path.Combine(spellsDirectory, "spells_areas.toml");
        var defsPath = Path.Combine(spellsDirectory, "spells_definitions.toml");
        var scriptsDir = Path.Combine(spellsDirectory, "scripts");

        var areas = LoadAreas(areasPath);
        var spells = LoadDefinitions(defsPath);
        var scripts = LoadScripts(scriptsDir, areas);

        return new SpellCatalog(areas, spells, scripts);
    }

    public bool TryGetScript(string scriptName, out SpellScript script) =>
        Scripts.TryGetValue(scriptName, out script!);

    private static Dictionary<string, SpellAreaPattern> LoadAreas(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, SpellAreaPattern>(StringComparer.OrdinalIgnoreCase);

        var model = TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(path)) ?? new TomlTable();
        var result = new Dictionary<string, SpellAreaPattern>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in model)
        {
            if (value is not TomlTable table)
                continue;

            result[key] = ParseAreaTable(key, table);
        }

        return result;
    }

    private static SpellAreaPattern ParseAreaTable(string name, TomlTable table)
    {
        if (!table.TryGetValue("rows", out var rowsObj) || rowsObj is not TomlArray rows)
            throw new InvalidDataException($"Area \"{name}\" is missing [rows].");

        return BuildPattern(name, ParseRowsFromToml(rows));
    }

    private static List<int[]> ParseRowsFromToml(TomlArray rows)
    {
        var list = new List<int[]>();
        for (var r = 0; r < rows.Count; r++)
        {
            if (rows[r] is not TomlArray row)
                throw new InvalidDataException($"Area row {r} must be an array.");
            var cells = new int[row.Count];
            for (var c = 0; c < row.Count; c++)
                cells[c] = ToInt(row[c]);
            list.Add(cells);
        }

        return list;
    }

    private static List<SpellDefinition> LoadDefinitions(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Spell definitions not found: \"{path}\".");

        var model = TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(path)) ?? new TomlTable();
        if (!model.TryGetValue("spell", out var spellObj))
            return [];

        var list = new List<SpellDefinition>();
        if (spellObj is TomlTableArray tableArray)
        {
            foreach (var table in tableArray)
                list.Add(ParseSpell(table));
        }
        else if (spellObj is TomlTable single)
            list.Add(ParseSpell(single));

        return list;
    }

    private static SpellDefinition ParseSpell(TomlTable table)
    {
        var name = ReadString(table, "name") ?? "unnamed";
        var words = ReadString(table, "words") ?? name.ToLowerInvariant();
        var script = ReadString(table, "script") ?? throw new InvalidDataException($"Spell \"{name}\" missing script.");

        return new SpellDefinition
        {
            Name = name,
            Words = words,
            NeedTarget = ReadBool(table, "needtarget"),
            SelfTarget = ReadBool(table, "selftarget"),
            Direction = ReadBool(table, "direction"),
            MouseTarget = ReadBool(table, "mousetarget"),
            ScriptName = script,
        };
    }

    private static Dictionary<string, SpellScript> LoadScripts(
        string scriptsDir,
        IReadOnlyDictionary<string, SpellAreaPattern> sharedAreas)
    {
        var result = new Dictionary<string, SpellScript>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(scriptsDir))
            return result;

        foreach (var file in Directory.EnumerateFiles(scriptsDir, "*.txt"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var lines = File.ReadAllLines(file);
            var script = ParseScriptFile(name, lines, sharedAreas);
            if (script is not null)
                result[name] = script;
        }

        return result;
    }

    private static SpellScript? ParseScriptFile(
        string name,
        string[] lines,
        IReadOnlyDictionary<string, SpellAreaPattern> sharedAreas)
    {
        uint? effectId = null;
        uint? missileId = null;
        string? areaRef = null;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();

            if (key.Equals("effectId", StringComparison.OrdinalIgnoreCase) &&
                uint.TryParse(value, out var eid))
                effectId = eid;
            else if (key.Equals("missileId", StringComparison.OrdinalIgnoreCase) &&
                     uint.TryParse(value, out var mid))
                missileId = mid;
            else if (key.Equals("area", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
                areaRef = value;
        }

        if (missileId is not null)
        {
            return new SpellScript
            {
                Name = name,
                MissileId = missileId,
            };
        }

        if (effectId is null)
        {
            Console.WriteLine($"Script \"{name}\": missing effectId or missileId — skipped.");
            return null;
        }

        if (areaRef is null)
            throw new InvalidDataException($"Script \"{name}\": missing area=…");

        if (!sharedAreas.TryGetValue(areaRef, out var area))
            throw new InvalidDataException($"Script \"{name}\": unknown area \"{areaRef}\".");

        return new SpellScript
        {
            Name = name,
            EffectId = effectId,
            Area = area,
        };
    }

    private static SpellAreaPattern BuildPattern(string name, List<int[]> rows)
    {
        if (rows.Count == 0)
            throw new InvalidDataException($"Area \"{name}\" has no rows.");

        var height = rows.Count;
        var width = 0;
        foreach (var row in rows)
            width = Math.Max(width, row.Length);

        var cells = new int[height, width];
        var casterRow = -1;
        var casterCol = -1;

        for (var r = 0; r < height; r++)
        {
            var row = rows[r];
            for (var c = 0; c < width; c++)
            {
                var v = c < row.Length ? row[c] : 0;
                cells[r, c] = v;
                if (v is 2 or 3)
                {
                    casterRow = r;
                    casterCol = c;
                }
            }
        }

        if (casterRow < 0)
            throw new InvalidDataException($"Area \"{name}\" has no caster cell (2 or 3).");

        return new SpellAreaPattern
        {
            Name = name,
            Cells = cells,
            CasterRow = casterRow,
            CasterCol = casterCol,
        };
    }

    private static int ToInt(object? value) => value switch
    {
        long l => (int)l,
        int i => i,
        double d => (int)d,
        string s when int.TryParse(s, out var n) => n,
        _ => 0,
    };

    private static string? ReadString(TomlTable table, string key) =>
        TryGetValueIgnoreCase(table, key, out var v) ? v?.ToString() : null;

    private static bool ReadBool(TomlTable table, string key)
    {
        if (!TryGetValueIgnoreCase(table, key, out var v) || v is null)
            return false;

        return v switch
        {
            bool b => b,
            long l => l != 0,
            int i => i != 0,
            string s => s is "1" or "true" or "yes",
            _ => false,
        };
    }

    private static bool TryGetValueIgnoreCase(TomlTable table, string key, out object? value)
    {
        foreach (var (k, v) in table)
        {
            if (!k.Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;
            value = v;
            return true;
        }

        value = null;
        return false;
    }
}
