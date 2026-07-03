namespace NyxAssets.Things;

/// <summary>Merges TFS-style <c>items.xml</c> attribute data into an existing <see cref="ThingCatalog"/>.</summary>
public static class ItemsXmlMerger
{
    public static void MergeFromFile(ThingCatalog catalog, string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("items.xml file not found.", filePath);

        using var stream = File.OpenRead(filePath);
        Merge(catalog, stream);
    }

    public static void Merge(ThingCatalog catalog, Stream input)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(input);

        var doc = System.Xml.Linq.XDocument.Load(input);
        var root = doc.Root;
        if (root == null || !root.Name.LocalName.Equals("items", StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var itemElem in root.Elements())
        {
            if (!itemElem.Name.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!TryParseItemIdRange(itemElem, out var fromId, out var toId))
                continue;

            var props = CollectProperties(itemElem);
            for (var id = fromId; id <= toId; id++)
            {
                var thing = catalog.TryGetItem(id);
                if (thing == null)
                    continue;

                foreach (var (key, value) in props)
                    thing.ExtraProperties[key] = value;
            }
        }
    }

    private static bool TryParseItemIdRange(System.Xml.Linq.XElement itemElem, out uint fromId, out uint toId)
    {
        fromId = 0;
        toId = 0;

        string? idValue = null;
        string? fromIdValue = null;
        string? toIdValue = null;

        foreach (var attr in itemElem.Attributes())
        {
            var name = attr.Name.LocalName.ToLowerInvariant();
            if (name == "id") idValue = attr.Value;
            else if (name == "fromid") fromIdValue = attr.Value;
            else if (name == "toid") toIdValue = attr.Value;
        }

        if (idValue != null && uint.TryParse(idValue, out var singleId))
        {
            fromId = singleId;
            toId = singleId;
            return true;
        }

        if (fromIdValue != null && toIdValue != null
            && uint.TryParse(fromIdValue, out var fid)
            && uint.TryParse(toIdValue, out var tid))
        {
            fromId = fid;
            toId = tid;
            return fromId != 0 && toId != 0;
        }

        return false;
    }

    private static List<(string Key, string Value)> CollectProperties(System.Xml.Linq.XElement itemElem)
    {
        var props = new List<(string Key, string Value)>();

        foreach (var attr in itemElem.Attributes())
        {
            var name = attr.Name.LocalName.ToLowerInvariant();
            if (name is "id" or "fromid" or "toid")
                continue;

            if (name is "name" or "article" or "plural")
                props.Add((name, attr.Value));
        }

        foreach (var attrElem in itemElem.Elements())
        {
            if (!attrElem.Name.LocalName.Equals("attribute", StringComparison.OrdinalIgnoreCase))
                continue;

            string? keyAttr = null;
            string? valueAttr = null;

            foreach (var attr in attrElem.Attributes())
            {
                var name = attr.Name.LocalName.ToLowerInvariant();
                if (name == "key") keyAttr = attr.Value;
                else if (name == "value") valueAttr = attr.Value;
            }

            if (keyAttr == null || valueAttr == null)
                continue;

            var key = keyAttr.ToLowerInvariant();
            props.Add((key, valueAttr));

            if (key == "slottype")
                props.Add(("slot", valueAttr));
            else if (key == "containersize")
                props.Add(("max-slots", valueAttr));

            foreach (var childAttrElem in attrElem.Elements())
            {
                if (!childAttrElem.Name.LocalName.Equals("attribute", StringComparison.OrdinalIgnoreCase))
                    continue;

                string? childKeyAttr = null;
                string? childValueAttr = null;

                foreach (var cattr in childAttrElem.Attributes())
                {
                    var cname = cattr.Name.LocalName.ToLowerInvariant();
                    if (cname == "key") childKeyAttr = cattr.Value;
                    else if (cname == "value") childValueAttr = cattr.Value;
                }

                if (childKeyAttr != null && childValueAttr != null)
                    props.Add(($"{key}.{childKeyAttr.ToLowerInvariant()}", childValueAttr));
            }
        }

        return props;
    }
}
