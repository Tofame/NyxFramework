using System.Text.Json;

namespace NyxAssets.Things;

/// <summary>Single source of truth for <see cref="ThingType"/> JSON field names and read/write rules.</summary>
internal static class ThingTypeJsonMapper
{
    private delegate bool BoolGetter(ThingType thing);
    private delegate void BoolSetter(ThingType thing, bool value);
    private delegate uint UIntGetter(ThingType thing);
    private delegate void UIntSetter(ThingType thing, uint value);
    private delegate int IntGetter(ThingType thing);
    private delegate void IntSetter(ThingType thing, int value);

    private readonly record struct BoolField(string Name, BoolGetter Get, BoolSetter Set);

    private readonly record struct UIntField(string Name, UIntGetter Get, UIntSetter Set);

    private readonly record struct IntField(string Name, IntGetter Get, IntSetter Set);

    private static readonly BoolField[] BoolFields =
    [
        new("isGround", t => t.IsGround, (t, v) => t.IsGround = v),
        new("isGroundBorder", t => t.IsGroundBorder, (t, v) => t.IsGroundBorder = v),
        new("isOnBottom", t => t.IsOnBottom, (t, v) => t.IsOnBottom = v),
        new("isOnTop", t => t.IsOnTop, (t, v) => t.IsOnTop = v),
        new("isContainer", t => t.IsContainer, (t, v) => t.IsContainer = v),
        new("stackable", t => t.Stackable, (t, v) => t.Stackable = v),
        new("forceUse", t => t.ForceUse, (t, v) => t.ForceUse = v),
        new("multiUse", t => t.MultiUse, (t, v) => t.MultiUse = v),
        new("hasCharges", t => t.HasCharges, (t, v) => t.HasCharges = v),
        new("writable", t => t.Writable, (t, v) => t.Writable = v),
        new("writableOnce", t => t.WritableOnce, (t, v) => t.WritableOnce = v),
        new("isFluidContainer", t => t.IsFluidContainer, (t, v) => t.IsFluidContainer = v),
        new("isFluid", t => t.IsFluid, (t, v) => t.IsFluid = v),
        new("isUnpassable", t => t.IsUnpassable, (t, v) => t.IsUnpassable = v),
        new("isUnmoveable", t => t.IsUnmoveable, (t, v) => t.IsUnmoveable = v),
        new("blockMissile", t => t.BlockMissile, (t, v) => t.BlockMissile = v),
        new("blockPathfind", t => t.BlockPathfind, (t, v) => t.BlockPathfind = v),
        new("noMoveAnimation", t => t.NoMoveAnimation, (t, v) => t.NoMoveAnimation = v),
        new("pickupable", t => t.Pickupable, (t, v) => t.Pickupable = v),
        new("hangable", t => t.Hangable, (t, v) => t.Hangable = v),
        new("isVertical", t => t.IsVertical, (t, v) => t.IsVertical = v),
        new("isHorizontal", t => t.IsHorizontal, (t, v) => t.IsHorizontal = v),
        new("rotatable", t => t.Rotatable, (t, v) => t.Rotatable = v),
        new("hasLight", t => t.HasLight, (t, v) => t.HasLight = v),
        new("dontHide", t => t.DontHide, (t, v) => t.DontHide = v),
        new("isTranslucent", t => t.IsTranslucent, (t, v) => t.IsTranslucent = v),
        new("floorChange", t => t.FloorChange, (t, v) => t.FloorChange = v),
        new("hasOffset", t => t.HasOffset, (t, v) => t.HasOffset = v),
        new("hasElevation", t => t.HasElevation, (t, v) => t.HasElevation = v),
        new("isLyingObject", t => t.IsLyingObject, (t, v) => t.IsLyingObject = v),
        new("animateAlways", t => t.AnimateAlways, (t, v) => t.AnimateAlways = v),
        new("miniMap", t => t.MiniMap, (t, v) => t.MiniMap = v),
        new("isLensHelp", t => t.IsLensHelp, (t, v) => t.IsLensHelp = v),
        new("isFullGround", t => t.IsFullGround, (t, v) => t.IsFullGround = v),
        new("ignoreLook", t => t.IgnoreLook, (t, v) => t.IgnoreLook = v),
        new("cloth", t => t.Cloth, (t, v) => t.Cloth = v),
        new("isMarketItem", t => t.IsMarketItem, (t, v) => t.IsMarketItem = v),
        new("hasDefaultAction", t => t.HasDefaultAction, (t, v) => t.HasDefaultAction = v),
        new("wrappable", t => t.Wrappable, (t, v) => t.Wrappable = v),
        new("unwrappable", t => t.Unwrappable, (t, v) => t.Unwrappable = v),
        new("bottomEffect", t => t.BottomEffect, (t, v) => t.BottomEffect = v),
        new("dontCenterOutfit", t => t.DontCenterOutfit, (t, v) => t.DontCenterOutfit = v),
        new("usable", t => t.Usable, (t, v) => t.Usable = v),
    ];

    private static readonly UIntField[] UIntFields =
    [
        new("groundSpeed", t => t.GroundSpeed, (t, v) => t.GroundSpeed = v),
        new("maxTextLength", t => t.MaxTextLength, (t, v) => t.MaxTextLength = v),
        new("lightLevel", t => t.LightLevel, (t, v) => t.LightLevel = v),
        new("lightColor", t => t.LightColor, (t, v) => t.LightColor = v),
        new("elevation", t => t.Elevation, (t, v) => t.Elevation = v),
        new("miniMapColor", t => t.MiniMapColor, (t, v) => t.MiniMapColor = v),
        new("lensHelp", t => t.LensHelp, (t, v) => t.LensHelp = v),
        new("clothSlot", t => t.ClothSlot, (t, v) => t.ClothSlot = v),
        new("marketCategory", t => t.MarketCategory, (t, v) => t.MarketCategory = v),
        new("marketTradeAs", t => t.MarketTradeAs, (t, v) => t.MarketTradeAs = v),
        new("marketShowAs", t => t.MarketShowAs, (t, v) => t.MarketShowAs = v),
        new("marketRestrictProfession", t => t.MarketRestrictProfession, (t, v) => t.MarketRestrictProfession = v),
        new("marketRestrictLevel", t => t.MarketRestrictLevel, (t, v) => t.MarketRestrictLevel = v),
        new("defaultAction", t => t.DefaultAction, (t, v) => t.DefaultAction = v),
    ];

    private static readonly IntField[] IntFields =
    [
        new("offsetX", t => t.OffsetX, (t, v) => t.OffsetX = v),
        new("offsetY", t => t.OffsetY, (t, v) => t.OffsetY = v),
    ];

    public static ThingType ReadThing(JsonElement elem, ThingKind kind)
    {
        var thing = new ThingType
        {
            Id = elem.GetProperty("id").GetUInt32(),
            Kind = kind,
        };

        ReadScalarFields(elem, thing);
        ReadFrameGroups(elem, thing);
        ReadExtraProperties(elem, thing.ExtraProperties);
        return thing;
    }

    public static void WriteThing(Utf8JsonWriter writer, ThingType thing)
    {
        writer.WriteStartObject();
        writer.WriteNumber("id", thing.Id);

        WriteScalarFields(writer, thing);
        WriteFrameGroups(writer, thing);
        WriteExtraProperties(writer, thing.ExtraProperties);

        writer.WriteEndObject();
    }

    private static void ReadScalarFields(JsonElement elem, ThingType thing)
    {
        foreach (var field in BoolFields)
        {
            if (elem.TryGetProperty(field.Name, out var value))
                field.Set(thing, value.GetBoolean());
        }

        foreach (var field in UIntFields)
        {
            if (elem.TryGetProperty(field.Name, out var value))
                field.Set(thing, value.GetUInt32());
        }

        foreach (var field in IntFields)
        {
            if (elem.TryGetProperty(field.Name, out var value))
                field.Set(thing, value.GetInt32());
        }

        if (elem.TryGetProperty("marketName", out var marketName))
            thing.MarketName = marketName.GetString();
    }

    private static void WriteScalarFields(Utf8JsonWriter writer, ThingType thing)
    {
        foreach (var field in BoolFields)
        {
            if (field.Get(thing))
                writer.WriteBoolean(field.Name, true);
        }

        foreach (var field in UIntFields)
        {
            var value = field.Get(thing);
            if (value != 0)
                writer.WriteNumber(field.Name, value);
        }

        foreach (var field in IntFields)
        {
            var value = field.Get(thing);
            if (value != 0)
                writer.WriteNumber(field.Name, value);
        }

        if (thing.MarketName != null)
            writer.WriteString("marketName", thing.MarketName);
    }

    private static void ReadFrameGroups(JsonElement elem, ThingType thing)
    {
        if (!elem.TryGetProperty("frameGroups", out var frameGroups))
            return;

        foreach (var fgElem in frameGroups.EnumerateArray())
            thing.FrameGroups.Add(ReadFrameGroup(fgElem));
    }

    private static ThingFrameGroup ReadFrameGroup(JsonElement elem)
    {
        var fg = new ThingFrameGroup();

        if (elem.TryGetProperty("width", out var value)) fg.Width = value.GetUInt32();
        if (elem.TryGetProperty("height", out value)) fg.Height = value.GetUInt32();
        if (elem.TryGetProperty("exactSize", out value)) fg.ExactSize = value.GetUInt32();
        if (elem.TryGetProperty("layers", out value)) fg.Layers = value.GetUInt32();
        if (elem.TryGetProperty("patternX", out value)) fg.PatternX = value.GetUInt32();
        if (elem.TryGetProperty("patternY", out value)) fg.PatternY = value.GetUInt32();
        if (elem.TryGetProperty("patternZ", out value)) fg.PatternZ = value.GetUInt32();
        if (elem.TryGetProperty("frames", out value)) fg.Frames = value.GetUInt32();
        if (elem.TryGetProperty("groupTypeId", out value)) fg.GroupTypeId = value.GetUInt32();

        if (elem.TryGetProperty("isAnimation", out value) && value.GetBoolean())
        {
            fg.IsAnimation = true;
            if (elem.TryGetProperty("animationMode", out value)) fg.AnimationMode = value.GetUInt32();
            if (elem.TryGetProperty("loopCount", out value)) fg.LoopCount = value.GetInt32();
            if (elem.TryGetProperty("startFrame", out value)) fg.StartFrame = value.GetInt32();

            if (elem.TryGetProperty("frameTimings", out var timings))
            {
                var timingList = new List<AnimationFrameTiming>();
                foreach (var timingElem in timings.EnumerateArray())
                {
                    var min = timingElem.GetProperty("min").GetUInt32();
                    var max = timingElem.GetProperty("max").GetUInt32();
                    timingList.Add(new AnimationFrameTiming(min, max));
                }

                fg.FrameTimings = timingList.ToArray();
            }
        }

        if (elem.TryGetProperty("spriteIds", out var spriteIds))
        {
            var ids = new List<uint>();
            foreach (var id in spriteIds.EnumerateArray())
                ids.Add(id.GetUInt32());
            fg.SpriteIds = ids.ToArray();
        }

        return fg;
    }

    private static void WriteFrameGroups(Utf8JsonWriter writer, ThingType thing)
    {
        if (thing.FrameGroups.Count == 0)
            return;

        writer.WriteStartArray("frameGroups");
        foreach (var fg in thing.FrameGroups)
            WriteFrameGroup(writer, fg);
        writer.WriteEndArray();
    }

    private static void WriteFrameGroup(Utf8JsonWriter writer, ThingFrameGroup fg)
    {
        writer.WriteStartObject();

        if (fg.Width != 1) writer.WriteNumber("width", fg.Width);
        if (fg.Height != 1) writer.WriteNumber("height", fg.Height);
        if (fg.ExactSize != 32) writer.WriteNumber("exactSize", fg.ExactSize);
        if (fg.Layers != 1) writer.WriteNumber("layers", fg.Layers);
        if (fg.PatternX != 1) writer.WriteNumber("patternX", fg.PatternX);
        if (fg.PatternY != 1) writer.WriteNumber("patternY", fg.PatternY);
        if (fg.PatternZ != 1) writer.WriteNumber("patternZ", fg.PatternZ);
        if (fg.Frames != 1) writer.WriteNumber("frames", fg.Frames);
        if (fg.GroupTypeId != 0) writer.WriteNumber("groupTypeId", fg.GroupTypeId);

        if (fg.IsAnimation)
        {
            writer.WriteBoolean("isAnimation", true);
            writer.WriteNumber("animationMode", fg.AnimationMode);
            writer.WriteNumber("loopCount", fg.LoopCount);
            writer.WriteNumber("startFrame", fg.StartFrame);

            if (fg.FrameTimings is { Length: > 0 })
            {
                writer.WriteStartArray("frameTimings");
                foreach (var timing in fg.FrameTimings)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("min", timing.MinimumMilliseconds);
                    writer.WriteNumber("max", timing.MaximumMilliseconds);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }
        }

        writer.WriteStartArray("spriteIds");
        foreach (var spriteId in fg.SpriteIds)
            writer.WriteNumberValue(spriteId);
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static void ReadExtraProperties(JsonElement elem, Dictionary<string, string> dest)
    {
        if (!elem.TryGetProperty("properties", out var props))
            return;

        foreach (var prop in props.EnumerateObject())
            ReadExtraProperty(dest, prop.Name, prop.Value);
    }

    private static void ReadExtraProperty(Dictionary<string, string> dest, string key, JsonElement val)
    {
        if (val.ValueKind == JsonValueKind.Object)
        {
            foreach (var subProp in val.EnumerateObject())
            {
                var subKey = subProp.Name;
                var subVal = JsonElementToString(subProp.Value);

                if (subKey.Equals("value", StringComparison.OrdinalIgnoreCase))
                    dest[key] = subVal;
                else
                    dest[$"{key}.{subKey}"] = subVal;
            }

            return;
        }

        dest[key] = JsonElementToString(val);
    }

    private static void WriteExtraProperties(Utf8JsonWriter writer, Dictionary<string, string> properties)
    {
        if (properties.Count == 0)
            return;

        writer.WriteStartObject("properties");

        var nestedGroups = new Dictionary<string, List<KeyValuePair<string, string>>>();
        var flatProperties = new List<KeyValuePair<string, string>>();

        foreach (var kvp in properties)
        {
            var dotIdx = kvp.Key.IndexOf('.');
            if (dotIdx > 0 && dotIdx < kvp.Key.Length - 1)
            {
                var parentKey = kvp.Key[..dotIdx];
                if (!nestedGroups.TryGetValue(parentKey, out var list))
                {
                    list = new List<KeyValuePair<string, string>>();
                    nestedGroups[parentKey] = list;
                }

                list.Add(kvp);
            }
            else
            {
                flatProperties.Add(kvp);
            }
        }

        foreach (var kvp in flatProperties)
        {
            if (nestedGroups.ContainsKey(kvp.Key))
            {
                writer.WriteStartObject(kvp.Key);
                WriteExtraPropertyValue(writer, "value", kvp.Value);
                foreach (var childKvp in nestedGroups[kvp.Key])
                {
                    var childSubKey = childKvp.Key[(kvp.Key.Length + 1)..];
                    WriteExtraPropertyValue(writer, childSubKey, childKvp.Value);
                }

                writer.WriteEndObject();
            }
            else
            {
                WriteExtraPropertyValue(writer, kvp.Key, kvp.Value);
            }
        }

        foreach (var group in nestedGroups)
        {
            if (flatProperties.Exists(p => p.Key == group.Key))
                continue;

            writer.WriteStartObject(group.Key);
            foreach (var childKvp in group.Value)
            {
                var childSubKey = childKvp.Key[(group.Key.Length + 1)..];
                WriteExtraPropertyValue(writer, childSubKey, childKvp.Value);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteExtraPropertyValue(Utf8JsonWriter writer, string key, string val)
    {
        if (bool.TryParse(val, out var b))
        {
            writer.WriteBoolean(key, b);
            return;
        }

        if (long.TryParse(val, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var l))
        {
            writer.WriteNumber(key, l);
            return;
        }

        if (double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)
            && !double.IsNaN(d)
            && !double.IsInfinity(d))
        {
            writer.WriteNumber(key, d);
            return;
        }

        writer.WriteString(key, val);
    }

    private static string JsonElementToString(JsonElement val) =>
        val.ValueKind switch
        {
            JsonValueKind.String => val.GetString() ?? "",
            JsonValueKind.Number => val.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => val.ToString(),
        };
}
