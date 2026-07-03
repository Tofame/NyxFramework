namespace NyxAssets.Things;

/// <summary>
/// Serializes a <see cref="ThingCatalog"/> in a specific format.
/// Implement this to write thing definitions to alternative formats.
/// </summary>
public interface IThingCatalogWriter
{
    void Write(ThingCatalog catalog, Stream output, ClientDataReadOptions options, uint? signatureOverride = null);
}
