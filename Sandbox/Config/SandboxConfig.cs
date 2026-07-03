using NyxDrawer.Appearance;
using NyxAssets.Things;
using Tomlyn;
using Tomlyn.Model;

namespace Sandbox;

/// <summary>Loads <c>config.toml</c> next to the executable (Nyx-style outfit fields per creature).</summary>
public sealed class SandboxConfig
{
    public CreatureSpawnConfig Player { get; init; } = CreatureSpawnConfig.DefaultPlayer;

    /// <summary>One entry per <c>[[npc]]</c> block (repeat the section for multiple NPCs).</summary>
    public IReadOnlyList<CreatureSpawnConfig> Npcs { get; init; } =
        new[] { CreatureSpawnConfig.DefaultNpc };

    public MapConfig Map { get; init; } = MapConfig.Default;

    public ClientConfig Client { get; init; } = ClientConfig.Default;



    public static SandboxConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"… config not found at \"{path}\" — using built-in defaults.");
            return new SandboxConfig();
        }

        var toml = Toml.ToModel(File.ReadAllText(path));
        var client = toml.TryGetValue("client", out var cObj) && cObj is TomlTable cTab ? ParseClient(cTab) : ClientConfig.Default;
        var map = toml.TryGetValue("map", out var mObj) && mObj is TomlTable mTab ? ParseMap(mTab) : MapConfig.Default;
        var player = toml.TryGetValue("player", out var pObj) && pObj is TomlTable pTab ? ParseCreatureSpawn(pTab, CreatureSpawnConfig.DefaultPlayer) : CreatureSpawnConfig.DefaultPlayer;

        var npcs = new List<CreatureSpawnConfig>();
        if (toml.TryGetValue("npc", out var npcObj))
        {
            if (npcObj is TomlTableArray tableArray)
            {
                foreach (var table in tableArray)
                    npcs.Add(ParseCreatureSpawn(table, CreatureSpawnConfig.DefaultNpc));
            }
            else if (npcObj is TomlTable single)
            {
                npcs.Add(ParseCreatureSpawn(single, CreatureSpawnConfig.DefaultNpc));
            }
        }
        if (npcs.Count == 0)
            npcs.Add(CreatureSpawnConfig.DefaultNpc);



        return new SandboxConfig
        {
            Player = player,
            Npcs = npcs,
            Map = map,
            Client = client,

        };
    }

    private static ClientConfig ParseClient(TomlTable s)
    {
        var d = ClientConfig.Default;
        return new ClientConfig
        {
            ClientVersion = ReadInt(s, "clientVersion", d.ClientVersion),
            ExtendedSpriteIds = ReadBoolNullable(s, "extendedSpriteIds") ?? d.ExtendedSpriteIds,
            ImprovedAnimations = ReadBoolNullable(s, "improvedAnimations") ?? d.ImprovedAnimations,
            OutfitFrameGroups = ReadBoolNullable(s, "outfitFrameGroups") ?? d.OutfitFrameGroups,
            TransparentSprites = ReadBool(s, "transparentSprites", d.TransparentSprites),
            ExportJson = ReadBool(s, "exportJson", false),
            ShadersDir = ReadString(s, "shadersDir", null),
            MaxCachedPages = ReadInt(s, "maxCachedPages", d.MaxCachedPages),
            MetadataFormat = ReadString(s, "metadataFormat", d.MetadataFormat) ?? d.MetadataFormat,
            SpritesFormat = ReadString(s, "spritesFormat", d.SpritesFormat) ?? d.SpritesFormat,
            InMemoryLoading = ReadBool(s, "inMemoryLoading", d.InMemoryLoading),
        };
    }

    private static CreatureSpawnConfig ParseCreatureSpawn(
        TomlTable s,
        CreatureSpawnConfig defaults)
    {
        return new CreatureSpawnConfig
        {
			Name = ReadString(s, "name", defaults.Name) ?? defaults.Name,
            TileX = ReadInt(s, "tileX", defaults.TileX),
            TileY = ReadInt(s, "tileY", defaults.TileY),
			TileZ = ReadInt(s, "tileZ", defaults.TileZ),
            Direction = ReadInt(s, "direction", defaults.Direction),
            Appearance = new CreatureOutfitAppearance(
                ReadUInt(s, "lookType", defaults.Appearance.LookType),
                ReadByte(s, "lookHead", defaults.Appearance.LookHead),
                ReadByte(s, "lookBody", defaults.Appearance.LookBody),
                ReadByte(s, "lookLegs", defaults.Appearance.LookLegs),
                ReadByte(s, "lookFeet", defaults.Appearance.LookFeet),
                ReadByte(s, "lookAddons", defaults.Appearance.LookAddons),
                ReadUInt(s, "lookMount", defaults.Appearance.LookMount),
                ReadString(s, "shader", defaults.Appearance.Shader)),
        };
    }



    private static MapConfig ParseMap(TomlTable s)
    {
        var d = MapConfig.Default;
        return new MapConfig
        {
            SectorsPath = ReadString(s, "sectorsPath", d.SectorsPath) ?? d.SectorsPath,
        };
    }

    private static int ReadInt(TomlTable s, string key, int fallback) =>
        s.TryGetValue(key, out var v) ? ToInt(v, fallback) : fallback;

    private static uint ReadUInt(TomlTable s, string key, uint fallback) =>
        s.TryGetValue(key, out var v) ? ToUInt(v, fallback) : fallback;

    private static byte ReadByte(TomlTable s, string key, byte fallback) =>
        s.TryGetValue(key, out var v) ? ToByte(v, fallback) : fallback;

    private static string? ReadString(TomlTable s, string key, string? fallback) =>
        s.TryGetValue(key, out var v) && v is string str ? str : fallback;

    private static bool ReadBool(TomlTable s, string key, bool fallback) =>
        s.TryGetValue(key, out var v) && v is bool b ? b : fallback;

    private static bool? ReadBoolNullable(TomlTable s, string key) =>
        s.TryGetValue(key, out var v) && v is bool b ? b : null;

    private static int ToInt(object? value, int fallback) => value switch
    {
        long l => (int)l,
        double d => (int)d,
        string s when int.TryParse(s, out var n) => n,
        _ => fallback,
    };

    private static uint ToUInt(object? value, uint fallback) => value switch
    {
        long l => (uint)l,
        double d => (uint)d,
        string s when uint.TryParse(s, out var n) => n,
        _ => fallback,
    };

    private static byte ToByte(object? value, byte fallback) => value switch
    {
        long l => (byte)l,
        double d => (byte)d,
        string s when byte.TryParse(s, out var n) => n,
        _ => fallback,
    };
}

public sealed class CreatureSpawnConfig
{
    public static CreatureSpawnConfig DefaultPlayer => new()
    {
		Name = "Player",
        TileX = -1,
        TileY = -1,
		TileZ = 7,
        Direction = 2,
        Appearance = new CreatureOutfitAppearance(142, 20, 30, 40, 50, 0),
    };

    public static CreatureSpawnConfig DefaultNpc => new()
    {
		Name = "NPC",
        TileX = -1,
        TileY = -1,
		TileZ = 7,
        Direction = 2,
        Appearance = new CreatureOutfitAppearance(39, 0, 0, 0, 0, 0),
    };

	public string Name { get; init; } = "Player";
    public int TileX { get; init; }
    public int TileY { get; init; }
	public int TileZ { get; init; } = 7;
    public int Direction { get; init; }
    public CreatureOutfitAppearance Appearance { get; init; } = new(39);
}



public sealed class ClientConfig
{
    public static ClientConfig Default => new()
    {
        ClientVersion = 860,
        ExtendedSpriteIds = null,
        TransparentSprites = false,
        MaxCachedPages = 64,
        MetadataFormat = "auto",
        SpritesFormat = "auto",
        InMemoryLoading = false,
    };

    public int ClientVersion { get; init; } = 860;
    public bool? ExtendedSpriteIds { get; init; }
    public bool? ImprovedAnimations { get; init; }
    public bool? OutfitFrameGroups { get; init; }
    public bool TransparentSprites { get; init; }
    public bool ExportJson { get; init; }
    public string? ShadersDir { get; init; }
    public int MaxCachedPages { get; init; } = 64;
    public string MetadataFormat { get; init; } = "auto";
    public string SpritesFormat { get; init; } = "auto";
    public bool InMemoryLoading { get; init; } = false;

    public ClientDataReadOptions ToReadOptions() => new()
    {
        ClientVersion = new ClientDataVersion((uint)ClientVersion),
        ExtendedSpriteIds = ExtendedSpriteIds,
        ImprovedAnimations = ImprovedAnimations,
        OutfitFrameGroups = OutfitFrameGroups,
        TransparentSprites = TransparentSprites,
    };
}

public sealed class MapConfig
{
    public static MapConfig Default => new()
    {
        SectorsPath = "resources/sectors",
    };

    public string SectorsPath { get; init; } = "resources/sectors";
}
