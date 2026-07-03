using NyxAssets.Data.Readers;
using NyxAssets.Data.Writers;
using NyxAssets.Sprites;

namespace NyxAssets.Things;

/// <summary>All object types loaded from a client <c>.dat</c> file.</summary>
public sealed class ThingCatalog
{
    public const uint FirstItemId = 100;
    public const uint FirstOutfitId = 1;
    public const uint FirstEffectId = 1;
    public const uint FirstMissileId = 1;

    private readonly Dictionary<uint, ThingType> _items = new();
    private readonly Dictionary<uint, ThingType> _outfits = new();
    private readonly Dictionary<uint, ThingType> _effects = new();
    private readonly Dictionary<uint, ThingType> _missiles = new();
    private int _maxLyingRedrawW = -1;
    private int _maxLyingRedrawH = -1;

    private ThingType?[] _itemsArray = Array.Empty<ThingType?>();
    private ThingType?[] _outfitsArray = Array.Empty<ThingType?>();
    private ThingType?[] _effectsArray = Array.Empty<ThingType?>();
    private ThingType?[] _missilesArray = Array.Empty<ThingType?>();

    public ThingCatalog() { }

    internal ThingCatalog(
        uint datSignature,
        uint itemCount,
        uint outfitCount,
        uint effectCount,
        uint missileCount,
        DatThingFormat datFormat)
    {
        DatSignature = datSignature;
        ItemCount = itemCount;
        OutfitCount = outfitCount;
        EffectCount = effectCount;
        MissileCount = missileCount;
        DatFormat = datFormat;
    }

    public uint DatSignature { get; set; }

    /// <summary>
    /// Inclusive last item id in this <c>.dat</c> (Asset Editor loops <c>MIN_ITEM_ID</c> … this value). Same on-disk field as "items count" in some docs.
    /// </summary>
    public uint ItemCount { get; private set; }

    /// <summary>Inclusive last outfit id (loops <see cref="FirstOutfitId"/> … this value).</summary>
    public uint OutfitCount { get; private set; }

    /// <summary>Inclusive last effect id (loops <see cref="FirstEffectId"/> … this value).</summary>
    public uint EffectCount { get; private set; }

    /// <summary>Inclusive last missile id (loops <see cref="FirstMissileId"/> … this value).</summary>
    public uint MissileCount { get; private set; }
    public DatThingFormat DatFormat { get; set; }

    internal Dictionary<uint, ThingType> ItemsMutable => _items;
    internal Dictionary<uint, ThingType> OutfitsMutable => _outfits;
    internal Dictionary<uint, ThingType> EffectsMutable => _effects;
    internal Dictionary<uint, ThingType> MissilesMutable => _missiles;

    public static ThingCatalog Load(ReadOnlyMemory<byte> datFile, ClientDataReadOptions options) =>
        new DatThingCatalogReader().Read(datFile, options);

    /// <summary>Registers or replaces an item. New ids must be contiguous: exactly <c>ItemCount + 1</c> when appending.</summary>
    public void PutItem(ThingType thing, bool rebuildArrays = true) =>
        PutThing(thing, ThingKind.Item, FirstItemId, _items, rebuildArrays);

    /// <summary>Registers or replaces an outfit. New ids must be contiguous: exactly <c>OutfitCount + 1</c> when appending.</summary>
    public void PutOutfit(ThingType thing, bool rebuildArrays = true) =>
        PutThing(thing, ThingKind.Outfit, FirstOutfitId, _outfits, rebuildArrays);

    /// <summary>Registers or replaces an effect. New ids must be contiguous: exactly <c>EffectCount + 1</c> when appending.</summary>
    public void PutEffect(ThingType thing, bool rebuildArrays = true) =>
        PutThing(thing, ThingKind.Effect, FirstEffectId, _effects, rebuildArrays);

    /// <summary>Registers or replaces a missile. New ids must be contiguous: exactly <c>MissileCount + 1</c> when appending.</summary>
    public void PutMissile(ThingType thing, bool rebuildArrays = true) =>
        PutThing(thing, ThingKind.Missile, FirstMissileId, _missiles, rebuildArrays);

    private void PutThing(
        ThingType thing,
        ThingKind expectedKind,
        uint firstId,
        Dictionary<uint, ThingType> bucket,
        bool rebuildArrays)
    {
        ArgumentNullException.ThrowIfNull(thing);
        if (thing.Kind != expectedKind)
            throw new ArgumentException($"Thing kind must be {expectedKind}.", nameof(thing));
        if (thing.Id < firstId)
            throw new ArgumentOutOfRangeException(nameof(thing), $"{expectedKind} id must be >= {firstId}.");

        var inclusiveMax = GetInclusiveMax(expectedKind);
        if (thing.Id > inclusiveMax && thing.Id != NextAppendId(inclusiveMax, firstId))
            throw new ArgumentException(
                $"New {expectedKind.ToString().ToLowerInvariant()} id must be {NextAppendId(inclusiveMax, firstId)} (contiguous append). Got {thing.Id}.",
                nameof(thing));

        EnsureWritableThing(thing);
        bucket[thing.Id] = thing;
        if (thing.Id > inclusiveMax)
            SetInclusiveMax(expectedKind, thing.Id);

        if (rebuildArrays)
            InitializeFastArrays();
    }

    private uint GetInclusiveMax(ThingKind kind) =>
        kind switch
        {
            ThingKind.Item => ItemCount,
            ThingKind.Outfit => OutfitCount,
            ThingKind.Effect => EffectCount,
            ThingKind.Missile => MissileCount,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private void SetInclusiveMax(ThingKind kind, uint value)
    {
        switch (kind)
        {
            case ThingKind.Item:
                ItemCount = value;
                break;
            case ThingKind.Outfit:
                OutfitCount = value;
                break;
            case ThingKind.Effect:
                EffectCount = value;
                break;
            case ThingKind.Missile:
                MissileCount = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static void EnsureWritableThing(ThingType thing)
    {
        if (thing.FrameGroups.Count == 0)
            throw new InvalidOperationException($"Thing {thing.Id} ({thing.Kind}) has no frame groups; writers require at least one.");
    }

    /// <summary>Next free id when appending to a section whose inclusive max is <paramref name="inclusiveMax"/>.</summary>
    private static uint NextAppendId(uint inclusiveMax, uint firstId) =>
        inclusiveMax < firstId ? Math.Max(firstId, inclusiveMax + 1) : inclusiveMax + 1;

    public ThingType? TryGetItem(uint id) => id < (uint)_itemsArray.Length ? _itemsArray[id] : null;

    public ThingType GetItem(uint id) =>
        TryGetItem(id) ?? throw new KeyNotFoundException($"Item id {id}.");

    public ThingType? TryGetOutfit(uint id) => id < (uint)_outfitsArray.Length ? _outfitsArray[id] : null;

    public ThingType GetOutfit(uint id) =>
        TryGetOutfit(id) ?? throw new KeyNotFoundException($"Outfit id {id}.");

    public ThingType? TryGetEffect(uint id) => id < (uint)_effectsArray.Length ? _effectsArray[id] : null;

    public ThingType GetEffect(uint id) =>
        TryGetEffect(id) ?? throw new KeyNotFoundException($"Effect id {id}.");

    /// <summary>Missile / "distance effect" slot from the <c>.dat</c> missile section.</summary>
    public ThingType? TryGetMissile(uint id) => id < (uint)_missilesArray.Length ? _missilesArray[id] : null;

    public ThingType GetMissile(uint id) =>
        TryGetMissile(id) ?? throw new KeyNotFoundException($"Missile id {id}.");

    /// <summary>
    /// Max (frame width−1, height−1) among <see cref="ThingType.IsLyingObject"/> items in this <c>.dat</c>.
    /// Cached after first call; used to bound top-correction scans (lying redraw reach west/north of origin).
    /// </summary>
    public (int RedrawW, int RedrawH) GetMaxLyingItemRedrawSpan()
    {
        if (_maxLyingRedrawW >= 0)
            return (_maxLyingRedrawW, _maxLyingRedrawH);

        var maxW = 0;
        var maxH = 0;
        foreach (var item in _items.Values)
        {
            if (!item.IsLyingObject || item.FrameGroups.Count == 0)
                continue;
            var fg = item.FrameGroups[0];
            var w = fg.Width == 0 ? 1u : fg.Width;
            var h = fg.Height == 0 ? 1u : fg.Height;
            maxW = Math.Max(maxW, (int)w - 1);
            maxH = Math.Max(maxH, (int)h - 1);
        }

        _maxLyingRedrawW = maxW;
        _maxLyingRedrawH = maxH;
        return (maxW, maxH);
    }

    public IEnumerable<ThingType> EnumerateItems() => EnumerateSection(FirstItemId, ItemCount, TryGetItem);
    public IEnumerable<ThingType> EnumerateOutfits() => EnumerateSection(FirstOutfitId, OutfitCount, TryGetOutfit);
    public IEnumerable<ThingType> EnumerateEffects() => EnumerateSection(FirstEffectId, EffectCount, TryGetEffect);
    public IEnumerable<ThingType> EnumerateMissiles() => EnumerateSection(FirstMissileId, MissileCount, TryGetMissile);

    private static IEnumerable<ThingType> EnumerateSection(
        uint firstId,
        uint inclusiveMax,
        Func<uint, ThingType?> tryGet)
    {
        if (inclusiveMax < firstId)
            yield break;

        for (var id = firstId; id <= inclusiveMax; id++)
        {
            var thing = tryGet(id);
            if (thing != null)
                yield return thing;
        }
    }

    /// <summary>
    /// Writes this catalog back to a <c>.dat</c> stream using the same binary layout as Asset Editor compile
    /// for the catalog's <see cref="DatFormat"/> (<c>MetadataWriter1</c>–<c>MetadataWriter6</c>).
    /// </summary>
    public void WriteDatTo(Stream output, ClientDataReadOptions formatOptions, uint? datSignatureOverride = null) =>
        new DatThingCatalogWriter().Write(this, output, formatOptions, datSignatureOverride);

    /// <summary>Exports this catalog to JSON format (compact, tool-friendly).</summary>
    public void ExportJson(Stream output, ClientDataReadOptions options, uint? signatureOverride = null, string? itemsXmlPath = null)
    {
        if (itemsXmlPath != null)
            ItemsXmlMerger.MergeFromFile(this, itemsXmlPath);

        new JsonThingCatalogWriter().Write(this, output, options, signatureOverride);
    }

    /// <summary>Exports this catalog to a JSON file.</summary>
    public void ExportJson(string filePath, ClientDataReadOptions options, uint? signatureOverride = null, string? itemsXmlPath = null)
    {
        using var fs = File.Create(filePath);
        ExportJson(fs, options, signatureOverride, itemsXmlPath);
    }

    /// <summary>Loads item properties from an items.xml file and merges them into this catalog.</summary>
    public void LoadItemsXml(string filePath) => ItemsXmlMerger.MergeFromFile(this, filePath);

    /// <summary>Loads item properties from an items.xml stream and merges them into this catalog.</summary>
    public void LoadItemsXml(Stream input) => ItemsXmlMerger.Merge(this, input);

    /// <summary>Loads a catalog from JSON format.</summary>
    public static ThingCatalog LoadJson(ReadOnlyMemory<byte> jsonData, ClientDataReadOptions options) =>
        new JsonThingCatalogReader().Read(jsonData, options);

    /// <summary>Loads a catalog from a JSON file.</summary>
    public static ThingCatalog LoadJson(string filePath, ClientDataReadOptions options) =>
        LoadJson(File.ReadAllBytes(filePath).AsMemory(), options);

    internal void InitializeFastArrays()
    {
        _itemsArray = new ThingType?[ItemCount + 1];
        foreach (var kv in _items)
            _itemsArray[kv.Key] = kv.Value;

        _outfitsArray = new ThingType?[OutfitCount + 1];
        foreach (var kv in _outfits)
            _outfitsArray[kv.Key] = kv.Value;

        _effectsArray = new ThingType?[EffectCount + 1];
        foreach (var kv in _effects)
            _effectsArray[kv.Key] = kv.Value;

        _missilesArray = new ThingType?[MissileCount + 1];
        foreach (var kv in _missiles)
            _missilesArray[kv.Key] = kv.Value;
    }

    internal static void LoadDatSection(
        ref LittleEndianSpanReader reader,
        DatThingFormat format,
        ClientDataReadOptions options,
        bool extendedIds,
        bool improvedAnimations,
        bool outfitFrameGroups,
        uint minId,
        uint maxId,
        ThingKind kind,
        Dictionary<uint, ThingType> bucket)
    {
        for (var id = minId; id <= maxId; id++)
        {
            var thing = new ThingType { Id = id, Kind = kind };
            ThingPropertyDecoder.Read(ref reader, thing, format);
            var defaultMs = options.ResolveDefaultFrameDurationMs(kind);
            if (format <= DatThingFormat.V2_7_40__7_50)
                ThingTextureDecoder.Read(ref reader, thing, extendedIds, improvedAnimations, outfitFrameGroups, defaultMs, includePatternZ: false);
            else
                ThingTextureDecoder.Read(ref reader, thing, extendedIds, improvedAnimations, outfitFrameGroups, defaultMs, includePatternZ: true);
            bucket[id] = thing;
        }
    }

    internal ThingType GetExisting(ThingKind kind, uint id) =>
        kind switch
        {
            ThingKind.Item => GetItem(id),
            ThingKind.Outfit => GetOutfit(id),
            ThingKind.Effect => GetEffect(id),
            ThingKind.Missile => GetMissile(id),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
}
