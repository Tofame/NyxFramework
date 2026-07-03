namespace Sandbox.Spells;

internal readonly record struct SpellMissileFlight(
    int FromTileX,
    int FromTileY,
    int ToTileX,
    int ToTileY,
    uint MissileId);
