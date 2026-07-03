namespace NyxAssets.Things;

/// <summary>
/// Reads raw asset data in a specific format and produces a populated <see cref="ThingCatalog"/>.
/// Implement this to support alternative thing-definition formats (JSON, XML, custom binary, etc.).
/// </summary>
public interface IThingCatalogReader
{
    ThingCatalog Read(ReadOnlyMemory<byte> data, ClientDataReadOptions options);
}
