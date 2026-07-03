using NyxDrawer.Appearance;
using NyxAssets.Things;

namespace Sandbox;

/// <summary>Static NPC on a tile, drawn via <see cref="NyxDrawer.AssetDrawer"/>.</summary>
internal sealed class Npc : ICreature
{
    public Npc(
        Position position,
        CreatureOutfitAppearance appearance,
        ThingType outfitThing,
        ThingType? mountThing = null,
        int direction = 2)
    {
        Position = position;
        Appearance = appearance;
        _outfit = outfitThing;
        MountThing = mountThing;
        IsMounted = appearance.HasMount && mountThing is not null;
        Direction = direction;
    }

    public Npc(
        int tileX,
        int tileY,
        int tileZ,
        CreatureOutfitAppearance appearance,
        ThingType outfitThing,
        ThingType? mountThing = null,
        int direction = 2)
        : this(new Position(tileX, tileY, tileZ), appearance, outfitThing, mountThing, direction)
    {
    }

    public Position Position { get; }
    public CreatureOutfitAppearance Appearance { get; }
    public uint OutfitId => Appearance.LookType;
    public int Direction { get; }
    public ThingType OutfitThing => _outfit;
    public ThingType? MountThing { get; }
    public bool IsMounted { get; }

    public void GetDrawPosition(float cameraOriginTileX, float cameraOriginTileY, out float px, out float py)
    {
        px = (Position.X - cameraOriginTileX) * Player.SpriteSize;
        py = (Position.Y - cameraOriginTileY) * Player.SpriteSize;
    }

    private readonly ThingType _outfit;
}
