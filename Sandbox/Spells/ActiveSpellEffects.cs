using NyxDrawer;
using NyxDrawer.Effects;
using NyxAssets.Client;
using NyxAssets.Things;

namespace Sandbox.Spells;

internal sealed class ActiveSpellEffects
{
    private readonly List<Entry> _entries = [];
    private float _elapsedMs;

    public void AddHits(IReadOnlyList<SpellTileHit> hits, float durationMs = 600f)
    {
        var started = _elapsedMs;
        foreach (var hit in hits)
            _entries.Add(new Entry(hit.Position, hit.EffectId, started, durationMs));
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

            if (things.TryGetEffect(entry.EffectId) is not { } effectThing)
                continue;

            var sx = (entry.Position.X - camXf) * 32f;
            var sy = (entry.Position.Y - camYf) * 32f;
            if (sx >= winW || sy >= winH || sx + 32f <= 0f || sy + 32f <= 0f)
                continue;

            var frame = drawer.Animator.GetEffectFrame(effectThing, ageMs);

            drawer.Effects.Draw(new EffectDrawRequest
            {
                Effect = effectThing,
                AnchorX = sx,
                AnchorY = sy,
                TileX = entry.Position.X,
                TileY = entry.Position.Y,
                Frame = frame,
            });
        }
    }

    private readonly record struct Entry(Position Position, uint EffectId, float StartedAtMs, float DurationMs);
}
