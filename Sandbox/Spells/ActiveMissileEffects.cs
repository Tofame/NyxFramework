using NyxDrawer;
using NyxDrawer.Distance;
using NyxAssets.Client;

namespace Sandbox.Spells;

internal sealed class ActiveMissileEffects
{
    /// <summary>
    /// NyxClient uses <c>150 * sqrt(dx²+dy²)</c> ms. Mouse-target shots span many tiles, so scale down for sandbox feel.
    /// </summary>
    private const float OtClientDurationScale = 0.45f;
    private const float MinDurationMs = 90f;
    private const float MaxDurationMs = 550f;

    private readonly List<Entry> _entries = [];
    private float _elapsedMs;

    public void Add(SpellMissileFlight flight)
    {
        var dx = flight.ToTileX - flight.FromTileX;
        var dy = flight.ToTileY - flight.FromTileY;
        var durationMs = DistanceEffectDrawer.DurationMs(dx, dy) * OtClientDurationScale;
        durationMs = Math.Clamp(durationMs, MinDurationMs, MaxDurationMs);

        _entries.Add(new Entry(flight, _elapsedMs, durationMs));
    }

    public void Update(float deltaSeconds) =>
        _elapsedMs += deltaSeconds * 1000f;

    public void Prune() =>
        _entries.RemoveAll(e => _elapsedMs >= e.StartedAtMs + e.DurationMs);

    public void Draw(
        ClientAssetBundle assets,
        AssetDrawer drawer,
        float camXf,
        float camYf,
        int winW,
        int winH)
    {
        var things = assets.Things;
        foreach (var entry in _entries)
        {
            var ageMs = _elapsedMs - entry.StartedAtMs;
            if (ageMs >= entry.DurationMs)
                continue;

            if (things.TryGetMissile(entry.Flight.MissileId) is not { } missileThing)
                continue;

            var f = entry.Flight;
            var dx = f.ToTileX - f.FromTileX;
            var dy = f.ToTileY - f.FromTileY;
            var fromX = (f.FromTileX - camXf) * 32f;
            var fromY = (f.FromTileY - camYf) * 32f;

            if (fromX >= winW && fromY >= winH)
                continue;

            var progress = Math.Clamp(ageMs / entry.DurationMs, 0f, 1f);

            drawer.DistanceEffects.Draw(new DistanceEffectDrawRequest
            {
                Missile = missileThing,
                FromAnchorX = fromX,
                FromAnchorY = fromY,
                TileDeltaX = dx,
                TileDeltaY = dy,
                Progress = progress,
            });
        }
    }

    private readonly record struct Entry(SpellMissileFlight Flight, float StartedAtMs, float DurationMs);
}
