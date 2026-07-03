namespace Sandbox.Spells;

internal static class SpellCaster
{
    public static bool TryCast(
        SpellCatalog catalog,
        SpellDefinition spell,
        Player player,
        IReadOnlyList<Npc> npcs,
        NyxGameMap.GameMap map,
        out IReadOnlyList<SpellTileHit> hits)
    {
        hits = [];

        if (spell.MouseTarget)
        {
            Console.WriteLine($"Spell \"{spell.Name}\": use missile cast (mouse target).");
            return false;
        }

        if (!catalog.TryGetScript(spell.ScriptName, out var script))
        {
            Console.WriteLine($"Spell \"{spell.Name}\": unknown script \"{spell.ScriptName}\".");
            return false;
        }

        if (!script.IsAreaSpell || script.Area is not { } area || script.EffectId is not { } effectId)
        {
            Console.WriteLine($"Spell \"{spell.Name}\": script is not an area spell.");
            return false;
        }

        if (!area.TryGetCasterAnchor(out var casterRow, out var casterCol))
        {
            Console.WriteLine($"Spell \"{spell.Name}\": area has no caster anchor.");
            return false;
        }

        var anchorX = player.Position.X;
        var anchorY = player.Position.Y;

        if (spell.NeedTarget)
        {
            if (!TryFindTargetTile(player, npcs, out var targetX, out var targetY))
            {
                Console.WriteLine($"Spell \"{spell.Name}\": no target.");
                return false;
            }

            anchorX = targetX;
            anchorY = targetY;
        }
        else if (spell.SelfTarget)
        {
            anchorX = player.Position.X;
            anchorY = player.Position.Y;
        }

        var waveDx = 0;
        var waveDy = 0;
        if (spell.Direction)
            (waveDx, waveDy) = ForwardDelta(player.Direction);

        var rows = area.Cells.GetLength(0);
        var cols = area.Cells.GetLength(1);
        var list = new List<SpellTileHit>();

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var cell = (SpellAreaCell)area.Cells[r, c];
                if (cell is not (SpellAreaCell.Effect or SpellAreaCell.Caster))
                    continue;

                var lateral = c - casterCol;
                var forward = casterRow - r;
                var (dx, dy) = MapLocalOffset(player.Direction, lateral, forward);

                var tileX = anchorX + dx + waveDx;
                var tileY = anchorY + dy + waveDy;

                if (!map.IsInside(new Position(tileX, tileY, 7)))
                    continue;

                list.Add(new SpellTileHit(new Position(tileX, tileY, player.Position.Z), effectId));
            }
        }

        if (list.Count == 0)
        {
            Console.WriteLine($"Spell \"{spell.Name}\": no tiles in range.");
            return false;
        }

        hits = list;
        Console.WriteLine($"Cast \"{spell.Name}\" ({spell.Words}) — {list.Count} tile(s), effect {effectId}.");
        return true;
    }

    internal static bool TryFindTargetTile(Player player, IReadOnlyList<Npc> npcs, out int targetX, out int targetY)
    {
        var (fwdX, fwdY) = ForwardDelta(player.Direction);
        var frontX = player.Position.X + fwdX;
        var frontY = player.Position.Y + fwdY;

        Npc? best = null;
        var bestDist = int.MaxValue;

        foreach (var npc in npcs)
        {
            if (npc.Position.X == frontX && npc.Position.Y == frontY)
            {
                targetX = npc.Position.X;
                targetY = npc.Position.Y;
                return true;
            }

            var dx = npc.Position.X - player.Position.X;
            var dy = npc.Position.Y - player.Position.Y;
            var dist = Math.Abs(dx) + Math.Abs(dy);
            if (dist >= bestDist)
                continue;

            if (!IsInFront(player.Direction, dx, dy))
                continue;

            bestDist = dist;
            best = npc;
        }

        if (best is not null)
        {
            targetX = best.Position.X;
            targetY = best.Position.Y;
            return true;
        }

        targetX = 0;
        targetY = 0;
        return false;
    }

    private static bool IsInFront(int direction, int dx, int dy)
    {
        return direction switch
        {
            0 => dy < 0,
            1 => dx > 0,
            2 => dy > 0,
            3 => dx < 0,
            _ => false,
        };
    }

    private static (int dx, int dy) MapLocalOffset(int direction, int lateral, int forward)
    {
        var (fwdX, fwdY) = ForwardDelta(direction);
        var (rightX, rightY) = RightDelta(direction);
        return (lateral * rightX + forward * fwdX, lateral * rightY + forward * fwdY);
    }

    public static bool TryCastMissileToMouse(
        SpellCatalog catalog,
        SpellDefinition spell,
        Player player,
        NyxGameMap.GameMap map,
        int targetTileX,
        int targetTileY,
        out SpellMissileFlight flight)
    {
        flight = default;

        if (!spell.MouseTarget)
        {
            Console.WriteLine($"Spell \"{spell.Name}\": not a mouse-target spell.");
            return false;
        }

        if (!catalog.TryGetScript(spell.ScriptName, out var script) || !script.IsMissileSpell || script.MissileId is not { } missileId)
        {
            Console.WriteLine($"Spell \"{spell.Name}\": script missing missileId.");
            return false;
        }

        var fromX = player.Position.X;
        var fromY = player.Position.Y;
        var toX = targetTileX;
        var toY = targetTileY;

        if (!map.IsInside(new Position(toX, toY, 7)))
        {
            Console.WriteLine($"Spell \"{spell.Name}\": target outside map.");
            return false;
        }

        if (fromX == toX && fromY == toY)
        {
            Console.WriteLine($"Spell \"{spell.Name}\": target is same tile as caster.");
            return false;
        }

        flight = new SpellMissileFlight(fromX, fromY, toX, toY, missileId);
        Console.WriteLine(
            $"Cast \"{spell.Name}\" missile {missileId} ({fromX},{fromY}) → ({toX},{toY}).");
        return true;
    }

    private static (int dx, int dy) ForwardDelta(int direction) => direction switch
    {
        0 => (0, -1),
        1 => (1, 0),
        2 => (0, 1),
        3 => (-1, 0),
        _ => (0, 0),
    };

    private static (int dx, int dy) RightDelta(int direction) => direction switch
    {
        0 => (1, 0),
        1 => (0, 1),
        2 => (-1, 0),
        3 => (0, -1),
        _ => (0, 0),
    };
}

internal readonly record struct SpellTileHit(Position Position, uint EffectId);
