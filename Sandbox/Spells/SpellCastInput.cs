using Silk.NET.Input;

namespace Sandbox.Spells;

internal static class SpellCastInput
{
    public static bool TryGetMouseTile(
        IInputContext input,
        float camXf,
        float camYf,
        NyxGameMap.GameMap map,
        out int tileX,
        out int tileY) =>
        TryGetMouseTile(input, camXf, camYf, map, mouseX: null, mouseY: null, out tileX, out tileY);

    public static bool TryGetMouseTile(
        IInputContext input,
        float camXf,
        float camYf,
        NyxGameMap.GameMap map,
        float? mouseX,
        float? mouseY,
        out int tileX,
        out int tileY)
    {
        tileX = 0;
        tileY = 0;

        float px;
        float py;
        if (mouseX is not null && mouseY is not null)
        {
            px = mouseX.Value;
            py = mouseY.Value;
        }
        else
        {
            if (input.Mice.Count == 0 || input.Mice[0] is not { } mouse)
                return false;
            var pos = mouse.Position;
            px = pos.X;
            py = pos.Y;
        }

        tileX = (int)Math.Floor(camXf + px / Player.SpriteSize);
        tileY = (int)Math.Floor(camYf + py / Player.SpriteSize);

		if (!map.IsInside(new Position(tileX, tileY, 7)))
			tileX = Math.Max(0, tileX);

        return true;
    }

    public static void GetCameraOrigin(
        Player player,
        int winW,
        int winH,
        out float camXf,
        out float camYf)
    {
        var spanTilesX = winW / Player.SpriteSize;
        var spanTilesY = winH / Player.SpriteSize;
        var halfViewTilesX = winW / (Player.SpriteSize * 2f);
        var halfViewTilesY = winH / (Player.SpriteSize * 2f);
		camXf = Math.Max(0f, player.CameraCenterTileX - halfViewTilesX);
		camYf = Math.Max(0f, player.CameraCenterTileY - halfViewTilesY);
    }
}
